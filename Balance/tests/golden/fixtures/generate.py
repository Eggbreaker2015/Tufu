"""Freeze the stage-5 golden fixtures (W0) from the extracted database.

Run from Balance/:

    .venv/bin/python tests/golden/fixtures/generate.py                      # write fixtures here
    .venv/bin/python tests/golden/fixtures/generate.py --out DIR            # write elsewhere
    .venv/bin/python tests/golden/fixtures/generate.py --check              # regenerate into a temp dir with
                                                                            # the committed date/git_rev and
                                                                            # compare byte for byte

Database: $TUFU_BALANCE_DB or Balance/out/balance.sqlite (read only).  The main checkout's database
(--main-db, default $TUFU_MAIN_BALANCE_DB or the main checkout path) is only read for extract_parity.json;
--check exits 2 when it cannot be checked, unless --skip-parity is given.

The survey scripts (session scratchpad, appendix B of Docs/Balance/Simulator/Design.md) are not
imported: their logic is ported below and their sha256 at port time is recorded in each fixture.
Stage-4 analysis scripts (Balance/analysis/*.py) are imported and run with DB/OUT redirected.
"""
from __future__ import annotations

import argparse
import collections
import contextlib
import csv
import datetime
import filecmp
import hashlib
import importlib.util
import io
import json
import math
import os
import sqlite3
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
BALANCE = HERE.parents[2]
REPO = BALANCE.parent
sys.path.insert(0, str(BALANCE))

from tufu_balance.model import bake as B  # noqa: E402
from tufu_balance.model import data as MD  # noqa: E402
from tufu_balance.model.defense import apply_damage_defense, get_battle_regen  # noqa: E402
from tufu_balance.model.enums import BalanceInterpolationType as BIT  # noqa: E402
from tufu_balance.model.enums import StatType as ST  # noqa: E402
from tufu_balance.model.f32 import f32  # noqa: E402
from tufu_balance.model.interpolation import parse_points  # noqa: E402
from tufu_balance.model.stats import calculate_specific_value as spec  # noqa: E402

# Survey location as named in appendix B of the design; only read by --compare-survey.
SURVEY_ROOT = "/private/tmp/claude-501/-Users-joyfort-MyProjects-Tufu/f3201c8d-2463-46c4-95af-b416f6c1537d/scratchpad/stage5"
SCRATCH = os.environ.get("TUFU_S5_SURVEY_DIR", SURVEY_ROOT)
# sha256 of the survey scripts at port time (2026-10-28); they may be deleted later.
SURVEY = {
    "forensics/load.py": "fb38bb3a3d93cae2f374faf2a312b91df56e6eb9c9b2b475bed0e60d16555b29",
    "forensics/spawn.py": "a791a540ec2654cd56a802afb26d119d639a64190a9166edb39adca03f7e583b",
    "forensics/fight.py": "7777a25aaa1d375406ba4105c6450d7710931fd99dc4a863123941e6ff057eda",
    "forensics/rulefit.py": "6c9b521a27b84ac45d86b6333ac5abc24e16dc010d52607ce8ec6fa68df8752b",
    "forensics/targets.py": "9b719705cda7bfd779a99f96f97e3cbc9cb7dd5e58be0fec87eda35474dedb1a",
    "forensics/golden_targets.json": "8d57da38fa46ee50e0fd21c480c759c74fda23cbe4ac35a7ada833c992ef36e6",
    "critic_fid/sens.py": "0bee2ce788c586cc52987303bd1eba48a2db7e0c4bfc98b9b89f4795ba2da413",
    "architect/l17check.py": "c105e48eb57b9681fcf278c2131b0938b0f46541934f48f8d6fb0441cafc0aac",
    "critique-fidelity/nextday.py": "01774ef4bf34407dd5fb7ca88dd923a2eba110108db1b077b1b344c6b4cc1a83",
    "critique-fidelity/killlag.py": "ca9416eec99c2c1a920557e47e9738f8a1dfd4d0e6d6f2603f2c244d819cb110",
    "model/golden_probe.py": "fdbc7e1c03a4ab1a09dd60f777fc5c1b0d053bd1df5161d55040c5f2d5bdb700",
    "forensics/rpm.py": "91e68357aeb011c618ed06369134394c63ec9d0a523cd4f2b93cd6d6466b42de",
    "critique-fidelity/lag.py": "25eac559cdb7ce376b9f94611a0540d294789bae304fec1cb570b8cf5ab7ca93",
    "critique-fidelity/fma3.py": "a3fca8faa0b3c07a59d97d36a3d9765dbdf4817c6f077dba6c96a246d121fa2a",
    "critique-fidelity/l3gate.py": "82f8c2338295bed718f1658aa756d42cccc99ff10ff92eb2e5c4c71b9b6d37ed",
    "architect/fma_check.py": "8e4e2c6854bca16bac4c65e6707150b0f40c1fb850c1d55e6d1efda99df594dc",
}
ANALYSIS = {"bake": "bake_check.py", "growth": "item_growth.py", "timeline": "island_timeline.py"}
MODEL_FILES = ["bake.py", "data.py", "defense.py", "enums.py", "f32.py", "interpolation.py", "snapshots.py", "stats.py"]
DEFAULT_MAIN_DB = Path(os.environ.get("TUFU_MAIN_BALANCE_DB", "/Users/joyfort/MyProjects/Tufu/Balance/out/balance.sqlite"))
FIXTURES = ["stage4_summaries.json", "stage4_slot_schedule.json", "item_growth_values.json",
            "forensics_targets.json", "forensics_killdays.json", "bake_probe_counts.json",
            "interpolation_rounding.json", "rpm_replay.json", "extract_parity.json"]


# ----------------------------------------------------------------------------- helpers
def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def rel(path: Path) -> str:
    return str(Path(path).resolve().relative_to(REPO))


def plain(o):
    """json default: numpy scalars -> python."""
    if hasattr(o, "item"):
        return o.item()
    raise TypeError(f"not JSON serialisable: {type(o)}")


def write_json(path: Path, obj) -> None:
    text = json.dumps(obj, indent=1, ensure_ascii=False, default=plain)
    path.write_text(text + "\n", encoding="utf-8")


class Ctx:
    def __init__(self, db: Path, date: str, git_rev: str):
        self.db = db
        self.db_sha256 = sha256_file(db)
        self.date = date
        self.git_rev = git_rev
        self.script = {"path": rel(Path(__file__)), "sha256": sha256_file(Path(__file__))}

    def meta(self, ported=(), imports=(), **extra):
        m = {"source": dict(self.script)}
        if ported:
            m["source"]["ported_from"] = [{"path": f"{SURVEY_ROOT}/{p}", "sha256": SURVEY[p]} for p in ported]
        if imports:
            m["source"]["imports"] = [{"path": rel(p), "sha256": sha256_file(p)} for p in imports]
        m.update({"db_sha256": self.db_sha256, "git_rev": self.git_rev, "date": self.date})
        m.update(extra)
        return m


def model_paths():
    return [BALANCE / "tufu_balance" / "model" / f for f in MODEL_FILES]


def q(con, sql, *args):
    cur = con.execute(sql, args)
    cols = [d[0] for d in cur.description]
    return [dict(zip(cols, r)) for r in cur.fetchall()]


