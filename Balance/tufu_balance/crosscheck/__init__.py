"""Oracle cross-checks of a simulation run (design 3.5 X1-X7, 3.4 DP8; unit P11).

After a C# run is exported (C1) the Python oracle recomputes what the engine wrote and compares item by item:

* X1 :mod:`.composition` -- snapshot stat columns from Value and K2/K3;
* X2 :mod:`.dps` -- DpsData/DefenseData from the exported items (``Evolved``), slots and stats;
* X3, X7, DP8 :mod:`.rpm` -- the day list's CurrentGpm along the run, the day-change order, and the spawnerData of
  the shipped day-snapshot states (``crosscheck_dp8`` output of a replay/checkpoint job);
* X4 :mod:`.bake` -- the C# bake report (C8) against ``model.bake`` on the run's snapshots;
* X5 :mod:`.eligibility` -- the daily eligible sets against ``model.simcalc``;
* X6 :mod:`.invariants` -- trajectory invariants.

Each check returns a :class:`CheckResult` (status ``pass``, ``fail`` or ``vacuous``; parts that had nothing to
check are listed in ``vacuous`` with the reason, never skipped silently) and writes one CSV row per compared
item to ``<job>/crosscheck/<id>.csv``. :func:`run_checks` runs them all; ``python -m tufu_balance.crosscheck``
does it from the command line.
"""

from __future__ import annotations

import copy
import csv
import json
import math
import struct
from dataclasses import dataclass, field, replace
from pathlib import Path
from typing import Any, Iterable, Mapping, Sequence

from ..model.enums import StatType
from ..model.f32 import f32
from ..model.snapshots import DIRECT_INTERPOLATION_STATS, MAIN_INTERPOLATION_STATS, SimulationItem, SimulationSnapshot, SimulationStat
from ..unity import schema, simio, trace as tr

OUTPUT_DP8 = "crosscheck_dp8"  # C# SimulationCrosscheck.OutputName
schema.register_output(OUTPUT_DP8, "P11")

CSV_DIR = "crosscheck"
REL_TOL = 1e-12
STATUSES = ("pass", "fail", "vacuous")


# ---------------------------------------------------------------- numbers

def bits(x: float) -> int:
    return struct.unpack("<q", struct.pack("<d", float(x)))[0]


def same(a: float, b: float) -> bool:
    """Bit-equal doubles; every NaN equals every NaN."""
    a, b = float(a), float(b)
    if math.isnan(a) or math.isnan(b):
        return math.isnan(a) and math.isnan(b)
    return bits(a) == bits(b)


def rel_diff(a: float, b: float) -> float:
    """|a - b| / max(|a|, |b|); 0 for bit-equal values (NaN, inf included), inf when only one is finite."""
    a, b = float(a), float(b)
    if same(a, b):
        return 0.0
    if not (math.isfinite(a) and math.isfinite(b)):
        return math.inf
    return abs(a - b) / max(abs(a), abs(b))


def ulps(a: float, b: float) -> int | None:
    """Signed distance in binary64 ulps (None when either is NaN or the signs differ)."""
    a, b = float(a), float(b)
    if math.isnan(a) or math.isnan(b) or (a < 0) != (b < 0):
        return None
    return bits(a) - bits(b)


def num(x: float) -> str:
    return repr(float(x))


def forecast_value(key: int, value: float, k2: float, k3: float) -> float:
    """``GetForecastStatValue``: ``Stat.FastValue`` with only the two K modifiers on the stat, in
    ``StatBase.CalculateValue``'s order ``((1 * K3) * (1 + K2)) * (BaseValue + 0)``. The snapshot columns use the
    data order ``(Value * (1 + K2)) * K3`` instead (G11); the two can differ in the last bit."""
    multiplicative = 1.0 * float(k3) if int(key) in DIRECT_INTERPOLATION_STATS else 1.0
    additive = 1.0 + float(k2) if int(key) in MAIN_INTERPOLATION_STATS else 1.0
    return 1.0 * multiplicative * additive * (float(value) + 0.0)


