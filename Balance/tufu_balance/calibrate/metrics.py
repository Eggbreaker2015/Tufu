"""Calibration metrics: per-cell residuals of a run against the targets (design 5.1; unit P10).

:func:`compute` turns a simulated :class:`~.targets.Series` (read from a run's ``sim.sqlite``) and the
:class:`~.targets.Targets` into :class:`Cell` rows, independent of any tolerance:

* comparisons (T01, T04-T07, T09, T11-T14, T16, T17): ``residual`` is sim minus reference (a distance for
  intervals, snapshot positions for first appearances), so the shipped data against itself gives 0;
* invariants of the run itself (T02, T03, T10, T18): ``residual`` is the size of the violation, 0 when
  the rule holds.

:func:`evaluate` applies ``tolerances.yaml`` for one mode (``open`` = replay columns, ``closed`` = free
or checkpoint columns): each cell gets ``ok`` from its group's criterion and each target a status.
T08, T15, T19 and T20 need run outputs that ``sim.sqlite`` does not carry yet (events, DpsData under
R2b, bake report); they are not computed here.
"""

from __future__ import annotations

import bisect
import math
import statistics
from dataclasses import dataclass, field, replace
from pathlib import Path
from typing import Any, Iterable, Mapping, Sequence

import yaml

from ..model.f32 import f32
from ..model.snapshots import MAIN_INTERPOLATION_STATS, DIRECT_INTERPOLATION_STATS
from ..model.enums import StatType
from ..model import timeaxis as TA
from ..model.simcalc import gpm_matches
from . import targets as T

TOLERANCES = Path(__file__).with_name("tolerances.yaml")
TARGET_IDS = ("T01", "T02", "T03", "T04", "T05", "T06", "T07", "T09", "T10", "T11", "T12", "T13", "T14", "T16",
              "T17", "T18")
MODES = ("open", "closed")
FAST_FROM_TC = 4  # FastTickStartChunkOffset (shipped config)
HOLDOUT_TC = 104  # design 5.1: training TC < 104, holdout TC >= 104
# Design 5.1's phases, with 57-154 cut at the holdout boundary (TC104 is a day snapshot)
T12_PHASES = ((4, 19), (19, 57), (57, 104), (104, 154), (154, 405))
T16_MAIN = ("Health", "HealthRegen", "MovementSpeed")
SLOT_ARMOR_FROM_TC = 9
EARLY_INTERVALS = ((0.0, 20.0), (1.0, 5.0))  # P06 defaults: (EarlySnapshotIntervalStarts, EarlySnapshotIntervals)
# A snapshot is taken at the first tick past its due time; one slow-phase action (fight + move) is assumed
# shorter than this many minutes (the shipped early list overshoots by at most 1.19).
EARLY_MAX_OVERSHOOT = 2.0


@dataclass
class Cell:
    target: str
    group: str
    key: str
    sim: Any
    ref: Any
    residual: float | int | None  # None: no value on one side (the run never reached the reference's state)
    rel: float | None = None
    stat: str | None = None
    tc: int | None = None  # TC the cell belongs to (end of an interval)
    unit: str | None = None  # cells sharing a unit pass or fail together (T05 intervals)
    split: str = ""  # train | holdout | structure | report
    bound: float | None = None  # a per-cell bound used by the criterion (T06, T16: one-clear reward)
    note: str = ""
    available: bool = True  # False: the run does not export this column at all (no verdict, "unavailable")
    ok: bool | None = None  # set by evaluate()

    def sort_key(self) -> tuple:
        return (self.target, self.group, self.key)


def _rel(sim: float | None, ref: float | None) -> float | None:
    if sim is None or ref is None:
        return None
    if _nan(sim) and _nan(ref):
        return 0.0
    if _nan(sim) or _nan(ref):
        return math.inf
    if ref == 0:
        return 0.0 if sim == 0 else math.inf
    return abs(sim - ref) / abs(ref)


def _nan(x: Any) -> bool:
    return isinstance(x, float) and math.isnan(x)


def _diff(sim: float | None, ref: float | None) -> float | None:
    if sim is None or ref is None:
        return None
    if _nan(sim) and _nan(ref):
        return 0.0
    return sim - ref


def _split(tc: int) -> str:
    return "train" if tc < HOLDOUT_TC else "holdout"


# ---------------------------------------------------------------- per target

def t01(sim: T.Series, tg: T.Targets) -> list[Cell]:
    a, b = sim.tc_days, tg.shipped.tc_days
    out = []
    for i in range(max(len(a), len(b))):
        s = a[i] if i < len(a) else None
        r = b[i] if i < len(b) else None
        out.append(Cell("T01", "days", f"day[{i:02d}]", s, r, 0 if s == r else 1, tc=r if r is not None else s,
                        split="structure"))
    return out