# ----------------------------------------------------------------------------- stage 4 analyses
def load_analysis(name: str):
    path = BALANCE / "analysis" / ANALYSIS[name]
    spec_ = importlib.util.spec_from_file_location(f"_stage4_{name}", path)
    mod = importlib.util.module_from_spec(spec_)
    spec_.loader.exec_module(mod)
    return mod


def run_stage4(ctx: Ctx, workdir: Path) -> dict:
    """Run the three stage-4 analysis scripts with DB/OUT redirected; return {name: (module, outdir)}."""
    res = {}
    for name in ANALYSIS:
        mod = load_analysis(name)
        out = workdir / name
        mod.OUT = out
        if hasattr(mod, "DB"):
            mod.DB = ctx.db
        old_default = MD.DEFAULT_DB
        MD.DEFAULT_DB = ctx.db  # bake_check uses data.connect()
        try:
            with contextlib.redirect_stdout(io.StringIO()):
                mod.main()
        finally:
            MD.DEFAULT_DB = old_default
        res[name] = (mod, out)
    return res


def fx_stage4_summaries(ctx: Ctx, s4: dict) -> dict:
    summaries = {}
    for name, (_, out) in s4.items():
        with open(out / "summary.json", encoding="utf-8") as fh:
            summaries[name] = json.load(fh)
    return {"meta": ctx.meta(imports=[BALANCE / "analysis" / f for f in ANALYSIS.values()] + model_paths(),
                             note="out/analysis/{bake,growth,timeline}/summary.json, parsed and re-serialised"),
            "summaries": summaries}


def fx_slot_schedule(ctx: Ctx, s4: dict) -> dict:
    mod, out = s4["growth"]
    with open(out / "dps_defense_recompute.csv", encoding="utf-8") as fh:
        rows = list(csv.DictReader(fh))
    chunks = []
    for r in rows:
        chunks.append({"TimeChunkIndex": int(r["TimeChunkIndex"]),
                       "weapon_slots": int(r["weapon_slots_fitted"]), "dps_max_rel_err": float(r["dps_max_rel_err"]),
                       "armor_slots": int(r["armor_slots_fitted"]), "defense_max_rel_err": float(r["defense_max_rel_err"])})
    return {"meta": ctx.meta(imports=[BALANCE / "analysis" / "item_growth.py"]),
            "kind": "fitted",
            "note": ("Per day snapshot, the UnlockedWeaponSlots / UnlockedArmorSlots N in 1..8 that minimise the max "
                     "relative error of the recomputed DpsData / DefenseData (item_growth.runtime_section; smallest N on "
                     "ties).  Fitted values, not independent facts: usable for R2 and replay override.slots, "
                     "never to validate P12."),
            "chunks": chunks}


def fx_item_growth_values(ctx: Ctx, s4: dict) -> dict:
    mod, _ = s4["growth"]
    con = sqlite3.connect(f"file:{ctx.db}?mode=ro", uri=True)
    D = mod.load(con)
    states = mod.snapshot_item_states(D)
    snaps = []
    for s in D["snaps"]:
        i = s["TimeChunkSnapshots#idx"]
        items = []
        for it in D["sitems"][i]:
            k = it["Key"]
            cfg = D["items"][k]
            st = states[i][k]
            armor = mod.is_armor_item(cfg)
            calc = mod.calc_armor if armor else mod.calc_damage
            items.append({"Key": k, "Level": it["Level"], "Copies": it["Copies"],
                          "kind": "armor" if armor else "weapon",
                          "evolved": st["evolved"], "evo_level": st["evo_level"],
                          "value": calc(D, cfg, st["evo_level"], st["copies"], st["evolved"])})
        snaps.append({"TimeChunkIndex": s["TimeChunkIndex"], "items": items})
    con.close()
    return {"meta": ctx.meta(imports=[BALANCE / "analysis" / "item_growth.py"]),
            "note": ("value = calc_damage (weapon) or calc_armor (armor), float32 then widened, original commit 6af011f. "
                     "evolved = item_growth.snapshot_item_states: evolved from the first day snapshot where "
                     "Level != Copies on; evo_level = Level when evolved else 0."),
            "snapshots": snaps}


# ----------------------------------------------------------------------------- forensics (ported)
SNAP_FLOAT = ("Value", "StatsFromFeaturesFlat", "PercentMultiplier", "TotalFlatValue", "TotalValue", "CurrentGpm")
KEYS_RULE = ["Health", "HealthRegen", "MovementSpeed", "StabDamageFlat", "CutDamageFlat", "BuckshotDamageFlat",
             "StabDefenceFlat", "CutDefenceFlat", "BuckshotDefenceFlat", "LifestealFlat", "CritChance", "Evasion",
             "CritDamagePercent", "BlockChance", "BlockAmount"]
KEYS_TARGETS = ["Health", "HealthRegen", "MovementSpeed", "StabDamageFlat", "CutDamageFlat", "BuckshotDamageFlat",
                "StabDefenceFlat", "CutDefenceFlat", "BuckshotDefenceFlat", "LifestealFlat", "CritChance",
                "CritDamagePercent", "Evasion", "BlockChance", "BlockAmount"]
END = 405


def raw_snaps(con, kind):
    """forensics/load.py snaps(): 'Snapshots' or 'TimeChunkSnapshots'."""
    T = f"c_SimulationData__{kind}"
    ix = f"{kind}#idx"
    out = []
    for s in q(con, f'select * from "{T}" order by "{ix}"'):
        i = s[ix]
        st = {r["Key__name"]: {k: (float(v) if k in SNAP_FLOAT else v) for k, v in r.items()}
              for r in q(con, f'select * from "{T}__Stats" where "{ix}"=?', i)}
        it = q(con, f'select Key,Level,Copies from "{T}__Items" where "{ix}"=? order by "Items#idx"', i)
        dps = {r["WeaponType__name"]: r["Value"] for r in q(con, f'select * from "{T}__DpsData" where "{ix}"=?', i)}
        de = {r["WeaponType__name"]: r["Value"] for r in q(con, f'select * from "{T}__DefenseData" where "{ix}"=?', i)}
        out.append(dict(i=i, Playtime=s["Playtime"], Total=s["TotalPlaytime"], Loc=s["Location"],
                        TC=s["TimeChunkIndex"], stats=st, items=it, dps=dps, de=de))
    return out


def load_spawners(con):
    """forensics/spawn.py spawners()."""
    stat = {r["value"]: r["member"] for r in q(con, "select member,value from enums where enum='StatType'")}
    rewards = collections.defaultdict(list)
    for r in q(con, "select spawner_key,StatType,Value from spawner_rewards"):
        rewards[r["spawner_key"]].append((stat[r["StatType"]], r["Value"]))
    nonelite_cd = {1: 4, 2: 6, 3: 8, 4: 10, 5: 12, 6: 14, 7: 16, 8: 18, 9: 20, 10: 22, 11: 24, 12: 26, 13: 28}
    elite_cd = {1: 60, 2: 90, 3: 120, 4: 150}
    out = []
    for s in q(con, "select * from spawners where enabled=1"):
        s["rewards"] = rewards.get(s["Key"], [])
        ab = (not s["IsEvent"]) and (not s["IsVip"]) and not (s["ItemReward"] or "") and s["Cooldown"] >= 10
        s["ab"] = ab
        elite = bool(s["IsEliteBoss"])
        s["charges"] = (2 if elite else 3) * s["Count"] if ab else s["ChargesBeforeBigRespawn"]
        if ab:
            s["cd_min"] = elite_cd.get(s["LocationNumber"], 180) if elite else nonelite_cd.get(s["LocationNumber"], 30)
        else:
            s["cd_min"] = s["Cooldown"] / 60
        s["limit"] = s["Count"] if s["DisableAfterFirstClear"] else s["charges"]
        out.append(s)
    return out