# ---------------------------------------------------------------- the editor's float arithmetic

def mono_best_armor_against_dps(s, caches):
    """``GetBestArmorAgainstDps`` as the editor's Mono runs it (SimulationSystem.cs:640): ``(double)(1f - Resist)``
    is evaluated in binary64 and never stored, so it is ``1.0 - Resist`` without the float rounding
    ``model.simcalc`` applies (DifferentialProbes.md section 3)."""
    import sys as _sys
    best, selected = -_sys.float_info.max, None
    for armor in s.armors:
        data = caches.dps_cache.get(armor.type)
        if data is not None:
            value = data * (1.0 - float(armor.resist))
            if value > best:
                best, selected = value, armor
    return selected if selected is not None else (s.armors[0] if s.armors else None)


def mono_dps_against_spawner(s, caches):
    """``GetDpsAgainstSpawner(spawner, true)`` (SimulationSystem.cs:611) under the editor's arithmetic."""
    armor = mono_best_armor_against_dps(s, caches)
    if armor is None:
        raise LookupError(f"spawner {s.d.go_path}: ArmorConfig has no armors (NullReferenceException)")
    value = caches.dps_cache.get(armor.type, 0.0)
    return (caches.ability_dps_multiplier * caches.global_attack_multiplier * caches.crit_multiplier * value
            * (1.0 - float(armor.resist)))


def mono_arithmetic():
    """Context manager: ``model.simcalc`` with the two ``(double)(1f - armor.Resist)`` sites as the editor computes
    them. Every check runs under it; the plain oracle rounds ``1f - Resist`` to float first and differs by up to
    a float32 ulp of that factor (3.7e-8 relative in playerDPS on the shipped states)."""
    from contextlib import ExitStack
    from unittest import mock
    from ..model import simcalc
    stack = ExitStack()
    stack.enter_context(mock.patch.object(simcalc, "best_armor_against_dps", mono_best_armor_against_dps))
    stack.enter_context(mock.patch.object(simcalc, "dps_against_spawner", mono_dps_against_spawner))
    return stack


def text_defect(got: float, want: float) -> bool:
    """C1's known text defect (C8, BakeTool.md 6.1): ``SimulationJson.FormatDouble`` writes the 15-digit ``"R"``
    text when Mono's own parser reads it back to the value, but Mono reads some 15-digit texts 1 ulp high, so a
    correct parser gets the neighbour. ``got`` is such a reading of ``want`` when ``want``'s 15-digit text parses to
    ``got`` and they are 1 ulp apart."""
    got, want = float(got), float(want)
    return math.isfinite(want) and abs(ulps(got, want) or 0) == 1 and float(format(want, ".15g")) == got


def apply_repair(s: "Snap", path: tuple, value: float) -> None:
    snap = s.snapshot
    if path[0] == "Stats":
        attr = STAT_ATTRS[path[2]]
        snap.stats[path[1]] = replace(snap.stats[path[1]], **{attr: value})
    else:
        lst = snap.dps_data if path[0] == "DpsData" else snap.defense_data
        lst[path[1]] = (lst[path[1]][0], value)


STAT_ATTRS = {"Value": "value", "StatsFromFeaturesFlat": "stats_from_features_flat",
              "PercentMultiplier": "percent_multiplier", "TotalFlatValue": "total_flat_value",
              "TotalValue": "total_value", "CurrentGpm": "current_gpm"}


# ---------------------------------------------------------------- results

@dataclass
class CheckResult:
    check: str
    title: str
    status: str
    items: int
    mismatches: int
    counts: dict = field(default_factory=dict)
    vacuous: dict = field(default_factory=dict)  # part -> why nothing was checked
    examples: list = field(default_factory=list)
    csv: list = field(default_factory=list)  # written files

    def summary(self) -> dict:
        return {"check": self.check, "title": self.title, "status": self.status, "items": self.items,
                "mismatches": self.mismatches, "counts": self.counts, "vacuous": self.vacuous,
                "examples": self.examples, "csv": self.csv}


