"""Per-spawner economics of ``SimulationSystem`` (the recovered part of the bot), as an independent oracle.

Ports of ``CodeBase/Simulation/Systems/SimulationSystem.cs`` at commit 6af011f (``SS`` below). Every
recovered method body there carries its native ELF VA; each port names it:

* ``GetRPMConfig`` 0x13e13dc, ``GetFastRPMConfig`` 0x13e1468 (:func:`.ab.fast_rpm_config`),
  ``GetSimulationEnemyMultiplier`` 0x13e13b0, ``GetCachedEnemyHpDamageMultiplier`` 0x13e68ac;
* ``RebuildPerFrameCaches`` 0x13e5bc0 -- :func:`combat_caches`;
* ``CalculateDpsData`` 0x13e4478, ``CalculateDefenseData`` 0x13e5104 -- :func:`calculate_dps_data`,
  :func:`calculate_defense_data`;
* ``GetBestArmorAgainstDps`` 0x13f2cf8, ``GetDpsAgainstSpawner`` 0x13ef248 (fast branch),
  ``GetSpawnerDps`` 0x13ef8d4 (fast branch), ``GetPlayerAbilityDpsMultiplier`` 0x13f2f98,
  ``GetPlayerAblityEhpMultiplier`` 0x13efd18, ``GetBattleRegen`` 0x13efd54, ``GetResourceValue`` 0x13efd98,
  ``CalculateSpawnerRewardValue`` 0x13e0cb8;
* ``CalculateSpawnerData`` 0x13e6074 -- :func:`spawner_data` / :func:`calculate_spawner_data`;
* ``CalculateRPMValueTimeChunk`` 0x13e7a00 / 0x13e7cf4 -- :func:`calculate_rpm_value_time_chunk`;
* ``CalculateSoulRPMValues`` 0x13e6dd0, ``CalculateSoulRPMValue`` 0x13e8424, ``SetSoulRPMValue`` 0x13e86b8.

Numeric semantics follow the C# declarations: ``double`` arithmetic in the original operation order,
``float`` literals and fields widened where the source widens them (``(double)0.1f``, ``(double)0.2f``,
``1f - armor.Resist``, the float32 count factor), interpolation coefficients from
:meth:`.interpolation.BalanceInterpolationsConfig.total_coefficient` (the oracle's default rounding).
Comparisons keep the source's strictness, and ``Math.Max``/``Math.Min`` keep .NET's NaN rule.

The player is a :class:`PlayerState`: what ``GetForecastStatValue`` returns per stat, the cached
DpsData/DefenseData lists (``currentDpsData`` / ``currentDefenseData``), the unlocked island and the
balance timing. :func:`snapshot_state` builds it from a Simulation Data snapshot. The day-change order
(design 4.4, ``RpmFirst``) decides which K2/K3 the three direct stats carry when a day snapshot's
CurrentGpm is computed: ``RpmFirst`` recomputes RPM before the interpolation modifier moves to the new
day, so Health/HealthRegen/LifestealFlat use K2/K3(d-1) while K4/K5 are read live at d.

Spawner order: ``sortedSpawners`` is ``EnemiesModel.Spawners`` (registration order) stably sorted by
``LocationNumber``; registration order is not in the data, so here the tie order is ``(scene, file_id)``.
Only floating-point summation order inside a location differs from the game.
"""

from __future__ import annotations

import math
import sqlite3
import sys
from dataclasses import dataclass, field, replace
from typing import Iterable, Mapping, Sequence

import numpy as np

from . import ab
from . import spawners as sp
from .bake import ArmorEntry
from .defense import apply_damage_defense, get_battle_regen, weapon_type_to_defence
from .enums import BalanceInterpolationType, Rarity, StatType, WeaponType
from .f32 import ddiv, f32
from .interpolation import BalanceInterpolationsConfig
from .items import ItemDef, ItemState, ItemTables, armor as item_armor, damage as item_damage, dps as item_dps
from .registry import Provenance, implements
from .snapshots import DIRECT_INTERPOLATION_STATS, SimulationSnapshot, composition_coefficients, compose_stat
from .stats import calculate_specific_value, clamp_specific_value
from .timeaxis import TimeAxisConfig, load_time_axis_config, optimum_time_over_cap

SS = "CodeBase/Simulation/Systems/SimulationSystem.cs"

RPM_CONFIG = (70.0, 50.0, 2.0, 2.0)  # GetRPMConfig: (move distance, regen factor, fight factor, path factor)
RPM_LOCATION_MULTIPLIERS = (2.0, 2.0)  # CalculateRPMValueTimeChunk: islands 1-2, non-elite
FIGHT_TIME_LIMIT = 300.0  # CalculateSpawnerData keeps fightTime < 300
DPS_EPSILON = float(f32(0.1))  # (double)0.1f added to player and enemy DPS
LOW_HEALTH_RPS_FACTOR = float(f32(0.2))  # (double)0.2f
BOSS_REWARD_VALUE = 1000000.0
ITEM_REWARD_VALUE = 10000.0
RPM_FLOOR = 0.5
SOUL_RPM_FIGHT_SECONDS = 10.0
SOUL_RPM_FLOOR = 0.03

WEAPON_TYPES = tuple(WeaponType)  # Enum.GetValues(typeof(WeaponType))
STAT_TYPES = tuple(StatType)  # Enum.GetValues(typeof(StatType))

# Day-change order (design 4.4, P07): which day the three direct stats' K2/K3 and the live K4/K5 use
# when a day snapshot's RPM is recomputed. Offsets are relative to the snapshot's day d (clamped at 0).
RPM_FIRST = "RpmFirst"  # K2/K3(d-1), K4/K5(d): what the shipped data shows (G20 1380/1380)
MODIFIER_FIRST = "ModifierFirst"  # K2/K3(d), K4/K5(d): ablation
DAY_ORDERS = {RPM_FIRST: (1, 0), MODIFIER_FIRST: (0, 0)}


@implements("simulation.spawner_fight_model", provenance=Provenance.PORT, code_ref=f"{SS}:226-230", va="0x13e13dc")
def rpm_config() -> tuple[float, float, float, float]:
    """``GetRPMConfig``: the original tuple constants in their order."""
    return RPM_CONFIG


def cs_max(a: float, b: float) -> float:
    """.NET ``Math.Max(double, double)``: NaN if either argument is NaN."""
    if a != a:
        return a
    if b != b:
        return b
    return a if a > b else b


