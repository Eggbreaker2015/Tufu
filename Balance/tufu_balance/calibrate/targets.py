"""Calibration targets T01-T20 rebuilt from the database (design 5.1; unit P10).

The original values come from the shipped ``Simulation Data`` in ``balance.sqlite``. The same loader
reads a run's ``sim.sqlite`` (C1's ingest gives it the same ``c_SimulationData*`` tables), so a
metric compares two :class:`Series` built by one function.

:func:`forensics_view` is the survey's ``golden_targets.json`` shape (forensics ``targets.py``); the
tests compare it with the frozen fixture ``forensics_targets.json`` value by value, but nothing here
reads that fixture.

Config-derived inputs of the metrics live in :class:`Targets` as well: the interpolations (T18), the
item definitions and shard tables (T11-T14), the largest one-clear reward per stat and location
(T06, T16), the evolution target timings (T13) and, optionally, the fitted slot schedule (T17).
"""

from __future__ import annotations

import json
import math
import sqlite3
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable, Mapping, Sequence

from ..model.enums import ItemSlotType, Rarity, StatType

BALANCE = Path(__file__).resolve().parents[2]
SLOT_SCHEDULE_FIXTURE = BALANCE / "tests" / "golden" / "fixtures" / "stage4_slot_schedule.json"

# forensics targets.py KEYS: the 15 stats the survey followed, in its order.
STATS = ("Health", "HealthRegen", "MovementSpeed", "StabDamageFlat", "CutDamageFlat", "BuckshotDamageFlat",
         "StabDefenceFlat", "CutDefenceFlat", "BuckshotDefenceFlat", "LifestealFlat", "CritChance",
         "CritDamagePercent", "Evasion", "BlockChance", "BlockAmount")
STAT_FIELDS = ("Value", "StatsFromFeaturesFlat", "PercentMultiplier", "TotalFlatValue", "TotalValue", "CurrentGpm")
LISTS = ("TimeChunkSnapshots", "Snapshots")
FULL_DAY_BASE = "c_SimulationDataFull"  # C1: untrimmed day list (timechunk_full.json)
ISLANDS = tuple(range(1, 18))


@dataclass
class Snapshot:
    """One snapshot as the survey read it: raw database numbers (Playtime as stored, not float32)."""

    index: int
    tc: int
    playtime: float
    total_playtime: float
    location: int
    stats: dict[str, dict[str, float]]  # Key__name -> field -> value
    items: list[dict[str, Any]]  # [{"Key", "Level", "Copies"}] in list order
    dps: dict[str, float]  # WeaponType__name -> value, in list order
    defense: dict[str, float]

    def value(self, stat: str) -> float:
        return self.stats[stat]["Value"]


@dataclass
class Series:
    """The two snapshot lists of one SimulationData (shipped or simulated) plus run side tables."""

    tc: list[Snapshot]
    early: list[Snapshot]
    full: list[Snapshot] | None = None  # untrimmed day list when the run exported it
    slots: dict[int, tuple[int | None, int | None]] | None = None  # TC -> (weapon, armor) slots (sim_snapshots_ext)
    items_evolved: dict[tuple[int, str], tuple[bool | None, int | None]] | None = None  # (TC, Key) -> (evolved, level)
    params: dict[str, Any] | None = None  # the job's parameter object (sim_manifest), None for the shipped data
    source: str = ""

    @property
    def tc_days(self) -> list[int]:
        return [s.tc for s in self.tc]

    def day(self, tc: int) -> Snapshot | None:
        """The day snapshot of ``tc``: trimmed list first, then the untrimmed one."""
        for s in self.tc:
            if s.tc == tc:
                return s
        for s in self.full or ():
            if s.tc == tc:
                return s
        return None


def _tables(con: sqlite3.Connection) -> set[str]:
    return {r[0] for r in con.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}


def _rows(con: sqlite3.Connection, sql: str, *args) -> list[dict]:
    cur = con.execute(sql, args)
    names = [d[0] for d in cur.description]
    return [dict(zip(names, r)) for r in cur.fetchall()]


def load_snapshots(con: sqlite3.Connection, list_name: str, base: str = "c_SimulationData") -> list[Snapshot]:
    """forensics ``load.snaps``: one list, ordered by its index column."""
    table = f"{base}__{list_name}"
    ix = f"{list_name}#idx"
    tables = _tables(con)
    if table not in tables:
        return []

    def child(name: str, order: str) -> dict[int, list[dict]]:
        out: dict[int, list[dict]] = {}
        if f"{table}__{name}" not in tables:
            return out
        for r in _rows(con, f'SELECT * FROM "{table}__{name}" ORDER BY "{ix}", "{order}"'):
            out.setdefault(r[ix], []).append(r)
        return out

    stats, items = child("Stats", "Stats#idx"), child("Items", "Items#idx")
    dps, dfn = child("DpsData", "DpsData#idx"), child("DefenseData", "DefenseData#idx")
    out = []
    for s in _rows(con, f'SELECT * FROM "{table}" ORDER BY "{ix}"'):
        i = s[ix]
        out.append(Snapshot(
            index=int(i), tc=int(s["TimeChunkIndex"]), playtime=s["Playtime"], total_playtime=s["TotalPlaytime"],
            location=int(s["Location"]),
            stats={r["Key__name"]: {f: float(r[f]) for f in STAT_FIELDS} for r in stats.get(i, [])},
            items=[{"Key": r["Key"], "Level": r["Level"], "Copies": r["Copies"]} for r in items.get(i, [])],
            dps={r["WeaponType__name"]: r["Value"] for r in dps.get(i, [])},
            defense={r["WeaponType__name"]: r["Value"] for r in dfn.get(i, [])}))
    return out


