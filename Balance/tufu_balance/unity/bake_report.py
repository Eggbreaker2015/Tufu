"""C# bake report, its oracle check and the guarded write-back (design 1.4.3, 3.5 X4, 7.3 C8; unit C8).

**Report.** A job with ``outputs.bake_report = true`` ends with the original ``SimulationConfig.BuildSpawnerBakeReport``
over every ``EnemySpawner`` of the loaded scenes (``SimulationBakeExport.cs``): day-mode spawners read the in-memory
``config.TimeChunkSnapshots`` (this run's untrimmed day list), minute-mode ones a transient ``SimulationData``
holding this run's ``Snapshots``, swapped in and back. ``job.bake.snapshots = "shipped"`` reads the shipped
Simulation Data instead (DP5's inputs, but in Play). ``bake_report.json`` has every report field as IEEE hex
(DP5's encoding), ``bake_report.csv`` the main fields as text (ingested as ``sim_bake_report``), manifest ``bake``
their sha256 and counts.

**X4.** :func:`compare_x4` runs ``model.bake`` on the same snapshots (``load_bake_context(con, simulation_data=...)``
with the run's exact ``Snapshots`` and day list from the report's ``inputs``) and compares NewHealth/NewDamage bit for
bit, the DP5 intermediates to 1e-12 relative and the three report-only fields (``DefenseFromSnapshot``,
``PlayerBlockChanceRaw``, ``PlayerBlockAmountRaw``) bit for bit. :func:`compare_dp5` compares a shipped report with the
edit-mode DP5 report. On the shipped and the 405-day data the ``double_tail`` and ``double_all`` interpolation
roundings give the same X4 result; the rounding is not identified here (C2 identified it).

**Write-back** (``ED/SimulationBakeTool.cs``, a fresh batch editor on the run worktree each time):

* :func:`plan` -- ``bake --plan``: ``bake_plan.json`` with old -> new Health/Damage per scene spawner and skip
  reasons (event, vip, boss_tower, dummy, hand_balance, non_finite, not_in_report, report_error, no_stat,
  duplicate_id). Nothing in the project may change (checked after the editor exits).
* :func:`apply` -- ``bake --apply --token sha256(plan)``: refused unless the caller is the explicit CLI (``caller="cli"``
  passed by ``__main__``; no default, no refresh, no CI), the token is the plan file's sha256 and the destination scene is tracked, clean, not an LFS pointer and
  byte-equal to the planned file and outside the run worktree; the editor checks its own guard again (never entered Play, edit mode, scene
  clean in git, token, caller). The written scene is copied to the destination, the run worktree restored.
* :func:`selftest` -- the plan and write-back logic on an unsaved ``EditorSceneManager.NewScene`` scene.
"""

from __future__ import annotations

import dataclasses
import hashlib
import json
import math
import os
import shutil
import sqlite3
import subprocess
import time
from pathlib import Path
from typing import Any, Mapping

from . import launcher, request as req, runner, schema

# The oracle (tufu_balance.model) is imported where it is used: importing tufu_balance.unity stays light.

REPORT_SCHEMA = "tufu.sim.bake-report/1"
PLAN_SCHEMA = "tufu.sim.bake-plan/1"
APPLY_SCHEMA = "tufu.sim.bake-apply/1"
SELFTEST_SCHEMA = "tufu.sim.bake-selftest/1"
JSON_FILE, CSV_FILE, PLAN_FILE = "bake_report.json", "bake_report.csv", "bake_plan.json"
APPLY_MANIFEST = "bake_apply_manifest.json"
TOOL = "Codebase.Simulation.EditorTools.SimulationBakeTool"
MAIN_SCENE = "Assets/Scenes/Main.unity"
SNAPSHOTS = ("run", "shipped")
REASONS = ("event", "vip", "boss_tower", "dummy", "hand_balance", "non_finite", "not_in_report", "report_error",
           "no_stat", "duplicate_id")
CALLER_CLI = "cli"
REFUSED_ENV = ("CI", "TUFU_REFRESH", "GITHUB_ACTIONS")  # same list as SimulationBakeTool.RefusedEnvironment
LFS_POINTER = b"version https://git-lfs.github.com/spec/"

CSV_COLUMNS = [
    ("index", "int"), ("id", "str"), ("name", "str"), ("location", "int"), ("is_days", "bool01"), ("registered", "bool01"),
    ("active", "bool01"), ("enabled", "bool01"), ("is_event", "bool01"), ("is_vip", "bool01"), ("is_dummy", "bool01"),
    ("hand_balance", "bool01"), ("count", "int"), ("optimum_time", "float?"), ("recalc_optimum_time", "float?"),
    ("snapshot_time_chunk_index", "int?"), ("snapshot_playtime", "float?"), ("snapshot_source", "str"),
    ("armor_type", "int?"), ("prev_health", "float?"), ("prev_damage", "float?"), ("player_dps", "float?"),
    ("defense", "float?"), ("new_health_raw", "float?"), ("new_health", "float?"), ("new_damage_raw", "float?"),
    ("new_damage", "float?"), ("error", "str"),
]
BAKE_KEYS = {"schema": "str", "snapshots": "str", "run_snapshots": "int", "run_time_chunk_snapshots": "int",
             "swap": "object", "files": "object", "spawners": "int", "registered": "int", "errors": "int",
             "non_finite": "int", "registered_not_in_scenes": "int", "error": "str"}

req.register_fields("job", {"bake": (dict, None)})
req.register_fields("bake", {"snapshots": (str,)})
schema.register_manifest_keys({"bake": "object"})
schema.register_csv_columns(CSV_FILE, CSV_COLUMNS, table="sim_bake_report")


