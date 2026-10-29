"""Spawner definitions and their effective charges and cooldowns (fresh save), plus per-island bosses.

Ports ``CodeBase/Enemy/EnemySpawner.cs`` (``CanUseSpawnerCooldownsAb``, ``GetBaseCooldown``,
``ComputeEffectiveChargesBeforeBigRespawn``, ``ChargesTotal``, ``ChargesBase``),
``CodeBase/Meta/SpawnerTimerCalculator.cs``, ``CodeBase/Meta/SpawnerChargesCalculator.cs`` and
``SimulationSystem.GetSpawnerChargesLimit``. AB tables come from :mod:`.ab`.

Spawner identity is ``SpawnerId = (scene, file_id)`` (design 1.5): ``Key`` is not unique (717 rows,
716 keys; ``Eventspawner_Boss_79e4...`` appears twice, once on an inactive GameObject that never
registers at runtime). Rows also carry the hierarchy path, which with ``Key`` is what the C# side sees.

Meta stats are taken at a fresh save (no modifiers): ``MetaStat.GetValue`` is then ``BaseValue`` for
every parameter (``Meta/MetaStat.cs:152-174``).
"""

from __future__ import annotations

import csv
import io
import json
import math
import sqlite3
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable, Mapping

from . import ab
from .enums import Rarity
from .f32 import f32, fdiv, fsub
from .registry import Provenance, implements

SpawnerId = tuple[str, int]  # (scene, file_id)


@dataclass(frozen=True)
class SpawnerDef:
    """One ``EnemySpawner`` from the ``spawners`` table (original field names in comments)."""

    scene: str
    file_id: int
    go_path: str
    go_name: str
    key: str  # Key (not unique)
    active: bool  # GameObject active in hierarchy: registers in EnemiesModel.Spawners at runtime
    enabled: bool
    location: int  # LocationNumber
    count: int  # Count
    cooldown: float  # Cooldown (float32)
    charges_before_big_respawn: int  # ChargesBeforeBigRespawn
    is_event: bool
    is_vip: bool
    is_dummy: bool
    is_elite: bool  # IsEliteBoss
    is_static: bool
    is_days: bool
    dafc: bool  # DisableAfterFirstClear
    item_reward: str | None  # ItemReward asset name
    item_rarity: Rarity | None
    slot_reward: str | None
    gate_next_locations: tuple[int, ...] = ()  # NextLocationNumber of each Gate in config.Gates
    optimum_time: float = 0.0

    @property
    def spawner_id(self) -> SpawnerId:
        return (self.scene, self.file_id)


@dataclass(frozen=True)
class MetaLevels:
    """``MetaStat.GetValue(param)`` for the three stats the spawners read; a fresh save by default."""

    items_rarity_charges: Mapping[int, float] | float = 0.0  # by rarity param (Rarity + 1)
    items_rarity_timer: Mapping[int, float] | float = 1.0  # by rarity param
    map_spawner_timer_reduce: Mapping[int, float] | float = 0.0  # by LocationNumber

    @staticmethod
    def _get(value, param: int) -> float:
        return float(value.get(param, 0.0) if isinstance(value, Mapping) else value)

    def rarity_charges(self, param: int) -> float:
        return self._get(self.items_rarity_charges, param)

    def rarity_timer(self, param: int) -> float:
        return self._get(self.items_rarity_timer, param)

    def timer_reduce(self, location: int) -> float:
        return self._get(self.map_spawner_timer_reduce, location)


def load_meta_levels(con: sqlite3.Connection) -> MetaLevels:
    """Base values from ``Meta Stats Config`` (no upgrades bought)."""
    rows = {r[0]: float(r[1]) for r in con.execute(
        "SELECT Key, BaseValue FROM c_MetaStatsConfig__Stats WHERE Key IN "
        "('items_rarity_charges', 'items_rarity_timer', 'map_spawner_timer_reduce')")}
    return MetaLevels(rows["items_rarity_charges"], rows["items_rarity_timer"], rows["map_spawner_timer_reduce"])


def _rows(con: sqlite3.Connection, sql: str) -> list[dict]:
    cur = con.execute(sql)
    cols = [d[0] for d in cur.description]
    return [dict(zip(cols, row)) for row in cur]


