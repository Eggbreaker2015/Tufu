"""Stage 4 / track "growth": work back the item (weapon / armor) growth rules.

Reads only Balance/out/balance.sqlite and writes machine-readable results to
Balance/out/analysis/growth/.  Deterministic; re-run with

    cd Balance && .venv/bin/python analysis/item_growth.py

Sections (keys in summary.json):
  units        TimeChunkIndex definition (verified on the Simulation Data snapshots)
  runtime      closed-form runtime damage / armor formulas, table coverage (and whether the gaps are
               reachable), and an approximate recomputation of Simulation Data DpsData / DefenseData
               (StatModifier coefficient exact; slot counts fitted; forecast stats proxied by TotalValue)
  generator    replay of the evolution-value generator (Value, CopyValue,
               ReferenceStatPercent) from ReferenceHeuristicValue + shards + levers
  reference    what the Reference* columns are (ReferenceStatValue vs snapshot stats,
               ReferenceHeuristicValue structure, ReferenceTimeChunkIndex vs target timings)
  shards       shards-requirement tables: grid, plateau, capture hypothesis vs snapshots
  growth       growth ratios between rarities / levels with fit quality (R2 and max relative error,
               target timing T vs actual evolution chunk tc)
  armor_base   ArmorBaseByRarity vs player DefenceFlat at the first acquisition (hypothesis test)
  exceptions   explicitly named deviations
"""
from __future__ import annotations

import csv
import json
import math
import sqlite3
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
DB = ROOT / "out" / "balance.sqlite"
OUT = ROOT / "out" / "analysis" / "growth"

f32 = np.float32
RARITY = ["Common", "Uncommon", "Rare", "Epic", "Legendary", "Divine", "Mythical"]
WT = {0: "Stabbing", 1: "Cutting", 2: "Magic", 3: "Buckshot", 4: "Universal"}
STAT_PREFIX = {0: "Stab", 1: "Cut", 3: "Buckshot"}
USED_TYPES = (0, 1, 3)          # CombatBalanceConfig.UsedDamageTypes minus Universal (placeholder rows)
TIMED_RARITIES = (0, 1, 2, 3, 4)


# ----------------------------------------------------------------------------- helpers
def q(con, sql, *args):
    cur = con.execute(sql, args)
    cols = [d[0] for d in cur.description]
    return [dict(zip(cols, r)) for r in cur.fetchall()]


def r6(x):
    return None if x is None else float(round(float(x), 6))


def nice(x: float) -> float:
    """'Nice' rounding recovered from the data.

    mantissa m = x / 10^floor(log10 x);  step = 0.1 (m < 4), 0.5 (4 <= m < 8), 1 (m >= 8);
    result = round_half_even(x / (step*10^e)) * step*10^e   (C# Math.Round default).
    """
    if x <= 0:
        return 0.0
    e = 10.0 ** math.floor(math.log10(x))
    m = x / e
    st = 0.1 if m < 4 else (0.5 if m < 8 else 1.0)
    step = st * e
    return float(round(x / step) * step)


def nice_half_up(x: float) -> float:
    if x <= 0:
        return 0.0
    e = 10.0 ** math.floor(math.log10(x))
    m = x / e
    st = 0.1 if m < 4 else (0.5 if m < 8 else 1.0)
    step = st * e
    return float(math.floor(x / step + 0.5) * step)


def is_nice(x: float) -> bool:
    return x > 0 and abs(nice(x) - x) <= 1e-9 * x


def fit_line(x, y):
    x = np.asarray(x, float)
    y = np.asarray(y, float)
    b, a = np.polyfit(x, y, 1)
    yh = a + b * x
    ss_res = float(((y - yh) ** 2).sum())
    ss_tot = float(((y - y.mean()) ** 2).sum())
    r2 = 1.0 - ss_res / ss_tot if ss_tot > 0 else 1.0
    return float(a), float(b), r2, yh


def stats_of(v):
    v = np.asarray(v, float)
    if len(v) == 0:
        return None
    return {"n": int(len(v)), "min": r6(v.min()), "median": r6(np.median(v)), "max": r6(v.max()),
            "mean": r6(v.mean())}


def write_csv(path, rows):
    if not rows:
        return
    keys = list(rows[0].keys())
    with open(path, "w", newline="") as fh:
        w = csv.DictWriter(fh, fieldnames=keys)
        w.writeheader()
        for r in rows:
            w.writerow(r)


def load_interpolations(D):
    return D["interp"]


def interp_evaluate(tp, cp, timing):
    """BalanceInterpolation.Evaluate (6af011f): clamp below / above, piecewise linear in between."""
    if tp[0] >= timing:
        return cp[0]
    for i in range(len(tp) - 1):
        left, right = tp[i], tp[i + 1]
        if right < timing:
            continue
        prog = 0.0 if left == right else min(1.0, max(0.0, (timing - left) / (right - left)))
        return cp[i] + prog * (cp[i + 1] - cp[i])
    return cp[-1]


def total_coefficient(interp, timing, typ):
    """BalanceInterpolationsConfig.GetTotalCoefficient: product for types 1/3, sum otherwise (rarity Rare)."""
    multiply = not (typ < 8 and ((1 << typ) & 0xF5) != 0)
    tot = 1.0 if multiply else 0.0
    for row in interp:
        if row["Type"] != typ or not row["Use"]:
            continue
        v = interp_evaluate(row["tp"], row["cp"], timing)
        tot = tot * v if multiply else tot + v
    return float(f32(tot))


# ----------------------------------------------------------------------------- load
def load(con):
    cb = q(con, "select * from c_CombatBalanceConfig")[0]
    arr = {k: json.loads(cb[k]) for k in cb if isinstance(cb[k], str) and cb[k].startswith("[")}
    levers = {k: cb[k] for k in ("MinCopyValueGrowthMultiplier", "CopyValueTargetShare",
                                  "MinEvolutionValueFromShardsMultiplier",
                                  "MinArmorEvolutionValueFromPreviousEvolutionMultiplier",
                                  "MaxEvolutionLevel", "SyncShieldValuesByLeader")}
    growth = {0: arr["PierceDamageGrowthPerLevelByRarity"], 1: arr["CutDamageGrowthPerLevelByRarity"],
              3: arr["SmashDamageGrowthPerLevelByRarity"]}
    growth[2] = growth[0]
    growth[4] = growth[0]
    base = {0: arr["PierceDamageBaseByRarity"], 1: arr["CutDamageBaseByRarity"],
            3: arr["SmashDamageBaseByRarity"]}
    base[2] = base[0]
    base[4] = base[0]
    ev = {}
    for kind, tab in (("weapon", "EvolutionDamageValues"), ("armor", "EvolutionArmorValues")):
        for r in q(con, f"select * from c_CombatBalanceConfig__{tab}"):
            ev[(kind, r["DamageType"], r["Rarity"], r["EvolutionLevel"])] = r
    shards = {}
    for kind, tab in (("weapon", "EvolutionDamageShardsRequirements"), ("armor", "EvolutionArmorShardsRequirements")):
        for r in q(con, f"select * from c_CombatBalanceConfig__{tab}"):
            shards[(kind, r["Rarity"], r["EvolutionLevel"])] = r["Value"]
    myth = q(con, 'select * from c_CombatBalanceConfig__MythicalEvolutionValues order by EvolutionLevel')
    timings = {
        "weapon": {i: arr[RARITY[i] + "EvolutionTargetTimings"] for i in TIMED_RARITIES},
        "armor": {i: arr[RARITY[i] + "ArmorEvolutionTargetTimings"] for i in TIMED_RARITIES},
    }
    items = {}
    for r in q(con, "select * from items"):
        items.setdefault(r["Key"], r)
    sim = q(con, "select * from c_SimulationConfig")[0]
    snaps = q(con, 'select * from c_SimulationData__TimeChunkSnapshots order by TimeChunkIndex')
    sidx = {s["TimeChunkSnapshots#idx"]: s for s in snaps}
    sstats = defaultdict(dict)
    for r in q(con, 'select * from c_SimulationData__TimeChunkSnapshots__Stats'):
        sstats[r["TimeChunkSnapshots#idx"]][r["Key__name"]] = r
    sitems = defaultdict(list)
    for r in q(con, 'select * from c_SimulationData__TimeChunkSnapshots__Items order by "TimeChunkSnapshots#idx", "Items#idx"'):
        sitems[r["TimeChunkSnapshots#idx"]].append(r)
    sdps = defaultdict(dict)
    for r in q(con, 'select * from c_SimulationData__TimeChunkSnapshots__DpsData'):
        sdps[r["TimeChunkSnapshots#idx"]][r["WeaponType"]] = r["Value"]
    sdef = defaultdict(dict)
    for r in q(con, 'select * from c_SimulationData__TimeChunkSnapshots__DefenseData'):
        sdef[r["TimeChunkSnapshots#idx"]][r["WeaponType"]] = r["Value"]
    interp = []
    for r in q(con, 'select * from "c_CombatBalanceConfig__Interpolations.Interpolations" order by "Interpolations.Interpolations#idx"'):
        interp.append({"Type": r["Type"], "Use": r["Use"], "Name": r["Name"],
                       "tp": [float(x) for x in str(r["TimingPoints"]).split(";") if x.strip()],
                       "cp": [float(x) for x in str(r["CoefPoints"]).split(";") if x.strip()]})
    spawner_item_rewards = q(con, "select ItemReward from spawners")
    return dict(interp=interp, spawner_item_rewards=spawner_item_rewards, cb=cb, arr=arr, levers=levers, growth=growth, base=base, ev=ev, shards=shards, myth=myth,
                timings=timings, items=items, sim=sim, snaps=snaps, sidx=sidx, sstats=sstats,
                sitems=sitems, sdps=sdps, sdef=sdef)