def t02(sim: T.Series, tg: T.Targets) -> list[Cell]:
    out = []
    for name, snaps in (("TimeChunkSnapshots", sim.tc), ("Snapshots", sim.early)):
        for s in snaps:
            floor = TA.time_chunk_index(f32(s.total_playtime))
            out.append(Cell("T02", "tc_floor", f"{name}[{s.index:02d}]", s.tc, floor, s.tc - floor, tc=s.tc,
                            split="structure"))
    for s in sim.early:
        r = TA.offline_residual(f32(s.playtime), f32(s.total_playtime))
        out.append(Cell("T02", "offline_multiple", f"Snapshots[{s.index:02d}]", r, 0.005, max(0.0, r - 0.005),
                        tc=s.tc, split="structure", note="|T-P-230k| minutes"))
    if sim.early:
        pt = lambda s: (float(f32(s.playtime)), float(f32(s.total_playtime)))
        pins = TA.offline_pins([(s.tc, *pt(s)) for s in sim.tc])
        check = TA.check_lifecycle([pt(s) for s in sim.early], pins)
        bad = len(check.violations) + (0 if check.chain_feasible else 1)
        out.append(Cell("T02", "session_rule", "period_end=P+10", bad, 0, bad, split="structure",
                        note="rows " + " ".join(map(str, check.violations)) if check.violations else ""))
    ref_last, sim_last = tg.shipped.tc[-1], sim.day(tg.shipped.tc[-1].tc)
    ref_v = ref_last.playtime / ref_last.total_playtime
    sim_v = None if sim_last is None else sim_last.playtime / sim_last.total_playtime
    out.append(Cell("T02", "p_over_t", f"P/T@{ref_last.tc}", sim_v, ref_v, _diff(sim_v, ref_v), _rel(sim_v, ref_v),
                    tc=ref_last.tc, split="report"))
    return out


def _forced_rows(early: Sequence[T.Snapshot], days: Sequence[T.Snapshot]) -> set[int]:
    pt = {(d.playtime, d.total_playtime) for d in days if d.tc >= 1}
    return {i for i, s in enumerate(early) if (s.playtime, s.total_playtime) in pt}


def early_intervals(series: T.Series) -> tuple[tuple[float, ...], tuple[float, ...]]:
    """P06 of the run (its job's parameter object), else the defaults."""
    p = series.params or {}
    starts = p.get("EarlySnapshotIntervalStarts", EARLY_INTERVALS[0])
    steps = p.get("EarlySnapshotIntervals", EARLY_INTERVALS[1])
    if len(starts) != len(steps) or not starts:
        raise ValueError(f"EarlySnapshotIntervalStarts {starts} and EarlySnapshotIntervals {steps} do not pair up")
    return tuple(float(x) for x in starts), tuple(float(x) for x in steps)


def t03(sim: T.Series, tg: T.Targets, interval_starts: Sequence[float] | None = None,
        intervals: Sequence[float] | None = None) -> list[Cell]:
    if interval_starts is None or intervals is None:
        interval_starts, intervals = early_intervals(sim)
    ref = tg.shipped.early
    out = [Cell("T03", "rows", "rows", len(sim.early), len(ref), len(sim.early) - len(ref), split="structure")]
    if sim.early:
        last, rlast = sim.early[-1], ref[-1]
        out.append(Cell("T03", "last_row_tc", "last.TC", last.tc, rlast.tc, last.tc - rlast.tc, split="structure"))
        out.append(Cell("T03", "last_row_t", "last.T", last.total_playtime, rlast.total_playtime,
                        _diff(last.total_playtime, rlast.total_playtime),
                        _rel(last.total_playtime, rlast.total_playtime), split="report"))
    late = sum(s.tc >= FAST_FROM_TC for s in sim.early)
    out.append(Cell("T03", "fast_days_only_in_day_list", f"early rows with TC>={FAST_FROM_TC}", late, 0, late,
                    split="structure"))
    forced = _forced_rows(sim.early, sim.tc)
    for j in range(1, len(sim.early)):
        if j in forced:
            continue
        p0, p1 = float(sim.early[j - 1].playtime), float(sim.early[j].playtime)
        step = intervals[max(0, bisect.bisect_right(interval_starts, p0) - 1)]
        late = max(step, intervals[max(0, bisect.bisect_right(interval_starts, p1) - 1)]) + EARLY_MAX_OVERSHOOT
        gap = p1 - p0
        out.append(Cell("T03", "cadence", f"Snapshots[{sim.early[j].index:02d}]", gap, step,
                        max(0.0, step - gap - 1e-3, gap - late), tc=sim.early[j].tc, split="structure",
                        note=f"interval(P_prev) <= dP <= max(interval(P_prev), interval(P)) + {EARLY_MAX_OVERSHOOT:g}"))
    pt = {(s.playtime, s.total_playtime) for s in sim.early}
    for d in sim.tc:
        if 1 <= d.tc < FAST_FROM_TC:
            hit = (d.playtime, d.total_playtime) in pt
            out.append(Cell("T03", "forced_day_snapshot", f"TC{d.tc}", int(hit), 1, 0 if hit else 1, tc=d.tc,
                            split="structure", note="cross-day forced snapshot also in the early list (G23)"))
    return out