def cs_min(a: float, b: float) -> float:
    """.NET ``Math.Min(double, double)``: NaN if either argument is NaN."""
    if a != a:
        return a
    if b != b:
        return b
    return a if a < b else b


# ---------------------------------------------------------------------------------------------- config


@dataclass(frozen=True)
class StatReward:
    """``StatModifier`` of ``EnemySpawnerConfig.StatRewards`` (``Value`` is float32)."""

    stat_type: int
    value: np.float32


@dataclass(frozen=True)
class SimSpawner:
    """A registered spawner with the ``EnemySpawnerConfig`` fields the economics read."""

    d: sp.SpawnerDef
    stats: tuple[tuple[int, float], ...]  # Config.Stats in list order: (Type, BaseValue double)
    stat_rewards: tuple[StatReward, ...]  # Config.StatRewards in list order
    armors: tuple[ArmorEntry, ...]  # Config.ArmorConfig.Armors in list order
    difficulty: np.float32
    soul_reward: str | None
    weapon_type: int
    item_key: str | None = None  # ItemReward's ItemConfig.Key (ItemReward itself is the asset name)

    @property
    def spawner_id(self) -> sp.SpawnerId:
        return self.d.spawner_id

    def base_stat(self, stat_type: int) -> float:
        """``Config.Stats.FirstOrDefault(stat => stat.Type == t).BaseValue`` (a missing stat is a C# NRE)."""
        for t, v in self.stats:
            if t == int(stat_type):
                return v
        raise LookupError(f"spawner {self.d.go_path}: no {StatType(int(stat_type)).name} stat (NullReferenceException)")

    def reward(self, stat_type: int) -> StatReward | None:
        for r in self.stat_rewards:
            if r.stat_type == int(stat_type):
                return r
        return None


@dataclass
class SimCalcContext:
    """Everything the ported methods read besides the player: configs, AB data (one variant), meta."""

    spawners: list[SimSpawner]  # sortedSpawners
    interpolations: BalanceInterpolationsConfig
    preferred_weapon_type: Mapping[int, int]  # LocationsConfig: Number -> PreferredWeaponType
    ab_config: ab.ABTestConfig
    variant: ab.Variant = ab.DEFAULT_VARIANT
    meta: sp.MetaLevels = field(default_factory=sp.MetaLevels)
    soul_drop_bonus: float = 0.0  # MetaStat soul_drop_bonus.GetValue(0)
    souls_rarity_amount: Mapping[str, float] | float = 0.0  # MetaStat souls_rarity_amount.GetValue(soulKey)
    soul_keys: tuple[str, ...] = ()  # amuletModel.SoulDefs keys (AmuletConfig.SoulDefs order)
    enemy_hp_damage_multiplier: np.float32 = f32(1.0)  # SimulationConfig.EnemyHpDamageMultiplier
    is_running: bool = True  # the batch always runs with IsRunning = true (design 4.3)
    time_axis: TimeAxisConfig = field(default_factory=TimeAxisConfig)
    items: Mapping[str, ItemDef] | None = None
    item_tables: ItemTables | None = None
    _charges: dict = field(default_factory=dict, repr=False)
    _ab_mult: dict = field(default_factory=dict, repr=False)

    def with_variant(self, variant: ab.Variant) -> "SimCalcContext":
        return replace(self, variant=variant, _charges={}, _ab_mult={})

    @property
    def cooldowns(self) -> ab.SpawnerCooldownsData:
        return self.ab_config.spawner_cooldowns(self.variant)

    def fast_rpm_config(self) -> tuple[float, float]:
        return ab.fast_rpm_config(self.ab_config.fast_rpm_params(self.variant))

    def charges_limit(self, s: SimSpawner) -> int:
        """``GetSpawnerChargesLimit`` (0x13e83ec) for the context's AB variant and meta levels."""
        v = self._charges.get(s.spawner_id)
        if v is None:
            v = self._charges[s.spawner_id] = sp.charges_limit(s.d, self.cooldowns, self.meta)
        return v

    @implements("simulation.enemy_multiplier", provenance=Provenance.PORT, code_ref=f"{SS}:350-357", va="0x13e68ac")
    def location_multiplier(self, location: int) -> np.float32:
        """``GetCachedEnemyHpDamageMultiplier`` (0x13e68ac): ``abTestService.GetEnemyHpDamageMultiplier``."""
        v = self._ab_mult.get(location)
        if v is None:
            v = self._ab_mult[location] = ab.enemy_hp_damage_multiplier(
                self.ab_config.enemy_hp_damage_mult(self.variant), location)
        return v

    def rpm_stat_value_multiplier(self, stat_type: int) -> np.float32:
        return ab.rpm_stat_value_multiplier(self.ab_config.common_reward_multipliers(self.variant), stat_type)

    def coefficient(self, timing, type_: int) -> float:
        """``(double)combatBalanceConfig.GetInterpolationCoefficient(timing, type, Rarity.Rare)``."""
        return float(self.interpolations.total_coefficient(f32(timing), type_, Rarity.Rare))

    def souls_rarity_bonus(self, soul_key: str) -> float:
        v = self.souls_rarity_amount
        return float(v.get(soul_key, 0.0) if isinstance(v, Mapping) else v)


def _rows(con: sqlite3.Connection, sql: str) -> list[dict]:
    cur = con.execute(sql)
    cols = [d[0] for d in cur.description]
    return [dict(zip(cols, r)) for r in cur]