# ----------------------------------------------------------------------------- runtime formulas
def is_armor_item(it):
    return it["SlotType"] in (1, 2)


def calc_damage(D, it, evo_level, copies, evolved):
    """ItemModel.CalcDamage / CalcEvolutionDamage (float32, original commit 6af011f)."""
    t, r = it["DamageType"], it["Rarity"]
    if evolved:
        Lc = min(evo_level, D["levers"]["MaxEvolutionLevel"])
        row = D["ev"][("weapon", t, r, Lc)]
        res = f32(f32(row["Value"]) + f32(row["CopyValue"]) * f32(copies))
        return float(f32(res + f32(it["FlatDamage"])))
    res = f32(f32(D["base"][t][r]) + f32(D["growth"][t][r]) * f32(copies))
    return float(f32(res + f32(it["FlatDamage"])))


def calc_armor(D, it, evo_level, copies, evolved):
    """ItemModel.CalcArmor / CalcEvolutionArmor (float32)."""
    t, r = it["DamageType"], it["Rarity"]
    if evolved:
        Lc = min(evo_level, D["levers"]["MaxEvolutionLevel"])
        row = D["ev"][("armor", t, r, Lc)]
        return float(f32(f32(row["Value"]) + f32(row["CopyValue"]) * f32(copies)))
    return float(f32(f32(D["arr"]["ArmorBaseByRarity"][r]) + f32(D["arr"]["ArmorGrowthPerLevelByRarity"][r]) * f32(copies)))


def snapshot_item_states(D):
    """Snapshot Items store (Level, Copies); Level is EffectiveLevel (= Copies before evolution).
    An item is treated as evolved from the first snapshot where Level != Copies on."""
    evolved = set()
    states = {}
    for s in D["snaps"]:
        i = s["TimeChunkSnapshots#idx"]
        st = {}
        for it in D["sitems"][i]:
            k = it["Key"]
            if it["Level"] != it["Copies"]:
                evolved.add(k)
            ev = k in evolved
            st[k] = {"evolved": ev, "evo_level": it["Level"] if ev else 0, "copies": it["Copies"]}
        states[i] = st
    return states


def runtime_section(D, states):
    res = {}
    lev = D["levers"]
    # --- coverage of the lookup tables for every shipped item config
    missing = []
    for key, it in sorted(D["items"].items()):
        kind = "armor" if is_armor_item(it) else "weapon"
        t, r = it["DamageType"], it["Rarity"]
        need = [(kind, t, r, L) for L in range(1, lev["MaxEvolutionLevel"] + 1)]
        miss_v = [n for n in need if n not in D["ev"]]
        miss_s = [L for L in range(1, lev["MaxEvolutionLevel"] + 1) if (kind, r, L) not in D["shards"]]
        placeholder = [n for n in need if n in D["ev"] and D["ev"][n]["Value"] == 1 and D["ev"][n]["CopyValue"] == 0]
        if miss_v or miss_s or placeholder:
            missing.append({"item": key, "kind": kind, "DamageType": WT[t], "Rarity": RARITY[r],
                            "missing_value_rows": len(miss_v), "missing_shard_rows": len(miss_s),
                            "placeholder_rows(Value=1,CopyValue=0)": len(placeholder)})
    res["table_coverage_problems"] = missing
    res["evolution_rows_by_kind_damage_type"] = {
        f"{kind}|{WT[t]}": sum(1 for k in D["ev"] if k[0] == kind and k[1] == t)
        for kind in ("weapon", "armor") for t in range(5)}
    # ReferenceShardsRequirement vs the shards tables
    sc = Counter()
    for (kind, t, r, L), row in D["ev"].items():
        key = "universal" if t == 4 and r != 6 else "other"
        sc[key + "_rows"] += 1
        sc[key + "_equal_to_shards_table"] += row["ReferenceShardsRequirement"] == D["shards"][(kind, r, L)]
        sc[key + "_zero"] += row["ReferenceShardsRequirement"] == 0
    res["reference_shards_vs_table"] = dict(sc)
    # --- per-item ladder: stat right before / after each evolution when evolving exactly at the threshold
    ladder = []
    for key, it in sorted(D["items"].items()):
        if it["Rarity"] == 5 or it["DamageType"] in (2, 4) and it["Rarity"] != 6:
            continue
        kind = "armor" if is_armor_item(it) else "weapon"
        calc = calc_armor if kind == "armor" else calc_damage
        if (kind, it["DamageType"], it["Rarity"], 1) not in D["ev"]:
            continue
        for L in range(1, lev["MaxEvolutionLevel"] + 1):
            sh = D["shards"][(kind, it["Rarity"], L)]
            before = calc(D, it, L - 1, sh, L - 1 >= 1)
            after = calc(D, it, L, 0, True)
            ladder.append({"item": key, "kind": kind, "DamageType": WT[it["DamageType"]],
                           "Rarity": RARITY[it["Rarity"]], "E": L, "shards": sh,
                           "stat_before_evolve(copies=shards)": r6(before),
                           "stat_after_evolve(copies=0)": r6(after),
                           "jump_ratio": r6(after / before) if before else None,
                           "stat_per_copy_after": r6(D["ev"][(kind, it["DamageType"], it["Rarity"], L)]["CopyValue"])})
    write_csv(OUT / "item_ladders.csv", ladder)
    jr = [r["jump_ratio"] for r in ladder if r["jump_ratio"] is not None and r["Rarity"] != "Mythical"]
    res["evolve_jump_ratio_at_threshold"] = stats_of(jr)
    # --- recompute DpsData / DefenseData of every snapshot (CalculateDpsData / CalculateDefenseData, 6af011f)
    #  coefficient = GetInterpolationCoefficient(GetBalanceTimingMinutes(), StatModifier, Rare): recomputed exactly
    #  from the Interpolations rows (BalanceInterpolationsConfig.GetTotalCoefficient, multiplicative for StatModifier).
    #  GetForecastStatValue(type) = Unit.GetStatValue(type, true) is NOT stored; snapshot TotalValue is a proxy.
    #  UnlockedWeaponSlots / UnlockedArmorSlots are NOT stored; they are fitted per snapshot (free parameter).
    interp = load_interpolations(D)
    kcheck = []
    for s in D["snaps"]:
        i = s["TimeChunkSnapshots#idx"]
        k_exact = total_coefficient(interp, s["TimeChunkIndex"], 3)
        kcheck.append(abs(k_exact - D["sstats"][i]["Health"]["PercentMultiplier"]))
    res["stat_modifier_coefficient_vs_health_percent_multiplier"] = {
        "snapshots": len(kcheck), "max_abs_diff": r6(max(kcheck)),
        "note": "coefficient recomputed from Interpolations(Type=StatModifier, Use=1), product of Evaluate(TimeChunkIndex)"}
    nm = {0: "Stab", 1: "Cut", 2: "Magic", 3: "Buckshot", 4: "Universal"}

    def recompute(i, stt, st, K):
        weapons = [(k, D["items"][k]) for k in st if not is_armor_item(D["items"][k])]
        armors = [(k, D["items"][k]) for k in st if is_armor_item(D["items"][k])]
        dmg = {k: calc_damage(D, it, st[k]["evo_level"], st[k]["copies"], st[k]["evolved"]) for k, it in weapons}
        arm = {k: calc_armor(D, it, st[k]["evo_level"], st[k]["copies"], st[k]["evolved"]) for k, it in armors}
        best = None
        for N in range(1, 9):
            err = 0.0
            for w in range(5):
                cand = sorted(weapons, key=lambda kv: (-(kv[1]["DamageType"] == w), -dmg[kv[0]] / kv[1]["AttackCooldown"]))
                tot = 0.0
                for k, it in cand[:N]:
                    F = stt[it["DamageStatTypeFlat__name"]]["TotalValue"]
                    P = stt[it["DamageStatTypePercent__name"]]["TotalValue"]
                    v = P * (F + dmg[k]) * K / it["AttackCooldown"]
                    if it["DamageType"] != w:
                        v /= 10.0
                    tot += v * it["SimulationDpsWeaponMultiplier"]
                ref = D["sdps"][i][w]
                err = max(err, abs(tot - ref) / ref)
            if best is None or err < best[1]:
                best = (N, err)
        bestA = None
        for N in range(1, 9):
            err = 0.0
            for w in (0, 1, 3):
                cand = sorted([kv for kv in armors if kv[1]["DamageType"] == w], key=lambda kv: -arm[kv[0]])
                F = stt[nm[w] + "DefenceFlat"]["TotalValue"]
                P = stt[nm[w] + "DefencePercent"]["TotalValue"]
                tot = P * (F + sum(arm[k] for k, _ in cand[:N])) * K
                ref = D["sdef"][i][w]
                err = max(err, abs(tot - ref) / ref) if ref else err
            if bestA is None or err < bestA[1]:
                bestA = (N, err)
        return best, bestA

    rows = []
    prev_st = None
    for s in D["snaps"]:
        i = s["TimeChunkSnapshots#idx"]
        stt = D["sstats"][i]
        K = total_coefficient(interp, s["TimeChunkIndex"], 3)
        st = states[i]
        best, bestA = recompute(i, stt, st, K)
        row = {"TimeChunkIndex": s["TimeChunkIndex"], "weapon_slots_fitted": best[0],
               "dps_max_rel_err": r6(best[1]), "armor_slots_fitted": bestA[0],
               "defense_max_rel_err": r6(bestA[1])}
        # variant B: drop items that are new since the previous snapshot (stale-items hypothesis)
        # variant C: previous snapshot's items with the current stats
        if prev_st is not None:
            st_b = {k: v for k, v in st.items() if k in prev_st}
            b1, b2 = recompute(i, stt, st_b, K)
            c1, c2 = recompute(i, stt, prev_st, K)
            row.update({"dps_err_without_new_items": r6(b1[1]), "defense_err_without_new_items": r6(b2[1]),
                        "dps_err_prev_snapshot_items": r6(c1[1]), "defense_err_prev_snapshot_items": r6(c2[1]),
                        "new_items": ";".join(sorted(k for k in st if k not in prev_st))})
        else:
            row.update({"dps_err_without_new_items": row["dps_max_rel_err"], "defense_err_without_new_items": row["defense_max_rel_err"],
                        "dps_err_prev_snapshot_items": None, "defense_err_prev_snapshot_items": None, "new_items": ""})
        rows.append(row)
        prev_st = st
    write_csv(OUT / "dps_defense_recompute.csv", rows)

    def errsum(col):
        v = [r[col] for r in rows if r[col] is not None]
        return {"exact_(<1e-6)": sum(x < 1e-6 for x in v), "max": max(v),
                "max_chunk": next(r["TimeChunkIndex"] for r in rows if r[col] == max(v)),
                "max_chunk_ge_100": max(r[col] for r in rows if r["TimeChunkIndex"] >= 100 and r[col] is not None),
                "last": rows[-1][col],
                "chunks_with_err_gt_1%": [r["TimeChunkIndex"] for r in rows if r[col] is not None and r[col] > 0.01]}
    res["dps_recompute"] = {
        "snapshots": len(rows),
        "status": "approximate; cause of mismatch unresolved (hypothesis)",
        "current_items": errsum("dps_max_rel_err"),
        "without_new_items": errsum("dps_err_without_new_items"),
        "prev_snapshot_items": errsum("dps_err_prev_snapshot_items"),
        "per_chunk_examples": {str(r["TimeChunkIndex"]): [r["dps_max_rel_err"], r["dps_err_without_new_items"], r["dps_err_prev_snapshot_items"]]
                               for r in rows if r["TimeChunkIndex"] in (6, 9, 15, 17, 19, 21, 24)},
        "fitted_weapon_slots_by_chunk": {str(r["TimeChunkIndex"]): r["weapon_slots_fitted"] for r in rows},
    }
    res["defense_recompute"] = {
        "status": "approximate; cause of mismatch unresolved (hypothesis)",
        "current_items": errsum("defense_max_rel_err"),
        "without_new_items": errsum("defense_err_without_new_items"),
        "prev_snapshot_items": errsum("defense_err_prev_snapshot_items"),
        "fitted_armor_slots_by_chunk": {str(r["TimeChunkIndex"]): r["armor_slots_fitted"] for r in rows},
    }
    # --- are the coverage gaps reachable?  (no Magic items; *_div configs never owned / rewarded)
    div = [it for it in D["items"].values() if it["Rarity"] == 5]
    div_keys = {it["Key"] for it in div}
    div_names = {it["asset_name"] for it in div}
    snap_keys = {it["Key"] for lst in D["sitems"].values() for it in lst}
    rewards = [r["ItemReward"] for r in D["spawner_item_rewards"]]
    res["coverage_gaps_usage"] = {
        "item_configs_with_DamageType_Magic": sum(1 for it in D["items"].values() if it["DamageType"] == 2),
        "rarity5_div_configs": len(div),
        "rarity5_configs_in_snapshot_items": len(div_keys & snap_keys),
        "rarity5_configs_in_spawner_ItemReward": sum(1 for x in rewards if x in div_names or x in div_keys),
        "spawner_ItemReward_non_empty": sum(1 for x in rewards if x),
    }
    return res