class Forensics:
    """forensics/fight.py + rulefit.py; snap_fn(day) supplies the snapshot used for a fight."""

    def __init__(self, con, mcon):
        self.SD = MD.load_simulation_data(mcon)
        self.INT = MD.load_interpolations(mcon)
        self.PREF = MD.load_preferred_weapon_types(mcon)
        self.ARM = MD.load_armor_configs(mcon)
        self.ALL = [s for s in load_spawners(con) if not (s["IsEvent"] or s["IsVip"] or s["IsDummy"])]
        self.TCS = raw_snaps(con, "TimeChunkSnapshots")
        self.gates = {}
        for s in self.ALL:
            if s["Gates#count"] and s["IsDays"]:
                L = s["LocationNumber"]
                if L not in self.gates or s["OptimumTime"] > self.gates[L]["OptimumTime"]:
                    self.gates[L] = s
        self.set_snap_fn(self.snap_at)

    def snap_at(self, day):
        return self.SD.get_by_time_chunk_index(day)

    def set_snap_fn(self, fn):
        self.snap_fn = fn
        self.cache = {}

    def fight(self, s, snap, day):
        t = day
        INT = self.INT
        adps = float(INT.get_interpolation_coefficient(t, BIT.PlayerAbilityDPSMult)) + 1.0
        aehp = float(INT.get_interpolation_coefficient(t, BIT.PlayerAblityEHPMult)) + 1.0
        cc = spec(ST.CritChance, snap.total(ST.CritChance))
        cd = spec(ST.CritDamagePercent, snap.total(ST.CritDamagePercent))
        critm = cc * (cd - 1.0) + 1.0
        gatk = spec(ST.GlobalAttack, snap.total(ST.GlobalAttack)) * 0.01 + 1.0
        gdef = 1.0 / (spec(ST.GlobalDefence, snap.total(ST.GlobalDefence)) * 0.01 + 1.0)
        dodge = spec(ST.Evasion, snap.total(ST.Evasion))
        blk = 1.0 - spec(ST.BlockChance, snap.total(ST.BlockChance)) * spec(ST.BlockAmount, snap.total(ST.BlockAmount))
        armors = self.ARM.get(s["ArmorConfig"], [])
        best = None
        bv = -1e300
        for a in armors:
            v = snap.dps(a.type, default=None)
            if v is None:
                continue
            val = v * float(1 - a.resist)
            if val > bv:
                bv = val
                best = a
        if best is None:
            best = armors[0] if armors else None
        dv = snap.dps(best.type, default=0.0) if best else 0.0
        res = float(best.resist) if best else 0.0
        pdps = adps * gatk * critm * dv * (1 - res) + 0.1
        if s["IsStatic"]:
            edps0 = 0.0
        else:
            defe = snap.defense(self.PREF[s["LocationNumber"]])
            dmg = apply_damage_defense(s["stat.Damage"] * 1.0, defe)
            edps0 = dmg / s["stat.AttackCooldown"] * (1 - dodge) * blk * gdef
        cf = min((s["Count"] + 1) / 2.0, 4.0)
        edps = (edps0 + 0.1) * cf / aehp
        ft = s["stat.Health"] * s["Count"] / pdps
        regen = get_battle_regen(snap.total(ST.HealthRegen), snap.total(ST.LifestealFlat), s["Difficulty"])
        delta = (regen - edps) * ft
        hd = min(delta, 0.0)
        H = snap.total(ST.Health)
        return dict(ft=ft, loss=-hd, H=H, lossfrac=-hd / H, pdps=pdps, edps=edps)

    def F(self, s, d, which):
        k = (s["Key"], d, which)
        if k not in self.cache:
            sn = self.snap_fn(d if which == "start" else d + 1)
            self.cache[k] = self.fight(s, sn, d)
        return self.cache[k]

    def make_rule(self, thr, which, ftmax=300):
        def ok(s, d):
            f = self.F(s, d, which)
            return f["ft"] < ftmax and f["lossfrac"] < thr
        return ok

    def killdays(self, rule):
        kd = {}
        for s in self.ALL:
            if not s["IsDays"]:
                kd[s["Key"]] = 0
                continue
            d = max(4, math.ceil(s["OptimumTime"] - 1e-6))
            while d < END and not rule(s, d):
                d += 1
            kd[s["Key"]] = d
        return kd

    def per_day(self, kd, unlock):
        def loc(d):
            return max([3] + [L for L, u in unlock.items() if d >= u])
        done = set()
        pd = {}
        for d in range(4, END):
            g = collections.Counter()
            Ld = loc(d)
            for s in self.ALL:
                if s["LocationNumber"] > Ld:
                    continue
                if s["IsDays"] and d < kd[s["Key"]]:
                    continue
                if s["DisableAfterFirstClear"]:
                    if s["Key"] in done:
                        continue
                    done.add(s["Key"])
                for st, v in s["rewards"]:
                    g[st] += v * s["limit"]
            pd[d] = g
        return pd

    def residuals(self, pd):
        bad = []
        tot = 0
        for a, b in zip(self.TCS, self.TCS[1:]):
            if a["TC"] < 4:
                continue
            pr = collections.Counter()
            for d in range(a["TC"], b["TC"]):
                pr += pd[d]
            res = {}
            for k in KEYS_RULE:
                obs = b["stats"][k]["Value"] - a["stats"][k]["Value"]
                r = obs - pr[k]
                if abs(r) > max(0.5, 2e-5 * abs(obs)):
                    res[k] = round(r)
            if res:
                bad.append((a["TC"], b["TC"], res))
            tot += 1
        return bad, tot

    def unlock_from(self, kd, shift=0):
        unlock = {4: 5}
        for L in range(4, 17):
            unlock[L + 1] = max(unlock[L], kd[self.gates[L]["Key"]] + shift)
        return unlock

    def run(self, rule, shift=0):
        kd = self.killdays(rule)
        unlock = self.unlock_from(kd, shift)
        bad, tot = self.residuals(self.per_day(kd, unlock))
        return unlock, bad, tot, kd


class LagSnap:
    """critique-fidelity/killlag.py: Health/HealthRegen/LifestealFlat composed with K2/K3 of day dl."""
    DIRECT = {ST.Health, ST.HealthRegen, ST.LifestealFlat}

    def __init__(self, sn, dl, INT):
        self.sn = sn
        self.dl = dl
        self.INT = INT

    def total(self, t):
        if t in self.DIRECT:
            v = self.sn.stat(t).value
            k2 = float(self.INT.get_interpolation_coefficient(self.dl, BIT.StatsFromFeaturesFlat))
            k3 = float(self.INT.get_interpolation_coefficient(self.dl, BIT.StatModifier))
            return v * (1 + k2) * k3
        return self.sn.total(t)

    def __getattr__(self, a):
        return getattr(self.sn, a)


def bad_list(bad):
    return [{"from": a, "to": b, "residual": res} for a, b, res in bad]


def unlock_json(unlock):
    return {str(k): v for k, v in sorted(unlock.items())}