def load_sim_spawners(con: sqlite3.Connection, defs: Iterable[sp.SpawnerDef] | None = None) -> list[SimSpawner]:
    """Registered spawners (``EnemiesModel.Spawners``) in ``sortedSpawners`` order."""
    from .data import load_armor_configs

    defs = sp.load_spawners(con) if defs is None else list(defs)
    armors = load_armor_configs(con)
    stats: dict[sp.SpawnerId, list] = {}
    for r in _rows(con, 'SELECT scene, file_id, Type, BaseValue FROM "s_EnemySpawner__config.Stats" '
                        'ORDER BY scene, file_id, "config.Stats#idx"'):
        stats.setdefault((r["scene"], int(r["file_id"])), []).append((int(r["Type"]), float(r["BaseValue"])))
    rewards: dict[sp.SpawnerId, list] = {}
    for r in _rows(con, 'SELECT scene, file_id, StatType, Value FROM "s_EnemySpawner__config.StatRewards" '
                        'ORDER BY scene, file_id, "config.StatRewards#idx"'):
        rewards.setdefault((r["scene"], int(r["file_id"])), []).append(StatReward(int(r["StatType"]), f32(r["Value"])))
    extra = {(r["scene"], int(r["file_id"])): r for r in _rows(
        con, "SELECT scene, file_id, ArmorConfig, Difficulty, SoulReward, WeaponType FROM spawners")}
    item_keys: dict[str, str] = {}
    for name, key in con.execute("SELECT asset_name, Key FROM c_ItemConfig ORDER BY asset"):
        item_keys.setdefault(str(name), str(key))
    out = []
    for d in sp.registered(defs):
        e = extra[d.spawner_id]
        out.append(SimSpawner(
            d=d, stats=tuple(stats.get(d.spawner_id, ())), stat_rewards=tuple(rewards.get(d.spawner_id, ())),
            armors=tuple(armors.get(str(e["ArmorConfig"]), ())),
            difficulty=f32(1.0 if e["Difficulty"] is None else e["Difficulty"]),
            soul_reward=e["SoulReward"] or None, weapon_type=int(e["WeaponType"] or 0),
            item_key=None if d.item_reward is None else item_keys[d.item_reward]))
    return sorted(out, key=lambda s: s.d.location)  # stable: (scene, file_id) inside a location


def load_simcalc_context(con: sqlite3.Connection, variant: ab.Variant = ab.DEFAULT_VARIANT) -> SimCalcContext:
    """The context for the shipped configs (read-only); AB variant A by default, meta at a fresh save."""
    from .data import load_interpolations, load_preferred_weapon_types
    from .items import load_item_defs, load_item_tables

    meta_rows = {r[0]: float(r[1]) for r in con.execute(
        "SELECT Key, BaseValue FROM c_MetaStatsConfig__Stats WHERE Key IN ('soul_drop_bonus', 'souls_rarity_amount')")}
    sc = con.execute("SELECT EnemyHpDamageMultiplier FROM c_SimulationConfig").fetchone()
    soul_keys = tuple(str(r[0]) for r in con.execute('SELECT Key FROM c_AmuletConfig__SoulDefs ORDER BY "SoulDefs#idx"'))
    return SimCalcContext(
        spawners=load_sim_spawners(con), interpolations=load_interpolations(con),
        preferred_weapon_type=load_preferred_weapon_types(con), ab_config=ab.load_ab_config(con), variant=variant,
        meta=sp.load_meta_levels(con), soul_drop_bonus=meta_rows.get("soul_drop_bonus", 0.0),
        souls_rarity_amount=meta_rows.get("souls_rarity_amount", 0.0), soul_keys=soul_keys,
        enemy_hp_damage_multiplier=f32(sc[0]),
        time_axis=load_time_axis_config(con), items=load_item_defs(con), item_tables=load_item_tables(con))


# ---------------------------------------------------------------------------------------------- player


@dataclass(frozen=True)
class PlayerState:
    """The player as the recovered methods see it.

    ``stats``: ``GetForecastStatValue`` per StatType (a stat the unit lacks reads 0). ``dps_data`` /
    ``defense_data``: ``currentDpsData`` / ``currentDefenseData`` by WeaponType (they also fill the
    per-frame caches). ``day``: ``timeChunkIndex``; ``timing``: ``GetBalanceTimingMinutes()`` (K3 of
    DpsData, K4/K5), normally ``day``; ``ability_timing`` overrides it for K4/K5 only (an ablation).
    ``current_health`` None means full health. ``item_keys``: configs in ``playerModel.Items``.
    """

    stats: Mapping[int, float]
    dps_data: Mapping[int, float]
    defense_data: Mapping[int, float]
    unlocked_location: int
    day: int
    timing: float | None = None
    ability_timing: float | None = None
    current_health: float | None = None
    current_playtime: float = 0.0
    item_keys: frozenset[str] = frozenset()

    def stat(self, stat_type: int) -> float:
        return float(self.stats.get(int(stat_type), 0.0))

    def specific(self, stat_type: int) -> float:
        """``GetForecastStatSpecificValue``: ``Stat.SpecificValue`` without post-formula modifiers."""
        t = int(stat_type)
        return clamp_specific_value(t, calculate_specific_value(t, self.stat(t)))

    @property
    def balance_timing(self) -> float:
        return float(f32(self.day if self.timing is None else self.timing))

    @property
    def ability_balance_timing(self) -> float:
        return self.balance_timing if self.ability_timing is None else float(f32(self.ability_timing))


@implements("pending:simulation.day_change_order", provenance=Provenance.INFERRED,
            code_ref=f"{SS}:183-191,588-590,894-896")
def snapshot_state(snapshot: SimulationSnapshot, ctx: SimCalcContext, *, order: str = RPM_FIRST,
                   k4k5_offset: int = 0, unlocked_location: int | None = None, day: int | None = None,
                   rounding=None) -> PlayerState:
    """The player at a day snapshot as ``RecalculateChunkRpmValues`` saw it (design 4.4).

    Stats are the stored ``TotalValue`` except Health, HealthRegen and LifestealFlat, which are recomposed
    from ``Value`` with K2/K3 of day ``d - offset`` (``order``; offset clamped at day 0). The cached
    DpsData/DefenseData are the snapshot's own. ``k4k5_offset`` lags the live K4/K5 too (ablation).
    ``day`` overrides the snapshot's TimeChunkIndex (an interpolated snapshot carries a neighbour's).
    """
    k2k3_offset, base_k45 = DAY_ORDERS[order]
    d = int(snapshot.time_chunk_index if day is None else day)
    stats = {st.key: st.total_value for st in snapshot.stats}
    if k2k3_offset:
        k2, k3 = composition_coefficients(ctx.interpolations, max(d - k2k3_offset, 0), rounding)
        for st in snapshot.stats:
            if st.key in DIRECT_INTERPOLATION_STATS:
                stats[st.key] = compose_stat(st.key, st.value, k2, k3).total_value
    lag = base_k45 + k4k5_offset
    return PlayerState(
        stats=stats, dps_data=dict(snapshot.dps_data), defense_data=dict(snapshot.defense_data),
        unlocked_location=int(snapshot.location if unlocked_location is None else unlocked_location), day=d,
        ability_timing=float(max(d - lag, 0)) if lag else None, current_playtime=float(snapshot.playtime),
        item_keys=frozenset(it.key for it in snapshot.items))


# ---------------------------------------------------------------------------------------------- caches