# ----------------------------------------------------------------------------- generator replay
def is_filled(row):
    return row["ReferenceStatValue"] == row["ReferenceHeuristicValue"]


def generator_section(D):
    lev = D["levers"]
    k_sh = lev["MinEvolutionValueFromShardsMultiplier"]
    k_prev = lev["MinArmorEvolutionValueFromPreviousEvolutionMultiplier"]
    k_cv = lev["MinCopyValueGrowthMultiplier"]
    share = lev["CopyValueTargetShare"]
    out_rows = []
    summary = {}
    variants = {
        "full_rule": dict(armor_prev=True, rnd=nice),
        "without_armor_prev_multiplier": dict(armor_prev=False, rnd=nice),
        "round_half_up": dict(armor_prev=True, rnd=nice_half_up),
        "value_round_only(no_min_constraints)": dict(armor_prev=False, rnd=nice, no_min=True),
    }
    for vname, var in variants.items():
        cnt = Counter()
        for kind in ("weapon", "armor"):
            for t in USED_TYPES:
                for r in TIMED_RARITIES:
                    Vp = D["arr"]["ArmorBaseByRarity"][r] if kind == "armor" else D["base"][t][r]
                    cvp = D["arr"]["ArmorGrowthPerLevelByRarity"][r] if kind == "armor" else D["growth"][t][r]
                    for L in range(1, lev["MaxEvolutionLevel"] + 1):
                        row = D["ev"][(kind, t, r, L)]
                        sh = row["ReferenceShardsRequirement"]
                        hv = row["ReferenceHeuristicValue"]
                        v_sh = Vp + k_sh * cvp * sh
                        v_prev = k_prev * Vp if (kind == "armor" and var["armor_prev"]) else 0.0
                        target = hv if var.get("no_min") else max(hv, v_sh, v_prev)
                        pV = var["rnd"](target)
                        cv_raw = share * (hv - Vp) / sh
                        pcv = max(k_cv * cvp, var["rnd"](cv_raw) if cv_raw > 0 else 0.0)
                        okV = abs(pV - row["Value"]) < 1e-6
                        okC = abs(pcv - row["CopyValue"]) < 1e-6
                        cnt[(kind, "V")] += okV
                        cnt[(kind, "CV")] += okC
                        cnt[(kind, "n")] += 1
                        if vname == "full_rule":
                            binding = "heuristic"
                            if target > hv:
                                binding = "shards_min" if v_sh >= v_prev else "armor_prev_x1.3"
                            tie = abs(target / (nice(target) - nice_half_up(target) + 1e-300)) if False else None
                            cv_binding = "copy_floor(prev*MinCopyValueGrowthMultiplier)" if k_cv * cvp > (var["rnd"](cv_raw) if cv_raw > 0 else 0) else "share_rule"
                            if L == 1:
                                cv_binding = cv_binding.replace("prev", "pre-evo growth")
                            out_rows.append({
                                "kind": kind, "DamageType": WT[t], "Rarity": RARITY[r], "E": L,
                                "filled_beyond_sim": is_filled(row),
                                "ReferenceHeuristicValue": r6(hv), "V_prev": r6(Vp), "CopyValue_prev": r6(cvp),
                                "shards": sh, "V_min_shards": r6(v_sh), "V_min_prev_x1.3": r6(v_prev),
                                "binding": binding, "Value_pred": r6(pV), "Value": row["Value"], "Value_ok": okV,
                                "CopyValue_raw": r6(cv_raw), "CopyValue_pred": r6(pcv), "CopyValue": row["CopyValue"],
                                "CopyValue_ok": okC, "cv_binding": cv_binding,
                                "ReferenceStatValue": r6(row["ReferenceStatValue"]),
                                "ReferenceStatPercent": row["ReferenceStatPercent"],
                                "pct_pred": int(round(100.0 * row["Value"] / row["ReferenceStatValue"])),
                                "ReferenceTimeChunkIndex": row["ReferenceTimeChunkIndex"],
                            })
                        Vp, cvp = row["Value"], row["CopyValue"]
        summary[vname] = {f"{k[0]}_{k[1]}": v for k, v in sorted(cnt.items())}
    write_csv(OUT / "generator_replay.csv", out_rows)
    # ReferenceStatPercent = Math.Round(100*Value/ReferenceStatValue) (banker's)
    pct_even = sum(r["pct_pred"] == r["ReferenceStatPercent"] for r in out_rows)
    pct_up = sum(int(math.floor(100.0 * r["Value"] / r["ReferenceStatValue"] + 0.5)) == r["ReferenceStatPercent"] for r in out_rows)
    ties = [r for r in out_rows if abs((100.0 * r["Value"] / r["ReferenceStatValue"]) % 1 - 0.5) < 1e-9]
    # tie cases of the nice rounding
    nice_ties = [r for r in out_rows if nice(max(r["ReferenceHeuristicValue"], r["V_min_shards"], r["V_min_prev_x1.3"] or 0)) != nice_half_up(max(r["ReferenceHeuristicValue"], r["V_min_shards"], r["V_min_prev_x1.3"] or 0))]
    binding = Counter((r["kind"], r["filled_beyond_sim"], r["binding"]) for r in out_rows)
    cvb = Counter((r["kind"], r["filled_beyond_sim"], r["cv_binding"]) for r in out_rows)
    # filled rows: heuristic == stat == Value at the last simulated level, Copy frozen
    filled_check = Counter()
    for kind in ("weapon", "armor"):
        for t in USED_TYPES:
            for r in TIMED_RARITIES:
                last_sim = None
                for L in range(1, 16):
                    row = D["ev"][(kind, t, r, L)]
                    if not is_filled(row):
                        last_sim = row
                        continue
                    filled_check["filled_rows"] += 1
                    filled_check["hv_eq_last_sim_Value"] += abs(row["ReferenceHeuristicValue"] - last_sim["Value"]) < 1e-6
                    filled_check["cv_eq_last_sim"] += row["CopyValue"] == last_sim["CopyValue"]
    # armor sync across types
    sync = Counter()
    for r in TIMED_RARITIES:
        for L in range(1, 16):
            rows = [D["ev"][("armor", t, r, L)] for t in USED_TYPES]
            sync["cells"] += 1
            for f in ("Value", "CopyValue", "ReferenceHeuristicValue", "ReferenceShardsRequirement"):
                sync[f + "_identical"] += len({x[f] for x in rows}) == 1
            for f in ("ReferenceTimeChunkIndex", "ReferenceStatValue"):
                sync[f + "_identical"] += len({x[f] for x in rows}) == 1
    # mythical: its own closed form (presumably CombatBalanceConfig.FillMythicalEvolutionValues /
    # CreateMythicalEvolutionValues, both empty in the APK), NOT the evolution-value generator above.
    myth = D["myth"]
    myth_ok = Counter()
    off_grid = []
    V = 0.0
    for m in myth:
        L = m["EvolutionLevel"]
        myth_ok["n"] += 1
        myth_ok["Value=50000L(L+1)"] += m["Value"] == 50000 * L * (L + 1)
        myth_ok["CopiesToEvolve=10L"] += m["CopiesToEvolve"] == 10 * L
        myth_ok["CopyValue=5000"] += m["CopyValue"] == 5000
        # consistency only: CopyValue equals CopyValueTargetShare * dV / Shards (dV = 100000 L, Shards = 10 L)
        myth_ok["CopyValue==share*dV/shards(consistency_only)"] += m["CopyValue"] == share * (m["Value"] - V) / m["CopiesToEvolve"]
        myth_ok["Value_on_nice_grid"] += is_nice(m["Value"])
        if not is_nice(m["Value"]):
            off_grid.append([L, m["Value"], nice(m["Value"])])
        V = m["Value"]
        for kind in ("weapon", "armor"):
            for t in (0, 1, 3, 4):
                row = D["ev"].get((kind, t, 6, L))
                myth_ok["copied_rows"] += 1
                myth_ok["copied_rows_equal"] += bool(row) and row["Value"] == m["Value"] and row["CopyValue"] == m["CopyValue"] and row["ReferenceShardsRequirement"] == m["CopiesToEvolve"]
            myth_ok["shards_rows_equal"] += D["shards"][(kind, 6, L)] == m["CopiesToEvolve"]
    # what the generator recursion would give at L=1 if it started from the pre-evolution Mythical item
    # (Value[0] = base, CopyValue[0] = growth): differs by type, yet the stored rows are identical across types.
    gen_v1 = {}
    for kind, t in (("weapon", 0), ("weapon", 1), ("weapon", 3), ("armor", 0)):
        v0 = D["arr"]["ArmorBaseByRarity"][6] if kind == "armor" else D["base"][t][6]
        cv0 = D["arr"]["ArmorGrowthPerLevelByRarity"][6] if kind == "armor" else D["growth"][t][6]
        gen_v1[f"{kind}|{WT[t] if kind == 'weapon' else 'all'}"] = {
            "Value0": v0, "CopyValue0": cv0, "shards_min_V1=V0+2*CV0*Shards1": v0 + k_sh * cv0 * myth[0]["CopiesToEvolve"],
            "stored_V1": myth[0]["Value"]}
    myth_summary = dict(myth_ok)
    myth_summary["values_off_nice_grid[L,Value,nice(Value)]"] = off_grid
    myth_summary["generator_shards_min_at_L1_from_pre_evolution"] = gen_v1
    # universal placeholders
    uni = Counter()
    for kind in ("weapon", "armor"):
        for r in TIMED_RARITIES:
            for L in range(1, 16):
                row = D["ev"][(kind, 4, r, L)]
                uni["rows"] += 1
                uni["Value=1,CopyValue=0,Ref*=0"] += (row["Value"] == 1 and row["CopyValue"] == 0 and row["ReferenceShardsRequirement"] == 0)
    return {
        "variants": summary,
        "ReferenceStatPercent": {"rows": len(out_rows), "round_half_even_ok": pct_even, "round_half_up_ok": pct_up,
                                 "exact_.5_ties": [(r["kind"], r["DamageType"], r["Rarity"], r["E"], r["ReferenceStatPercent"]) for r in ties]},
        "nice_rounding_ties": [(r["kind"], r["DamageType"], r["Rarity"], r["E"], r["Value"]) for r in nice_ties],
        "value_binding_counts": {f"{k[0]}|{'filled' if k[1] else 'simulated'}|{k[2]}": v for k, v in sorted(binding.items())},
        "copyvalue_binding_counts": {f"{k[0]}|{'filled' if k[1] else 'simulated'}|{k[2]}": v for k, v in sorted(cvb.items())},
        "filled_rows": dict(filled_check),
        "armor_sync_across_types": dict(sync),
        "mythical": myth_summary,
        "universal_placeholders": dict(uni),
    }, out_rows