def t04(sim: T.Series, tg: T.Targets) -> list[Cell]:
    out = []
    ref = tg.shipped.tc
    for a, b in zip(ref, ref[1:]):
        r = (b.playtime - a.playtime) / (b.tc - a.tc)
        sa, sb = sim.day(a.tc), sim.day(b.tc)
        s = None if sa is None or sb is None else (sb.playtime - sa.playtime) / (b.tc - a.tc)
        group = "day_0_3" if a.tc < FAST_FROM_TC else "tc_4_10" if a.tc < 11 else "tc_ge_11"
        out.append(Cell("T04", group, f"{a.tc:03d}->{b.tc:03d}", s, r, _diff(s, r), _rel(s, r), tc=b.tc,
                        split=_split(a.tc), note="active minutes per day"))
    last = ref[-1]
    s = sim.day(last.tc)
    sp = None if s is None else s.playtime
    out.append(Cell("T04", "p_final", f"P@{last.tc}", sp, last.playtime, _diff(sp, last.playtime),
                    _rel(sp, last.playtime), tc=last.tc, split=_split(last.tc)))
    return out


def t05(sim: T.Series, tg: T.Targets) -> list[Cell]:
    out = []
    ref = [s for s in tg.shipped.tc if s.tc >= FAST_FROM_TC]
    for a, b in zip(ref, ref[1:]):
        sa, sb = sim.day(a.tc), sim.day(b.tc)
        unit = f"{a.tc:03d}->{b.tc:03d}"
        group = "daily" if b.tc - a.tc == 1 else "multi_day"
        for k in T.STATS:
            r = b.value(k) - a.value(k)
            s = None if sa is None or sb is None else sb.value(k) - sa.value(k)
            out.append(Cell("T05", group, f"{unit}|{k}", s, r, _diff(s, r), _rel(s, r), stat=k, tc=b.tc, unit=unit,
                            split=_split(a.tc)))
    return out


def t06(sim: T.Series, tg: T.Targets) -> list[Cell]:
    out = []
    for r in tg.shipped.tc:
        if r.tc < 1:
            continue
        s = sim.day(r.tc)
        group = "tc_1_4" if r.tc <= 4 else "tc_5_23" if r.tc < 24 else "tc_ge_24"
        for k in T.STATS:
            sv, rv = (None if s is None else s.value(k)), r.value(k)
            out.append(Cell("T06", group, f"{r.tc:03d}|{k}", sv, rv, _diff(sv, rv), _rel(sv, rv), stat=k, tc=r.tc,
                            split=_split(r.tc), bound=tg.one_clear_reward[k][r.location]))
    return out


def unlock_interval_days(series: T.Series, island: int) -> list[int] | None:
    """Days the island was unlocked in: ``[prev TC, first TC - 1]`` of the first day snapshot at or past it
    (exact when the run exported its untrimmed day list)."""
    days = series.full or series.tc
    prev = None
    for s in days:
        if s.location >= island:
            if prev is None:
                return [0, 0]
            return [prev.tc, s.tc - 1]
        prev = s
    return None


def unlock_interval_playtime(series: T.Series, island: int) -> list[float] | None:
    """``(P of the last early row before, P of the first early row at or past the island]``."""
    prev = None
    for s in series.early:
        if s.location >= island:
            return [float(prev.playtime) if prev else 0.0, float(s.playtime)]
        prev = s
    return None


def unlock_day_early(series: T.Series, island: int) -> int | None:
    """TC of the first early row at or past the island."""
    return next((s.tc for s in series.early if s.location >= island), None)


def _interval_distance(a: Sequence[float] | None, b: Sequence[float] | None) -> float | None:
    if a is None or b is None:
        return None
    return max(0.0, a[0] - b[1], b[0] - a[1])


def t07(sim: T.Series, tg: T.Targets) -> list[Cell]:
    out = []
    for island in range(2, 18):
        tc = None
        if island <= 3:
            s, r, group = unlock_interval_playtime(sim, island), unlock_interval_playtime(tg.shipped, island), "by_playtime"
            tc = unlock_day_early(sim, island)  # the run's unlock day, for the "<= 1 day" clause
        else:
            s, r = unlock_interval_days(sim, island), unlock_interval_days(tg.shipped, island)
            group = "island_4" if island == 4 else "islands_5_17"
        out.append(Cell("T07", group, f"island {island:02d}", s, r, _interval_distance(s, r), tc=tc, split="report"))
    return out