@dataclass(frozen=True)
class CombatCaches:
    """``RebuildPerFrameCaches`` (0x13e5bc0) plus the DpsData cache and both ability multipliers."""

    dodge_chance: float
    block_damage_multiplier: float
    global_defence_multiplier: float
    crit_multiplier: float
    global_attack_multiplier: float
    ability_dps_multiplier: float
    ability_ehp_multiplier: float
    dps_cache: Mapping[int, float]
    defense_cache: Mapping[int, float]
    health: float
    speed: float


@implements("simulation.combat_multiplier_cache", provenance=Provenance.PORT, code_ref=f"{SS}:331-348",
            va="0x13e5bc0")
def combat_caches(ctx: SimCalcContext, state: PlayerState) -> CombatCaches:
    block_chance = state.specific(StatType.BlockChance)
    block_amount = state.specific(StatType.BlockAmount)
    crit_chance = state.specific(StatType.CritChance)
    crit_multiplier = state.specific(StatType.CritDamagePercent)
    return CombatCaches(
        dodge_chance=state.specific(StatType.Evasion),
        block_damage_multiplier=1.0 - block_chance * block_amount,
        global_defence_multiplier=ddiv(1.0, state.specific(StatType.GlobalDefence) * 0.01 + 1.0),
        crit_multiplier=crit_chance * (crit_multiplier + -1.0) + 1.0,
        global_attack_multiplier=state.specific(StatType.GlobalAttack) * 0.01 + 1.0,
        ability_dps_multiplier=player_ability_dps_multiplier(ctx, state),
        ability_ehp_multiplier=player_ability_ehp_multiplier(ctx, state),
        dps_cache=dict(state.dps_data),  # UpdateDpsDataCache (0x13e4f24): overwrites every WeaponType key
        defense_cache=dict(state.defense_data),
        health=state.stat(StatType.Health),
        speed=state.specific(StatType.MovementSpeed))


@implements("simulation.ability_multipliers", provenance=Provenance.PORT, code_ref=f"{SS}:875-880", va="0x13f2f98")
def player_ability_dps_multiplier(ctx: SimCalcContext, state: PlayerState) -> float:
    """``GetPlayerAbilityDpsMultiplier``: K4(GetBalanceTimingMinutes(), Rare) + 1."""
    return ctx.coefficient(state.ability_balance_timing, BalanceInterpolationType.PlayerAbilityDPSMult) + 1.0


@implements("simulation.ability_multipliers", provenance=Provenance.PORT, code_ref=f"{SS}:882-887", va="0x13efd18")
def player_ability_ehp_multiplier(ctx: SimCalcContext, state: PlayerState) -> float:
    """``GetPlayerAblityEhpMultiplier``: K5(GetBalanceTimingMinutes(), Rare) + 1."""
    return ctx.coefficient(state.ability_balance_timing, BalanceInterpolationType.PlayerAblityEHPMult) + 1.0


@implements("simulation.enemy_multiplier", provenance=Provenance.PORT, code_ref=f"{SS}:220-224", va="0x13e13b0")
def simulation_enemy_multiplier(ctx: SimCalcContext) -> float:
    """``GetSimulationEnemyMultiplier``: ``IsRunning ? (double)EnemyHpDamageMultiplier : 1.0``."""
    return float(ctx.enemy_hp_damage_multiplier) if ctx.is_running else 1.0


# ---------------------------------------------------------------------------------------------- DpsData


ItemEntry = tuple[ItemDef, ItemState]


@implements("simulation.dps_data", provenance=Provenance.PORT, code_ref=f"{SS}:908-933", va="0x13e4478")
def calculate_dps_data(ctx: SimCalcContext, stats: Mapping[int, float], items: Sequence[ItemEntry],
                       weapon_slots: int, timing) -> list[tuple[int, float]]:
    """``CalculateDpsData``: per WeaponType, the best ``UnlockedWeaponSlots`` weapons (matching type first,
    then by DPS, stable on ``playerModel.Items`` order); off-type weapons count a tenth."""
    tables = ctx.item_tables
    weapons = [(it, st) for it, st in items if not it.is_armor]
    dps_of = [item_dps(it, st, tables) for it, st in weapons]
    dmg_of = [item_damage(it, st, tables) for it, st in weapons]
    coefficient = ctx.coefficient(timing, BalanceInterpolationType.StatModifier)
    out = []
    for wt in WEAPON_TYPES:
        order = sorted(range(len(weapons)), key=lambda i: (weapons[i][0].damage_type != wt, -dps_of[i]))
        total = 0.0
        for n, i in enumerate(order):
            if n >= weapon_slots:
                break
            it = weapons[i][0]
            flat = float(stats.get(int(it.damage_stat_flat), 0.0)) if it.damage_stat_flat is not None else 0.0
            percent = float(stats.get(int(it.damage_stat_percent), 0.0)) if it.damage_stat_percent is not None else 0.0
            dps = ddiv(percent * (flat + dmg_of[i]) * coefficient, float(it.attack_cooldown))
            if it.damage_type != wt:
                dps /= 10.0
            total += dps * float(f32(it.simulation_dps_weapon_multiplier))
        out.append((int(wt), total))
    return out


@implements("simulation.defense_data", provenance=Provenance.PORT, code_ref=f"{SS}:935-958", va="0x13e5104")
def calculate_defense_data(ctx: SimCalcContext, stats: Mapping[int, float], items: Sequence[ItemEntry],
                           armor_slots: int, timing) -> list[tuple[int, float]]:
    """``CalculateDefenseData``: per WeaponType, ``percent * (flat + sum of the best UnlockedArmorSlots
    armors of that DamageType) * K3``."""
    tables = ctx.item_tables
    coefficient = ctx.coefficient(timing, BalanceInterpolationType.StatModifier)
    out = []
    for wt in WEAPON_TYPES:
        cands = [(item_armor(it, st, tables), n) for n, (it, st) in enumerate(items)
                 if it.is_armor and it.damage_type == wt]
        cands.sort(key=lambda c: -c[0])  # OrderByDescending(Armor), stable
        flat = float(stats.get(int(weapon_type_to_defence(wt, False)), 0.0))
        percent = float(stats.get(int(weapon_type_to_defence(wt, True)), 0.0))
        armor = 0.0
        for n, (value, _) in enumerate(cands):
            if n >= armor_slots:
                break
            armor += value
        out.append((int(wt), percent * (flat + armor) * coefficient))
    return out


# ---------------------------------------------------------------------------------------------- spawner fight