class BakeToolError(ValueError):
    """A bake input is unusable, or the write-back is refused before Unity starts."""


def check_job(job: Mapping[str, Any], where: str = "job") -> None:
    """Python side of ``SimulationRunRequest.ValidateBakeFields`` (registered with ``request.validate``)."""
    on = (job.get("outputs") or {}).get("bake_report") is True
    bake = job.get("bake")
    if bake is not None:
        if not isinstance(bake, Mapping):
            raise req.RequestError(f"{where}.bake must be an object")
        for name in bake:
            if name != "snapshots":
                raise req.RequestError(f"{where}.bake.{name}: unknown field")
        if not on:
            raise req.RequestError(f"{where}.bake is given but outputs.bake_report is not true")
    if not on:
        return
    snapshots = (bake or {}).get("snapshots", "run")
    if snapshots not in SNAPSHOTS:
        raise req.RequestError(f"{where}.bake.snapshots must be one of {SNAPSHOTS}")
    if snapshots == "run" and job.get("mode", "free") == "empty":
        raise req.RequestError(f'{where}.bake.snapshots "run" needs a job that runs the simulation (mode empty)')


req.register_job_check(check_job)


def bake_outputs(job: dict, snapshots: str = "run") -> dict:
    """Turn the bake report on for ``job`` (in place) and return it."""
    job.setdefault("outputs", {})["bake_report"] = True
    if snapshots != "run":
        job["bake"] = {"snapshots": snapshots}
    check_job(job, job.get("job_id", "job"))
    return job


# ---------------------------------------------------------------- reading a job's report

def _sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_manifest(job_dir: str | Path) -> dict:
    manifest = json.loads((Path(job_dir) / "manifest.json").read_text(encoding="utf-8"))
    schema.validate_manifest(manifest)
    block = manifest.get("bake")
    if not isinstance(block, Mapping):
        raise BakeToolError(f"{job_dir}: the manifest has no bake block (outputs.bake_report was off)")
    for key, value in block.items():
        if key not in BAKE_KEYS:
            raise BakeToolError(f"{job_dir}: manifest bake: unknown key {key!r}")
    if block.get("schema") != REPORT_SCHEMA:
        raise BakeToolError(f"{job_dir}: manifest bake.schema must be {REPORT_SCHEMA}")
    return manifest


def read_report(job_dir: str | Path) -> dict:
    """``bake_report.json`` after checking both files against the manifest (sha256, bytes, CSV rows and schema)."""
    job_dir = Path(job_dir)
    manifest = read_manifest(job_dir)
    files = manifest["bake"].get("files") or {}
    for name in (JSON_FILE, CSV_FILE):
        raw = (job_dir / name).read_bytes()
        info = files.get(name) or {}
        if _sha(raw) != info.get("sha256") or len(raw) != info.get("bytes"):
            raise BakeToolError(f"{job_dir / name}: sha256/bytes differ from the manifest")
        if name == CSV_FILE and len(schema.read_csv(CSV_FILE, raw.decode("utf-8"))) != info.get("rows"):
            raise BakeToolError(f"{job_dir / name}: row count differs from the manifest")
    report = json.loads((job_dir / JSON_FILE).read_text(encoding="utf-8"))
    if report.get("schema") != REPORT_SCHEMA:
        raise BakeToolError(f"{job_dir / JSON_FILE}: schema must be {REPORT_SCHEMA}")
    if report.get("snapshots") != manifest["bake"].get("snapshots"):
        raise BakeToolError(f"{job_dir / JSON_FILE}: snapshots differ from the manifest")
    return report


def _hex_snapshot(d: Mapping[str, Any], index: int):
    from . import probes as P
    from ..model.f32 import f32
    from ..model.snapshots import SimulationItem, SimulationSnapshot, SimulationStat
    return SimulationSnapshot(
        playtime=f32(P.floats(d["p"])[0]), total_playtime=f32(P.floats(d["tp"])[0]), location=int(d["loc"]),
        time_chunk_index=int(d["tci"]),
        stats=[SimulationStat(key=k, value=v, stats_from_features_flat=sff, percent_multiplier=pm, total_flat_value=tf,
                              total_value=tv, current_gpm=gpm) for k, v, sff, pm, tf, tv, gpm in P._decode_stats(d["stats"])],
        items=[SimulationItem(str(k), int(lv), int(c)) for k, lv, c in d["items"]],
        dps_data=P._decode_pairs(d["dps"]), defense_data=P._decode_pairs(d["def"]), source_index=index)


def run_simulation_data(report: Mapping[str, Any]):
    """What the C# bake read for ``snapshots = "run"``: this run's ``Snapshots`` and untrimmed day list, from the
    report's ``inputs`` (IEEE hex, so independent of the text round trip of the exported snapshot files)."""
    from ..model.snapshots import SimulationData
    inputs = report.get("inputs")
    if not inputs:
        raise BakeToolError("the report has no inputs (snapshots shipped?)")
    return SimulationData(snapshots=[_hex_snapshot(s, i) for i, s in enumerate(inputs["Snapshots"])],
                          time_chunk_snapshots=[_hex_snapshot(s, i) for i, s in enumerate(inputs["TimeChunkSnapshots"])])