def fx_forensics_targets(ctx: Ctx, con) -> dict:
    """forensics/targets.py."""
    TC = raw_snaps(con, "TimeChunkSnapshots")
    E = raw_snaps(con, "Snapshots")
    K = KEYS_TARGETS
    out = {"tc_days": [s["TC"] for s in TC],
           "tc": [{"TC": s["TC"], "Playtime": s["Playtime"], "TotalPlaytime": s["Total"], "Location": s["Loc"],
                   "Value": {k: s["stats"][k]["Value"] for k in K}, "Gpm": {k: s["stats"][k]["CurrentGpm"] for k in K},
                   "Dps": s["dps"], "Defense": s["de"], "Items": s["items"]} for s in TC],
           "early": [{"i": s["i"], "Playtime": s["Playtime"], "TotalPlaytime": s["Total"], "Location": s["Loc"],
                      "Value": {k: s["stats"][k]["Value"] for k in K}, "Items": s["items"]} for s in E]}
    out["active_min_per_day"] = [{"from": a["TC"], "to": b["TC"],
                                  "min_per_day": (b["Playtime"] - a["Playtime"]) / (b["TC"] - a["TC"])}
                                 for a, b in zip(TC, TC[1:])]
    lfs = {}
    for s in TC + E:
        lfs.setdefault(s["Loc"], None)
    for L in range(1, 18):
        prev = None
        for s in TC:
            if s["Loc"] >= L:
                lfs[L] = {"tc_bracket": [prev["TC"] if prev else 0, s["TC"]]}
                break
            prev = s
    out["location_first_seen"] = {str(k): v for k, v in lfs.items()}
    return {"meta": ctx.meta(ported=["forensics/load.py", "forensics/targets.py"],
                             note="same content as forensics/golden_targets.json (verified by --compare-survey)"),
            **out}


def fx_forensics_killdays(ctx: Ctx, con, mcon) -> dict:
    R = Forensics(con, mcon)
    # theta = 1, fight state at the start of the day (architect/l17check.py)
    unlock, bad, tot, kd = R.run(R.make_rule(1.0, "start"))
    spawners = []
    for s in sorted(R.ALL, key=lambda s: (s["LocationNumber"], s["OptimumTime"], s["Key"], s["file_id"])):
        if not s["IsDays"]:
            continue
        spawners.append({"Key": s["Key"], "file_id": s["file_id"], "go_name": s["go_name"],
                         "LocationNumber": s["LocationNumber"], "OptimumTime": s["OptimumTime"],
                         "is_gate": bool(s["Gates#count"]), "kill_day": kd[s["Key"]]})
    gates = {str(L): {"Key": g["Key"], "OptimumTime": g["OptimumTime"], "kill_day": kd[g["Key"]]}
             for L, g in sorted(R.gates.items())}
    base = {"bad": len(bad), "intervals": tot, "unlock_days": unlock_json(unlock), "bad_intervals": bad_list(bad)}
    # theta grid (forensics/rulefit.py __main__), ablation only
    grid = []
    for which in ["start", "end"]:
        for thr in [0.5, 0.75, 0.9, 1.0, 1.1, 1e9]:
            u, b, t, _ = R.run(R.make_rule(thr, which))
            grid.append({"which": which, "theta": thr, "bad": len(b), "intervals": t, "unlock_days": unlock_json(u),
                         "bad_spans": [[x[0], x[1]] for x in b]})
    # player DPS scaling (critic_fid/sens.py)
    sens = []
    for m in [0.95, 0.98, 0.99, 1.0, 1.01, 1.02, 1.05]:
        def rule(s, d, m=m):
            f = R.F(s, d, "start")
            return f["ft"] / m < 300 and f["loss"] / m < f["H"]
        u, b, t, _ = R.run(rule)
        sens.append({"player_dps_scale": m, "bad": len(b), "intervals": t,
                     "unlock_L5_to_L17": [u[L] for L in range(5, 18)], "bad_spans": [[x[0], x[1]] for x in b]})
    # new island farmed the same day vs the next day (critique-fidelity/nextday.py)
    nextday = []
    kd1 = R.killdays(R.make_rule(1.0, "start"))
    for shift in (0, 1):
        u = R.unlock_from(kd1, shift)
        b, t = R.residuals(R.per_day(kd1, u))
        nextday.append({"new_island_farmed": "same_day" if shift == 0 else "next_day", "bad": len(b), "intervals": t,
                        "unlock_days": unlock_json(u), "bad_spans": [[x[0], x[1]] for x in b]})
    # K2/K3 of day d vs d-1 for the direct stats in the kill predicate (critique-fidelity/killlag.py)
    killlag = []
    for mode in ["nolag", "lag"]:
        if mode == "nolag":
            R.set_snap_fn(R.snap_at)
        else:
            R.set_snap_fn(lambda d: LagSnap(R.snap_at(d), max(d - 1, 0), R.INT))
        u, b, t, _ = R.run(R.make_rule(1.0, "start"))
        killlag.append({"k2k3_day": "d" if mode == "nolag" else "d-1", "bad": len(b), "intervals": t,
                        "unlock_days": unlock_json(u), "bad_intervals": bad_list(b)})
    R.set_snap_fn(R.snap_at)
    return {"meta": ctx.meta(ported=["forensics/load.py", "forensics/spawn.py", "forensics/fight.py",
                                     "forensics/rulefit.py", "architect/l17check.py", "critic_fid/sens.py",
                                     "critique-fidelity/nextday.py", "critique-fidelity/killlag.py",
                                     "critique-fidelity/l3gate.py"],
                             imports=model_paths()),
            "rule": ("fast-phase kill predicate: fightTime < 300 and loss/H < theta (CalculateSpawnerData fast branch, "
                     "bundled TimeChunkSnapshots as DpsData/DefenseData cache, AB group A); first kill day = first "
                     "d >= max(4, ceil(OptimumTime)) passing, capped at 405; island L+1 unlocks at max(unlock[L], "
                     "kill day of the latest-OT day gate of L), unlock[4] = 5; predicted gain per day = sum of "
                     "StatRewards x limit over unlocked, killable spawners (DAFC once); an interval [a, b) of day "
                     "snapshots with a >= 4 is bad when some stat residual exceeds max(0.5, 2e-5*|obs|)"),
            "theta1_start": base,
            "gates": gates,
            "day_spawners": spawners,
            "theta_grid": grid,
            "player_dps_scaling": sens,
            "new_island_planning": nextday,
            "kill_predicate_k2k3_lag": killlag,
            "l3_gate_lethality": l3_gate_probe(R, con)}


def l3_gate_probe(R: Forensics, con) -> dict:
    """critique-fidelity/l3gate.py: fast-branch fight of the island-3 gate and the first dragon at given days."""
    by_key = {}
    for s in load_spawners(con):  # survey: dict over all enabled spawners, first key match in rowid order
        by_key[s["Key"]] = s
    probes = []
    for prefix, days in (("Spawner_boss_cut_rare", [3, 4, 5, 6]), ("dragon_", [6, 7, 8])):
        s = [v for v in by_key.values() if v["Key"].startswith(prefix)][0]
        rows = []
        for d in days:
            f = R.fight(s, R.snap_at(d), d)
            rows.append({"day": d, "fight_time": f["ft"], "loss": f["loss"], "H": f["H"], "lossfrac": f["lossfrac"]})
        probes.append({"key_prefix": prefix, "Key": s["Key"], "file_id": s["file_id"], "go_name": s["go_name"],
                       "LocationNumber": s["LocationNumber"], "OptimumTime": s["OptimumTime"],
                       "IsDays": bool(s["IsDays"]), "is_gate": bool(s["Gates#count"]), "days": rows})
    return {"note": ("design 4.5 SlowLethalityGate evidence: fight state = day snapshot of that day, fast branch; "
                     "survey printed ft 41.9 / loss/H 4.16 at day 3, 1.066 at day 4, 0.21 at day 5 for the L3 gate"),
            "probes": probes}