@implements("simulation.dps_against_spawner", provenance=Provenance.PORT, code_ref=f"{SS}:853-867", va="0x13f2cf8")
def best_armor_against_dps(s: SimSpawner, caches: CombatCaches) -> ArmorEntry | None:
    """``GetBestArmorAgainstDps``: the armor maximising ``DpsData * (1f - Resist)`` (first on ties),
    else the first armor (None when the spawner has none: a C# NRE downstream)."""
    best = -sys.float_info.max  # double.MinValue
    selected = None
    for armor in s.armors:
        data = caches.dps_cache.get(armor.type)
        if data is not None:
            value = data * float(f32(f32(1.0) - armor.resist))
            if value > best:
                best, selected = value, armor
    return selected if selected is not None else (s.armors[0] if s.armors else None)


@implements("simulation.dps_against_spawner", provenance=Provenance.PORT, code_ref=f"{SS}:826-834", va="0x13ef248")
def dps_against_spawner(s: SimSpawner, caches: CombatCaches) -> float:
    """``GetDpsAgainstSpawner(spawner, isFast: true)``."""
    armor = best_armor_against_dps(s, caches)
    if armor is None:
        raise LookupError(f"spawner {s.d.go_path}: ArmorConfig has no armors (NullReferenceException)")
    value = caches.dps_cache.get(armor.type, 0.0)
    return (caches.ability_dps_multiplier * caches.global_attack_multiplier * caches.crit_multiplier * value
            * float(f32(f32(1.0) - armor.resist)))


@implements("simulation.spawner_dps", provenance=Provenance.PORT, code_ref=f"{SS}:960-978", va="0x13ef8d4")
def spawner_dps(ctx: SimCalcContext, s: SimSpawner, caches: CombatCaches) -> float:
    """``GetSpawnerDps(spawner, isFast: true)``: static spawners deal nothing."""
    d = s.d
    if d.is_static:
        return 0.0
    preferred = ctx.preferred_weapon_type[d.location]  # GetCachedLocation: First(Number == L)
    enemy_multiplier = 1.0 if d.is_event else float(ctx.location_multiplier(d.location))
    base_damage = s.base_stat(StatType.Damage)
    defense = caches.defense_cache.get(preferred, 0.0)
    damage = apply_damage_defense(simulation_enemy_multiplier(ctx) * (base_damage * enemy_multiplier), defense)
    attack_cooldown = s.base_stat(StatType.AttackCooldown)
    dps = ddiv(damage, attack_cooldown)
    dps *= 1.0 - caches.dodge_chance
    dps *= caches.block_damage_multiplier
    dps *= caches.global_defence_multiplier
    return dps


@implements("combat.battle_regen", provenance=Provenance.PORT, code_ref=f"{SS}:686-691", va="0x13efd54")
def battle_regen(state: PlayerState, difficulty: float) -> float:
    """``GetBattleRegen((double)Difficulty)``."""
    return get_battle_regen(state.stat(StatType.HealthRegen), state.stat(StatType.LifestealFlat), float(difficulty))


@implements("simulation.spawner_reward_value", provenance=Provenance.PORT, code_ref=f"{SS}:758-763", va="0x13efd98")
def resource_value(s: SimSpawner) -> float:
    """``GetResourceValue``: the first configured reward only."""
    return float(s.stat_rewards[0].value) if s.stat_rewards else 0.0


@implements("simulation.spawner_reward_value", provenance=Provenance.PORT, code_ref=f"{SS}:765-787", va="0x13e0cb8")
def spawner_reward_value(s: SimSpawner, state: PlayerState, *, stats_benefit: Mapping[int, float] | None = None,
                         item_benefit: Mapping[str, float] | None = None, current_boss: sp.SpawnerId | None = None,
                         stat_type: int | None = None) -> float:
    """``CalculateSpawnerRewardValue``. ``stats_benefit`` defaults to the initial 1f for every stat;
    ``item_benefit`` is keyed by item Key (absent = no entry); ``current_boss`` by SpawnerId."""
    if stat_type is not None:
        r = s.reward(stat_type)
        return 0.0 if r is None else float(r.value)
    if current_boss is not None and s.spawner_id == current_boss:
        return BOSS_REWARD_VALUE
    value = 0.0
    for r in s.stat_rewards:
        benefit = 1.0 if stats_benefit is None else stats_benefit.get(r.stat_type)
        if benefit is not None:
            value += float(f32(benefit)) * float(r.value)
    if s.d.item_reward is not None:
        item_value = ITEM_REWARD_VALUE
        if item_benefit is not None and s.item_key in state.item_keys and s.item_key in item_benefit:
            item_value = value + float(f32(item_benefit[s.item_key]))
        value = item_value
    return value * float(s.d.count)


@dataclass
class SpawnerData:
    """``SpawnerData`` (SpawnerData.cs): the per-spawner fight/farm model."""

    spawner: SimSpawner
    health_delta: float = 0.0
    fight_time: float = 0.0
    player_dps: float = 0.0
    enemy_dps: float = 0.0
    reward_value: float = 0.0
    player_rps: float = 0.0
    farmed_time: float = 0.0
    av_move_time: float = 0.0
    av_regen_time: float = 0.0

    @property
    def loss(self) -> float:
        return -self.health_delta


@implements("simulation.spawner_fight_model", provenance=Provenance.PORT, code_ref=f"{SS}:656-671", va="0x13e6074")
def spawner_data(ctx: SimCalcContext, state: PlayerState, s: SimSpawner, caches: CombatCaches | None = None,
                 **reward_kw) -> SpawnerData:
    """The body of the ``CalculateSpawnerData`` loop for one spawner, before the ``fightTime < 300`` test."""
    caches = combat_caches(ctx, state) if caches is None else caches
    health = caches.health
    missing_health = 0.0 if state.current_health is None else cs_max(0.0, health - float(state.current_health))
    rpm = rpm_config()
    move_time = ddiv(rpm[0], caches.speed)
    d = s.d
    ab_multiplier = float(ctx.location_multiplier(d.location))
    data = SpawnerData(spawner=s)
    data.player_dps = dps_against_spawner(s, caches) + DPS_EPSILON
    count_factor = float(min(f32(f32(d.count + 1) / f32(2.0)), f32(4.0)))
    data.enemy_dps = ddiv((spawner_dps(ctx, s, caches) + DPS_EPSILON) * count_factor, caches.ability_ehp_multiplier)
    base_health = s.base_stat(StatType.Health)
    data.fight_time = ddiv(simulation_enemy_multiplier(ctx) * (base_health * float(d.count)) * ab_multiplier,
                           data.player_dps)
    delta = (battle_regen(state, s.difficulty) - data.enemy_dps) * data.fight_time
    data.health_delta = missing_health if delta > missing_health else delta
    data.reward_value = spawner_reward_value(s, state, **reward_kw)
    regen_time = rpm[1] * cs_max(0.0, ddiv(-data.health_delta, cs_max(1.0, health)))
    data.farmed_time = regen_time + (move_time + rpm[2] * data.fight_time)
    data.av_move_time = move_time
    data.av_regen_time = regen_time
    data.player_rps = ddiv(resource_value(s), data.farmed_time)
    if health * 0.5 < -data.health_delta:
        data.player_rps *= LOW_HEALTH_RPS_FACTOR
    return data