def export_text_check(job_dir: str | Path, report: Mapping[str, Any]) -> dict:
    """The report's exact inputs against C1's exported text (``simulation_data.json`` Snapshots and
    ``timechunk_full.json``): numbers whose text does not parse back to the C# bits, per field, with the
    difference in ulps (a diagnostic of the exporter, not part of X4)."""
    import struct
    job_dir = Path(job_dir)
    sd = run_simulation_data(report)
    files = {"Snapshots": ("simulation_data.json", sd.snapshots), "TimeChunkSnapshots": ("timechunk_full.json", sd.time_chunk_snapshots)}
    res: dict[str, Any] = {"values": 0, "differ": 0, "by_field": {}, "ulps": {}, "examples": []}

    def bits(v: float) -> int:
        return struct.unpack("<q", struct.pack("<d", v))[0]

    for name, (file, snaps) in files.items():
        text = json.loads((job_dir / file).read_text(encoding="utf-8"))[name]
        if len(text) != len(snaps):
            raise BakeToolError(f"{file}: {len(text)} {name}, the bake read {len(snaps)}")
        for i, (t, s) in enumerate(zip(text, snaps)):
            pairs = []
            for ts, st in zip(t["Stats"], s.stats):
                for field, attr in (("Value", "value"), ("StatsFromFeaturesFlat", "stats_from_features_flat"),
                                    ("PercentMultiplier", "percent_multiplier"), ("TotalFlatValue", "total_flat_value"),
                                    ("TotalValue", "total_value"), ("CurrentGpm", "current_gpm")):
                    pairs.append((f"Stats.{field}", float(ts[field]), getattr(st, attr)))
            for key, lst in (("DpsData", s.dps_data), ("DefenseData", s.defense_data)):
                pairs += [(key, float(tv["Value"]), v) for tv, (_, v) in zip(t[key], lst)]
            for field, a, b in pairs:
                res["values"] += 1
                if a == b or (math.isnan(a) and math.isnan(b)):
                    continue
                res["differ"] += 1
                res["by_field"][field] = res["by_field"].get(field, 0) + 1
                u = str(bits(a) - bits(b))
                res["ulps"][u] = res["ulps"].get(u, 0) + 1
                if len(res["examples"]) < 10:
                    res["examples"].append({"list": name, "index": i, "field": field, "text": repr(a), "csharp": repr(b)})
    return res


def bake_context(job_dir: str | Path, con: sqlite3.Connection, report: Mapping[str, Any] | None = None,
                 rounding=None):
    """The oracle's context for a job's report: the run's snapshots (``run``) or the database's (``shipped``)."""
    from ..model import data as MD
    report = report if report is not None else read_report(job_dir)
    if report["snapshots"] == "shipped":
        return MD.load_bake_context(con, rounding=rounding)
    sd = run_simulation_data(report)
    if len(sd.snapshots) != report["run_snapshots"] or len(sd.time_chunk_snapshots) != report["run_time_chunk_snapshots"]:
        raise BakeToolError(f"{job_dir}: exported snapshot counts differ from the ones the bake read")
    ctx = MD.load_bake_context(con, simulation_data=sd, rounding=rounding)
    ctx.config_time_chunk_snapshots = sd.time_chunk_snapshots  # GetSnapshotForBake: config.TimeChunkSnapshots
    return ctx


# ---------------------------------------------------------------- X4 and DP5