def _gate_next_locations(con: sqlite3.Connection) -> dict[SpawnerId, tuple[int, ...]]:
    gates = {(r[0], int(r[1])): int(r[2]) for r in con.execute("SELECT scene, file_id, NextLocationNumber FROM s_Gate")}
    out: dict[SpawnerId, tuple[int, ...]] = {}
    for scene, file_id, refs in con.execute('SELECT scene, file_id, "config.Gates" FROM s_EnemySpawner'):
        nexts = []
        for ref in json.loads(refs or "[]"):
            if isinstance(ref, str) and ref.startswith("local:"):
                target = gates.get((scene, int(ref.split(":", 1)[1])))
                if target is None:
                    raise ValueError(f"spawner {(scene, file_id)}: gate reference {ref} is not a Gate component")
                nexts.append(target)
        out[(scene, int(file_id))] = tuple(nexts)
    return out


def load_spawners(con: sqlite3.Connection) -> list[SpawnerDef]:
    """All spawner rows (717, inactive ones included) ordered by ``(scene, file_id)``."""
    rarity = {str(r[0]).rsplit("/", 1)[-1].removesuffix(".asset"): Rarity(int(r[1]))
              for r in con.execute("SELECT asset, Rarity FROM items")}
    gates = _gate_next_locations(con)
    out = []
    for r in _rows(con, "SELECT * FROM spawners ORDER BY scene, file_id"):
        item = r["ItemReward"] or None
        if item is not None and item not in rarity:
            raise ValueError(f"spawner {r['go_path']}: ItemReward {item!r} not in the items table")
        sid = (r["scene"], int(r["file_id"]))
        out.append(SpawnerDef(
            scene=r["scene"], file_id=int(r["file_id"]), go_path=r["go_path"], go_name=r["go_name"],
            key=r["Key"], active=bool(r["active"]), enabled=bool(r["enabled"]),
            location=int(r["LocationNumber"]), count=int(r["Count"]), cooldown=f32(r["Cooldown"] or 0),
            charges_before_big_respawn=int(r["ChargesBeforeBigRespawn"] or 0),
            is_event=bool(r["IsEvent"]), is_vip=bool(r["IsVip"]), is_dummy=bool(r["IsDummy"]),
            is_elite=bool(r["IsEliteBoss"]), is_static=bool(r["IsStatic"]), is_days=bool(r["IsDays"]),
            dafc=bool(r["DisableAfterFirstClear"]), item_reward=item,
            item_rarity=rarity[item] if item else None, slot_reward=r["SlotReward"] or None,
            gate_next_locations=gates.get(sid, ()), optimum_time=float(r["OptimumTime"] or 0.0)))
    return out


def registered(spawners: Iterable[SpawnerDef]) -> list[SpawnerDef]:
    """Spawners whose ``Start`` runs and registers them (GameObject active in hierarchy): 716."""
    return [s for s in spawners if s.active]


# ---------------------------------------------------------------- EnemySpawner charges and cooldown

@implements("spawners.ab_spawner_override_predicate", provenance=Provenance.PORT,
            code_ref="CodeBase/Enemy/EnemySpawner.cs:717-720")
def uses_spawner_cooldowns_ab(s: SpawnerDef) -> bool:
    """``CanUseSpawnerCooldownsAb``: ``!IsEvent && !IsVip && ItemReward == null && Cooldown >= 10f``."""
    return not s.is_event and not s.is_vip and s.item_reward is None and s.cooldown >= f32(10.0)


def _rarity_param(s: SpawnerDef) -> int:
    return int(s.item_rarity) + 1  # RarityStatParam.From


@implements("spawners.base_cooldown", provenance=Provenance.PORT,
            code_ref="CodeBase/Enemy/EnemySpawner.cs:691-695; CodeBase/Meta/SpawnerTimerCalculator.cs:9-24")
def base_cooldown(s: SpawnerDef, cooldowns: ab.SpawnerCooldownsData, meta: MetaLevels = MetaLevels()):
    """``GetBaseCooldown`` (float32 seconds): AB table or ``Cooldown``, then ``SpawnerTimerCalculator.Apply``."""
    seconds = (ab.spawner_cooldown_seconds(cooldowns, s.is_elite, s.location)
               if uses_spawner_cooldowns_ab(s) else f32(s.cooldown))
    if seconds <= 0:
        return seconds
    if s.item_reward is not None:
        return f32(float(seconds) / max(1.0, meta.rarity_timer(_rarity_param(s))))
    if not s.is_event:
        return max(f32(0.0), fsub(seconds, f32(meta.timer_reduce(s.location))))
    return seconds