def keeps_fight_time(data: SpawnerData) -> bool:
    """``if (data.fightTime < 300.0) spawnerData[spawner] = data`` (NaN is dropped)."""
    return data.fight_time < FIGHT_TIME_LIMIT


@dataclass(frozen=True)
class BotFilter:
    """The ``forGPMBot = false`` filters of ``CalculateSpawnerData`` and the bot state they read.

    ``cooldowns``: ``EnemySpawner.Cooldown`` by SpawnerId (absent = 0, ready); a cleared DAFC spawner is
    ``+inf`` (design 1.5 item 8). ``cleared_dafc`` is a shortcut that sets those. ``charges_used``:
    ``spawnerChargesUsage``. ``alive``: ``AnyoneAlive`` (default: units on a new save, design 2.3).
    """

    charges_used: Mapping[sp.SpawnerId, int] = field(default_factory=dict)
    cooldowns: Mapping[sp.SpawnerId, float] = field(default_factory=dict)
    cleared_dafc: frozenset[sp.SpawnerId] = frozenset()
    alive: frozenset[sp.SpawnerId] | None = None

    def cooldown(self, sid: sp.SpawnerId) -> np.float32:
        if sid in self.cleared_dafc:
            return f32(math.inf)
        return f32(self.cooldowns.get(sid, 0.0))


def _skip(ctx: SimCalcContext, state: PlayerState, s: SimSpawner, for_gpm_bot: bool, bot: BotFilter | None) -> bool:
    d = s.d
    if d.is_event or d.is_vip:
        return True
    if not for_gpm_bot:
        bot = BotFilter() if bot is None else bot
        alive = sp.alive_on_new_save(d) if bot.alive is None else s.spawner_id in bot.alive
        if not alive:
            return True
        if not (bot.cooldown(s.spawner_id) <= f32(0.0)) and ctx.is_running:
            return True
        if ctx.time_axis.use_spawner_charges and bot.charges_used.get(s.spawner_id, 0) >= ctx.charges_limit(s):
            return True
        if optimum_time_over_cap(d.optimum_time, d.is_days, state.current_playtime, state.day, ctx.time_axis):
            return True
    elif d.dafc:
        return True
    return d.location > state.unlocked_location


@implements("simulation.spawner_fight_model", provenance=Provenance.PORT, code_ref=f"{SS}:623-674", va="0x13e6074")
def calculate_spawner_data(ctx: SimCalcContext, state: PlayerState, for_gpm_bot: bool = True,
                           bot: BotFilter | None = None, *, spawners: Iterable[SimSpawner] | None = None,
                           **reward_kw) -> dict[sp.SpawnerId, SpawnerData]:
    """``CalculateSpawnerData(forGPMBot)``: ``spawnerData`` in insertion order.

    ``forGPMBot = true`` (what ``CalculateSimulationData`` and therefore RPM use) skips DAFC spawners and
    applies no bot filter; ``false`` applies ``AnyoneAlive``, cooldown, charges and the OT cap (``bot``).
    ``spawners`` restricts the loop to a subset of ``sortedSpawners`` (same results for those spawners).
    """
    caches = combat_caches(ctx, state)
    out: dict[sp.SpawnerId, SpawnerData] = {}
    for s in (ctx.spawners if spawners is None else spawners):
        if _skip(ctx, state, s, for_gpm_bot, bot):
            continue
        data = spawner_data(ctx, state, s, caches, **reward_kw)
        if keeps_fight_time(data):
            out[s.spawner_id] = data
    return out


# ---------------------------------------------------------------------------------------------- RPM


@implements("simulation.rpm_time_chunk", provenance=Provenance.PORT, code_ref=f"{SS}:438-439", va="0x13e7cf4")
def rpm_survivable(loss: float, health: float, max_health_loss_multiplier: float) -> bool:
    """The RPM survivability test, verbatim: ``if (parameters.Item2 * health < loss) continue;`` -- a
    spawner counts unless ``Item2 * H < loss`` (non-strict for the player: ``loss == Item2 * H`` counts)."""
    return not (max_health_loss_multiplier * health < loss)


@implements("simulation.rpm_time_chunk", provenance=Provenance.PORT, code_ref=f"{SS}:426-461", va="0x13e7cf4")
def rpm_value_time_chunk(ctx: SimCalcContext, state: PlayerState, data: Mapping[sp.SpawnerId, SpawnerData],
                         stat_type: int, previous: float | None) -> float | None:
    """``CalculateRPMValueTimeChunk(StatType)``: the new ``RPMCalculatedValue[stat]``.

    ``previous`` is the stored value (None = no key). Returns ``previous`` unchanged when no spawner in
    ``data`` rewards the stat (the C# returns early).
    """
    fight_seconds, loss_multiplier = ctx.fast_rpm_config()
    st = int(stat_type)
    candidates = [x for x in data.values() if x.spawner.reward(st) is not None]
    if not candidates:
        return previous
    health = state.stat(StatType.Health)
    total = 0.0
    for x in candidates:
        loss = -x.health_delta
        if not rpm_survivable(loss, health, loss_multiplier):
            continue
        reward = x.spawner.reward(st)
        fight_time = x.fight_time
        health_multiplier = 1.0
        if health < loss:
            health_multiplier = math.sqrt(health / loss)
        location_multiplier = 1.0
        d = x.spawner.d
        index = d.location - 1
        if not d.is_elite and 0 <= index < len(RPM_LOCATION_MULTIPLIERS):
            location_multiplier = RPM_LOCATION_MULTIPLIERS[index]
        charges = ctx.charges_limit(x.spawner)
        contribution = ddiv(fight_seconds, fight_seconds + fight_time)
        contribution *= float(reward.value)
        contribution *= float(charges)
        contribution *= location_multiplier
        contribution *= health_multiplier
        total += contribution
    value = total / 60.0
    if value > 0.0:
        value = cs_max(RPM_FLOOR, value)
    value *= float(ctx.rpm_stat_value_multiplier(st))
    if previous is not None:
        value = cs_max(value, previous)
    return value