def compare_x4(job_dir: str | Path, con: sqlite3.Connection, rounding=None, report: Mapping[str, Any] | None = None,
               ctx=None) -> dict:
    """NewHealth/NewDamage bit for bit, intermediates to 1e-12 relative (NewHealthRaw differences must be the
    editor's arithmetic, ``probes.mono_new_health_raw``), the snapshot each spawner read, errors on both sides."""
    from . import probes as P
    from .spawner_map import load_identity_map
    from ..model import bake as B
    from ..model import data as MD
    from ..model.enums import StatType
    from ..model.f32 import f32

    report = report if report is not None else read_report(job_dir)
    ctx = ctx or bake_context(job_dir, con, report, rounding)
    identity = load_identity_map(con)
    armors = MD.load_armor_configs(con)
    rows = {(str(r["scene"]), int(r["file_id"])): r for r in MD.load_spawner_rows(con)}
    res: dict[str, Any] = {"snapshots": report["snapshots"], "spawners": len(report["spawners"]), "unknown": [],
                           "registered": 0, "unregistered": [], "new_bits_equal": 0, "intermediate_values": 0,
                           "intermediate_bits_equal": 0, "intermediate_rel_ok": 0, "new_health_raw_explained_by_mono": 0,
                           "new_health_raw_rel_diff_max": 0.0, "snapshot_source_equal": 0, "errors_both": [],
                           "mismatches": [], "non_finite": [], "minute_n": 0, "day_n": 0, "hp_exact_minute": [],
                           "interpolated_day": 0, "report_only_values": 0, "report_only_bits_equal": 0}
    fingerprint = hashlib.sha256()
    seen = set()
    for c in report["spawners"]:
        e = identity.by_runtime_id.get(c["id"])
        if e is None:
            res["unknown"].append(c["id"])
            continue
        sid = e.spawner_id
        seen.add(sid)
        tag = f"{sid[0]}:{sid[1]}"
        if bool(c["registered"]) != e.registers:
            res["mismatches"].append({"id": tag, "registered": c["registered"], "predicted": e.registers})
        if e.registers:
            res["registered"] += 1
        else:
            res["unregistered"].append(tag)
        sp = MD.spawner_input_from_row(rows[sid], armors)
        try:
            o = B.bake_spawner(ctx, sp)
        except B.BakeError as err:
            o = err
        if "error" in c or isinstance(o, B.BakeError):
            if "error" in c and isinstance(o, B.BakeError):
                res["errors_both"].append(tag)
            else:
                res["mismatches"].append({"id": tag, "csharp": c.get("error"), "oracle": str(o) if isinstance(o, B.BakeError) else "ok"})
            continue
        rep = c["report"]
        ch, cd = P.doubles(rep["NewHealth"])[0], P.doubles(rep["NewDamage"])[0]
        fingerprint.update(f"{tag}|{rep['NewHealth']}|{rep['NewDamage']}\n".encode())
        new_ok = P._same(ch, o.new_health) and P._same(cd, o.new_damage)
        res["new_bits_equal"] += new_ok
        if not (math.isfinite(ch) and math.isfinite(cd)):
            res["non_finite"].append(tag)
        bad = []
        for attr, (field, kind) in P.DP5_FIELDS.items():
            cv = (P.floats if kind == "f" else P.doubles)(rep[field])[0]
            ov = float(getattr(o, attr))
            res["intermediate_values"] += 1
            res["intermediate_bits_equal"] += P._same(cv, ov)
            ok = P._rel_ok(cv, ov)
            res["intermediate_rel_ok"] += ok
            if not ok:
                if field == "NewHealthRaw" and P._same(cv, P.mono_new_health_raw(o, sp)):
                    res["new_health_raw_explained_by_mono"] += 1
                    res["new_health_raw_rel_diff_max"] = max(res["new_health_raw_rel_diff_max"], abs(cv - ov) / abs(cv))
                else:
                    bad.append(field)
        for attr, field in P.DP5_INTS.items():
            res["intermediate_values"] += 1
            ok = int(getattr(o, attr)) == int(rep[field])
            res["intermediate_bits_equal"] += ok
            res["intermediate_rel_ok"] += ok
            if not ok:
                bad.append(field)
        for field, ov in (("PlayerAbilityDpsMultiplier", o.ability_dps + 1.0), ("PlayerAblityEhpMultiplier", o.ability_ehp + 1.0)):
            cv = P.doubles(rep[field])[0]
            res["intermediate_values"] += 1
            res["intermediate_bits_equal"] += P._same(cv, ov)
            ok = P._rel_ok(cv, ov)
            res["intermediate_rel_ok"] += ok
            if not ok:
                bad.append(field)
        # Fields DP5 does not carry: the defence copy and the two raw block reads of the snapshot the bake read.
        snap = ctx.snapshot_for_bake(f32(o.recalc_optimum_time), sp.is_days)
        for field, ov in (("DefenseFromSnapshot", o.defense), ("PlayerBlockChanceRaw", snap.total(StatType.BlockChance)),
                          ("PlayerBlockAmountRaw", snap.total(StatType.BlockAmount))):
            res["report_only_values"] += 1
            ok = P._same(P.doubles(rep[field])[0], float(ov))
            res["report_only_bits_equal"] += ok
            if not ok:
                bad.append(field)
        o_src = f"{'t' if sp.is_days else 's'}:{o.snapshot_source_index}" if o.snapshot_source_index is not None else None
        src_ok = rep.get("SnapshotSource") == o_src
        res["snapshot_source_equal"] += src_ok
        if sp.is_days:
            res["day_n"] += 1
            res["interpolated_day"] += o_src is None
        else:
            res["minute_n"] += 1
            if sp.health_base == ch:
                res["hp_exact_minute"].append(tag)
        if not new_ok or bad or not src_ok:
            res["mismatches"].append({"id": tag, "new_ok": new_ok, "fields": bad, "csharp_src": rep.get("SnapshotSource"),
                                      "oracle_src": o_src})
    res["missing"] = sorted(f"{s[0]}:{s[1]}" for s in rows if s not in seen)
    for key in ("unregistered", "errors_both", "non_finite", "hp_exact_minute", "unknown"):
        res[key].sort()
    res["new_values_sha256"] = fingerprint.hexdigest()
    return res


def compare_dp5(report: Mapping[str, Any], dp5: Mapping[str, Any]) -> dict:
    """Every field of a shipped report against the edit-mode DP5 report of the same spawner (``probes_edit.json``
    ``dp5``): equal hex text means equal bits."""
    dp5_rows = {r["id"]: r for r in dp5["spawners"]}
    res: dict[str, Any] = {"spawners": len(report["spawners"]), "dp5_spawners": len(dp5_rows), "identical": 0,
                           "fields_compared": 0, "differ": [], "only_report": [], "only_dp5": []}
    seen = set()
    for c in report["spawners"]:
        d = dp5_rows.get(c["id"])
        if d is None:
            res["only_report"].append(c["id"])
            continue
        seen.add(c["id"])
        if ("error" in c) != ("error" in d):
            res["differ"].append({"id": c["id"], "fields": ["error"]})
            continue
        if "error" in c:
            same = c["error"] == d["error"]
            res["identical"] += same
            if not same:
                res["differ"].append({"id": c["id"], "fields": ["error"]})
            continue
        fields = [k for k in d["report"] if c["report"].get(k) != d["report"][k]]
        res["fields_compared"] += len(d["report"])
        res["identical"] += not fields
        if fields:
            res["differ"].append({"id": c["id"], "fields": fields})
    res["only_dp5"] = sorted(set(dp5_rows) - seen)
    return res


# ---------------------------------------------------------------- plan checks (independent of C#)

def read_plan(path: str | Path) -> tuple[dict, str]:
    """The plan and its token (sha256 of the file bytes)."""
    raw = Path(path).read_bytes()
    plan = json.loads(raw.decode("utf-8"))
    if plan.get("schema") != PLAN_SCHEMA:
        raise BakeToolError(f"{path}: schema must be {PLAN_SCHEMA}")
    return plan, _sha(raw)