# ----------------------------------------------------------------------------- reference columns
def reference_section(D, gen_rows, states):
    res = {}
    snap_by_tc = {s["TimeChunkIndex"]: s for s in D["snaps"]}
    # units: TimeChunkIndex = floor(TotalPlaytime / SpawnerChargesChunkMinutes)
    cm = D["sim"]["SpawnerChargesChunkMinutes"]
    units_ok = sum(int(math.floor(s["TotalPlaytime"] / cm)) == s["TimeChunkIndex"] for s in D["snaps"])
    res_units = {"SpawnerChargesChunkMinutes": cm, "TimeChunkDurationMinutes": D["sim"]["TimeChunkDurationMinutes"],
                 "snapshots": len(D["snaps"]), "TimeChunkIndex==floor(TotalPlaytime/1440)": units_ok,
                 "active_minutes_per_chunk_last": r6(D["snaps"][-1]["Playtime"] / max(1, D["snaps"][-1]["TimeChunkIndex"])),
                 "Playtime/TotalPlaytime_last": r6(D["snaps"][-1]["Playtime"] / D["snaps"][-1]["TotalPlaytime"]),
                 "design_active_minutes_per_day(TimeChunkDurationMinutes)": D["sim"]["TimeChunkDurationMinutes"],
                 "note": "GetSpawnerOptimumTimingForCurrentMode divides OptimumTime (minutes, IsDays=0) by TimeChunkDurationMinutes to put it on the floor(TotalPlaytime/1440) day scale"}
    # ReferenceStatValue vs snapshot flat stat
    exact = []
    sv_by = defaultdict(set)
    for (kind, t, r, L), row in D["ev"].items():
        if t not in USED_TYPES or r > 4 or is_filled(row):
            continue
        tc = row["ReferenceTimeChunkIndex"]
        sv_by[(kind, t, tc)].add(row["ReferenceStatValue"])
        if tc in snap_by_tc:
            stat = STAT_PREFIX[t] + ("DamageFlat" if kind == "weapon" else "DefenceFlat")
            tv = D["sstats"][snap_by_tc[tc]["TimeChunkSnapshots#idx"]][stat]["TotalValue"]
            exact.append({"kind": kind, "DamageType": WT[t], "Rarity": RARITY[r], "E": L, "tc": tc,
                          "ReferenceStatValue": r6(row["ReferenceStatValue"]), "snapshot_TotalValue": r6(tv),
                          "rel_diff": r6(abs(tv - row["ReferenceStatValue"]) / tv)})
    res["stat_value_vs_snapshot"] = {"rows_at_snapshot_chunks": len(exact),
                                     "match_float32(rel<1e-6)": sum(e["rel_diff"] < 1e-6 for e in exact),
                                     "rows": exact}
    res["stat_value_consistency"] = {"(kind,type,tc)_groups": len(sv_by),
                                     "groups_with_single_value": sum(len(v) == 1 for v in sv_by.values())}
    # bracket check: sv must lie between snapshot values around tc (stat is non-decreasing)
    br = Counter()
    outside = []
    tcs = sorted(snap_by_tc)
    for (kind, t, tc), vals in sv_by.items():
        if tc in snap_by_tc:
            continue
        lo = max([x for x in tcs if x < tc], default=None)
        hi = min([x for x in tcs if x > tc], default=None)
        if lo is None or hi is None:
            br["outside_snapshot_range"] += 1
            continue
        stat = STAT_PREFIX[t] + ("DamageFlat" if kind == "weapon" else "DefenceFlat")
        a = D["sstats"][snap_by_tc[lo]["TimeChunkSnapshots#idx"]][stat]["TotalValue"]
        b = D["sstats"][snap_by_tc[hi]["TimeChunkSnapshots#idx"]][stat]["TotalValue"]
        for v in vals:
            br["checked"] += 1
            ok = (a * (1 - 1e-7) <= v <= b * (1 + 1e-7))
            br["inside_bracket"] += ok
            if not ok:
                other = []
                for t2 in USED_TYPES:
                    st2 = STAT_PREFIX[t2] + ("DamageFlat" if kind == "weapon" else "DefenceFlat")
                    a2 = D["sstats"][snap_by_tc[lo]["TimeChunkSnapshots#idx"]][st2]["TotalValue"]
                    b2 = D["sstats"][snap_by_tc[hi]["TimeChunkSnapshots#idx"]][st2]["TotalValue"]
                    if a2 * (1 - 1e-7) <= v <= b2 * (1 + 1e-7):
                        other.append(WT[t2])
                br["outside_but_inside_other_type_bracket"] += bool(other)
                outside.append({"kind": kind, "type": WT[t], "tc": tc, "sv": r6(v), "lo_chunk": lo, "lo": r6(a),
                                "hi_chunk": hi, "hi": r6(b), "fits_bracket_of": other})
    res["stat_value_bracket"] = dict(br)
    res["stat_value_outside_bracket"] = outside
    # ReferenceTimeChunkIndex vs target timing
    lag = []
    for (kind, t, r, L), row in D["ev"].items():
        if t not in USED_TYPES or r > 4 or is_filled(row):
            continue
        T = D["timings"][kind][r][L - 1]
        lag.append({"kind": kind, "t": t, "r": r, "L": L, "T": T, "tc": row["ReferenceTimeChunkIndex"],
                    "lag": row["ReferenceTimeChunkIndex"] - T})
    lagsum = {}
    for kind in ("weapon", "armor"):
        for t in USED_TYPES:
            ls = [x for x in lag if x["kind"] == kind and x["t"] == t]
            lagsum[f"{kind}|{WT[t]}"] = {
                "rows": len(ls),
                "tc==ceil(T)": sum(x["tc"] == math.ceil(x["T"] - 1e-6) for x in ls),
                "tc<ceil(T)": sum(x["tc"] < math.ceil(x["T"] - 1e-6) for x in ls),
                "lag_days": stats_of([x["lag"] for x in ls]),
            }
    res["timechunk_vs_target_timing"] = lagsum
    res["timechunk_lag_gt_40_days"] = [(x["kind"], WT[x["t"]], RARITY[x["r"]], x["L"], r6(x["T"]), x["tc"], r6(x["lag"]))
                                       for x in sorted(lag, key=lambda x: -x["lag"]) if x["lag"] > 40]
    res["simulated_levels_horizon"] = {
        f"{kind}|{WT[t]}|{RARITY[r]}": max(L for L in range(1, 16) if not is_filled(D["ev"][(kind, t, r, L)]))
        for kind in ("weapon", "armor") for t in USED_TYPES for r in TIMED_RARITIES}
    # ReferenceTimeChunkIndex vs the evolution seen in the snapshot items (item of that type+rarity reaches level L)
    chk = Counter()
    bad = []
    snaps = D["snaps"]
    for (kind, t, r, L), row in sorted(D["ev"].items()):
        if t not in USED_TYPES or r > 4 or is_filled(row):
            continue
        tc = row["ReferenceTimeChunkIndex"]
        keys = [k for k, it in D["items"].items() if it["DamageType"] == t and it["Rarity"] == r
                and is_armor_item(it) == (kind == "armor") and k != "player_claw"]
        before = [s_ for s_ in snaps if s_["TimeChunkIndex"] < tc]
        after = [s_ for s_ in snaps if s_["TimeChunkIndex"] > tc]
        if not before or not after:
            chk["not_bracketed"] += 1
            continue
        sb, sa = before[-1]["TimeChunkSnapshots#idx"], after[0]["TimeChunkSnapshots#idx"]
        lvb = [states[sb].get(k, {"evo_level": 0})["evo_level"] for k in keys]
        lva = [states[sa].get(k, {"evo_level": 0})["evo_level"] for k in keys]
        chk["checked"] += 1
        # some item of this type+rarity (weapon: 1 item, armor: helmet+shield) evolves to E within the bracket
        ok = any(b_ <= L - 1 and a_ >= L for b_, a_ in zip(lvb, lva))
        chk["item_reaches_E_in_bracket"] += ok
        if not ok:
            bad.append({"kind": kind, "type": WT[t], "rarity": RARITY[r], "E": L, "tc": tc,
                        "levels_before": lvb, "levels_after": lva, "items": keys})
    res["timechunk_vs_snapshot_items"] = {**chk, "mismatches": bad}
    res["last_snapshot_chunk"] = D["snaps"][-1]["TimeChunkIndex"]
    # ReferenceHeuristicValue structure
    #  armor: one value per target timing T, shared by all rarities/types?
    groups = defaultdict(set)
    for (kind, t, r, L), row in D["ev"].items():
        if kind != "armor" or t != 0 or r > 4 or is_filled(row):
            continue
        groups[round(D["timings"]["armor"][r][L - 1], 3)].add((RARITY[r], L, r6(row["ReferenceHeuristicValue"])))
    shared = {str(T): sorted(v) for T, v in groups.items() if len(v) > 1}
    res["armor_heuristic_shared_by_timing"] = {
        "timings_with_several_rows": len(shared),
        "timings_where_all_rows_equal": sum(len({x[2] for x in v}) == 1 for v in shared.values()),
        "groups": shared}
    #  armor: hv / sv of the type that evolved at ceil(T) (the on-time type)
    arm_ratio = []
    for (kind, t, r, L), row in sorted(D["ev"].items()):
        if kind != "armor" or t not in USED_TYPES or r > 4 or is_filled(row):
            continue
        T = D["timings"]["armor"][r][L - 1]
        if row["ReferenceTimeChunkIndex"] == math.ceil(T - 1e-6):
            arm_ratio.append({"t": WT[t], "r": RARITY[r], "L": L, "T": T,
                              "hv/sv": r6(row["ReferenceHeuristicValue"] / row["ReferenceStatValue"]),
                              "hv-0.8*sv": r6(row["ReferenceHeuristicValue"] - 0.8 * row["ReferenceStatValue"])})
    res["armor_hv_over_sv_on_time_rows"] = {"stats": stats_of([x["hv/sv"] for x in arm_ratio]),
                                            "stats_T_ge_150": stats_of([x["hv/sv"] for x in arm_ratio if x["T"] >= 150]),
                                            "rows": arm_ratio}
    #  weapons: hv/sv per series (first, last, min, max) -> rarity-specific ratio that decays towards an asymptote
    wser = {}
    for t in USED_TYPES:
        for r in TIMED_RARITIES:
            rat = [D["ev"][("weapon", t, r, L)]["ReferenceHeuristicValue"] / D["ev"][("weapon", t, r, L)]["ReferenceStatValue"]
                   for L in range(1, 16) if not is_filled(D["ev"][("weapon", t, r, L)])]
            pct = [D["ev"][("weapon", t, r, L)]["ReferenceStatPercent"] for L in range(1, 16)
                   if not is_filled(D["ev"][("weapon", t, r, L)])]
            wser[f"{WT[t]}|{RARITY[r]}"] = {"levels": len(rat), "hv/sv_first": r6(rat[0]), "hv/sv_last": r6(rat[-1]),
                                            "hv/sv_min": r6(min(rat)), "hv/sv_max": r6(max(rat)),
                                            "monotone_nonincreasing": all(rat[i + 1] <= rat[i] + 1e-9 for i in range(len(rat) - 1)),
                                            "ReferenceStatPercent_simulated": pct}
    res["weapon_hv_over_sv"] = wser
    #  fitted heuristic model for weapons: hv ~= k_t * M_r * AttackCooldown * sv * (1 + eps), eps >= 0
    cd = {}
    for key, it in sorted(D["items"].items()):
        if it["SlotType"] == 0 and it["Rarity"] <= 4 and key != "player_claw":
            cd[(it["DamageType"], it["Rarity"])] = (it["AttackCooldown"], key)
    qtr = {}
    rho_rows = []
    for t in USED_TYPES:
        for r in TIMED_RARITIES:
            c, key = cd[(t, r)]
            rr = []
            for L in range(1, 16):
                row = D["ev"][("weapon", t, r, L)]
                if is_filled(row):
                    continue
                rho = row["ReferenceHeuristicValue"] / (c * row["ReferenceStatValue"])
                rr.append(rho)
                rho_rows.append((t, r, L, row["ReferenceTimeChunkIndex"], rho))
            qtr[(t, r)] = min(rr)
    M_fit = [1.0, 1.25, 1.5, 1.875, 2.5]
    k_t = {t: qtr[(t, 0)] for t in USED_TYPES}
    m_emp = {f"{WT[t]}": [r6(qtr[(t, r)] / qtr[(t, 0)]) for r in TIMED_RARITIES] for t in USED_TYPES}
    # k_t: common least-squares on log(min ratio / M_r)
    k_ls = {WT[t]: r6(math.exp(np.mean([math.log(qtr[(t, r)] / M_fit[r]) for r in TIMED_RARITIES]))) for t in USED_TYPES}
    eps = []
    eps_rows = []
    for t, r, L, tc, rho in rho_rows:
        k = math.exp(np.mean([math.log(qtr[(t, x)] / M_fit[x]) for x in TIMED_RARITIES]))
        e = rho / (k * M_fit[r]) - 1
        eps.append(e)
        # envelope row = the row that defines qtr[(t, r)] (minimum ratio of its series): in-sample by construction
        eps_rows.append({"t": WT[t], "r": RARITY[r], "E": L, "tc": tc, "eps": r6(e), "envelope_row": rho == qtr[(t, r)]})
    oos = [x for x in eps_rows if not x["envelope_row"]]
    ins = [x for x in eps_rows if x["envelope_row"]]

    def spearman(x, y):
        rx = np.argsort(np.argsort(x)).astype(float)
        ry = np.argsort(np.argsort(y)).astype(float)
        return float(np.corrcoef(rx, ry)[0, 1])

    pos = [x for x in oos if x["eps"] > 0]
    a_, b_, r2_, _ = fit_line(np.log([x["tc"] for x in pos]), np.log([x["eps"] for x in pos]))
    # additive alternative: A = hv - k_t*M_r*cd*sv should be constant if eps were an additive flat term
    add_alt = {}
    for t in USED_TYPES:
        for r in (0,):
            c, key = cd[(t, r)]
            k = math.exp(np.mean([math.log(qtr[(t, x)] / M_fit[x]) for x in TIMED_RARITIES]))
            A = [r6(D["ev"][("weapon", t, r, L)]["ReferenceHeuristicValue"] - k * M_fit[r] * c * D["ev"][("weapon", t, r, L)]["ReferenceStatValue"])
                 for L in range(1, 16) if not is_filled(D["ev"][("weapon", t, r, L)])]
            add_alt[f"{WT[t]}|{RARITY[r]}({key})"] = A
    early = defaultdict(list)
    for x in oos:
        if 40 <= x["tc"] <= 60:
            early[x["r"]].append([x["t"], x["E"], x["tc"], x["eps"]])
    res["weapon_heuristic_fit"] = {
        "model": "hv ~= k_t * M_r * AttackCooldown(item) * ReferenceStatValue * (1+eps)",
        "parameters": "3 k_t + 5 M_r read off the 15 envelope rows (per-series minimum of hv/(cd*sv)); M_r then rounded to the rule 1/1.25/1.5/1.875/2.5",
        "items_used_for_cooldown": {f"{WT[t]}|{RARITY[r]}": [cd[(t, r)][1], cd[(t, r)][0]] for t in USED_TYPES for r in TIMED_RARITIES},
        "min_hv_over_cd_sv_by_type_rarity": {f"{WT[t]}|{RARITY[r]}": r6(qtr[(t, r)]) for t in USED_TYPES for r in TIMED_RARITIES},
        "empirical_M_r(min ratio / common)": m_emp,
        "M_r_rule": M_fit,
        "k_t(geo-mean of min ratio / M_r)": k_ls,
        "envelope_rows": {"n": len(ins), "tc_range": [min(x["tc"] for x in ins), max(x["tc"] for x in ins)],
                          "eps": stats_of([x["eps"] for x in ins])},
        "eps_stats_all_in_and_out_of_sample": stats_of(eps),
        "eps_stats_out_of_sample": stats_of([x["eps"] for x in oos]),
        "eps_stats_out_of_sample_tc_ge_200": stats_of([x["eps"] for x in oos if x["tc"] >= 200]),
        "eps_stats_out_of_sample_tc_lt_50": stats_of([x["eps"] for x in oos if x["tc"] < 50]),
        "eps_stats_tc_ge_200_incl_envelope": stats_of([x["eps"] for x in eps_rows if x["tc"] >= 200]),
        "out_of_sample_eps_trend": {
            "spearman(eps, tc)": r6(spearman([x["tc"] for x in oos], [x["eps"] for x in oos])),
            "loglog_fit_eps_vs_tc(eps>0)": {"n": len(pos), "slope": r6(b_), "R2": r6(r2_)},
            "rows_tc_40_60_by_rarity[t,E,tc,eps]": dict(early),
        },
        "additive_alternative_A=hv-k*M*cd*sv(Common series)": add_alt,
        "eps_out_of_sample_rows_lag_gt_40_days": stats_of([x["eps"] for x in oos
                                                           if x["tc"] - D["timings"]["weapon"][RARITY.index(x["r"])][x["E"] - 1] > 40]),
    }
    write_csv(OUT / "weapon_heuristic_eps.csv", eps_rows)
    #  same tc, same type, different rarity -> hv differs (so not a function of time alone)
    same = defaultdict(list)
    for (kind, t, r, L), row in D["ev"].items():
        if kind == "weapon" and t in USED_TYPES and r <= 4 and not is_filled(row):
            same[(t, row["ReferenceTimeChunkIndex"])].append((RARITY[r], L, r6(row["ReferenceHeuristicValue"]), r6(row["ReferenceHeuristicValue"] / row["ReferenceStatValue"])))
    res["weapon_same_tc_examples"] = {f"{WT[k[0]]}|tc={k[1]}": v for k, v in sorted(same.items()) if len(v) > 1}
    return res_units, res