def small_cooldown_seconds(s: SpawnerDef, cooldowns: ab.SpawnerCooldownsData, meta: MetaLevels = MetaLevels()) -> int:
    """``GetSmallCooldownSeconds``: ``(int)Math.Ceiling(GetBaseCooldown())``."""
    return int(math.ceil(float(base_cooldown(s, cooldowns, meta))))


@implements("spawners.effective_charges", provenance=Provenance.PORT,
            code_ref="CodeBase/Enemy/EnemySpawner.cs:697-715; CodeBase/Meta/SpawnerChargesCalculator.cs:23-41")
def effective_charges(s: SpawnerDef, cooldowns: ab.SpawnerCooldownsData, meta: MetaLevels = MetaLevels()) -> int:
    """``ComputeEffectiveChargesBeforeBigRespawn``: AB charges, else ``ChargesBeforeBigRespawn`` plus
    ``(int)items_rarity_charges.GetValue(rarity param)`` for item spawners."""
    if uses_spawner_cooldowns_ab(s):
        return ab.spawner_charges(cooldowns, s.is_elite, s.location, s.count)
    bonus = int(meta.rarity_charges(_rarity_param(s))) if s.item_reward is not None else 0
    return s.charges_before_big_respawn + bonus


def charges_enabled(s: SpawnerDef, cooldowns: ab.SpawnerCooldownsData, meta: MetaLevels = MetaLevels()) -> bool:
    """``IsChargesEnabled``: ``!IsEvent && effective > 0``."""
    return not s.is_event and effective_charges(s, cooldowns, meta) > 0


def charges_total(s: SpawnerDef, cooldowns: ab.SpawnerCooldownsData, meta: MetaLevels = MetaLevels()) -> int:
    """``ChargesTotal``: effective charges when charges are enabled, else 0."""
    return effective_charges(s, cooldowns, meta) if charges_enabled(s, cooldowns, meta) else 0


def charges_base(s: SpawnerDef, cooldowns: ab.SpawnerCooldownsData, meta: MetaLevels = MetaLevels()) -> int:
    """``ChargesBase``: AB charges or ``ChargesBeforeBigRespawn`` (no meta bonus) when enabled, else 0."""
    if not charges_enabled(s, cooldowns, meta):
        return 0
    if uses_spawner_cooldowns_ab(s):
        return ab.spawner_charges(cooldowns, s.is_elite, s.location, s.count)
    return s.charges_before_big_respawn