@implements("simulation.rpm_time_chunk", provenance=Provenance.PORT, code_ref=f"{SS}:276,286", va="0x13e14f8")
def initial_rpm() -> dict[int, float]:
    """``RPMCalculatedValue`` after ``OnAwake``: 0 for every StatType."""
    return {int(t): 0.0 for t in STAT_TYPES}


@implements("simulation.soul_rpm", provenance=Provenance.PORT, code_ref=f"{SS}:277,287", va="0x13e14f8")
def initial_soul_rpm(ctx: SimCalcContext) -> dict[str, float]:
    """``SoulRPMCalculatedValue`` after ``OnAwake``: 0 for every ``amuletModel.SoulDefs`` key."""
    return {k: 0.0 for k in ctx.soul_keys}


@implements("simulation.rpm_time_chunk", provenance=Provenance.PORT, code_ref=f"{SS}:376-382", va="0x13e7a00")
def calculate_rpm_value_time_chunk(ctx: SimCalcContext, state: PlayerState, rpm: Mapping[int, float] | None = None,
                                   soul_rpm: Mapping[str, float] | None = None
                                   ) -> tuple[dict[int, float], dict[str, float], dict[sp.SpawnerId, SpawnerData]]:
    """``CalculateRPMValueTimeChunk()``: ``CalculateSimulationData`` (forGPMBot), soul RPM, then every stat.

    Returns the updated ``RPMCalculatedValue``, ``SoulRPMCalculatedValue`` and the ``spawnerData`` used;
    ``None`` starts from the ``OnAwake`` values.
    """
    data = calculate_spawner_data(ctx, state, for_gpm_bot=True)
    souls = calculate_soul_rpm_values(ctx, state, data, initial_soul_rpm(ctx) if soul_rpm is None else soul_rpm)
    out = dict(initial_rpm() if rpm is None else rpm)
    for t in STAT_TYPES:
        v = rpm_value_time_chunk(ctx, state, data, t, out.get(int(t)))
        if v is not None:
            out[int(t)] = v
    return out, souls, data


@implements("simulation.soul_rpm", provenance=Provenance.PORT, code_ref=f"{SS}:463-470", va="0x13e6dd0")
@implements("simulation.soul_rpm", provenance=Provenance.PORT, code_ref=f"{SS}:472-486", va="0x13e8424")
@implements("simulation.soul_rpm", provenance=Provenance.PORT, code_ref=f"{SS}:488-495", va="0x13e86b8")
@implements("meta.soul_drop_for_kill_count", provenance=Provenance.PORT,
            code_ref="CodeBase/Meta/SoulDropAmountCalculator.cs:9-13", va="0x14889a4")
def calculate_soul_rpm_values(ctx: SimCalcContext, state: PlayerState, data: Mapping[sp.SpawnerId, SpawnerData],
                              soul_rpm: Mapping[str, float] | None = None) -> dict[str, float]:
    """``CalculateSoulRPMValues`` -> ``CalculateSoulRPMValue`` (0x13e8424) -> ``SetSoulRPMValue`` (0x13e86b8)."""
    out = dict(soul_rpm or {})
    keys: list[str] = []
    for x in data.values():
        key = x.spawner.soul_reward
        if key is not None and key.strip() and key not in keys:
            keys.append(key)
    health = state.stat(StatType.Health)
    for key in keys:
        total = 0.0
        for x in data.values():
            if health < -x.health_delta:
                continue
            if x.spawner.soul_reward != key:
                continue
            total += ddiv(SOUL_RPM_FIGHT_SECONDS, x.fight_time + SOUL_RPM_FIGHT_SECONDS) * float(ctx.charges_limit(x.spawner))
        kill_count = total / 60.0
        value = ((1.0 + ctx.soul_drop_bonus) + ctx.souls_rarity_bonus(key)) * kill_count
        if value > 0.0:
            value = cs_max(SOUL_RPM_FLOOR, value)
        if key in out:
            value = cs_max(value, out[key])
        out[key] = value
    return out


# ---------------------------------------------------------------------------------------------- bot eligibility


@implements("pending:simulation.fast_kill_gate", provenance=Provenance.INFERRED, code_ref=f"{SS}:438-439")
def fast_killable(data: SpawnerData, health: float, kill_loss_multiplier: float = 1.0) -> bool:
    """Design 4.5: the fast phase farms a spawner only if :func:`rpm_survivable` with ``L = KillLossMultiplier``
    (default 1.0, not the AB value) -- the RPM predicate reused as the bot's kill gate (inferred)."""
    return rpm_survivable(data.loss, health, kill_loss_multiplier)


@implements("pending:simulation.eligible_spawners", provenance=Provenance.INFERRED, code_ref=f"{SS}:623-674")
def eligible_spawners(ctx: SimCalcContext, state: PlayerState, bot: BotFilter | None = None,
                      kill_loss_multiplier: float = 1.0, *,
                      spawners: Iterable[SimSpawner] | None = None) -> dict[sp.SpawnerId, SpawnerData]:
    """The fast phase's eligible set (design 4.5): ``CalculateSpawnerData(false)`` (bot filter: alive,
    cooldown -- a cleared DAFC spawner's ``+inf`` excludes it --, charges, OT cap, unlocked island),
    ``fightTime < 300``, and :func:`fast_killable`."""
    data = calculate_spawner_data(ctx, state, for_gpm_bot=False, bot=bot, spawners=spawners)
    health = state.stat(StatType.Health)
    return {sid: x for sid, x in data.items() if fast_killable(x, health, kill_loss_multiplier)}


# ---------------------------------------------------------------------------------------------- replays


@dataclass(frozen=True)
class GpmReplay:
    """CurrentGpm of every stat of every day snapshot, replayed (G20)."""

    k2k3_offset: int
    k4k5_offset: int
    replayed: list[tuple[int, dict[int, float]]]  # (TimeChunkIndex, {stat key: RPM after the running max})