# ----------------------------------------------------------------------------- shards
def shards_section(D, states):
    res = {}
    tab = {}
    for kind in ("weapon", "armor"):
        for r in TIMED_RARITIES:
            seq = [D["shards"][(kind, r, L)] for L in range(1, 16)]
            plateau_from = next(L for L in range(1, 16) if all(x == seq[L - 1] for x in seq[L - 1:]))
            T = D["timings"][kind][r]
            sim_levels = [L for L in range(1, 16) if not is_filled(D["ev"][(kind, 0, r, L)])]
            # power law Sh ~ T^p over the levels before the plateau
            pre = [L for L in range(1, plateau_from + 1)]
            a, b, r2, _ = fit_line(np.log([T[L - 1] for L in pre]), np.log([seq[L - 1] for L in pre])) if len(pre) >= 3 else (None, None, None, None)
            tab[f"{kind}|{RARITY[r]}"] = {
                "shards": seq, "all_on_nice_grid": all(is_nice(x) for x in seq),
                "plateau_from_E": plateau_from, "last_simulated_E": max(sim_levels),
                "cum_copies_to_plateau": int(sum(seq[:plateau_from])),
                "loglog_fit_vs_T_before_plateau": None if a is None else {"exponent": r6(b), "R2": r6(r2), "n": len(pre)},
                "step_ratio": [r6(seq[i + 1] / seq[i]) for i in range(plateau_from - 1)],
            }
    res["tables"] = tab
    # capture hypothesis: Sh[L] = nice(copies held at time T_L by an item of rarity r that is at evolution level L-1)
    snaps = D["snaps"]
    days = [s["TotalPlaytime"] / D["sim"]["SpawnerChargesChunkMinutes"] for s in snaps]
    rows = []
    single = defaultdict(list)
    for kind in ("weapon", "armor"):
        for r in TIMED_RARITIES:
            for L in range(1, 16):
                T = D["timings"][kind][r][L - 1]
                if T > days[-1]:
                    continue
                j = max(i for i in range(len(snaps)) if days[i] <= T)
                if j + 1 >= len(snaps):
                    continue
                i0, i1 = snaps[j]["TimeChunkSnapshots#idx"], snaps[j + 1]["TimeChunkSnapshots#idx"]
                sh = D["shards"][(kind, r, L)]
                cands = []
                for key, it in D["items"].items():
                    if it["Rarity"] != r or (kind == "armor") != is_armor_item(it) or it["DamageType"] not in USED_TYPES:
                        continue
                    a, b = states[i0].get(key), states[i1].get(key)
                    if a is None and b is None:
                        continue
                    a = a or {"evo_level": 0, "copies": 0, "evolved": False}
                    if not b or a["evo_level"] != L - 1 or b["evo_level"] not in (L - 1, L):
                        continue
                    # copies acquired in the bracket (an evolution to L in between consumed `sh` copies)
                    gained = b["copies"] - a["copies"] + (sh if b["evo_level"] == L else 0)
                    c = a["copies"] + gained * (T - days[j]) / (days[j + 1] - days[j])
                    cands.append((key, WT[it["DamageType"]] + ("*" if b["evo_level"] == L else ""), r6(c)))
                best = min(cands, key=lambda x: abs(x[2] / sh - 1)) if cands else None
                # pre-specified single-reference rules (no choice of the best candidate)
                for rule, pick in (("stabbing_only", lambda cs: [x for x in cs if x[1].rstrip("*") == "Stabbing"]),
                                   ("cutting_only", lambda cs: [x for x in cs if x[1].rstrip("*") == "Cutting"]),
                                   ("buckshot_only", lambda cs: [x for x in cs if x[1].rstrip("*") == "Buckshot"]),
                                   ("any_type", lambda cs: cs)):
                    sel = pick(cands)
                    for agg in ("max", "min"):
                        if sel:
                            v = (max if agg == "max" else min)(x[2] for x in sel)
                            single[(kind, f"{rule}|{agg}_copies")].append((RARITY[r], L, r6(v / sh - 1)))
                        else:
                            single[(kind, f"{rule}|{agg}_copies")].append((RARITY[r], L, None))
                rows.append({"kind": kind, "Rarity": RARITY[r], "E": L, "T": T, "shards": sh,
                             "candidates": len(cands),
                             "best_item": best[0] if best else "", "best_type": best[1] if best else "",
                             "interp_copies_at_T": best[2] if best else None,
                             "rel_err": r6(best[2] / sh - 1) if best else None,
                             "nice(interp)==shards": (nice(best[2]) == sh) if best else None,
                             "all_candidates": ";".join(f"{k}:{c}" for k, _, c in cands)})
    write_csv(OUT / "shards_capture_check.csv", rows)
    ok = [x for x in rows if x["rel_err"] is not None]
    # plateau: beyond the last level simulated by any damage type the table holds the last captured value
    pl = Counter()
    for kind in ("weapon", "armor"):
        for r in TIMED_RARITIES:
            last = max(L for t in USED_TYPES for L in range(1, 16) if not is_filled(D["ev"][(kind, t, r, L)]))
            for L in range(last + 1, 16):
                pl["levels_beyond_last_simulated"] += 1
                pl["equal_to_last_captured"] += D["shards"][(kind, r, L)] == D["shards"][(kind, r, last)]
    res["plateau_check"] = dict(pl)
    res["capture_hypothesis"] = {
        "levels_checked": len(rows), "with_candidate": len(ok),
        "|rel_err|<=5%": sum(abs(x["rel_err"]) <= 0.05 for x in ok),
        "|rel_err|<=15%": sum(abs(x["rel_err"]) <= 0.15 for x in ok),
        "nice(interp)==shards": sum(bool(x["nice(interp)==shards"]) for x in ok),
        "abs_rel_err": stats_of([abs(x["rel_err"]) for x in ok]),
        "best_type_counts": dict(Counter(x["best_type"] for x in ok)),
        "best_type_counts_by_kind": {kind: dict(Counter(x["best_type"] for x in ok if x["kind"] == kind)) for kind in ("weapon", "armor")},
        "note": "best of up to 6 candidates per level: optimistic",
    }
    st_res = {}
    for (kind, rule), lst in sorted(single.items()):
        e = [x[2] for x in lst if x[2] is not None]
        st_res[f"{kind}|{rule}"] = {
            "levels": len(lst), "with_candidate": len(e),
            "|rel_err|<=5%": sum(abs(x) <= 0.05 for x in e), "|rel_err|<=15%": sum(abs(x) <= 0.15 for x in e),
            "abs_rel_err": stats_of([abs(x) for x in e]),
            "levels_err_gt_5%[rarity,E,rel_err]": [list(x) for x in lst if x[2] is not None and abs(x[2]) > 0.05],
        }
    res["capture_single_reference_tests"] = st_res
    return res