@implements("simulation.spawner_charges_limit", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/Systems/SimulationSystem.cs:608-612", va="0x13e83ec")
def charges_limit(s: SpawnerDef, cooldowns: ab.SpawnerCooldownsData, meta: MetaLevels = MetaLevels()) -> int:
    """``GetSpawnerChargesLimit``: ``DisableAfterFirstClear ? Count : ChargesTotal``."""
    return s.count if s.dafc else charges_total(s, cooldowns, meta)


# ---------------------------------------------------------------- readiness (new save)

@implements("spawners.ready_alive_new_save", provenance=Provenance.INFERRED,
            code_ref="CodeBase/Enemy/EnemySpawner.cs:746,860-863,922-925")
def alive_on_new_save(s: SpawnerDef) -> bool:
    """Whether a registered spawner has units when the game is ready on a new save.

    Event spawners never initialize (``TryInitializeEventSpawner`` is a stub returning false), VIP
    spawners refuse below location 2, ``IsDummy`` refuses always, ``Count = 0`` (BossTower) spawns none.
    Every other spawner fills on bootstrap (Runbook 6.1: 699 of 716).
    """
    return s.active and not (s.is_event or s.is_vip or s.is_dummy or s.count <= 0)


def no_unit_reason(s: SpawnerDef) -> str | None:
    if not s.active:
        return "unregistered"
    for flag, name in ((s.is_event, "event"), (s.is_vip, "vip"), (s.is_dummy, "dummy"), (s.count <= 0, "count0")):
        if flag:
            return name
    return None


@dataclass(frozen=True)
class ReadySpawner:
    """Expected runtime values of one registered spawner at the readiness check (DP6)."""

    scene: str
    file_id: int
    go_path: str
    key: str
    charges_total: int
    base_cooldown: float  # float32 widened to double
    charges_limit: int
    uses_ab: bool
    alive: bool

    @property
    def spawner_id(self) -> SpawnerId:
        return (self.scene, self.file_id)


READY_CSV_COLUMNS = ("scene", "file_id", "go_path", "key", "charges_total", "base_cooldown", "charges_limit",
                     "uses_ab", "alive")


def expected_ready_spawners(variant: ab.Variant = ab.DEFAULT_VARIANT, *, con: sqlite3.Connection | None = None,
                            spawners: Iterable[SpawnerDef] | None = None, ab_config: ab.ABTestConfig | None = None,
                            meta: MetaLevels | None = None) -> dict[SpawnerId, ReadySpawner]:
    """``ChargesTotal`` and ``BaseCooldown`` of every registered spawner (716) for an AB variant, by SpawnerId.

    Give ``con`` (the extracted database) or all of ``spawners``, ``ab_config`` and ``meta``.
    """
    if spawners is None or ab_config is None or meta is None:
        if con is None:
            raise ValueError("pass con, or spawners, ab_config and meta")
        spawners = load_spawners(con) if spawners is None else spawners
        ab_config = ab.load_ab_config(con) if ab_config is None else ab_config
        meta = load_meta_levels(con) if meta is None else meta
    cooldowns = ab_config.spawner_cooldowns(variant)
    out: dict[SpawnerId, ReadySpawner] = {}
    for s in registered(spawners):
        out[s.spawner_id] = ReadySpawner(
            scene=s.scene, file_id=s.file_id, go_path=s.go_path, key=s.key,
            charges_total=charges_total(s, cooldowns, meta), base_cooldown=float(base_cooldown(s, cooldowns, meta)),
            charges_limit=charges_limit(s, cooldowns, meta), uses_ab=uses_spawner_cooldowns_ab(s),
            alive=alive_on_new_save(s))
    return out


def ready_spawners_csv(rows: Mapping[SpawnerId, ReadySpawner] | Iterable[ReadySpawner]) -> str:
    """CSV text for the request's ``expected_ready_spawners`` file, sorted by SpawnerId; floats as ``repr``."""
    items = rows.values() if isinstance(rows, Mapping) else rows
    buf = io.StringIO()
    writer = csv.writer(buf, lineterminator="\n")
    writer.writerow(READY_CSV_COLUMNS)
    for r in sorted(items, key=lambda r: r.spawner_id):
        writer.writerow([r.scene, r.file_id, r.go_path, r.key, r.charges_total, repr(r.base_cooldown),
                         r.charges_limit, int(r.uses_ab), int(r.alive)])
    return buf.getvalue()


def write_expected_ready_spawners(path: str | Path, rows: Mapping[SpawnerId, ReadySpawner]) -> Path:
    path = Path(path)
    path.write_text(ready_spawners_csv(rows), encoding="utf-8")
    return path


# ---------------------------------------------------------------- bosses

@implements("simulation.current_boss", provenance=Provenance.INFERRED)
def boss_for_location(location: int, spawners: Iterable[SpawnerDef]) -> SpawnerDef | None:
    """``currentBoss`` while ``location`` is the last unlocked island (design P17, B11).

    The spawner on island ``location`` with a gate whose ``NextLocationNumber == location + 1``;
    None if there is none (island 17). Gates elsewhere can also lead to ``location + 1`` (islands
    11, 12, 13 carry back-gates to 8, 7, 2); those are not on the current island and are ignored.
    """
    hits = [s for s in spawners if s.location == location and (location + 1) in s.gate_next_locations]
    if len(hits) > 1:
        raise ValueError(f"island {location}: {len(hits)} spawners gate to {location + 1}: "
                         f"{[s.go_path for s in hits]}")
    return hits[0] if hits else None


def bosses_by_location(spawners: Iterable[SpawnerDef], locations: Iterable[int] = range(1, 18)) -> dict[int, SpawnerDef | None]:
    spawners = list(spawners)
    return {n: boss_for_location(n, spawners) for n in locations}