def expected_reasons(row: Mapping[str, Any], new_health: float | None, new_damage: float | None, *, in_report: bool,
                     report_error: bool, duplicate: bool) -> list[str]:
    """The skip reasons of a ``spawners`` row of the database (the scene on disk) for the given report values."""
    reasons = []
    if int(row["IsEvent"] or 0):
        reasons.append("event")
    if int(row["IsVip"] or 0):
        reasons.append("vip")
    if str(row["go_name"]).startswith("BossTower"):
        reasons.append("boss_tower")
    if int(row["IsDummy"] or 0):
        reasons.append("dummy")
    if int(row["HandBalance"] or 0):
        reasons.append("hand_balance")
    if not in_report:
        reasons.append("not_in_report")
    elif report_error:
        reasons.append("report_error")
    elif not (math.isfinite(new_health) and math.isfinite(new_damage)):
        reasons.append("non_finite")
    if row["stat.Health"] in (None, "") or row["stat.Damage"] in (None, ""):
        reasons.append("no_stat")
    if duplicate:
        reasons.append("duplicate_id")
    return [r for r in REASONS if r in reasons]


def check_plan(plan: Mapping[str, Any], report: Mapping[str, Any], con: sqlite3.Connection) -> dict:
    """Recompute every plan row from the database's scene values and the report: old values, new values,
    actions and skip reasons. Returns counts and the rows that differ."""
    from . import probes as P
    from .spawner_map import load_identity_map
    from ..model import data as MD

    identity = load_identity_map(con)
    rows = {(str(r["scene"]), int(r["file_id"])): r for r in MD.load_spawner_rows(con)}
    by_id = {c["id"]: c for c in report["spawners"]}
    counts: dict[str, Any] = {"write": 0, "unchanged": 0, "skip": 0}
    by_reason = {r: 0 for r in REASONS}
    differ, unknown = [], []
    ids = [r["id"] for r in plan["rows"]]
    for prow in plan["rows"]:
        e = identity.by_runtime_id.get(prow["id"])
        if e is None:
            unknown.append(prow["id"])
            continue
        db = rows[e.spawner_id]
        c = by_id.get(prow["id"])
        nh = nd = None
        if c is not None and "report" in c:
            nh, nd = P.doubles(c["report"]["NewHealth"])[0], P.doubles(c["report"]["NewDamage"])[0]
        reasons = expected_reasons(db, nh, nd, in_report=c is not None, report_error=c is not None and "report" not in c,
                                   duplicate=ids.count(prow["id"]) > 1)
        old_h = None if db["stat.Health"] in (None, "") else float(db["stat.Health"])
        old_d = None if db["stat.Damage"] in (None, "") else float(db["stat.Damage"])
        if reasons:
            action = "skip"
        elif P._same(old_h, nh) and P._same(old_d, nd):
            action = "unchanged"
        else:
            action = "write"
        counts[action] += 1
        for r in reasons:
            by_reason[r] += 1
        bad = []
        if prow["action"] != action:
            bad.append("action")
        if prow["reasons"] != reasons:
            bad.append("reasons")
        for name, want in (("old_health", old_h), ("old_damage", old_d), ("new_health", nh), ("new_damage", nd)):
            got = prow.get(f"{name}_bits")
            if (got is None) != (want is None) or (want is not None and not P._same(P.doubles(got)[0], want)):
                bad.append(name)
        if bad:
            differ.append({"id": prow["id"], "fields": bad})
    counts["by_reason"] = by_reason
    return {"rows": len(plan["rows"]), "counts": counts, "differ": differ, "unknown": unknown,
            "counts_equal": {k: plan["counts"].get(k) == counts[k] for k in ("write", "unchanged", "skip", "by_reason")}}


# ---------------------------------------------------------------- editor entries

@dataclasses.dataclass
class EntryResult:
    exit_code: int
    out_dir: Path
    report: dict           # launcher side (bake_launch.json)
    tool: dict | None      # the C# entry's own report
    plan: Path | None = None
    token: str | None = None


def _entry(method: str, args: list[str], out_dir: Path, report_file: str, *, rev: str, opts: launcher.Options,
           run_tree: Path, worktree: Path, paths: launcher.ProtectedPaths | None, timeout_s: float,
           allow_scene_change: bool = False, after=None, log=print) -> EntryResult:
    """Run ``SimulationBakeTool.<method>`` in a fresh batch editor on the run worktree at ``rev``; afterwards the
    run worktree must be clean and no protected file changed (``allow_scene_change``: Main.unity may, and
    ``after(project)`` runs before it is restored)."""
    project = runner.project_dir(run_tree)
    launcher._refuse_main(project, opts)
    if not (project / "Library").is_dir():
        raise launcher.LauncherError(f"{project} has no Library; the bake tool runs only in the run worktree")
    lock = launcher.UnityLock(run_tree)
    lock.acquire(f"bake-{method.lower()}", opts.lock_wait_s)
    try:
        others = launcher.unity_processes(project)
        if others:
            raise launcher.LauncherError(f"Unity already has {project} open: {others}")
        runner.require_clean(run_tree)
        code_rev = runner.checkout(run_tree, rev)
        dll_status = launcher.check_dlls(project)
        out_dir.mkdir(parents=True, exist_ok=True)
        return _launch(method, args, out_dir, report_file, project=project, run_tree=run_tree, code_rev=code_rev,
                       opts=opts, paths=paths, timeout_s=timeout_s, allow_scene_change=allow_scene_change, after=after,
                       material={"dll_status": dll_status, "worktree": str(worktree)}, log=log)
    finally:
        lock.release()