# ----------------------------------------------------------------------------- ArmorBaseByRarity
def armor_base_section(D):
    """ArmorBaseByRarity is written by the simulation at 6af011f (SimulationSystem.TryApplyArmorBaseAutoBalance(item),
    CombatBalanceConfig.TrySetArmorBaseValue(rarity, value), ResetArmorBaseValues(); all bodies empty).
    Hypothesis tested here: ArmorBase[r] ~= nice(k * <Type>DefenceFlat) at the first acquisition of an armor of rarity r.
    The snapshots only bracket the acquisition moment (previous snapshot .. first snapshot that holds the item)."""
    AB = D["arr"]["ArmorBaseByRarity"]
    out = {"values": AB, "rarity5_6_are_zero": AB[5] == 0 and AB[6] == 0, "rows": []}
    seen = set()
    prev = None
    cnt = Counter()
    for s_ in D["snaps"]:
        i = s_["TimeChunkSnapshots#idx"]
        for it in D["sitems"][i]:
            c = D["items"][it["Key"]]
            r = c["Rarity"]
            if not is_armor_item(c) or r in seen:
                continue
            seen.add(r)
            ip = prev["TimeChunkSnapshots#idx"] if prev else i
            br = {}
            fits_raw, fits_nice = [], []
            for t in USED_TYPES:
                st = STAT_PREFIX[t] + "DefenceFlat"
                lo = D["sstats"][ip][st]["TotalValue"]
                hi = D["sstats"][i][st]["TotalValue"]
                br[WT[t]] = [r6(lo), r6(hi), r6(AB[r] / hi) if hi else None]
                if lo * (1 - 1e-7) <= AB[r] <= hi * (1 + 1e-7):
                    fits_raw.append(WT[t])
                if lo > 0 and nice(lo) <= AB[r] <= nice(hi):
                    fits_nice.append(WT[t])
            row = {"Rarity": RARITY[r], "ArmorBase": AB[r], "first_item": it["Key"], "first_item_type": WT[c["DamageType"]],
                   "copies_when_first_seen": it["Copies"],
                   "bracket_chunks": [prev["TimeChunkIndex"] if prev else s_["TimeChunkIndex"], s_["TimeChunkIndex"]],
                   "DefenceFlat_bracket[lo,hi,ArmorBase/hi]": br,
                   "inside_raw_bracket_of": fits_raw, "inside_nice_bracket_of": fits_nice}
            out["rows"].append(row)
            cnt["rarities"] += 1
            cnt["inside_raw_bracket_of_some_type"] += bool(fits_raw)
            cnt["inside_nice_bracket_of_some_type"] += bool(fits_nice)
            cnt["inside_raw_bracket_of_own_type"] += WT[c["DamageType"]] in fits_raw
        prev = s_
    out["counts"] = dict(cnt)
    out["on_nice_grid"] = sum(is_nice(x) for x in AB[:5])
    return out