def t09(sim: T.Series, tg: T.Targets) -> list[Cell]:
    out = []
    for r in tg.shipped.tc:
        s = sim.day(r.tc)
        for k, fields in r.stats.items():
            rv = fields["CurrentGpm"]
            sv = None if s is None or k not in s.stats else s.stats[k]["CurrentGpm"]
            out.append(Cell("T09", "nonzero" if rv != 0 else "zero", f"{r.tc:03d}|{k}", sv, rv, _diff(sv, rv),
                            _rel(sv, rv), stat=k, tc=r.tc, split="validation"))
    return out


def t10(sim: T.Series, tg: T.Targets) -> list[Cell]:
    """G19 on the run's early list: ``CurrentGpm = dValue/dPlaytime`` (relative 1e-5), NaN when dP = 0."""
    out = []
    for a, b in zip(sim.early, sim.early[1:]):
        dp = float(f32(b.playtime)) - float(f32(a.playtime))
        for k, fields in b.stats.items():
            stored = fields["CurrentGpm"]
            if dp == 0.0:
                good = _nan(stored)
                out.append(Cell("T10", "nan", f"{b.index:02d}|{k}", stored, math.nan, 0 if good else 1, stat=k,
                                tc=b.tc, split="structure"))
                continue
            g = (fields["Value"] - a.stats[k]["Value"]) / dp
            err = abs(stored - g) if not _nan(stored) else math.inf
            bad = err > 1e-5 * max(1.0, abs(g))
            out.append(Cell("T10", "gpm", f"{b.index:02d}|{k}", stored, g, err if bad else 0.0, stat=k, tc=b.tc,
                            split="structure"))
    return out


def _evolution(series: T.Series, tc: int, x: Mapping, info: T.ItemInfo | None = None) -> tuple[bool, int, str]:
    """(evolved, evolution level, how) of a snapshot item: the run's own record when it exported one
    (``run``), else the survey's reading (``heuristic``: Level != Copies means evolved, and then Level is the
    evolution level). A reading whose level is outside ``1..len(info.shards)`` cannot be an evolution level:
    it is ``invalid`` and counts as not evolved, so a perturbed copy count never invents levels."""
    rec = (series.items_evolved or {}).get((tc, x["Key"]))
    if rec is not None and rec[0] is not None:
        evolved = bool(rec[0])
        level = rec[1] if rec[1] is not None else (x["Level"] if evolved else 0)
        return evolved, int(level) if evolved else 0, "run"
    if x["Level"] == x["Copies"]:
        return False, 0, "heuristic"
    level = int(x["Level"])
    if info is not None and not 1 <= level <= len(info.shards):
        return False, 0, "invalid"
    return True, level, "heuristic"


def _position(days: Sequence[int], tc: int | None) -> int | None:
    """Position of ``tc`` on the reference day list (the first day snapshot at or after it)."""
    if tc is None:
        return None
    return bisect.bisect_left(days, tc)


def t11(sim: T.Series, tg: T.Targets) -> list[Cell]:
    def first(series: T.Series) -> dict[tuple[str, str], int]:
        out: dict[tuple[str, str], int] = {}
        for s in series.tc:
            for x in s.items:
                info = tg.items.get(x["Key"])
                if info is not None:
                    out.setdefault((info.slot, info.rarity), s.tc)
        return out

    days = tg.shipped.tc_days
    a, b = first(sim), first(tg.shipped)
    out = []
    for key in sorted(set(a) | set(b)):
        s, r = a.get(key), b.get(key)
        ps, pr = _position(days, s), _position(days, r)
        out.append(Cell("T11", "first_appearance", f"{key[0]}|{key[1]}", s, r,
                        None if ps is None or pr is None else ps - pr, tc=r, split="report",
                        note="residual in day snapshots"))
    return out


def _acquired(info: T.ItemInfo, evolved: bool, level: int, copies: int) -> int:
    """Copies plus the shards paid for evolution levels 1..L (forensics emission.py)."""
    return copies + (sum(info.shards[:level]) if evolved else 0)


def _gains(series: T.Series, tg: T.Targets, a_tc: int, b_tc: int,
           invalid: list | None = None) -> dict[tuple[str, str], int] | None:
    """Copies acquired between two day snapshots per (kind, rarity); ``invalid`` collects the items whose
    evolution reading was rejected."""
    a, b = series.day(a_tc), series.day(b_tc)
    if a is None or b is None:
        return None

    def acquired(tc: int, x: Mapping, info: T.ItemInfo) -> int:
        evolved, level, how = _evolution(series, tc, x, info)
        if how == "invalid" and invalid is not None:
            invalid.append((tc, x["Key"]))
        return _acquired(info, evolved, level, x["Copies"])

    before = {x["Key"]: acquired(a_tc, x, tg.items[x["Key"]]) for x in a.items if x["Key"] in tg.items}
    out: dict[tuple[str, str], int] = {}
    for x in b.items:
        info = tg.items.get(x["Key"])
        if info is None:
            continue
        gain = acquired(b_tc, x, info) - before.get(x["Key"], 0)
        key = ("armor" if info.is_armor else "weapon", info.rarity)
        out[key] = out.get(key, 0) + gain
    return out


