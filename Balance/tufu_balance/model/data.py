"""Load model inputs from the extracted database (``Balance/out/balance.sqlite``).

Only reads; never writes. Tables used:

* ``c_SimulationData__Snapshots`` / ``__TimeChunkSnapshots`` and their ``__Stats``,
  ``__DpsData``, ``__DefenseData``, ``__Items`` child tables (list order = ``#idx``).
* ``c_SimulationConfig`` (early-spawner offset settings, and its own ``Snapshots`` /
  ``TimeChunkSnapshots`` columns, which must be the empty list ``[]``; the extractor creates no
  child table for them, so a non-empty value raises instead of being silently ignored).
* ``config_arrays`` (``Combat Balance Config`` / ``IslandTargetDays``).
* ``c_CombatBalanceConfig__Interpolations.Interpolations``.
* ``c_LocationsConfig__Locations`` (PreferredWeaponType).
* ``c_ArmorConfig__Armors`` (by asset name, list order kept).
* ``spawners`` (one row per EnemySpawner, original field names, ``stat.*`` base values).

The bake can run on another simulation's snapshots: ``load_bake_context(con, simulation_data=...)``
takes a :class:`SimulationData`, a connection to (or the path of) a database with the same
``c_SimulationData__*`` tables, e.g. the ``sim.sqlite`` that ``tufu_balance.unity`` imports from a
run. Paths are opened read-only.
"""

from __future__ import annotations

import sqlite3
from collections import defaultdict
from pathlib import Path

from .bake import ArmorEntry, BakeContext, SpawnerBakeInput
from .f32 import f32
from .interpolation import BalanceInterpolation, BalanceInterpolationsConfig, Rounding
from .snapshots import SimulationData, SimulationItem, SimulationSnapshot, SimulationStat

DEFAULT_DB = Path(__file__).resolve().parents[2] / "out" / "balance.sqlite"


def connect(db_path: str | Path | None = None) -> sqlite3.Connection:
    con = sqlite3.connect(f"file:{Path(db_path or DEFAULT_DB)}?mode=ro", uri=True)
    con.row_factory = sqlite3.Row
    return con


def _load_snapshot_list(con: sqlite3.Connection, list_name: str) -> list[SimulationSnapshot]:
    base = f"c_SimulationData__{list_name}"
    idx = f'"{list_name}#idx"'
    stats = defaultdict(list)
    for r in con.execute(f'SELECT * FROM {base}__Stats ORDER BY {idx}, "Stats#idx"'):
        stats[r[f"{list_name}#idx"]].append(SimulationStat(
            key=int(r["Key"]), value=float(r["Value"]), stats_from_features_flat=float(r["StatsFromFeaturesFlat"]),
            percent_multiplier=float(r["PercentMultiplier"]), total_flat_value=float(r["TotalFlatValue"]),
            total_value=float(r["TotalValue"]), current_gpm=float(r["CurrentGpm"])))
    dps = defaultdict(list)
    for r in con.execute(f'SELECT * FROM {base}__DpsData ORDER BY {idx}, "DpsData#idx"'):
        dps[r[f"{list_name}#idx"]].append((int(r["WeaponType"]), float(r["Value"])))
    dfn = defaultdict(list)
    for r in con.execute(f'SELECT * FROM {base}__DefenseData ORDER BY {idx}, "DefenseData#idx"'):
        dfn[r[f"{list_name}#idx"]].append((int(r["WeaponType"]), float(r["Value"])))
    items = defaultdict(list)
    for r in con.execute(f'SELECT * FROM {base}__Items ORDER BY {idx}, "Items#idx"'):
        items[r[f"{list_name}#idx"]].append(SimulationItem(str(r["Key"]), int(r["Level"]), int(r["Copies"])))
    out = []
    for r in con.execute(f"SELECT * FROM {base} ORDER BY {idx}"):
        i = r[f"{list_name}#idx"]
        out.append(SimulationSnapshot(
            playtime=f32(r["Playtime"]), total_playtime=f32(r["TotalPlaytime"]), location=int(r["Location"]),
            time_chunk_index=int(r["TimeChunkIndex"]), stats=stats[i], items=items[i], dps_data=dps[i],
            defense_data=dfn[i], source_index=int(i)))
    return out


def load_simulation_data(con: sqlite3.Connection) -> SimulationData:
    return SimulationData(snapshots=_load_snapshot_list(con, "Snapshots"),
                          time_chunk_snapshots=_load_snapshot_list(con, "TimeChunkSnapshots"))


def load_island_target_days(con: sqlite3.Connection) -> list:
    rows = con.execute("SELECT idx, value FROM config_arrays WHERE owner LIKE '%Combat Balance Config.asset' "
                       "AND field_path = 'IslandTargetDays' ORDER BY idx").fetchall()
    return [f32(r["value"]) for r in rows]