def load_series(con: sqlite3.Connection, source: str = "") -> Series:
    """Both lists, plus the run side tables a ``sim.sqlite`` may carry (untrimmed days, slots, evolved)."""
    tables = _tables(con)
    full = load_snapshots(con, "TimeChunkSnapshots", FULL_DAY_BASE) or None
    slots = None
    if "sim_snapshots_ext" in tables:
        slots = {}
        for r in _rows(con, "SELECT list, time_chunk_index, unlocked_weapon_slots, unlocked_armor_slots "
                            "FROM sim_snapshots_ext WHERE list = 'TimeChunkSnapshots' ORDER BY \"index\""):
            if r["unlocked_weapon_slots"] is None and r["unlocked_armor_slots"] is None:
                continue
            slots[int(r["time_chunk_index"])] = (r["unlocked_weapon_slots"], r["unlocked_armor_slots"])
        slots = slots or None
    evolved = None
    if "sim_items_ext" in tables and "sim_snapshots_ext" in tables:
        tc_of = {(r["list"], r["index"]): r["time_chunk_index"]
                 for r in _rows(con, 'SELECT list, "index", time_chunk_index FROM sim_snapshots_ext')}
        evolved = {}
        for r in _rows(con, 'SELECT list, "index", key, evolved, evolution_level FROM sim_items_ext '
                            "WHERE list = 'TimeChunkSnapshots' ORDER BY \"index\", item_index"):
            if r["evolved"] is None:
                continue
            tc = tc_of.get((r["list"], r["index"]))
            if tc is not None:
                evolved[(int(tc), str(r["key"]))] = (bool(r["evolved"]), r["evolution_level"])
        evolved = evolved or None
    params = None
    if "sim_manifest" in tables:
        for r in _rows(con, "SELECT manifest FROM sim_manifest"):
            job = (json.loads(r["manifest"] or "null") or {}).get("job") or {}
            if isinstance(job.get("params"), dict):
                params = job["params"]
    return Series(tc=load_snapshots(con, "TimeChunkSnapshots"), early=load_snapshots(con, "Snapshots"),
                  full=full, slots=slots, items_evolved=evolved, params=params, source=source)


# ---------------------------------------------------------------- the survey's target series

def active_minutes_per_day(tc: Sequence[Snapshot]) -> list[dict]:
    return [{"from": a.tc, "to": b.tc, "min_per_day": (b.playtime - a.playtime) / (b.tc - a.tc)}
            for a, b in zip(tc, tc[1:])]


def location_first_seen(tc: Sequence[Snapshot], early: Sequence[Snapshot] = ()) -> dict[int, dict | None]:
    """First day snapshot at or past island L; ``tc_bracket = [previous TC or 0, that TC]``."""
    out: dict[int, dict | None] = {}
    for s in list(tc) + list(early):
        out.setdefault(s.location, None)
    for loc in ISLANDS:
        prev = None
        for s in tc:
            if s.location >= loc:
                out[loc] = {"tc_bracket": [prev.tc if prev else 0, s.tc]}
                break
            prev = s
    return out


def forensics_view(series: Series) -> dict:
    """The survey's ``golden_targets.json`` (forensics ``targets.py``), built from ``series``."""
    def common(s: Snapshot) -> dict:
        return {"Playtime": s.playtime, "TotalPlaytime": s.total_playtime, "Location": s.location,
                "Value": {k: s.stats[k]["Value"] for k in STATS}}

    return {
        "tc_days": series.tc_days,
        "tc": [{"TC": s.tc, **common(s), "Gpm": {k: s.stats[k]["CurrentGpm"] for k in STATS},
                "Dps": dict(s.dps), "Defense": dict(s.defense), "Items": [dict(x) for x in s.items]}
               for s in series.tc],
        "early": [{"i": s.index, **common(s), "Items": [dict(x) for x in s.items]} for s in series.early],
        "active_min_per_day": active_minutes_per_day(series.tc),
        "location_first_seen": location_first_seen(series.tc, series.early),
    }


# ---------------------------------------------------------------- config-derived inputs