def t12(sim: T.Series, tg: T.Targets) -> list[Cell]:
    """Copies gained per (kind, rarity) and segment. Both runs are divided by the same spawner-days, so the
    relative residual of the gains is that of the copies per item spawner per day."""
    segments = [(d, d + 1, "daily") for d in range(FAST_FROM_TC, 11)]
    segments += [(a, b, f"phase_{a}_{b}") for a, b in T12_PHASES]
    out = []
    for a, b, group in segments:
        ref = _gains(tg.shipped, tg, a, b) or {}
        invalid: list = []
        got = _gains(sim, tg, a, b, invalid)
        note = f"{len(invalid)} item readings rejected (Level outside the evolution table)" if invalid else ""
        for key in sorted(set(ref) | set(got or {})):
            r = ref.get(key, 0)
            s = None if got is None else got.get(key, 0)
            out.append(Cell("T12", group, f"{a:03d}->{b:03d}|{key[0]}|{key[1]}", s, r, _diff(s, r), _rel(s, r),
                            tc=b, split="holdout" if b > HOLDOUT_TC else "train", note=note))
    return out


def t12_expected(expected_daily: Mapping[int, Mapping[str, float]], tg: T.Targets) -> list[Cell]:
    """Pre-roll copies vs shipped integers, with one copy per active item stream of noise.

    A carry stream's count over an interval differs from its real expectation by less
    than one. Keep the 5% structural allowance in addition; phase checks stay unchanged.
    Missing trace days produce missing cells, never a zero-residual success.
    """
    out = []
    for d in range(FAST_FROM_TC, 11):
        ref = _gains(tg.shipped, tg, d, d + 1) or {}
        totals: dict[tuple[str, str], float] = {}
        streams: dict[tuple[str, str], int] = {}
        amounts = expected_daily.get(d)
        for key, value in (amounts or {}).items():
            item = tg.items[key]
            group = ("armor" if item.is_armor else "weapon", item.rarity)
            totals[group] = totals.get(group, 0.0) + value
            if value > 0:
                streams[group] = streams.get(group, 0) + 1
        for key in sorted(set(ref) | set(totals)):
            r = ref.get(key, 0)
            pred = totals.get(key, 0.0) if amounts is not None else None
            noise = streams.get(key, 0)
            out.append(Cell("T12", "daily_expected", f"{d:03d}->{d+1:03d}|{key[0]}|{key[1]}",
                            pred, r, _diff(pred, r), _rel(pred, r), tc=d+1, split="train",
                            bound=noise + .05*abs(r),
                            note=f"pre-roll expectation; {noise} active item streams, <1 copy/stream integer noise + 5%"))
    return out


def evolution_first_reached(series: T.Series, items: Mapping[str, T.ItemInfo] | None = None
                            ) -> dict[tuple[str, int], int]:
    """(item, level) -> first day snapshot at that evolution level or above."""
    out: dict[tuple[str, int], int] = {}
    for s in series.tc:
        for x in s.items:
            evolved, level, _ = _evolution(series, s.tc, x, (items or {}).get(x["Key"]))
            for k in range(1, level + 1):
                out.setdefault((x["Key"], k), s.tc)
    return out


def t13(sim: T.Series, tg: T.Targets) -> list[Cell]:
    days = tg.shipped.tc_days
    a, b = evolution_first_reached(sim, tg.items), evolution_first_reached(tg.shipped, tg.items)
    out = []
    for item, k in sorted(set(a) | set(b)):
        info = tg.items[item]
        s, r = a.get((item, k)), b.get((item, k))
        ps, pr = _position(days, s), _position(days, r)
        target_day = info.target_timings[k - 1] if k <= len(info.target_timings) else None
        note = f"target day {target_day:g} -> position {_position(days, math.ceil(target_day))}" if target_day else ""
        split = "train" if info.rarity in ("Common", "Uncommon", "Rare") else "holdout"
        out.append(Cell("T13", split, f"{item}|L{k:02d}", s, r,
                        None if ps is None or pr is None else ps - pr, tc=r, split=split, note=note))
    return out


def t14(sim: T.Series, tg: T.Targets) -> list[Cell]:
    out = []
    for r in tg.shipped.tc:
        s = sim.day(r.tc)
        ref_items = T.tc_items(r)
        sim_items = {} if s is None else T.tc_items(s)
        for key in T.union_keys(ref_items, sim_items):
            x, y = sim_items.get(key), ref_items.get(key)
            sl, rl = (x or {}).get("Level"), (y or {}).get("Level")
            sc, rc = (x or {}).get("Copies"), (y or {}).get("Copies")
            out.append(Cell("T14", "level", f"{r.tc:03d}|{key}", sl, rl,
                            None if sl is None or rl is None else sl - rl, tc=r.tc, split="validation"))
            out.append(Cell("T14", "copies", f"{r.tc:03d}|{key}", sc, rc, _diff(sc, rc),
                            None if sc is None or rc is None else _rel(float(sc), float(rc)), tc=r.tc,
                            split="validation"))
    return out