class Check:
    """Collects the per-item rows of one check. ``table`` names a CSV (one per table)."""

    def __init__(self, check_id: str, title: str, columns: Sequence[str] | Mapping[str, Sequence[str]]):
        self.id, self.title = check_id, title
        self.columns = dict(columns) if isinstance(columns, Mapping) else {check_id: list(columns)}
        self.rows: dict[str, list[dict]] = {t: [] for t in self.columns}
        self.items = self.mismatches = 0
        self.counts: dict[str, Any] = {}
        self.vacuous: dict[str, str] = {}
        self.examples: list[dict] = []

    def item(self, ok: bool, table: str | None = None, **row) -> bool:
        table = table or next(iter(self.columns))
        row["ok"] = int(bool(ok))
        self.rows[table].append(row)
        self.items += 1
        if not ok:
            self.mismatches += 1
            if len(self.examples) < 10:
                self.examples.append({"table": table, **row})
        return ok

    def same(self, run: "RunData", s: "Snap", path: tuple, got: float, want: float) -> bool:
        """Bit-equal, or a repaired reading of C1's text defect (counted as ``text_repaired``)."""
        if same(got, want):
            return True
        ok = run.resolve(s, path, got, want)
        if ok:
            self.count("text_repaired")
        return ok

    def count(self, key: str, n: int = 1) -> None:
        self.counts[key] = self.counts.get(key, 0) + n

    def vacuous_part(self, part: str, reason: str) -> None:
        self.vacuous[part] = reason

    def result(self, out_dir: str | Path | None = None) -> CheckResult:
        status = "fail" if self.mismatches else ("vacuous" if self.items == 0 else "pass")
        written = []
        if out_dir is not None:
            out = Path(out_dir)
            out.mkdir(parents=True, exist_ok=True)
            for table, cols in self.columns.items():
                path = out / f"{table}.csv"
                with path.open("w", newline="", encoding="utf-8") as fh:
                    w = csv.DictWriter(fh, fieldnames=[*cols, "ok"], lineterminator="\n", extrasaction="raise")
                    w.writeheader()
                    w.writerows(self.rows[table])
                written.append(path.name)
        return CheckResult(self.id, self.title, status, self.items, self.mismatches, dict(self.counts),
                           dict(self.vacuous), list(self.examples), written)


# ---------------------------------------------------------------- the run

LIST_EARLY, LIST_DAY = "Snapshots", "TimeChunkSnapshots"
SIM_FILE, FULL_FILE = "simulation_data.json", "timechunk_full.json"


@dataclass
class Snap:
    """One snapshot of the run: the oracle form (exact numbers from C1's JSON) and the side-file columns."""

    list: str
    index: int
    snapshot: SimulationSnapshot
    ext: dict | None  # snapshots_ext.csv row
    items_ext: list[dict]  # items_ext.csv rows in item order

    @property
    def tc(self) -> int:
        return int(self.snapshot.time_chunk_index)

    @property
    def slots(self) -> tuple[int | None, int | None]:
        e = self.ext or {}
        return e.get("unlocked_weapon_slots"), e.get("unlocked_armor_slots")

    @property
    def current_health(self) -> float | None:
        return None if self.ext is None else self.ext.get("current_health")

    def evolved(self) -> list[bool | None]:
        flags = [r.get("evolved") for r in self.items_ext]
        return flags if len(flags) == len(self.snapshot.items) else [None] * len(self.snapshot.items)

    def key(self) -> tuple:
        return (self.list, self.index)


def snapshot_from_json(s: Mapping[str, Any], index: int | None = None) -> SimulationSnapshot:
    return SimulationSnapshot(
        playtime=f32(s["Playtime"]), total_playtime=f32(s["TotalPlaytime"]), location=int(s["Location"]),
        time_chunk_index=int(s["TimeChunkIndex"]),
        stats=[SimulationStat(int(st["Key"]), float(st["Value"]), float(st["StatsFromFeaturesFlat"]),
                              float(st["PercentMultiplier"]), float(st["TotalFlatValue"]), float(st["TotalValue"]),
                              float(st["CurrentGpm"])) for st in s["Stats"]],
        items=[SimulationItem(str(it["Key"]), int(it["Level"]), int(it["Copies"])) for it in s["Items"]],
        dps_data=[(int(x["WeaponType"]), float(x["Value"])) for x in s["DpsData"]],
        defense_data=[(int(x["WeaponType"]), float(x["Value"])) for x in s["DefenseData"]], source_index=index)