def _launch(method, args, out_dir: Path, report_file: str, *, project: Path, run_tree: Path, code_rev: str, opts,
            paths, timeout_s: float, allow_scene_change: bool, after, material: dict, log) -> EntryResult:
    paths = paths or launcher.ProtectedPaths.for_project(project)
    scene = project / MAIN_SCENE
    scene_git = (runner.PROJECT_SUBDIR / MAIN_SCENE).as_posix()
    report: dict[str, Any] = {"schema": "tufu.sim.bake-launcher/1", "method": method, "code_rev": code_rev,
                              "started": launcher.now_iso(), **material, "scene_sha256_before": runner.sha256_file(scene)}
    main_before = launcher.main_checkout_editors()
    before = launcher.snapshot(paths, project)
    backup = out_dir / "_real_backup"
    launcher.backup_real(paths, backup)
    log_file = out_dir / "unity.log"
    (out_dir / report_file).unlink(missing_ok=True)
    cmd = [str(opts.unity), "-batchmode", "-projectPath", str(project), *launcher.FIXED_ARGS, "-logFile", str(log_file),
           "-executeMethod", f"{TOOL}.{method}", *args]
    if opts.nographics:
        cmd.append("-nographics")
    report["command"] = cmd
    log(f"unity bake {method.lower()} at {code_rev[:10]}")
    t0 = time.monotonic()
    proc = subprocess.Popen(cmd, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                            start_new_session=True)
    others_seen: list = []
    reason = None
    while proc.poll() is None:
        for pid, cmdline in launcher.unity_processes(project):
            if pid != proc.pid and "AssetImportWorker" not in cmdline and pid not in [p for p, _ in others_seen]:
                others_seen.append((pid, cmdline))
        if time.monotonic() - t0 > timeout_s:
            reason = f"timeout {timeout_s:g} s"
            launcher._kill(proc)
            break
        time.sleep(0.5)
    unity_exit = launcher.EXIT_TIMEOUT if reason else proc.returncode
    report.update(unity_exit=unity_exit, reason=reason, process_wall_s=time.monotonic() - t0,
                  workers_left=launcher.wait_for_project_free(project, opts.worker_exit_wait_s))
    try:
        tool = json.loads((out_dir / report_file).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        tool = None
    report["tool"] = tool
    report["scene_sha256_after"] = runner.sha256_file(scene)

    after_snap = launcher.snapshot(paths, project)
    problems = launcher.diff_snapshots(before, after_snap)
    analytics = [p for p in problems if launcher._is_editor_analytics(p)]
    problems = [p for p in problems if not launcher._is_editor_analytics(p)]
    report["editor_analytics_leftovers"] = analytics
    status = [line for line in runner.porcelain(run_tree).splitlines() if line.strip()]
    scene_changed = [line for line in status if line[3:] == scene_git]
    unexpected = [line for line in status if line not in scene_changed or not allow_scene_change]
    if unexpected:
        problems.append("run worktree changed: " + " | ".join(unexpected[:10]))
    exit_code = unity_exit if unity_exit in launcher.EXIT_NAMES else launcher.EXIT_EXCEPTION
    if exit_code == launcher.EXIT_OK and not (tool and tool.get("ok") is True and tool.get("exit_code") == 0):
        problems.append(f"{report_file}: missing or not ok")
        exit_code = launcher.EXIT_EXCEPTION
    if after is not None and exit_code == launcher.EXIT_OK and not unexpected:
        try:
            report["after"] = after(project)
        except (OSError, ValueError) as e:
            problems.append(f"after the editor: {e}")
    if status:
        expected = {scene_git} if allow_scene_change else set()
        report["run_worktree_changes_kept"] = _restore_run_tree(run_tree, out_dir / "run_worktree_changes", expected)
    report["run_worktree_restored"] = not runner.porcelain(run_tree).strip()
    if not report["run_worktree_restored"]:
        problems.append("run worktree could not be restored to HEAD")

    # The tool never enters Play: a change of the real save while the user's editor on the main checkout ran
    # for the whole call is that editor's (Runbook 13, 17).
    real_problems = [p for p in problems if p.startswith("real ")]
    main_editors = sorted(main_before & launcher.main_checkout_editors())
    report["main_checkout_editors"] = main_editors
    foreign = others_seen or launcher.unity_processes(runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR)
    restored = []
    if real_problems:
        if (main_editors and unity_exit == 0 and not others_seen and len(real_problems) == len(problems)
                and tool and tool.get("entered_play", False) is False):
            report["real_changed_by_main_editor"] = real_problems
            problems = []
        elif not foreign:
            restored = launcher.restore_real(paths, backup, save=any("save file" in p for p in real_problems),
                                             prefs=any("PlayerPrefs" in p for p in real_problems))
    if analytics and not foreign:
        restored += launcher.restore_real(paths, backup, save=True, prefs=False)
    report["restored"] = restored
    if any(not p.endswith("missing or not ok") for p in problems):
        exit_code = launcher.EXIT_GUARD
    if not (real_problems or analytics) or restored:
        shutil.rmtree(backup, ignore_errors=True)
    report.update(guard_problems=problems, others_seen=others_seen, exit_code=exit_code, finished=launcher.now_iso())
    (out_dir / "bake_launch.json").write_text(json.dumps(report, indent=1, ensure_ascii=False, default=str) + "\n",
                                              encoding="utf-8")
    log(f"unity bake {method.lower()}: exit {exit_code} ({launcher.EXIT_NAMES.get(exit_code, '?')})"
        + (f", {'; '.join(problems[:3])}" if problems else "") + f" in {report['process_wall_s']:.1f} s")
    return EntryResult(exit_code, out_dir, report, tool)


def _restore_run_tree(run_tree: Path, keep_dir: Path, expected: set[str]) -> list[str]:
    """Put the run worktree back to HEAD after the editor (the shared run worktree must stay usable): every changed
    or new file other than ``expected`` is copied to ``keep_dir`` first (evidence; such a change is already a guard
    problem), then the index is reset, changed tracked files are checked out and untracked files removed."""
    entries = runner.git(run_tree, "status", "--porcelain", "-uall", "-z").split("\0")
    kept, i = [], 0
    while i < len(entries):
        entry, i = entries[i], i + 1
        if not entry:
            continue
        paths = [entry[3:]]
        if "R" in entry[:2] or "C" in entry[:2]:  # -z: the source path follows as its own entry
            paths.append(entries[i])
            i += 1
        for path in paths:
            src = run_tree / path
            if path not in expected and src.is_file():
                (keep_dir / path).parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(src, keep_dir / path)
                kept.append(path)
    runner.git(run_tree, "reset", "--quiet")
    tracked = [p for p in runner.git(run_tree, "diff", "--name-only", "-z", "HEAD").split("\0") if p]
    if tracked:
        runner.git(run_tree, "checkout", "--quiet", "HEAD", "--", *tracked)
    for p in runner.git(run_tree, "ls-files", "--others", "--exclude-standard", "-z").split("\0"):
        if p:
            (run_tree / p).unlink(missing_ok=True)
    return sorted(set(kept))


def _scene_blob(tree: Path, rev: str) -> str:
    return runner.git(tree, "rev-parse", f"{rev}:{(runner.PROJECT_SUBDIR / MAIN_SCENE).as_posix()}").strip()


def _cached(out_dir: Path, report_file: str, rev: str) -> EntryResult | None:
    try:
        report = json.loads((out_dir / "bake_launch.json").read_text(encoding="utf-8"))
        tool = json.loads((out_dir / report_file).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None
    if report.get("exit_code") != 0 or report.get("code_rev") != rev:
        return None
    return EntryResult(0, out_dir, report, tool)


def plan(job_dir: str | Path, *, rev: str | None = None, out_dir: str | Path | None = None,
         opts: launcher.Options | None = None, worktree: Path | None = None, run_tree: Path | None = None,
         paths: launcher.ProtectedPaths | None = None, out_root: Path | None = None, timeout_s: float = 900.0,
         reuse: bool = False, log=print) -> EntryResult:
    """``bake --plan``: the plan for a job's report, made on the commit the job ran (``rev`` may name another
    commit only when Main.unity is the same blob there). Touches no asset. ``reuse``: return an earlier
    successful plan for the same report and commit instead of starting Unity."""
    job_dir = Path(job_dir).resolve()
    manifest = read_manifest(job_dir)
    if manifest.get("exit_code") != 0:
        raise BakeToolError(f"{job_dir}: the job did not succeed (exit {manifest.get('exit_code')})")
    report = read_report(job_dir)
    report_sha = _sha((job_dir / JSON_FILE).read_bytes())
    worktree = Path(worktree or runner.WORKTREE)
    run_tree = Path(run_tree or runner.run_worktree())
    job_rev = manifest.get("code_rev")
    rev = runner.resolve(worktree, rev or job_rev or "HEAD")
    if job_rev and rev != runner.resolve(worktree, job_rev) and _scene_blob(worktree, rev) != _scene_blob(worktree, job_rev):
        raise BakeToolError(f"Main.unity at {rev[:10]} differs from the job's commit {job_rev[:10]}; plan on the job's commit")
    out_dir = Path(out_dir) if out_dir else Path(out_root or launcher.OUT_ROOT) / "bake" / f"plan-{report_sha[:16]}-{rev[:12]}"
    plan_path = out_dir / PLAN_FILE
    cached = _cached(out_dir, "bake_plan_report.json", rev) if reuse else None
    if cached is not None and plan_path.exists():
        cached.plan, cached.token = plan_path, read_plan(plan_path)[1]
        log(f"cached: {out_dir}")
        return cached
    plan_path.unlink(missing_ok=True)
    result = _entry("Plan", ["-tufuSimBakeReport", str(job_dir / JSON_FILE), "-tufuSimBakePlan", str(plan_path),
                             "-tufuSimBakeOut", str(out_dir)], out_dir, "bake_plan_report.json", rev=rev,
                    opts=opts or launcher.Options(), run_tree=run_tree, worktree=worktree, paths=paths,
                    timeout_s=timeout_s, log=log)
    if result.exit_code == 0:
        p, token = read_plan(plan_path)
        if p.get("report", {}).get("sha256") != report_sha:
            raise BakeToolError(f"{plan_path}: made from another report")
        result.plan, result.token = plan_path, token
    return result


def selftest(*, rev: str | None = None, out_dir: str | Path | None = None, opts: launcher.Options | None = None,
             worktree: Path | None = None, run_tree: Path | None = None, paths: launcher.ProtectedPaths | None = None,
             out_root: Path | None = None, timeout_s: float = 900.0, reuse: bool = False, log=print) -> EntryResult:
    """The write-back logic on an unsaved temporary scene (``SimulationBakeTool.SelfTest``)."""
    worktree = Path(worktree or runner.WORKTREE)
    rev = runner.resolve(worktree, rev or "HEAD")
    out_dir = Path(out_dir) if out_dir else Path(out_root or launcher.OUT_ROOT) / "bake" / f"selftest-{rev[:12]}"
    cached = _cached(out_dir, "bake_selftest.json", rev) if reuse else None
    if cached is not None:
        log(f"cached: {out_dir}")
        return cached
    return _entry("SelfTest", ["-tufuSimBakeOut", str(out_dir)], out_dir, "bake_selftest.json", rev=rev,
                  opts=opts or launcher.Options(), run_tree=Path(run_tree or runner.run_worktree()), worktree=worktree,
                  paths=paths, timeout_s=timeout_s, log=log)


def apply_refusal(plan_path: str | Path, token: str | None, *, dest: Path, caller: str | None,
                  environ: Mapping[str, str] | None = None, run_tree: Path | None = None) -> str | None:
    """Why ``bake --apply`` must not start (None: it may). Checked before Unity starts; the editor checks the
    process, the caller, the token and the scene again."""
    environ = os.environ if environ is None else environ
    for name in REFUSED_ENV:
        if environ.get(name):
            return f"refused: {name} is set (refresh or CI call)"
    if caller != CALLER_CLI:
        return f"refused: caller {caller!r}; only an explicit bake --apply writes"
    raw = Path(plan_path).read_bytes()
    if not token or token.strip() != _sha(raw):
        return f"refused: token {token!r} is not the plan's sha256 {_sha(raw)}"
    plan_doc = json.loads(raw.decode("utf-8"))
    if plan_doc.get("schema") != PLAN_SCHEMA or plan_doc.get("scene") != MAIN_SCENE or not plan_doc.get("code_rev"):
        return "refused: not a Main.unity plan with a code_rev"
    if plan_doc["counts"].get("write", 0) == 0:
        return "refused: the plan writes nothing"
    dest = Path(dest)
    if not dest.is_file():
        return f"refused: {dest} does not exist"
    if dest.read_bytes()[:len(LFS_POINTER)] == LFS_POINTER:
        return f"refused: {dest} is an LFS pointer (smudge it first)"
    if run_tree is not None and dest.resolve().is_relative_to(Path(run_tree).resolve()):
        return f"refused: {dest} is inside the run worktree, which is restored after the write-back"
    top = Path(runner.git(dest.parent, "rev-parse", "--show-toplevel").strip())
    rel = dest.resolve().relative_to(top.resolve()).as_posix()
    if runner.git(top, "status", "--porcelain", "-uall", "--", rel).strip():
        return f"refused: {dest} is not clean in git"
    if subprocess.run(["git", "-C", str(top), "ls-files", "--error-unmatch", "--", rel], capture_output=True).returncode:
        return f"refused: {dest} is not tracked in git"
    if runner.sha256_file(dest) != plan_doc.get("scene_sha256"):
        return f"refused: {dest} is not the planned scene file (sha256 differs)"
    return None


def apply(plan_path: str | Path, token: str | None, *, dest: str | Path | None = None, caller: str | None = None,
          out_dir: str | Path | None = None, opts: launcher.Options | None = None, worktree: Path | None = None,
          run_tree: Path | None = None, paths: launcher.ProtectedPaths | None = None, out_root: Path | None = None,
          timeout_s: float = 900.0, log=print) -> EntryResult:
    """``bake --apply --token T``: write the plan into Main.unity in a fresh editor on the run worktree at the plan's
    commit, then copy the written scene to ``dest`` (default: this worktree's Main.unity). ``caller`` has no
    default: only ``__main__``'s ``bake --apply`` passes ``"cli"``, and it is forwarded to the editor's own check."""
    worktree = Path(worktree or runner.WORKTREE)
    dest = Path(dest) if dest else runner.project_dir(worktree) / MAIN_SCENE
    run_tree = Path(run_tree or runner.run_worktree())
    refusal = apply_refusal(plan_path, token, dest=dest, caller=caller, run_tree=run_tree)
    if refusal:
        raise BakeToolError(refusal)
    plan_doc, token = read_plan(plan_path)
    out_dir = Path(out_dir) if out_dir else Path(out_root or launcher.OUT_ROOT) / "bake" / f"apply-{token[:16]}"
    copied: dict[str, Any] = {}

    def copy_out(project: Path) -> dict:
        written = project / MAIN_SCENE
        manifest = json.loads((out_dir / APPLY_MANIFEST).read_text(encoding="utf-8"))
        if runner.sha256_file(written) != manifest.get("scene_sha256_after"):
            raise BakeToolError("the written scene is not the one the editor reported")
        shutil.copyfile(written, dest)
        copied.update(dest=str(dest), sha256=runner.sha256_file(dest))
        return copied

    result = _entry("Apply", ["-tufuSimBakePlan", str(Path(plan_path).resolve()), "-tufuSimBakeToken", token,
                              "-tufuSimBakeOut", str(out_dir), "-tufuSimBakeCaller", str(caller)],
                    out_dir, APPLY_MANIFEST, rev=plan_doc["code_rev"], opts=opts or launcher.Options(), run_tree=run_tree,
                    worktree=worktree, paths=paths, timeout_s=timeout_s, allow_scene_change=True, after=copy_out, log=log)
    result.plan, result.token = Path(plan_path), token
    return result


def find_plan(job_dir: str | Path, out_root: Path | None = None) -> Path:
    """The single plan made from a job's report under ``out/sim/bake/``."""
    report_sha = _sha((Path(job_dir) / JSON_FILE).read_bytes())
    found = sorted((Path(out_root or launcher.OUT_ROOT) / "bake").glob(f"plan-{report_sha[:16]}-*/{PLAN_FILE}"))
    if len(found) != 1:
        raise BakeToolError(f"{len(found)} plans for {job_dir}; pass --plan-file")
    return found[0]