@dataclass(frozen=True)
class ItemInfo:
    key: str
    rarity: str  # Rarity name
    slot: str  # ItemSlotType name
    is_armor: bool
    shards: tuple[int, ...]  # shards required for evolution levels 1..max (index k-1)
    target_timings: tuple[float, ...]  # *EvolutionTargetTimings (days), index k-1


@dataclass
class Targets:
    shipped: Series
    items: dict[str, ItemInfo]
    one_clear_reward: dict[str, dict[int, float]]  # stat -> location -> max(Value x Count) up to that island
    one_clear_source: dict[str, dict[int, str | None]] = field(default_factory=dict)  # stat -> location -> spawner Key
    interpolations: Any = None  # model.interpolation.BalanceInterpolationsConfig (T18)
    slot_schedule: dict[int, tuple[int, int]] | None = None  # TC -> fitted (weapon, armor) slots (T17)
    meta: dict = field(default_factory=dict)

    def forensics(self) -> dict:
        return forensics_view(self.shipped)


def load_items(con: sqlite3.Connection) -> dict[str, ItemInfo]:
    from ..model.items import load_item_defs, load_item_tables

    defs, tables = load_item_defs(con), load_item_tables(con)
    cbc = _rows(con, "SELECT * FROM c_CombatBalanceConfig")[0]
    out = {}
    for key, d in sorted(defs.items()):
        rarity = Rarity(d.rarity).name
        column = f"{rarity}{'Armor' if d.is_armor else ''}EvolutionTargetTimings"
        timings = tuple(float(x) for x in json.loads(cbc[column])) if cbc.get(column) else ()
        shards = []
        for level in range(1, tables.max_evolution_level + 1):
            try:
                shards.append(int(tables.shards_requirement(d.is_armor, d.rarity, level)))
            except LookupError:
                break
        out[key] = ItemInfo(key=key, rarity=rarity, slot=ItemSlotType(d.slot_type).name, is_armor=d.is_armor,
                            shards=tuple(shards), target_timings=timings)
    return out


def load_one_clear_rewards(con: sqlite3.Connection, sources: dict | None = None) -> dict[str, dict[int, float]]:
    """Largest ``Value x Count`` one clear of a registered, non-event, non-VIP, non-dummy spawner on islands
    ``<= L`` gives, per stat (design 5.1 T16: the tolerance of the small flat stats). ``sources`` receives
    the Key of the spawner each bound comes from."""
    from ..model.simcalc import load_sim_spawners

    best: dict[str, dict[int, float]] = {k: {loc: 0.0 for loc in ISLANDS} for k in STATS}
    src: dict[str, dict[int, str | None]] = {k: {loc: None for loc in ISLANDS} for k in STATS}
    for s in load_sim_spawners(con):
        d = s.d
        if d.is_event or d.is_vip or d.is_dummy or d.count <= 0:
            continue
        for r in s.stat_rewards:
            name = StatType(r.stat_type).name
            if name not in best:
                continue
            amount = float(r.value) * d.count
            for loc in ISLANDS:
                if loc >= d.location and amount > best[name][loc]:
                    best[name][loc], src[name][loc] = amount, d.key
    if sources is not None:
        sources.update(src)
    return best


def load_slot_schedule(path: str | Path = SLOT_SCHEDULE_FIXTURE) -> dict[int, tuple[int, int]]:
    """T17's reference: the fitted per-day slots (W0 fixture ``stage4_slot_schedule.json``)."""
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    return {int(r["TimeChunkIndex"]): (int(r["weapon_slots"]), int(r["armor_slots"])) for r in data["chunks"]}


def build(con: sqlite3.Connection, *, slot_schedule: Mapping[int, tuple[int, int]] | None = None,
          with_interpolations: bool = True) -> Targets:
    """All targets from the extracted database (read-only connection)."""
    from ..model.data import load_interpolations

    if con.row_factory is None:
        con.row_factory = sqlite3.Row  # the model loaders index rows by name
    shipped = load_series(con, source="balance.sqlite")
    sources: dict = {}
    return Targets(shipped=shipped, items=load_items(con), one_clear_reward=load_one_clear_rewards(con, sources),
                   one_clear_source=sources,
                   interpolations=load_interpolations(con) if with_interpolations else None,
                   slot_schedule=dict(slot_schedule) if slot_schedule is not None else None,
                   meta={"tc_snapshots": len(shipped.tc), "early_snapshots": len(shipped.early)})


def connect_ro(path: str | Path) -> sqlite3.Connection:
    con = sqlite3.connect(f"file:{Path(path)}?mode=ro", uri=True)
    con.row_factory = sqlite3.Row
    return con


def is_nan(x: Any) -> bool:
    return isinstance(x, float) and math.isnan(x)


def tc_items(snapshot: Snapshot) -> dict[str, dict]:
    return {x["Key"]: x for x in snapshot.items}


def union_keys(*maps: Iterable[str]) -> list[str]:
    seen: dict[str, None] = {}
    for m in maps:
        for k in m:
            seen.setdefault(k, None)
    return list(seen)