@dataclass
class RunData:
    job_dir: Path
    manifest: dict
    early: list[Snap]
    full: list[Snap]  # the untrimmed day list (timechunk_full.json)
    exported_days: list[int]  # TimeChunkIndex of simulation_data.json's (trimmed) TimeChunkSnapshots
    trace: tr.Trace | None
    digests: dict[int, dict[str, str]]  # day -> digest fields (only with the digest_text output)
    override_days: list[dict]  # override_days.jsonl records (replay/checkpoint jobs)
    raw: dict = field(default_factory=dict)  # snapshot file -> parsed JSON
    expected: dict = field(default_factory=dict)  # snapshot file -> the manifest's export entry
    text_exact: dict = field(default_factory=dict)  # snapshot file -> its numbers passed C1's bit digest
    repairs: dict = field(default_factory=dict)  # (list, index, path) -> value read through the text defect

    @property
    def job_id(self) -> str:
        return str(self.manifest.get("job_id", self.job_dir.name))

    @property
    def mode(self) -> str:
        return str(self.manifest.get("mode"))

    @property
    def override_active(self) -> bool:
        return bool((self.manifest.get("override") or {}).get("active"))

    @property
    def null_policy(self) -> bool:
        return bool((self.manifest.get("engine") or {}).get("null_policy"))

    def file_of(self, s: "Snap") -> str:
        return FULL_FILE if s.list == LIST_DAY and FULL_FILE in self.raw else SIM_FILE

    def resolve(self, s: "Snap", path: tuple, got: float, want: float) -> bool:
        """Whether the exported ``got`` stands for ``want``: bit-equal, or (only in a file that failed C1's bit
        digest) a 15-digit text of ``want`` that a correct parser reads 1 ulp off (:func:`text_defect`). Such a
        value is recorded as a repair, applied to ``s`` and confirmed later against the exporter's digest."""
        if same(got, want):
            return True
        if self.text_exact.get(self.file_of(s), True) or not text_defect(got, want):
            return False
        self.repairs[(s.list, s.index, path)] = float(want)
        apply_repair(s, path, float(want))
        return True

    def confirm_repairs(self) -> dict:
        """C1's bit digests recomputed with every repair (day-list repairs also go to the trimmed copy in
        ``simulation_data.json``): ``confirmed`` when each file that failed the digest now matches it."""
        data = {f: copy.deepcopy(d) for f, d in self.raw.items()}
        for (lst, index, path), value in self.repairs.items():
            targets = []
            if lst == LIST_EARLY:
                targets.append(data[SIM_FILE][LIST_EARLY][index])
            else:
                full = data.get(FULL_FILE, data[SIM_FILE])[LIST_DAY]
                tc = full[index]["TimeChunkIndex"]
                targets.append(full[index])
                if FULL_FILE in data:
                    targets += [x for x in data[SIM_FILE][LIST_DAY] if x["TimeChunkIndex"] == tc]
            for t in targets:
                if path[0] == "Stats":
                    t["Stats"][path[1]][path[2]] = value
                else:
                    t[path[0]][path[1]]["Value"] = value
        files = {}
        for f, d in data.items():
            if self.text_exact.get(f, True):
                continue
            digest = simio.bits_digest(d)
            files[f] = all(digest[k] == self.expected[f].get(k) for k in schema.EXPORT_BITS_KEYS)
        return {"repairs": len(self.repairs), "files": files, "confirmed": all(files.values())}


def _ext_rows(job_dir: Path, name: str) -> list[dict]:
    path = job_dir / name
    return schema.read_csv(name, path.read_text(encoding="utf-8")) if path.exists() else []