def t16(sim: T.Series, tg: T.Targets) -> list[Cell]:
    out = []
    for r in tg.shipped.tc:
        if not 1 <= r.tc <= 4:
            continue
        s = sim.day(r.tc)
        for k in T.STATS:
            sv, rv = (None if s is None else s.value(k)), r.value(k)
            out.append(Cell("T16", "main" if k in T16_MAIN else "small", f"{r.tc:03d}|{k}", sv, rv, _diff(sv, rv),
                            _rel(sv, rv), stat=k, tc=r.tc, split="train",
                            bound=tg.one_clear_reward[k][r.location]))
    return out


def t17(sim: T.Series, tg: T.Targets) -> list[Cell]:
    """Weapon slots against the fitted fixture (report), and no armor slot before TC9 (design 5.1). The
    fixture itself has armor 1 at TC0-8, where no armor item exists yet (unidentifiable); the armor rule
    compares with the design's 0, not with the fixture. The shipped Simulation Data carries no slots, so
    the shipped run has no values here."""
    if tg.slot_schedule is None:
        return []
    out = []
    exported = sim.slots is not None
    for tc, (weapon, armor) in sorted(tg.slot_schedule.items()):
        got = (sim.slots or {}).get(tc)
        sw = None if got is None else got[0]
        out.append(Cell("T17", "weapon", f"{tc:03d}|weapon", sw, weapon, _diff(sw, weapon), tc=tc, split="train",
                        note="reference is fitted (stage4_slot_schedule)", available=exported))
        if tc < SLOT_ARMOR_FROM_TC:
            sa = None if got is None else got[1]
            out.append(Cell("T17", "armor_before_9", f"{tc:03d}|armor", sa, 0, sa, tc=tc, split="train",
                            available=exported))
    return out


def t18(sim: T.Series, tg: T.Targets) -> list[Cell]:
    """X1/G11 on the run: SFF = K2(d), PM = K3(d), TF, TV recomposed from Value (default rounding)."""
    from ..model.snapshots import composition_coefficients

    out = []
    for name, snaps in (("TimeChunkSnapshots", sim.tc), ("Snapshots", sim.early)):
        for s in snaps:
            k2, k3 = composition_coefficients(tg.interpolations, s.tc)
            for stat, f in s.stats.items():
                key = int(StatType[stat])
                v = f["Value"]
                if key in MAIN_INTERPOLATION_STATS:
                    tf = v * (1.0 + k2)
                    exp = (k2, k3, tf, tf * k3 if key in DIRECT_INTERPOLATION_STATS else tf)
                else:
                    exp = (0.0, 1.0, v, v)
                got = (f["StatsFromFeaturesFlat"], f["PercentMultiplier"], f["TotalFlatValue"], f["TotalValue"])
                bad = sum(not (x == y or (_nan(x) and _nan(y))) for x, y in zip(got, exp))
                out.append(Cell("T18", "composition", f"{name}[{s.index:02d}]|{stat}", bad, 0, bad, stat=stat, tc=s.tc,
                                split="structure", note="mismatching columns of SFF, PM, TF, TV"))
    return out


COMPUTE = {"T01": t01, "T02": t02, "T03": t03, "T04": t04, "T05": t05, "T06": t06, "T07": t07, "T09": t09,
           "T10": t10, "T11": t11, "T12": t12, "T13": t13, "T14": t14, "T16": t16, "T17": t17, "T18": t18}


def compute(sim: T.Series, tg: T.Targets, ids: Iterable[str] = TARGET_IDS) -> list[Cell]:
    cells = []
    for tid in ids:
        cells.extend(COMPUTE[tid](sim, tg))
    return sorted(cells, key=Cell.sort_key)


# ---------------------------------------------------------------- tolerances

def load_tolerances(path: str | Path = TOLERANCES) -> dict:
    data = yaml.safe_load(Path(path).read_text(encoding="utf-8"))
    if data.get("schema") != "tufu.calib.tolerances/1":
        raise ValueError(f"{path}: unknown schema {data.get('schema')!r}")
    for tid, spec in data["targets"].items():
        for mode in MODES:
            if mode not in spec:
                raise ValueError(f"{path}: {tid} has no {mode!r} entry")
    return data