# ----------------------------------------------------------------------------- growth ratios
def growth_section(D):
    res = {}
    arr = D["arr"]
    # pre-evolution arrays across rarities 0..4
    pre = {}
    for name in ("CutDamageGrowthPerLevelByRarity", "PierceDamageGrowthPerLevelByRarity",
                 "SmashDamageGrowthPerLevelByRarity", "ArmorBaseByRarity", "ArmorGrowthPerLevelByRarity"):
        v = arr[name][:5]
        a, b, r2, yh = fit_line(range(5), np.log(v))
        pre[name] = {"values_C..L": v, "step_ratios": [r6(v[i + 1] / v[i]) for i in range(4)],
                     "geometric_fit": {"ratio_per_rarity": r6(math.exp(b)), "R2_log": r6(r2),
                                       "max_abs_log_resid": r6(float(np.max(np.abs(np.log(v) - yh))))},
                     "C_to_L": r6(v[4] / v[0])}
    res["pre_evolution_arrays"] = pre
    res["smash_over_pierce_per_copy"] = [r6(a / b) for a, b in zip(arr["SmashDamageGrowthPerLevelByRarity"][:5], arr["PierceDamageGrowthPerLevelByRarity"][:5])]
    res["cut_over_pierce_per_copy"] = [r6(a / b) for a, b in zip(arr["CutDamageGrowthPerLevelByRarity"][:5], arr["PierceDamageGrowthPerLevelByRarity"][:5])]
    # evolution Value ladders
    ser = {}
    for kind in ("weapon", "armor"):
        for t in (USED_TYPES if kind == "weapon" else (0,)):
            for r in TIMED_RARITIES:
                rows = [D["ev"][(kind, t, r, L)] for L in range(1, 16)]
                sim = [x for x in rows if not is_filled(x)]
                fil = [x for x in rows if is_filled(x)]
                T = D["timings"][kind][r]
                Ls = [x["EvolutionLevel"] for x in sim]
                Vs = [x["Value"] for x in sim]
                e = {}
                if len(sim) >= 3:
                    a, b, r2, yh = fit_line(Ls, np.log(Vs))
                    e["sim_geometric_in_E"] = {"ratio": r6(math.exp(b)), "R2_log": r6(r2),
                                               "max_rel_err": r6(float(np.max(np.abs(np.exp(np.log(Vs) - yh) - 1))))}
                    a, b, r2, yh = fit_line(np.log([T[L - 1] for L in Ls]), np.log(Vs))
                    e["sim_power_in_T"] = {"exponent": r6(b), "R2_log": r6(r2),
                                           "max_rel_err": r6(float(np.max(np.abs(np.exp(np.log(Vs) - yh) - 1))))}
                    tcs_ = [x["ReferenceTimeChunkIndex"] for x in sim]
                    a, b, r2, yh = fit_line(np.log(tcs_), np.log(Vs))
                    e["sim_power_in_tc"] = {"exponent": r6(b), "R2_log": r6(r2),
                                            "max_rel_err": r6(float(np.max(np.abs(np.exp(np.log(Vs) - yh) - 1))))}
                    hv_ = [x["ReferenceHeuristicValue"] for x in sim]
                    a, b, r2, yh = fit_line(np.log([T[L - 1] for L in Ls]), np.log(hv_))
                    e["sim_H_power_in_T"] = {"exponent": r6(b), "R2_log": r6(r2)}
                    a, b, r2, yh = fit_line(np.log(tcs_), np.log(hv_))
                    e["sim_H_power_in_tc"] = {"exponent": r6(b), "R2_log": r6(r2)}
                    sv_ = [x["ReferenceStatValue"] for x in sim]
                    e["sim_sv_power_in_T_R2"] = r6(fit_line(np.log([T[L - 1] for L in Ls]), np.log(sv_))[2])
                    e["sim_sv_power_in_tc_R2"] = r6(fit_line(np.log(tcs_), np.log(sv_))[2])
                    a, b, r2, yh = fit_line(np.log(Ls), np.log(Vs))
                    e["sim_power_in_E"] = {"exponent": r6(b), "R2_log": r6(r2)}
                e["sim_step_ratios"] = [r6(Vs[i + 1] / Vs[i]) for i in range(len(Vs) - 1)]
                allV = [x["Value"] for x in rows]
                if fil:
                    i0 = len(sim)
                    e["filled_steps"] = [r6(allV[i] - allV[i - 1]) for i in range(i0, 15)]
                    e["filled_ratios"] = [r6(allV[i] / allV[i - 1]) for i in range(i0, 15)]
                e["E1"] = allV[0]
                e["E15"] = allV[14]
                ser[f"{kind}|{WT[t] if kind == 'weapon' else 'all(synced)'}|{RARITY[r]}"] = e
    res["evolution_ladders"] = ser
    # summary of fits
    g = [v["sim_geometric_in_E"]["R2_log"] for v in ser.values() if "sim_geometric_in_E" in v]
    p = [v["sim_power_in_T"]["R2_log"] for v in ser.values() if "sim_power_in_T" in v]
    pe = [v["sim_power_in_T"]["exponent"] for v in ser.values() if "sim_power_in_T" in v]
    gm = [v["sim_geometric_in_E"]["max_rel_err"] for v in ser.values() if "sim_geometric_in_E" in v]
    pm = [v["sim_power_in_T"]["max_rel_err"] for v in ser.values() if "sim_power_in_T" in v]
    tcr = [v["sim_power_in_tc"]["R2_log"] for v in ser.values() if "sim_power_in_tc" in v]
    tcm = [v["sim_power_in_tc"]["max_rel_err"] for v in ser.values() if "sim_power_in_tc" in v]
    res["fit_summary"] = {"series_with_>=3_simulated_levels": len(g),
                          "geometric_in_E_R2": stats_of(g), "geometric_in_E_max_rel_err": stats_of(gm),
                          "power_in_T_R2": stats_of(p), "power_in_T_max_rel_err": stats_of(pm),
                          "power_in_T_exponent": stats_of(pe),
                          "power_in_tc_R2": stats_of(tcr), "power_in_tc_max_rel_err": stats_of(tcm)}
    res["fit_table"] = {k: {"geoE_R2": v["sim_geometric_in_E"]["R2_log"], "geoE_maxerr": v["sim_geometric_in_E"]["max_rel_err"],
                            "powT_R2": v["sim_power_in_T"]["R2_log"], "powT_maxerr": v["sim_power_in_T"]["max_rel_err"],
                            "powT_exp": v["sim_power_in_T"]["exponent"],
                            "powtc_R2": v["sim_power_in_tc"]["R2_log"], "powtc_maxerr": v["sim_power_in_tc"]["max_rel_err"],
                            "H_powT_R2": v["sim_H_power_in_T"]["R2_log"], "H_powtc_R2": v["sim_H_power_in_tc"]["R2_log"],
                            "sv_powT_R2": v["sim_sv_power_in_T_R2"], "sv_powtc_R2": v["sim_sv_power_in_tc_R2"]}
                        for k, v in ser.items() if "sim_geometric_in_E" in v}
    res["T_vs_tc_R2_comparison"] = {
        "Value: series where T fits better": sum(v["powT_R2"] > v["powtc_R2"] for v in res["fit_table"].values()),
        "Value: series where tc fits better": sum(v["powT_R2"] < v["powtc_R2"] for v in res["fit_table"].values()),
        "H: series where T fits better": sum(v["H_powT_R2"] > v["H_powtc_R2"] for v in res["fit_table"].values()),
        "H: series where tc fits better": sum(v["H_powT_R2"] < v["H_powtc_R2"] for v in res["fit_table"].values()),
        "sv: series where T fits better": sum(v["sv_powT_R2"] > v["sv_powtc_R2"] for v in res["fit_table"].values()),
        "sv: series where tc fits better": sum(v["sv_powT_R2"] < v["sv_powtc_R2"] for v in res["fit_table"].values()),
        "series where H and sv prefer the same clock": sum((v["H_powT_R2"] > v["H_powtc_R2"]) == (v["sv_powT_R2"] > v["sv_powtc_R2"]) for v in res["fit_table"].values()),
    }
    armor_fill = [x for k, v in ser.items() if k.startswith("armor") for x in v.get("filled_ratios", [])]
    res["armor_filled_step_ratio"] = stats_of(armor_fill)
    # player flat stats along the simulation (what evolution values track)
    st = []
    for s in D["snaps"]:
        if s["TimeChunkIndex"] < 1:
            continue
        stt = D["sstats"][s["TimeChunkSnapshots#idx"]]
        st.append((s["TimeChunkIndex"], stt["StabDamageFlat"]["TotalValue"], stt["CutDamageFlat"]["TotalValue"],
                   stt["BuckshotDamageFlat"]["TotalValue"], stt["StabDefenceFlat"]["TotalValue"]))
    fits = {}
    for j, name in enumerate(("StabDamageFlat", "CutDamageFlat", "BuckshotDamageFlat", "StabDefenceFlat"), start=1):
        x = [a[0] for a in st if a[0] >= 17]
        y = [a[j] for a in st if a[0] >= 17]
        a, b, r2, yh = fit_line(np.log(x), np.log(y))
        fits[name] = {"power_exponent_vs_chunk(chunk>=17)": r6(b), "R2_log": r6(r2),
                      "max_rel_err": r6(float(np.max(np.abs(np.exp(np.log(y) - yh) - 1))))}
    res["player_flat_stat_curve"] = fits
    # rarity premium: Value as % of the player's flat stat (ReferenceStatPercent, simulated rows)
    prem = {}
    for kind in ("weapon", "armor"):
        for r in TIMED_RARITIES:
            v = [D["ev"][(kind, t, r, L)]["ReferenceStatPercent"] for t in USED_TYPES for L in range(1, 16)
                 if not is_filled(D["ev"][(kind, t, r, L)])]
            prem[f"{kind}|{RARITY[r]}"] = stats_of(v)
    res["stat_percent_by_rarity(simulated rows)"] = prem
    return res