# ----------------------------------------------------------------------------- bake probe (ported)
def fx_bake_probe(ctx: Ctx, mcon) -> dict:
    """model/golden_probe.py, counts only (timings and the per-process hash() dropped)."""
    out = {}
    bctx = MD.load_bake_context(mcon)
    armors = MD.load_armor_configs(mcon)
    rows = MD.load_spawner_rows(mcon)
    sps = [(r, MD.spawner_input_from_row(r, armors)) for r in rows]
    reps = [B.bake_spawner(bctx, sp) for _, sp in sps]
    mm = [(sp, rep) for (_, sp), rep in zip(sps, reps) if not sp.is_days]
    dm = [(sp, rep) for (_, sp), rep in zip(sps, reps) if sp.is_days]
    out["bake_minute_mode"] = {
        "n": len(mm), "hp_exact": sum(sp.health_base == rep.new_health for sp, rep in mm),
        "damage_exact": sum(sp.damage_base == rep.new_damage for sp, rep in mm),
        "both_exact": sum(sp.health_base == rep.new_health and sp.damage_base == rep.new_damage for sp, rep in mm)}
    out["bake_day_mode"] = {
        "n": len(dm), "hp_exact": sum(sp.health_base == rep.new_health for sp, rep in dm),
        "damage_exact": sum(sp.damage_base == rep.new_damage for sp, rep in dm)}
    bi = [(r, sp, rep) for (r, sp), rep in zip(sps, reps) if r["bake.Health"] is not None and not sp.is_days]
    out["bake_infos_minute_mode"] = {
        "n": len(bi), "health_eq_pred": sum(float(r["bake.Health"]) == rep.new_health for r, sp, rep in bi),
        "damage_eq_pred": sum(float(r["bake.Damage"]) == rep.new_damage for r, sp, rep in bi)}
    exact_keys = sorted(sp.key + "|" + sp.name for sp, rep in mm if sp.health_base == rep.new_health)
    out["minute_mode_hp_exact_set"] = {"n": len(exact_keys),
                                       "sha256": hashlib.sha256("\n".join(exact_keys).encode()).hexdigest(),
                                       "keys": exact_keys}

    interp = bctx.interpolations
    MAIN = {6, 8, 12, 14, 16, 20, 1, 2, 22}
    DIRECT = {1, 2, 22}
    comp = {}
    for name, lst in (("chunk", bctx.simulation_data.time_chunk_snapshots), ("playtime", bctx.simulation_data.snapshots)):
        c = dict(n_snap=len(lst), sff_ok=0, pm_ok=0, tf_ok=0, tv_ok=0, n_stats=0, other_ok=0, n_other=0)
        for s in lst:
            day = f32(s.time_chunk_index)
            k2 = float(interp.get_total_coefficient(day, 2))
            k3 = float(interp.get_total_coefficient(day, 3))
            for st in s.stats:
                if st.key in MAIN:
                    c["n_stats"] += 1
                    c["sff_ok"] += abs(st.stats_from_features_flat - k2) <= 1e-6 * max(1, abs(k2))
                    c["pm_ok"] += abs(st.percent_multiplier - k3) <= 1e-6 * max(1, abs(k3))
                    tf = st.value * (1 + k2)
                    c["tf_ok"] += abs(st.total_flat_value - tf) <= 1e-9 * max(1, abs(tf))
                    tv = tf * k3 if st.key in DIRECT else tf
                    c["tv_ok"] += abs(st.total_value - tv) <= 1e-9 * max(1, abs(tv))
                else:
                    c["n_other"] += 1
                    c["other_ok"] += (st.stats_from_features_flat == 0 and st.percent_multiplier == 1
                                      and st.total_flat_value == st.value and st.total_value == st.value)
        comp[name] = {k: int(v) for k, v in c.items()}
    out["snapshot_composition"] = comp

    itd = bctx.island_target_days

    def conv(s):
        s = f32(s)
        n = int(math.floor(s))
        prev = f32(0.0) if n == 1 else f32(itd[n - 2])
        frac = f32(s - f32(n))
        return prev if frac == 0 else f32(prev + f32(frac * f32(itd[n - 1] - prev)))

    kn = [(a, b) for it in interp.interpolations for a, b in zip(parse_points(it.island_points), parse_points(it.timing_points))]
    errs = [abs(float(conv(a)) - float(b)) for a, b in kn]
    out["interpolation_knots"] = {
        "n": len(kn), "f32_exact": sum(e == 0 for e in errs), "le_5e-5": sum(e <= 5e-5 for e in errs), "max_err": max(errs),
        "round4_exact": sum(abs(round(float(conv(a)), 4) - float(b)) < 1e-6 for a, b in kn),
        "round3_exact": sum(abs(round(float(conv(a)), 3) - float(b)) < 1e-6 for a, b in kn)}
    fill = mcon.execute('select * from "c_CombatBalanceConfig__EvolutionIslandFillRuleRows" '
                        'order by "EvolutionIslandFillRuleRows#idx"').fetchall()
    ev_ok = ev_f32 = ev_r3 = ev_n = 0
    for fr in fill:
        armor = str(fr["Name"]).startswith("Armor")
        field = f"{fr['Rarity__name']}{'Armor' if armor else ''}EvolutionTargetTimings"
        tgt = [r[0] for r in mcon.execute("select value from config_arrays where class='CombatBalanceConfig' "
                                          "and field_path=? order by idx", (field,))]
        for k in range(1, 16):
            e = float(fr["StartValue"]) + (k - 1) * float(fr["StepPerEvolution"])
            d = float(conv(e))
            ev_n += 1
            ev_ok += abs(d - tgt[k - 1]) <= 5e-5
            ev_f32 += bool(f32(d) == f32(tgt[k - 1]))
            ev_r3 += abs(round(d, 3) - tgt[k - 1]) < 1e-6
    out["evolution_target_timings"] = {"n": ev_n, "le_5e-5": ev_ok, "f32_exact": ev_f32, "round3_exact": ev_r3}
    ok = 0
    for i in range(16, len(itd) + 1):
        c = 5.0 / 6.0 * (i + 1) * (i + 2) - 105
        ok += bool(f32(c) == f32(itd[i - 1]))
    out["island_target_days_tail"] = {"islands": f"16..{len(itd)}", "n": len(itd) - 15, "f32_exact": ok}
    diff = 0
    for _, sp in sps:
        a = float(B.recalc_optimum_time(sp, bctx))
        L, rel_, ot = sp.location_number, float(sp.relative_optimum_time), float(sp.optimum_time)
        b = ot
        if L >= 11 and rel_ < 0.5:
            dlt = (float(itd[L - 1]) - float(itd[L - 2])) * (0.5 - rel_)
            if not sp.is_days:
                dlt *= 60
            b = ot - dlt
        diff += abs(a - b) > 1e-3
    out["recalc_optimum_time_model_vs_f64_differ_gt_1e-3"] = diff
    return {"meta": ctx.meta(ported=["model/golden_probe.py"], imports=model_paths(),
                             note=("snapshot_composition uses the model's current Evaluate (stepwise rounding) with "
                                   "tolerances; its tf/tv counts equal the stepwise ablation of G11, see "
                                   "interpolation_rounding.json for the exact per-rounding counts")),
            **out}