def replay_day_gpm(ctx: SimCalcContext, day_snapshots: Sequence[SimulationSnapshot], *, order: str = RPM_FIRST,
                   k4k5_offset: int = 0) -> GpmReplay:
    """Design G20: per day snapshot, ``CalculateRPMValueTimeChunk`` on the snapshot's own state (stored
    DpsData/DefenseData as caches, full health, AB variant of ``ctx``), running max across days."""
    rpm = initial_rpm()
    out = []
    for snap in day_snapshots:
        state = snapshot_state(snap, ctx, order=order, k4k5_offset=k4k5_offset)
        rpm, _, _ = calculate_rpm_value_time_chunk(ctx, state, rpm)
        out.append((int(snap.time_chunk_index), {st.key: rpm.get(st.key, 0.0) for st in snap.stats}))
    k2k3, k45 = DAY_ORDERS[order]
    return GpmReplay(k2k3, k45 + k4k5_offset, out)


def gpm_matches(stored: float, replayed: float, rel: float = 1e-4) -> bool:
    """The G20 cell test of the frozen fixture: ``|stored - replayed| <= rel * max(1, |stored|)``."""
    return abs(stored - replayed) <= rel * max(1.0, abs(stored))


@implements("pending:simulation.weapon_slot_unlocks", provenance=Provenance.INFERRED)
def slots_p12(location: int, slot_unlock_locations: Mapping[str, int]) -> tuple[int, int]:
    """Weapon and armor slots unlocked at ``location`` under the closed-form rule (design P12): the default
    slot, slot rewards when their gate's next island is reached, weapon_3 at island 4, weapon_4 at island 8."""
    weapons = 1 + sum(1 for k, loc in slot_unlock_locations.items() if k.startswith("weapon") and location >= loc)
    armors = sum(1 for k, loc in slot_unlock_locations.items() if not k.startswith("weapon") and location >= loc)
    return weapons, armors


P12_WEAPON_SLOT_LOCATIONS = {"weapon_3": 4, "weapon_4": 8}


def slot_unlock_locations(spawners: Iterable[sp.SpawnerDef]) -> dict[str, int]:
    """Slot -> island whose arrival opens it: ``SlotReward`` gates open their slot with the next island
    (weapon_2 at 2, helmet_1 at 5, shield_1 at 6 in the shipped data), plus the P12 weapon slots."""
    out = dict(P12_WEAPON_SLOT_LOCATIONS)
    for s in spawners:
        if s.slot_reward:
            nexts = [n for n in s.gate_next_locations if n == s.location + 1]
            if not nexts:
                raise ValueError(f"slot reward {s.slot_reward} on {s.go_path} has no gate to island {s.location + 1}")
            out[s.slot_reward] = s.location + 1
    return out


def snapshot_items(snapshot: SimulationSnapshot, ctx: SimCalcContext,
                   evolved: Mapping[str, bool | None]) -> list[ItemEntry]:
    """``playerModel.Items`` of a snapshot (list order), with the evolved flag from ``evolved`` (None/missing
    = unevolved, design R2)."""
    out = []
    for it in snapshot.items:
        ev = bool(evolved.get(it.key))
        out.append((ctx.items[it.key], ItemState.from_snapshot(it.level, it.copies, ev)))
    return out


@dataclass(frozen=True)
class DpsRecompute:
    """R2/R2b for one day snapshot: recomputed vs stored DpsData/DefenseData."""

    time_chunk_index: int
    weapon_slots: int
    armor_slots: int
    dps: list[tuple[int, float]]
    defense: list[tuple[int, float]]
    dps_max_rel_err: float
    defense_max_rel_err: float


def _max_rel_err(calc: Sequence[tuple[int, float]], stored: Sequence[tuple[int, float]]) -> float:
    ref = dict(stored)
    err = 0.0
    for wt, v in calc:
        r = ref.get(wt, 0.0)
        if v == r:
            continue
        err = max(err, math.inf if r == 0.0 else abs(v - r) / abs(r))
    return err


def recompute_dps_defense(ctx: SimCalcContext, snapshot: SimulationSnapshot, evolved: Mapping[str, bool | None],
                          weapon_slots: int, armor_slots: int, *, k3_offset: int = 0) -> DpsRecompute:
    """Design R2: DpsData/DefenseData of a day snapshot from its items and stored TotalValues at K3(d)
    (``k3_offset`` = 1 reads K3(d-1), clamped at day 0: an ablation)."""
    stats = {st.key: st.total_value for st in snapshot.stats}
    items = snapshot_items(snapshot, ctx, evolved)
    d = snapshot.time_chunk_index
    timing = max(d - k3_offset, 0)
    dps = calculate_dps_data(ctx, stats, items, weapon_slots, timing)
    defense = calculate_defense_data(ctx, stats, items, armor_slots, timing)
    return DpsRecompute(int(d), weapon_slots, armor_slots, dps, defense, _max_rel_err(dps, snapshot.dps_data),
                        _max_rel_err(defense, snapshot.defense_data))


def initial_player_state_stats(con: sqlite3.Connection, ctx: SimCalcContext, rounding=None) -> dict[int, float]:
    """``GetForecastStatValue`` of a new player: ``Player Config`` base stats composed with K2/K3(0)."""
    k2, k3 = composition_coefficients(ctx.interpolations, 0, rounding)
    return {int(r[0]): compose_stat(int(r[0]), float(r[1]), k2, k3).total_value
            for r in con.execute('SELECT Type, BaseValue FROM c_PlayerConfig__Stats ORDER BY "Stats#idx"')}


def first_kill_days(ctx: SimCalcContext, day_state, spawners: Iterable[SimSpawner], *, first_day: int = 4,
                    last_day: int = 405, kill_loss_multiplier: float = 1.0,
                    bot: BotFilter | None = None) -> dict[sp.SpawnerId, int]:
    """First day ``d`` in ``[first_day, last_day)`` on which each spawner is in :func:`eligible_spawners` of
    ``day_state(d)`` (a :class:`PlayerState`; ``bot`` defaults to fresh charges and ready cooldowns);
    ``last_day`` when it never is."""
    remaining = list(spawners)
    out: dict[sp.SpawnerId, int] = {}
    for d in range(first_day, last_day):
        if not remaining:
            break
        eligible = eligible_spawners(ctx, day_state(d), bot, kill_loss_multiplier, spawners=remaining)
        for s in remaining:
            if s.spawner_id in eligible:
                out[s.spawner_id] = d
        remaining = [s for s in remaining if s.spawner_id not in eligible]
    for s in remaining:
        out[s.spawner_id] = last_day
    return out