def load_interpolations(con: sqlite3.Connection) -> BalanceInterpolationsConfig:
    rows = con.execute('SELECT * FROM "c_CombatBalanceConfig__Interpolations.Interpolations" '
                       'ORDER BY "Interpolations.Interpolations#idx"').fetchall()
    return BalanceInterpolationsConfig([
        BalanceInterpolation(use=bool(int(r["Use"])), name=str(r["Name"]), type=int(r["Type"]),
                             timing_points=r["TimingPoints"] or "", coef_points=r["CoefPoints"] or "",
                             island_points=r["IslandPoints"] or "") for r in rows])


def load_preferred_weapon_types(con: sqlite3.Connection) -> dict[int, int]:
    out: dict[int, int] = {}
    for r in con.execute('SELECT Number, PreferredWeaponType FROM c_LocationsConfig__Locations ORDER BY "Locations#idx"'):
        out.setdefault(int(r["Number"]), int(r["PreferredWeaponType"]))  # .First(Number == L)
    return out


def load_armor_configs(con: sqlite3.Connection) -> dict[str, list[ArmorEntry]]:
    out: dict[str, list[ArmorEntry]] = defaultdict(list)
    for r in con.execute('SELECT asset_name, Type, Resist FROM c_ArmorConfig__Armors ORDER BY asset, "Armors#idx"'):
        out[str(r["asset_name"])].append(ArmorEntry(int(r["Type"]), f32(r["Resist"])))
    return dict(out)


def config_snapshot_lists_empty(con: sqlite3.Connection) -> dict[str, bool]:
    """Whether SimulationConfig's own ``Snapshots`` / ``TimeChunkSnapshots`` are empty (read from the DB)."""
    sc = con.execute("SELECT Snapshots, TimeChunkSnapshots FROM c_SimulationConfig").fetchone()
    return {k: str(sc[k]).strip() == "[]" for k in ("Snapshots", "TimeChunkSnapshots")}


def _simulation_data(source) -> SimulationData:
    if isinstance(source, SimulationData):
        return source
    if isinstance(source, sqlite3.Connection):
        return load_simulation_data(source)
    if isinstance(source, (str, Path)):
        if not Path(source).exists():
            raise FileNotFoundError(source)
        sim = connect(source)
        try:
            return load_simulation_data(sim)
        finally:
            sim.close()
    raise TypeError(f"simulation_data must be SimulationData, sqlite3.Connection or a path, not {type(source).__name__}")


def load_bake_context(con: sqlite3.Connection, simulation_data=None,
                      rounding: Rounding | str | None = None) -> BakeContext:
    """Bake inputs from ``con``; snapshots from ``simulation_data`` when given (see module doc).

    ``rounding``: K4/K5 interpolation rounding (``None`` = ``interpolation_default.json``).
    """
    sc = con.execute("SELECT * FROM c_SimulationConfig").fetchone()
    empty = config_snapshot_lists_empty(con)
    if not empty["TimeChunkSnapshots"]:
        # GetSnapshotForBake would use these instead of Simulation Data; they are not extracted.
        raise NotImplementedError("SimulationConfig.TimeChunkSnapshots is not empty; extract it before baking")
    return BakeContext(
        simulation_data=load_simulation_data(con) if simulation_data is None else _simulation_data(simulation_data),
        interpolations=load_interpolations(con),
        island_target_days=load_island_target_days(con),
        preferred_weapon_type=load_preferred_weapon_types(con),
        early_offset_start_location=int(sc["EarlySpawnerOptimumTimeOffsetStartLocation"]),
        early_offset_relative_part=f32(sc["EarlySpawnerOptimumTimeOffsetRelativePart"]),
        early_offset_multiplier=f32(sc["EarlySpawnerOptimumTimeOffsetMultiplier"]),
        config_time_chunk_snapshots=[],  # checked above: SimulationConfig.TimeChunkSnapshots == []
        rounding=rounding,
    )


def _num(v, default: float | None = 0.0) -> float | None:
    if v is None or v == "":
        return default
    return float(v)


def load_spawner_rows(con: sqlite3.Connection) -> list[sqlite3.Row]:
    return con.execute("SELECT rowid AS _rowid, * FROM spawners ORDER BY scene, file_id").fetchall()


def spawner_input_from_row(r: sqlite3.Row, armor_configs: dict[str, list[ArmorEntry]]) -> SpawnerBakeInput:
    return SpawnerBakeInput(
        key=str(r["Key"]), name=str(r["go_name"]), location_number=int(r["LocationNumber"]),
        is_days=bool(int(r["IsDays"])), optimum_time=f32(_num(r["OptimumTime"])),
        relative_optimum_time=f32(_num(r["RelativeOptimumTime"])),
        average_fight_time=f32(_num(r["AverageFightTime"])), count=int(r["Count"]),
        difficulty=f32(_num(r["Difficulty"], 1.0)), armors=list(armor_configs.get(str(r["ArmorConfig"]), [])),
        health_base=_num(r["stat.Health"], None), damage_base=_num(r["stat.Damage"], None),
        attack_cooldown_base=_num(r["stat.AttackCooldown"], None),  # None: no such stat (bake raises BakeError)
        spawner_id=(str(r["scene"]), int(r["file_id"])),
    )