# ----------------------------------------------------------------------------- interpolation rounding (ported)
ROUNDINGS = ["stepwise", "fma", "double_tail", "double_all"]


def make_evaluate(kind):
    """architect/fma_check.py and critique-fidelity/fma3.py: Evaluate with the final lerp rounded as fma/double."""
    def ev(self, timing):
        timing = f32(timing)
        timings = parse_points(self.timing_points)
        coefs = parse_points(self.coef_points)
        if len(timings) < 2 or len(coefs) < 2 or len(timings) != len(coefs):
            return f32(0.0)
        if timings[0] >= timing:
            return coefs[0]
        for i in range(len(timings) - 1):
            left, right = timings[i], timings[i + 1]
            if right < timing:
                continue
            progress = f32(0.0)
            if left != right:
                if kind == "double_all":
                    raw = f32((float(timing) - float(left)) / (float(right) - float(left)))
                else:
                    raw = f32(f32(timing - left) / f32(right - left))
                progress = f32(0.0) if raw < 0.0 else (f32(1.0) if raw > 1.0 else raw)
            first, second = coefs[i], coefs[i + 1]
            if kind == "fma":
                return f32(math.fma(float(progress), float(f32(second - first)), float(first)))
            return f32(float(first) + float(progress) * (float(second) - float(first)))
        return coefs[-1]
    return ev


def fx_interpolation_rounding(ctx: Ctx, mcon) -> dict:
    sd = MD.load_simulation_data(mcon)
    interp = MD.load_interpolations(mcon)
    cls = type(interp.interpolations[0])
    orig = cls.evaluate
    MAIN = {6, 8, 12, 14, 16, 20, 1, 2, 22}
    DIRECT = {1, 2, 22}
    modes = {}
    grid = {}
    try:
        for kind in ROUNDINGS:
            cls.evaluate = orig if kind == "stepwise" else make_evaluate(kind)
            snap = {"n": 0, "k2_ok": 0, "k3_ok": 0}
            cells = {"n": 0, "sff": 0, "pm": 0, "tf": 0, "tv": 0}
            other = {"n": 0, "ok": 0}
            fails = []
            for lname, lst in (("time_chunk_snapshots", sd.time_chunk_snapshots), ("snapshots", sd.snapshots)):
                for i, s in enumerate(lst):
                    snap["n"] += 1
                    day = f32(s.time_chunk_index)
                    k2 = float(interp.get_total_coefficient(day, 2))
                    k3 = float(interp.get_total_coefficient(day, 3))
                    m = [st for st in s.stats if st.key in MAIN]
                    a = all(st.stats_from_features_flat == k2 for st in m)
                    b = all(st.percent_multiplier == k3 for st in m)
                    snap["k2_ok"] += a
                    snap["k3_ok"] += b
                    if not (a and b):
                        fails.append({"list": lname, "index": i, "TimeChunkIndex": s.time_chunk_index,
                                      "k2_ok": a, "k3_ok": b, "k2": k2, "k3": k3})
                    for st in s.stats:
                        if st.key in MAIN:
                            cells["n"] += 1
                            cells["sff"] += st.stats_from_features_flat == k2
                            cells["pm"] += st.percent_multiplier == k3
                            tf = st.value * (1.0 + k2)
                            cells["tf"] += st.total_flat_value == tf
                            tv = tf * k3 if st.key in DIRECT else tf
                            cells["tv"] += st.total_value == tv
                        else:
                            other["n"] += 1
                            other["ok"] += (st.stats_from_features_flat == 0 and st.percent_multiplier == 1
                                            and st.total_flat_value == st.value and st.total_value == st.value)
            modes[kind] = {"snapshots": snap, "main_cells": cells, "other_cells": other, "failing_snapshots": fails}
            grid[kind] = [float(it.evaluate(f32(t / 4))) for it in interp.interpolations for t in range(3200)]
    finally:
        cls.evaluate = orig
    n_grid = len(grid["stepwise"])
    pairs = [{"a": a, "b": b, "differ": sum(x != y for x, y in zip(grid[a], grid[b])), "of": n_grid}
             for i, a in enumerate(ROUNDINGS) for b in ROUNDINGS[i + 1:]]
    return {"meta": ctx.meta(ported=["architect/fma_check.py", "critique-fidelity/fma3.py"], imports=model_paths()),
            "note": ("G11. Snapshot attribute composition, exact float equality: SFF = K2(d), PM = K3(d), "
                     "TF = Value*(1+K2), TV = TF*K3 for Health/HealthRegen/LifestealFlat else TF; main stats are "
                     "StatType {1,2,6,8,12,14,16,20,22}, other stats must be 0/1/identity. d = f32(TimeChunkIndex), "
                     "K2/K3 = GetTotalCoefficient type 2/3. Rounding of the final lerp of Evaluate: stepwise = "
                     "binary32 per operation (model as of 5806ec7), fma = one fused multiply-add then binary32, "
                     "double_tail = progress in binary32 then lerp in double, double_all = progress and lerp in double. "
                     "grid = every curve (enabled or not) at t = 0, 0.25, ..., 799.75."),
            "roundings": modes,
            "grid_differences": pairs}