def _cell_ok(cell: Cell, crit: Mapping[str, Any]) -> bool | None:
    if not crit:
        return None
    if cell.residual is None:
        return False
    r = cell.residual
    if _nan(r):
        return False
    ok = True
    if "exact" in crit:
        ok &= r == 0
    if "abs_max" in crit:
        ok &= abs(r) <= crit["abs_max"]
    if "rel_max" in crit:
        ok &= cell.rel is not None and cell.rel <= crit["rel_max"]
    if "g20" in crit:  # G20's cell test: |sim - ref| <= x * max(1, |ref|) (simcalc.gpm_matches)
        ok &= not _nan(cell.sim) and not _nan(cell.ref) and gpm_matches(cell.ref, cell.sim, crit["g20"])
    if "abs_or_rel" in crit:
        a, rel = crit["abs_or_rel"]
        ok &= abs(r) <= a or (cell.rel is not None and cell.rel <= rel)
    if "rel_or_bound" in crit:  # |sim - ref| <= max(rel * |ref|, the cell's bound)
        ok &= abs(r) <= max(crit["rel_or_bound"] * abs(cell.ref), cell.bound or 0.0)
    if "bound" in crit:  # |sim - ref| <= the cell's bound
        ok &= abs(r) <= (cell.bound or 0.0)
    if "window" in crit:
        lo, hi = crit["window"]
        ok &= lo <= r <= hi
    if "ranges" in crit:  # T07: within the design range or at most `slack` away (or the day clause)
        island = int(cell.key.split()[-1])
        rng = crit["ranges"].get(island)
        if rng is None or cell.sim is None:
            return False
        near = _interval_distance(cell.sim, rng) <= crit.get("slack", 0)
        days = (crit.get("day_ranges") or {}).get(island)
        if not near and days is not None and cell.tc is not None:
            near = _interval_distance([cell.tc, cell.tc], days) <= crit.get("slack_days", 0)
        ok &= near
    return bool(ok)


@dataclass
class GroupResult:
    target: str
    group: str
    status: str  # pass | fail | report | pending | n/a | unavailable
    n: int
    n_ok: int | None
    rule: str
    detail: str = ""


@dataclass
class Evaluation:
    mode: str
    cells: list[Cell]
    groups: list[GroupResult]
    targets: dict[str, str] = field(default_factory=dict)  # id -> status

    def failing_cells(self) -> list[Cell]:
        return [c for c in self.cells if c.ok is False]

    def status(self, tid: str) -> str:
        return self.targets[tid]


_STATUS_ORDER = ("fail", "unavailable", "pending", "pass", "report", "n/a")


def _rule_text(rule: Mapping[str, Any]) -> str:
    return ", ".join(f"{k}={v}" for k, v in sorted(rule.items()) if k != "note")


def _group_status(tid: str, group: str, cells: list[Cell], rule: Any) -> GroupResult:
    oks = [c.ok for c in cells if c.ok is not None]
    n_ok = sum(oks) if oks else None
    if all(not c.available for c in cells):
        return GroupResult(tid, group, "unavailable", len(cells), n_ok, "", "the run does not export this group")
    if rule in (None, "report"):
        return GroupResult(tid, group, "report", len(cells), n_ok, "report")
    if rule == "n/a":
        return GroupResult(tid, group, "n/a", len(cells), n_ok, "n/a")
    if "pending" in rule:
        return GroupResult(tid, group, "pending", len(cells), n_ok, "pending", str(rule["pending"]))
    if rule.get("report"):
        return GroupResult(tid, group, "report", len(cells), n_ok, _rule_text(rule), str(rule.get("note", "")))
    ok = True
    details = []
    if rule.get("all_ok"):
        bad = [c.key for c in cells if c.ok is False]
        ok &= not bad
        if bad:
            details.append(f"{len(bad)} cells out of tolerance")
    if "frac_ok_min" in rule:
        frac = (n_ok or 0) / len(cells) if cells else 1.0
        ok &= frac >= rule["frac_ok_min"]
        details.append(f"ok fraction {frac:.4f}")
    if "median_rel_max" in rule:
        rels = [c.rel for c in cells if c.rel is not None]
        med = statistics.median(rels) if rels else math.inf
        ok &= med <= rule["median_rel_max"]
        details.append(f"median rel {med:.6g}")
    if "rel_max_all" in rule:
        worst = max((c.rel if c.rel is not None else math.inf for c in cells), default=0.0)
        ok &= worst <= rule["rel_max_all"]
        details.append(f"max rel {worst:.6g}")
    if "max_failing_units" in rule:
        units: dict[str, bool] = {}
        for c in cells:
            units[c.unit] = units.get(c.unit, True) and c.ok is not False
        failing = sum(not v for v in units.values())
        limit = rule["max_failing_units"]
        details.append(f"{failing}/{len(units)} units failing")
        if limit is None:
            return GroupResult(tid, group, "report", len(cells), n_ok, _rule_text(rule),
                               "; ".join(details) + "; limit frozen at the first run")
        ok &= failing <= limit
    if ok and "pending_clause" in rule:  # the rest of the rule holds; a clause set by a later unit is open
        details.append(f"pending: {rule['pending_clause']}")
        return GroupResult(tid, group, "pending", len(cells), n_ok, _rule_text(rule), "; ".join(details))
    return GroupResult(tid, group, "pass" if ok else "fail", len(cells), n_ok, _rule_text(rule), "; ".join(details))