def load_run(job_dir: str | Path) -> RunData:
    """A job directory as the checks read it: the manifest (schema-checked), both snapshot lists with every number
    checked against the exporter's bit digest (C1), the side files, the trace and the override records."""
    job_dir = Path(job_dir)
    manifest = json.loads((job_dir / "manifest.json").read_text(encoding="utf-8"))
    schema.validate_manifest(manifest)
    files = (manifest.get("export") or {}).get("files") or {}
    simio.verify_export_files(job_dir, files)
    raw, text_exact = {}, {}
    for name, lists in ((SIM_FILE, schema.SIMULATION_DATA_LISTS), (FULL_FILE, (LIST_DAY,))):
        if (job_dir / name).exists():
            raw[name], text_exact[name] = read_exported(job_dir / name, lists, files.get(name))
    data = raw[SIM_FILE]
    full_json = raw.get(FULL_FILE, {LIST_DAY: data[LIST_DAY]})
    ext = {(r["list"], r["index"]): r for r in _ext_rows(job_dir, "snapshots_ext.csv")}
    items: dict[tuple, list[dict]] = {}
    for r in _ext_rows(job_dir, "items_ext.csv"):
        items.setdefault((r["list"], r["index"]), []).append(r)

    def snaps(lst: str, rows: list) -> list[Snap]:
        out = []
        for i, s in enumerate(rows):
            its = sorted(items.get((lst, i), []), key=lambda r: r["item_index"])
            out.append(Snap(lst, i, snapshot_from_json(s, i), ext.get((lst, i)), its))
        return out

    trace = None
    digests: dict[int, dict[str, str]] = {}
    if manifest.get("trace") is not None:
        trace = tr.read(job_dir)
        digests = {day: tr.digest_fields(text) for day, (_, text) in trace.texts.items()}
    days_path = job_dir / "override_days.jsonl"
    override_days = ([json.loads(l) for l in days_path.read_text(encoding="utf-8").splitlines() if l.strip()]
                     if days_path.exists() else [])
    return RunData(job_dir, manifest, snaps(LIST_EARLY, data["Snapshots"]), snaps(LIST_DAY, full_json["TimeChunkSnapshots"]),
                   [int(s["TimeChunkIndex"]) for s in data["TimeChunkSnapshots"]], trace, digests, override_days,
                   raw, {k: v for k, v in files.items() if k in raw}, text_exact)


def read_exported(path: Path, lists, expected) -> tuple[dict, bool]:
    """A C1 snapshot file and whether its numbers passed the exporter's bit digest. The sha256 must match; a digest
    mismatch is C1's text defect (:func:`text_defect`), which the checks repair and confirm against the digest."""
    try:
        return simio.read_snapshot_file(path, lists, expected), True
    except simio.SimioError as e:
        if "bits_sha256" not in str(e):
            raise
    return simio.read_snapshot_file(path, lists), False


# ---------------------------------------------------------------- all checks

CHECKS = ("X1", "X2", "X3", "X4", "X5", "X6", "X7", "DP8")


def run_checks(job_dir: str | Path, con, *, checks: Iterable[str] = CHECKS, out_dir: str | Path | None = None,
               rounding=None) -> dict[str, CheckResult]:
    """Every requested check on one job; CSVs and ``summary.json`` go to ``out_dir`` (default ``<job>/crosscheck``).
    DP8 runs only on jobs with ``crosscheck_dp8`` records, X1-X7 on the rest."""
    from . import bake, composition, dps, eligibility, invariants, rpm

    run = load_run(job_dir)
    out = Path(out_dir) if out_dir is not None else run.job_dir / CSV_DIR
    env = Env.load(con, rounding)
    fns = {"X1": lambda: composition.check_x1(run, env, out), "X2": lambda: dps.check_x2(run, env, out),
           "X3": lambda: rpm.check_x3(run, env, out), "X4": lambda: bake.check_x4(run, env, out),
           "X5": lambda: eligibility.check_x5(run, env, out), "X6": lambda: invariants.check_x6(run, env, out),
           "X7": lambda: rpm.check_x7(run, env, out), "DP8": lambda: rpm.check_dp8(run, env, out)}
    with mono_arithmetic():
        results = {c: fns[c]() for c in checks}
    text = {"exact": dict(run.text_exact)}
    if run.repairs:
        text.update(run.confirm_repairs())
        for r in results.values():
            n = r.counts.get("text_repaired", 0)
            if n and not text["confirmed"]:
                r.status, r.mismatches = "fail", r.mismatches + n
                r.examples.append({"text_repairs": n, "problem": "the repaired values do not reproduce C1's bit digest"})
    for r in results.values():
        r.counts["text_defect_files"] = sorted(f for f, ok in run.text_exact.items() if not ok)
    out.mkdir(parents=True, exist_ok=True)
    (out / "summary.json").write_text(json.dumps({"job_id": run.job_id, "job_dir": str(run.job_dir), "export_text": text,
                                                  "checks": {c: r.summary() for c, r in results.items()}},
                                                 indent=1, sort_keys=True, allow_nan=True) + "\n", encoding="utf-8")
    return results