# ----------------------------------------------------------------------------- RPM replay (ported)
def fx_rpm_replay(ctx: Ctx, con, mcon) -> dict:
    """critique-fidelity/lag.py on forensics/rpm.py: day-snapshot CurrentGpm replay with K lag variants."""
    R = Forensics(con, mcon)
    spa = [s for s in load_spawners(con) if not (s["IsEvent"] or s["IsVip"])]  # rpm.py keeps dummies
    TC = R.TCS
    SD_TC = R.SD.time_chunk_snapshots
    assert len(TC) == len(SD_TC) and all(a["TC"] == b.time_chunk_index for a, b in zip(TC, SD_TC))

    def rpm(idx, lag_k, lag_t):
        sn = SD_TC[idx]
        d = TC[idx]["TC"]
        s2 = LagSnap(sn, max(d - 1, 0), R.INT) if lag_k else sn
        H = s2.total(ST.Health)
        tot = collections.Counter()
        for s in spa:
            if s["DisableAfterFirstClear"] or s["LocationNumber"] > TC[idx]["Loc"] or not s["rewards"]:
                continue
            f = R.fight(s, s2, max(d - 1, 0) if lag_t else d)
            if not f["ft"] < 300:
                continue
            loss = f["loss"]
            if H < loss:
                continue
            hm = 1.0 if H >= loss else (H / loss) ** 0.5
            lm = 2.0 if (not s["IsEliteBoss"] and s["LocationNumber"] <= 2) else 1.0
            for st, v in s["rewards"]:
                tot[st] += 10 / (10 + f["ft"]) * v * s["limit"] * lm * hm
        return {k: max(0.5, v / 60) for k, v in tot.items() if v > 0}

    variants = []
    default_values = None
    for lag_k, lag_t in [(True, False), (False, False), (True, True), (False, True)]:
        prev = {}
        ok = n = 0
        misses = []
        values = []
        for idx in range(len(TC)):
            r = rpm(idx, lag_k, lag_t)
            for k in r:
                prev[k] = max(prev.get(k, 0), r[k])
            row = {}
            for k, st in TC[idx]["stats"].items():
                g = st["CurrentGpm"]
                p = prev.get(k, 0.0)
                row[k] = p
                n += 1
                if abs(g - p) <= 1e-4 * max(1, abs(g)):
                    ok += 1
                else:
                    misses.append({"TC": TC[idx]["TC"], "stat": k, "stored": g, "replayed": p})
            values.append({"TC": TC[idx]["TC"], "replayed": dict(sorted(row.items()))})
        variants.append({"k2k3_day": "d-1" if lag_k else "d", "k4k5_day": "d-1" if lag_t else "d",
                         "order": "RpmFirst" if (lag_k and not lag_t) else None,
                         "match": ok, "cells": n, "misses": misses})
        if lag_k and not lag_t:
            default_values = values
    return {"meta": ctx.meta(ported=["forensics/load.py", "forensics/spawn.py", "forensics/fight.py",
                                     "forensics/rpm.py", "critique-fidelity/lag.py"], imports=model_paths()),
            "note": ("G20. Per day snapshot (TimeChunkSnapshots, 46 x 30 stats): replay CalculateSpawnerData fast "
                     "branch + CalculateRPMValueTimeChunk on the snapshot's own state (DpsData/DefenseData as cache, "
                     "full health), spawners = enabled, not event/VIP (dummies kept), not DAFC, LocationNumber <= "
                     "snapshot Location, with rewards; keep if fightTime < 300 and H >= loss; rpm += "
                     "10/(10+ft) * reward * limit * (2 if non-elite and L <= 2 else 1); value = max(0.5, rpm/60); "
                     "running max over days. match when |stored - replayed| <= 1e-4*max(1,|stored|). Lag d-1 for "
                     "K2/K3 recomposes Health/HealthRegen/LifestealFlat as Value*(1+K2(d-1))*K3(d-1); lag for "
                     "K4/K5 passes day d-1 to the fight (ability DPS/EHP multipliers)."),
            "variants": variants,
            "rpm_first_replayed": default_values}


# ----------------------------------------------------------------------------- extraction parity
def table_digest(con):
    out = {}
    for (t,) in con.execute("select name from sqlite_master where type='table' order by name"):
        cols = [r[1] for r in con.execute(f'pragma table_info("{t}")')]
        h = hashlib.sha256()
        for row in con.execute(f'select * from "{t}" order by rowid'):
            h.update(repr(tuple(row)).encode())
        out[t] = (cols, h.hexdigest())
    return out


def table_rows(con, t):
    cols = [r[1] for r in con.execute(f'pragma table_info("{t}")')]
    return cols, [dict(zip(cols, r)) for r in con.execute(f'select * from "{t}" order by rowid')]


PET_ASSETS = {"MonoBehaviour/player_claw.asset", "Resources/Pet Config.asset"}
PET_ENUMS = {"PetState", "MissingHealthBonusReceiver"}
PET_COLUMNS = {"c_ItemConfig": "AbilityDefs", "c_PlayerConfig": "PlayerAttackItem"}


def classify(t, row, other):
    """Return the pet-related cause of a row present in only one database, or None."""
    if t in ("items", "c_ItemConfig") and row.get("asset") in PET_ASSETS:
        return "row of pet item player_claw"
    if t == "assets":
        if row["owner"] in PET_ASSETS:
            return "pet asset"
        if row["owner"] == "MonoBehaviour/Player Config.asset":
            peer = [o for o in other if o["owner"] == row["owner"]]
            if len(peer) == 1:
                a, b = json.loads(row["data"]), json.loads(peer[0]["data"])
                keys = sorted(k for k in set(a) | set(b) if a.get(k, "<missing>") != b.get(k, "<missing>"))
                if keys == ["PlayerAttackItem"]:
                    return "Player Config data differs only in PlayerAttackItem"
        return None
    if t == "config_values":
        if row["owner"] in PET_ASSETS:
            return "config value of a pet asset"
        if row["field_path"] == "PlayerAttackItem":
            return "PlayerConfig.PlayerAttackItem"
        return None
    if t == "enums" and row["enum"] in PET_ENUMS:
        return "enum added by the pet branch (7972f29)"
    if t == "data_dictionary":
        node = row["node"]
        if node.startswith("PetConfig.") or node in ("ItemConfig.AbilityDefs", "PlayerConfig.PlayerAttackItem"):
            return "dictionary node of a pet table/column"
        if node.startswith("ItemConfig.") or node.startswith("PlayerConfig."):
            peer = [o for o in other if o["node"] == node]
            if len(peer) == 1:
                diff = sorted(k for k in row if row[k] != peer[0][k])
                if set(diff) <= {"rows", "non_null", "distinct", "min", "max", "sample"} and \
                        (row["tables"] != "c_ItemConfig" or abs(row["rows"] - peer[0]["rows"]) == 1):
                    return "dictionary statistics shifted by the extra player_claw row"
    return None


def fx_extract_parity(ctx: Ctx, main_db: Path) -> dict:
    a = sqlite3.connect(f"file:{main_db}?mode=ro", uri=True)
    b = sqlite3.connect(f"file:{ctx.db}?mode=ro", uri=True)
    A, Bt = table_digest(a), table_digest(b)
    only_main = sorted(set(A) - set(Bt))
    only_s5 = sorted(set(Bt) - set(A))
    differ = sorted(t for t in set(A) & set(Bt) if A[t] != Bt[t])
    tables = []
    unexplained = 0
    for t in differ:
        ca, ra = table_rows(a, t)
        cb, rb = table_rows(b, t)
        common = [c for c in ca if c in cb]
        def rk(r):
            return json.dumps([r[c] for c in common], default=str)
        ka = collections.Counter(rk(r) for r in ra)
        kb = collections.Counter(rk(r) for r in rb)
        ma = [r for r in ra if ka[rk(r)] > kb[rk(r)]]
        mb = [r for r in rb if kb[rk(r)] > ka[rk(r)]]
        entry = {"table": t, "rows_main": len(ra), "rows_stage5": len(rb),
                 "columns_only_main": [c for c in ca if c not in cb], "columns_only_stage5": [c for c in cb if c not in ca]}
        for col in entry["columns_only_main"]:
            vals = [r[col] for r in ra if r[col] is not None]
            entry.setdefault("column_values_only_main", {})[col] = {
                "non_null_rows": len(vals),
                "non_null_assets": sorted({r.get("asset") for r in ra if r[col] is not None}),
                "cause": "pet column" if PET_COLUMNS.get(t) == col else None}
            if PET_COLUMNS.get(t) != col:
                unexplained += 1
        rows = []
        for side, lst, other in (("main", ma, rb), ("stage5", mb, ra)):
            for r in lst:
                cause = classify(t, r, other)
                if cause is None:
                    unexplained += 1
                key = {k: r[k] for k in ("asset", "owner", "node", "enum", "member", "field_path", "Key") if k in r}
                rows.append({"side": side, "key": key, "cause": cause})
        rows.sort(key=lambda x: (x["side"], json.dumps(x["key"], sort_keys=True)))
        entry["rows_only_one_side"] = rows
        entry["rows_only_one_side_by_cause"] = dict(sorted(collections.Counter(
            f"{x['side']}: {x['cause']}" for x in rows).items()))
        tables.append(entry)
    for t in only_main + only_s5:
        if not t.startswith("c_PetConfig"):
            unexplained += 1
    a.close()
    b.close()
    return {"meta": ctx.meta(note="comparison logic written for W0, not a survey port",
                             main_db={"path": str(main_db), "sha256": sha256_file(main_db),
                                      "branch": "feature/pet-system (working tree incl. uncommitted changes)"}),
            "tables_main": len(A), "tables_stage5": len(Bt),
            "tables_only_main": only_main, "tables_only_stage5": only_s5,
            "tables_differ": differ,
            "unexplained": unexplained,
            "verdict": "all differences are pet-related" if unexplained == 0 else "UNEXPLAINED DIFFERENCES",
            "deviation_from_design": (
                "Design 2.10/7.3 expected only c_PetConfig* and PlayerConfig.PlayerAttackItem. The actual set is "
                "broader but still pet-only: pet item asset player_claw (rows in items, c_ItemConfig, assets, "
                "config_values; its empty AbilityDefs list adds the scalar column c_ItemConfig.AbilityDefs), "
                "Pet Config asset (assets, config_values), enums PetState and MissingHealthBonusReceiver, and "
                "data_dictionary nodes/statistics derived from these."),
            "tables": tables}