# ----------------------------------------------------------------------------- main
def main():
    OUT.mkdir(parents=True, exist_ok=True)
    con = sqlite3.connect(DB)
    D = load(con)
    states = snapshot_item_states(D)
    summary = {"levers": D["levers"]}
    summary["runtime"] = runtime_section(D, states)
    gen, gen_rows = generator_section(D)
    summary["generator"] = gen
    units, ref = reference_section(D, gen_rows, states)
    summary["units"] = units
    summary["reference"] = ref
    summary["shards"] = shards_section(D, states)
    summary["growth"] = growth_section(D)
    summary["armor_base"] = armor_base_section(D)
    # exceptions (explicit)
    exc = {}
    exc["value_not_reproduced"] = [(r["kind"], r["DamageType"], r["Rarity"], r["E"], r["Value"], r["Value_pred"])
                                   for r in gen_rows if not r["Value_ok"]]
    exc["copyvalue_not_reproduced"] = [(r["kind"], r["DamageType"], r["Rarity"], r["E"], r["CopyValue"], r["CopyValue_pred"])
                                       for r in gen_rows if not r["CopyValue_ok"]]
    exc["simulated_rows_where_min_rule_changes_value"] = [
        (r["kind"], r["DamageType"], r["Rarity"], r["E"], r["binding"], r["ReferenceHeuristicValue"],
         nice(r["ReferenceHeuristicValue"]), r["Value"], r6(r["Value"] / r["ReferenceHeuristicValue"]))
        for r in gen_rows if not r["filled_beyond_sim"] and abs(nice(r["ReferenceHeuristicValue"]) - r["Value"]) > 1e-6
        and (r["kind"] == "weapon" or r["DamageType"] == "Stabbing")]
    # armor cells whose (tc, sv) are shared by several types: written once and copied by SyncShieldValuesByLeader
    snap_by_tc = {s_["TimeChunkIndex"]: s_ for s_ in D["snaps"]}
    tcs = sorted(snap_by_tc)
    shared_ref = []
    for r in TIMED_RARITIES:
        for L in range(1, 16):
            rows = {t: D["ev"][("armor", t, r, L)] for t in USED_TYPES}
            if is_filled(rows[0]):
                continue
            grp = defaultdict(list)
            for t, x in rows.items():
                grp[(x["ReferenceTimeChunkIndex"], x["ReferenceStatValue"])].append(WT[t])
            for (tc, sv), types in grp.items():
                if len(types) < 2:
                    continue
                lo = max([x for x in tcs if x <= tc], default=None)
                hi = min([x for x in tcs if x >= tc], default=None)
                owner = []
                if lo is not None and hi is not None:
                    for t2 in USED_TYPES:
                        st2 = STAT_PREFIX[t2] + "DefenceFlat"
                        a2 = D["sstats"][snap_by_tc[lo]["TimeChunkSnapshots#idx"]][st2]["TotalValue"]
                        b2 = D["sstats"][snap_by_tc[hi]["TimeChunkSnapshots#idx"]][st2]["TotalValue"]
                        if a2 * (1 - 1e-7) <= sv <= b2 * (1 + 1e-7):
                            owner.append(WT[t2])
                last_sim = max(LL for LL in range(1, 16) if not is_filled(D["ev"][("armor", 0, r, LL)]))
                shared_ref.append({"Rarity": RARITY[r], "E": L, "tc": tc, "sv": r6(sv), "types_sharing": types,
                                   "all_types(tc,sv)": {WT[t]: [x["ReferenceTimeChunkIndex"], r6(x["ReferenceStatValue"])] for t, x in rows.items()},
                                   "types_with_own_values": [WT[t] for t, x in rows.items() if WT[t] not in types],
                                   "stat_bracket_fits": owner, "last_simulated_E_of_rarity": last_sim,
                                   "is_last_simulated_E": L == last_sim})
    exc["armor_reference_fields_shared_by_types"] = shared_ref
    exc["armor_hv_not_shared_at_same_T"] = {k: v for k, v in summary["reference"]["armor_heuristic_shared_by_timing"]["groups"].items()
                                           if len({x[2] for x in v}) > 1}
    summary["exceptions"] = exc
    with open(OUT / "summary.json", "w") as fh:
        json.dump(summary, fh, indent=1, ensure_ascii=False, default=str)
    # short console digest
    g = summary["generator"]["variants"]["full_rule"]
    print("generator full_rule:", g)
    print("pct:", {k: v for k, v in summary["generator"]["ReferenceStatPercent"].items() if k != "exact_.5_ties"})
    print("stat vs snapshot:", {k: v for k, v in summary["reference"]["stat_value_vs_snapshot"].items() if k != "rows"})
    print("dps:", {k: v for k, v in summary["runtime"]["dps_recompute"].items() if not k.startswith("inferred")})
    print("defense:", {k: v for k, v in summary["runtime"]["defense_recompute"].items() if not k.startswith("inferred")})
    print("shards capture:", summary["shards"]["capture_hypothesis"])
    print("wrote", OUT)


if __name__ == "__main__":
    main()