@dataclass
class Env:
    """The oracle side, loaded once per database: configs, the simcalc context, the identity map."""

    con: Any
    interp: Any
    ctx: Any
    identity: Any
    rounding: Any = None
    _cache: dict = field(default_factory=dict)

    @classmethod
    def load(cls, con, rounding=None) -> "Env":
        from ..model.data import load_interpolations
        from ..model.simcalc import load_simcalc_context
        from ..unity.spawner_map import load_identity_map
        return cls(con, load_interpolations(con), load_simcalc_context(con), load_identity_map(con), rounding)

    def k(self, day: int) -> tuple[float, float]:
        """(K2, K3) of day ``day`` (clamped at 0) under the oracle's rounding, as doubles widened from float."""
        from ..model.snapshots import composition_coefficients
        d = max(int(day), 0)
        key = ("k", d)
        if key not in self._cache:
            self._cache[key] = composition_coefficients(self.interp, d, self.rounding)
        return self._cache[key]

    def runtime_id(self, sid) -> str:
        return self.identity.by_spawner_id[tuple(sid)].runtime_id

    def spawner_id(self, rid: str):
        e = self.identity.by_runtime_id.get(rid)
        return None if e is None else e.spawner_id


def stat_name(key: int) -> str:
    try:
        return StatType(int(key)).name
    except ValueError:
        return str(key)


# ---------------------------------------------------------------- the acceptance jobs

FLAT_GRANTS = {8: 10.0, 16: 10.0}  # CutDamageFlat, CutDefenceFlat: K2 then shows in DpsData/DefenseData (X2, X7)


def standard_jobs(con, *, null_policy: bool = True, last_day: int = 405) -> list[dict]:
    """The runs P11 checks: ``smoke-11`` and ``golden-405`` (X1-X7), ``golden-405-flat`` (the same with flat Cut
    damage/defence so the K2 lag shows in the caches), ``dp8-checkpoint`` (the 46 shipped day-snapshot states with
    the DP8 export) and ``dp8-reference`` (the same without it: the export must not change the run). Every job writes
    the digest text. ``null_policy`` keeps FindBestSpawner out; K3 runs the calibrated policy with False."""
    from ..unity import engine, override as ov
    probe = engine.engine_probe(null_policy=null_policy)
    text = {"digest_text": True}
    checkpoint = ov.default_override(con, unlock="snapshot")
    return [
        engine.engine_job("smoke-11", day=11, max_wall_seconds=300.0, probe=probe, outputs=text),
        engine.engine_job("golden-405", day=last_day, probe=probe, outputs=text),
        engine.engine_job("golden-405-flat", day=last_day, outputs=text,
                          probe=engine.engine_probe(null_policy=null_policy, grant_flat=FLAT_GRANTS)),
        ov.override_job("dp8-checkpoint", "checkpoint", checkpoint, probe=probe, day=last_day,
                        outputs={**text, OUTPUT_DP8: True}),
        ov.override_job("dp8-reference", "checkpoint", checkpoint, probe=probe, day=last_day, outputs=text),
    ]