# ----------------------------------------------------------------------------- survey comparison
def compare_survey(outdir: Path) -> list[str]:
    """Report-only checks against the survey outputs (not part of the fixtures)."""
    msgs = []
    gt = Path(SCRATCH) / "forensics" / "golden_targets.json"
    if gt.exists():
        mine = json.loads((outdir / "forensics_targets.json").read_text())
        mine.pop("meta")
        msgs.append(f"forensics_targets == survey golden_targets.json: {mine == json.loads(gt.read_text())}")
    s4 = json.loads((outdir / "stage4_summaries.json").read_text())["summaries"]
    for label, base in (("survey analysis_before", Path(SCRATCH) / "model" / "analysis_before"),
                        ("main checkout out/analysis", REPO.parent / "Tufu" / "Balance" / "out" / "analysis")):
        for name in ANALYSIS:
            p = base / name / "summary.json"
            if p.exists():
                msgs.append(f"stage4 {name} == {label}: {s4[name] == json.loads(p.read_text())}")
    return msgs


# ----------------------------------------------------------------------------- main
def git_head() -> str:
    return subprocess.run(["git", "-C", str(REPO), "rev-parse", "HEAD"], capture_output=True, text=True,
                          check=True).stdout.strip()


def generate(outdir: Path, db: Path, date: str, git_rev: str, main_db: Path | None) -> None:
    ctx = Ctx(db, date, git_rev)
    outdir.mkdir(parents=True, exist_ok=True)
    con = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
    mcon = MD.connect(db)
    with tempfile.TemporaryDirectory() as tmp:
        s4 = run_stage4(ctx, Path(tmp))
        write_json(outdir / "stage4_summaries.json", fx_stage4_summaries(ctx, s4))
        write_json(outdir / "stage4_slot_schedule.json", fx_slot_schedule(ctx, s4))
        write_json(outdir / "item_growth_values.json", fx_item_growth_values(ctx, s4))
    write_json(outdir / "forensics_targets.json", fx_forensics_targets(ctx, con))
    write_json(outdir / "forensics_killdays.json", fx_forensics_killdays(ctx, con, mcon))
    write_json(outdir / "bake_probe_counts.json", fx_bake_probe(ctx, mcon))
    write_json(outdir / "interpolation_rounding.json", fx_interpolation_rounding(ctx, mcon))
    write_json(outdir / "rpm_replay.json", fx_rpm_replay(ctx, con, mcon))
    if main_db is not None:
        write_json(outdir / "extract_parity.json", fx_extract_parity(ctx, main_db))
    con.close()
    mcon.close()


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", type=Path, default=HERE)
    ap.add_argument("--date", help="metadata date (default: today)")
    ap.add_argument("--git-rev", help="metadata git_rev (default: HEAD of this worktree)")
    ap.add_argument("--main-db", type=Path, default=DEFAULT_MAIN_DB)
    ap.add_argument("--check", action="store_true",
                    help="regenerate with the committed date/git_rev into a temp dir and compare byte for byte")
    ap.add_argument("--skip-parity", action="store_true",
                    help="with --check: accept that extract_parity.json cannot be checked (main DB changed/missing)")
    ap.add_argument("--compare-survey", action="store_true", help="also compare with the survey outputs")
    ap.add_argument("--force", action="store_true", help="allow writing into the committed fixtures directory")
    args = ap.parse_args()
    db = Path(os.environ.get("TUFU_BALANCE_DB") or BALANCE / "out" / "balance.sqlite").resolve()

    if args.check:
        meta = json.loads((HERE / "stage4_summaries.json").read_text())["meta"]
        parity = json.loads((HERE / "extract_parity.json").read_text())["meta"]["main_db"]
        main_db = args.main_db if args.main_db.exists() and sha256_file(args.main_db) == parity["sha256"] else None
        with tempfile.TemporaryDirectory() as tmp:
            generate(Path(tmp), db, meta["date"], meta["git_rev"], main_db)
            names = FIXTURES if main_db else [f for f in FIXTURES if f != "extract_parity.json"]
            bad, drift = [], set()
            for f in names:
                if filecmp.cmp(HERE / f, Path(tmp) / f, shallow=False):
                    continue
                # Imported model modules keep evolving after the freeze; their recorded hashes are
                # provenance only, so a hash change alone is reported but does not fail the check.
                old, new = (json.loads(p.read_text()) for p in (HERE / f, Path(tmp) / f))
                old_imp, new_imp = (d["meta"]["source"].pop("imports", []) for d in (old, new))
                if old["meta"]["source"].pop("sha256") != new["meta"]["source"].pop("sha256"):
                    drift.add(old["meta"]["source"]["path"])
                if old != new:
                    bad.append(f)
                drift |= {a["path"] for a, b in zip(old_imp, new_imp) if a != b}
                if [a["path"] for a in old_imp] != [b["path"] for b in new_imp]:
                    bad.append(f)
        print("identical" if not bad else f"DIFFERENT: {sorted(set(bad))}")
        if drift:
            print("imported modules changed since the freeze (provenance only):", ", ".join(sorted(drift)))
        if bad:
            return 1
        if main_db is None:
            print("extract_parity.json NOT CHECKED: main checkout database changed or missing"
                  + (" (accepted by --skip-parity)" if args.skip_parity else "; pass --skip-parity to accept"))
            return 0 if args.skip_parity else 2
        return 0

    if args.out.resolve() == HERE.resolve() and not args.force:
        print("refusing to overwrite the frozen fixtures; pass --out DIR, or --force to re-freeze")
        return 2
    date = args.date or datetime.date.today().isoformat()
    git_rev = args.git_rev or git_head()
    generate(args.out, db, date, git_rev, args.main_db if args.main_db.exists() else None)
    if args.compare_survey:
        for m in compare_survey(args.out):
            print(m)
    print("wrote", args.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