def _group_rule(spec: Mapping[str, Any], mode: str, group: str) -> Any:
    mode_spec = spec[mode]
    return mode_spec if isinstance(mode_spec, str) else (mode_spec or {}).get(group, "report")


def evaluate(cells: Sequence[Cell], tolerances: Mapping[str, Any], mode: str) -> Evaluation:
    if mode not in MODES:
        raise ValueError(f"mode must be one of {MODES}")
    out_cells = []
    by_group: dict[tuple[str, str], list[Cell]] = {}
    specs = tolerances["targets"]
    missing = {(c.target, c.group) for c in cells}
    missing -= {(c.target, c.group) for c in cells if c.available}
    for c in cells:
        spec = specs[c.target]
        crit = (spec.get(f"cells_{mode}") or {}).get(c.group) or (spec.get("cells") or {}).get(c.group) or {}
        if _group_rule(spec, mode, c.group) == "n/a" or (c.target, c.group) in missing:
            crit = {}  # no verdict: a mode the design leaves empty ("—"), or a group the run does not export
        c = replace(c, ok=_cell_ok(c, crit))
        out_cells.append(c)
        by_group.setdefault((c.target, c.group), []).append(c)
    groups = []
    status: dict[str, list[str]] = {}
    for (tid, group), members in sorted(by_group.items()):
        g = _group_status(tid, group, members, _group_rule(specs[tid], mode, group))
        groups.append(g)
        status.setdefault(tid, []).append(g.status)
    targets = {}
    for tid in specs:
        st = status.get(tid)
        if not st:
            targets[tid] = "unavailable" if tid in TARGET_IDS else "not computed"
            continue
        targets[tid] = next(s for s in _STATUS_ORDER if s in st)
    return Evaluation(mode, out_cells, groups, targets)


def evaluate_run(sim_db: str | Path, tg: T.Targets, mode: str, tolerances: Mapping[str, Any] | None = None,
                 ids: Iterable[str] = TARGET_IDS) -> Evaluation:
    con = T.connect_ro(sim_db)
    try:
        sim = T.load_series(con, source=str(sim_db))
    finally:
        con.close()
    return evaluate(compute(sim, tg, ids), tolerances or load_tolerances(), mode)


# ---------------------------------------------------------------- the shipped data as a run

def write_shipped_job(job_dir: str | Path, *, slot_schedule: Mapping[int, tuple[int, int]] | None = None,
                      asset: str | Path | None = None) -> Path:
    """A job directory holding the shipped ``Simulation Data`` the way a run exports it (C1's
    ``simulation_data.json`` read from the asset's YAML and a manifest with the export digests).
    ``simio.ingest`` turns it into the shipped ``sim.sqlite``: the reference run of the metrics. The shipped
    data has no slot counts, so T17 has no values for it; ``slot_schedule`` writes the given slots verbatim
    into a ``snapshots_ext.csv`` (tests of T17 only)."""
    import hashlib
    import json

    from ..unity import schema, simio

    job = Path(job_dir)
    job.mkdir(parents=True, exist_ok=True)
    data = simio.snapshot_json_from_yaml_asset(asset or (simio.extract.ASSETS / "MonoBehaviour" / "Simulation Data.asset"))
    body = json.dumps(data, allow_nan=True).encode()
    (job / "simulation_data.json").write_bytes(body)
    files = {"simulation_data.json": {"sha256": hashlib.sha256(body).hexdigest(), "bytes": len(body),
                                      **simio.bits_digest(data)}}
    if slot_schedule is not None:
        header = [n for n, _ in schema.CSV_SCHEMAS["snapshots_ext.csv"]]
        lines = [",".join(header)]
        for i, s in enumerate(data["TimeChunkSnapshots"]):
            tc = int(s["TimeChunkIndex"])
            weapon, armor = slot_schedule.get(tc, (None, None))
            cells = ["TimeChunkSnapshots", i, i, tc, repr(float(s["Playtime"])), repr(float(s["TotalPlaytime"])),
                     int(s["Location"]), "" if weapon is None else weapon, "" if armor is None else armor, ""]
            lines.append(",".join(str(c) for c in cells))
        text = "\n".join(lines) + "\n"
        (job / "snapshots_ext.csv").write_text(text, encoding="utf-8")
        raw = text.encode()
        files["snapshots_ext.csv"] = {"sha256": hashlib.sha256(raw).hexdigest(), "bytes": len(raw),
                                      "rows": len(lines) - 1}
    manifest = {"schema": schema.MANIFEST_SCHEMA, "job_id": job.name, "exit_code": 0, "run_key": None,
                "code_rev": None, "mode": "shipped", "export": {"schema": schema.EXPORT_SCHEMA, "files": files}}
    (job / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n", encoding="utf-8")
    return job
