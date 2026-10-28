"""Spawner Health/Damage bake (the editor generator that wrote EnemySpawnerConfig.Stats).

Ports of ``Simulation/SimulationConfig.cs`` at commit 6af011f (pets disabled / not present):

* :func:`recalc_optimum_time` -- ``GetReCalcOptimumTime``, lines 219-231 (early-spawner shift).
* :meth:`BakeContext.snapshot_for_bake` -- ``GetSnapshotForBake``, lines 233-239.
* :func:`build_spawner_bake_report` -- private ``BuildSpawnerBakeReport(spawner, optimumTime,
  reCalcOptimumTime, isDays)``, lines 113-217 (native ELF VA 0x13db16c).
* :func:`bake_spawner` -- public ``BuildSpawnerBakeReport(spawner)``, lines 103-106.
* :func:`calculate_baked_spawner_health` -- ``CalculateBakedSpawnerHealth``, lines 108-111.

Numeric types follow the C# declarations: ``EnemySpawnerConfig`` OptimumTime /
RelativeOptimumTime / AverageFightTime / Difficulty are float, Count is int, Stat.BaseValue is
double, ``Armor.Resist`` is float, ``CombatBalanceConfig.IslandTargetDays`` is float[]; every
float expression is rounded to binary32 step by step (see :mod:`.f32`).

The ability curves K4/K5 (``GetInterpolationCoefficient``) go through
:meth:`~.interpolation.BalanceInterpolationsConfig.total_coefficient` with ``BakeContext.rounding``
(``None`` = the default of ``interpolation_default.json``). The G13 counts are the same under
``stepwise`` and ``fma``; 47 day-mode spawners get a different K4 or K5 (and raw Health/Damage).
``analysis/bake_check.py`` pins ``stepwise`` to keep its stage-4 numbers, and its H9 test swaps
this module's ``f32`` for ``np.float64``: while swapped, K4/K5 take the legacy double path of
``get_interpolation_coefficient``.

Inputs C# would throw on (no armor rows, missing DpsData/DefenseData/stat rows, no location row,
an ``IslandTargetDays`` index out of range) raise :class:`BakeError` naming the spawner.

The method that applied the report (writing ``NewHealth``/``NewDamage`` into the scene and
appending ``SpawnerBakeInfo`` rows) is not in the APK (``canRebake``/``canApplyBalance`` return
false, lines 79-80); this module only reproduces the report.
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field

import numpy as np

from . import interpolation as _interp
from .defense import get_battle_regen, solve_attack_damage_for_incoming
from .enums import BalanceInterpolationType, StatType
from .f32 import ddiv, f32, round_half_even
from .interpolation import BalanceInterpolationsConfig, Rounding
from .registry import Provenance, implements
from .snapshots import SimulationData, SimulationSnapshot
from .stats import calculate_specific_value
from .timeline import island_start_day

_F32 = f32  # analysis/bake_check.py (H9) swaps ``f32`` for np.float64; see _ability_coefficient

MAX_SIMULTANEOUS_ATTACKERS = 4.0  # SimulationConfig.cs:21 (the bake uses the literal 4f, line 139)
BAKE_RESIST_CLAMP = 0.9  # SimulationConfig.cs:130
MINUTES_PER_DAY_FOR_BAKE = 60.0  # SimulationConfig.cs:118 and :226


class BakeError(ValueError):
    """An input the C# bake would throw on (``First`` on an empty sequence, index out of range)."""

    def __init__(self, spawner: "SpawnerBakeInput", reason: str):
        self.spawner_name = spawner.name
        self.spawner_key = spawner.key
        self.spawner_id = spawner.spawner_id
        self.reason = reason
        where = f" {self.spawner_id[0]}:{self.spawner_id[1]}" if self.spawner_id else ""
        super().__init__(f"spawner {spawner.name!r} (key {spawner.key!r}{where}): {reason}")


@dataclass(frozen=True)
class ArmorEntry:
    type: int
    resist: np.float32


@dataclass
class SpawnerBakeInput:
    """The EnemySpawnerConfig fields the bake reads (EnemySpawnerConfig.cs)."""

    key: str
    name: str
    location_number: int
    is_days: bool
    optimum_time: np.float32
    relative_optimum_time: np.float32
    average_fight_time: np.float32
    count: int
    difficulty: np.float32
    armors: list[ArmorEntry]
    health_base: float
    damage_base: float
    attack_cooldown_base: float
    spawner_id: tuple[str, int] | None = None  # (scene, file_id); Key is not unique


@dataclass
class BakeContext:
    """What ``SimulationConfig`` holds for the bake.

    ``rounding``: interpolation rounding of K4/K5 (``None`` = the JSON default, see module doc).
    """

    simulation_data: SimulationData
    interpolations: BalanceInterpolationsConfig
    island_target_days: list[np.float32]
    preferred_weapon_type: dict[int, int]  # LocationsConfig.Locations: Number -> PreferredWeaponType
    early_offset_start_location: int
    early_offset_relative_part: np.float32
    early_offset_multiplier: np.float32
    config_time_chunk_snapshots: list[SimulationSnapshot] = field(default_factory=list)
    rounding: Rounding | str | None = None

    def snapshot_for_bake(self, play_time, is_days: bool) -> SimulationSnapshot:
        """``GetSnapshotForBake`` (SimulationConfig.cs:233-239)."""
        if not is_days:
            return self.simulation_data.get_by_playtime(play_time)
        snaps = self.config_time_chunk_snapshots if self.config_time_chunk_snapshots else self.simulation_data.time_chunk_snapshots
        return self.simulation_data.get_by_time_chunk_index(play_time, snaps)


@dataclass
class ArmorCandidate:
    weapon_type: int
    resist: np.float32
    dps_by_weapon_type: float
    score: float


@dataclass
class SpawnerBakeReport:
    """Subset of ``SpawnerBakeReportData`` (Simulation/SpawnerBakeReportData.cs)."""

    key: str
    location_number: int
    optimum_time: float
    recalc_optimum_time: float
    day: float
    snapshot_time_chunk_index: int
    snapshot_playtime: float
    snapshot_source_index: int | None
    armor_type: int
    armor_resist_raw: float
    armor_resist_used: float
    preferred_weapon_type: int
    crit_chance: float
    crit_damage: float
    dps_by_armor_type: float
    player_dps: float
    defense: float
    battle_regen: float
    evasion: float
    block_multiplier: float
    enemies_count: float
    difficulty_used: float
    need_total_hp_base_health: float
    need_damage_from_one_unit: float
    incoming_percent: float
    ability_dps: float
    ability_ehp: float
    new_damage_raw: float
    new_damage: float
    new_health_raw: float
    new_health: float
    prev_health: float
    prev_damage: float
    candidates: list[ArmorCandidate]
    name: str = ""
    spawner_id: tuple[str, int] | None = None


def _ability_coefficient(ctx: BakeContext, day, type_: int):
    """``CombatBalanceConfig.GetInterpolationCoefficient(day, type)`` under ``ctx.rounding``."""
    if f32 is not _F32 or _interp.f32 is not _interp._F32:
        return ctx.interpolations.get_interpolation_coefficient(day, type_)  # H9 double run
    return ctx.interpolations.total_coefficient(day, type_, rounding=ctx.rounding)


def _island_span(sp: SpawnerBakeInput, itd) -> np.float32:
    """``ITD[L-1] - ITD[L-2]`` (float - float), i.e. the length of island L in days."""
    L = sp.location_number
    if not 2 <= L <= len(itd):
        raise BakeError(sp, f"IslandTargetDays[{L - 1}] / [{L - 2}] out of range (LocationNumber {L}, "
                            f"{len(itd)} entries)")
    return island_start_day(itd, L + 1) - island_start_day(itd, L)


@implements("simulation.bake_recalc_optimum_time", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/SimulationConfig.cs:219-231")
def recalc_optimum_time(sp: SpawnerBakeInput, ctx: BakeContext) -> np.float32:
    """``SimulationConfig.GetReCalcOptimumTime`` (SimulationConfig.cs:219-231), the early-bake offset.

    For ``L >= StartLocation`` (11) and ``Rel < RelPart`` (0.5):
    ``OT - (ITD[L-1] - ITD[L-2]) * (RelPart - Rel) * Multiplier`` (delta x60 when !IsDays).
    """
    if sp.location_number >= ctx.early_offset_start_location and sp.relative_optimum_time < ctx.early_offset_relative_part:
        span = f32(_island_span(sp, ctx.island_target_days))
        delta = f32(f32(span * f32(ctx.early_offset_relative_part - sp.relative_optimum_time)) * ctx.early_offset_multiplier)
        if not sp.is_days:
            delta = f32(delta * f32(MINUTES_PER_DAY_FOR_BAKE))
        return f32(sp.optimum_time - delta)
    return f32(sp.optimum_time)


@implements("simulation.bake_spawner_health", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/SimulationConfig.cs:113-217", va="0x13db16c")
@implements("simulation.bake_spawner_damage", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/SimulationConfig.cs:113-217", va="0x13db16c")
def build_spawner_bake_report(ctx: BakeContext, sp: SpawnerBakeInput, optimum_time, recalc_time, is_days: bool,
                              snapshot: SimulationSnapshot | None = None) -> SpawnerBakeReport:
    """Private ``BuildSpawnerBakeReport`` (SimulationConfig.cs:113-217, native VA 0x13db16c).

    ``snapshot`` may be passed to override ``GetSnapshotForBake`` (used by the analysis to test
    alternative simulation runs); by default it is looked up exactly as in C#.
    """
    try:
        return _build_report(ctx, sp, optimum_time, recalc_time, is_days, snapshot)
    except (KeyError, IndexError) as e:
        raise BakeError(sp, e.args[0] if e.args else type(e).__name__) from e


def _build_report(ctx, sp, optimum_time, recalc_time, is_days, snapshot) -> SpawnerBakeReport:
    if not sp.armors:
        raise BakeError(sp, "ArmorConfig has no armor rows (rows.First() throws)")
    if sp.location_number not in ctx.preferred_weapon_type:
        raise BakeError(sp, f"no LocationsConfig row with Number {sp.location_number}")
    recalc_time = f32(recalc_time)
    day = recalc_time if is_days else f32(recalc_time / f32(MINUTES_PER_DAY_FOR_BAKE))  # :118
    snap = snapshot if snapshot is not None else ctx.snapshot_for_bake(recalc_time, is_days)  # :119
    ability_dps = _ability_coefficient(ctx, day, BalanceInterpolationType.PlayerAbilityDPSMult)  # :120
    ability_ehp = _ability_coefficient(ctx, day, BalanceInterpolationType.PlayerAblityEHPMult)  # :121
    raw_cc = snap.total(StatType.CritChance)
    raw_cd = snap.total(StatType.CritDamagePercent)
    cc = calculate_specific_value(StatType.CritChance, raw_cc)  # :124
    cd = calculate_specific_value(StatType.CritDamagePercent, raw_cd)  # :125

    # :126 rows ordered by Score desc (stable); Score uses FirstOrDefault (0 if missing) and the unclamped resist.
    cands = []
    for a in sp.armors:
        v = snap.dps(a.type, default=0.0)
        cands.append(ArmorCandidate(a.type, a.resist, v, v * float(f32(f32(1.0) - a.resist))))
    rows = sorted(cands, key=lambda r: -r.score if not math.isnan(r.score) else math.inf)
    chosen = next(a for a in sp.armors if a.type == rows[0].weapon_type)  # :127
    dps = snap.dps(chosen.type)  # :128 First -> throws if missing (KeyError -> BakeError)
    preferred = ctx.preferred_weapon_type[sp.location_number]  # :129
    used_resist = f32(min(max(chosen.resist, f32(-BAKE_RESIST_CLAMP)), f32(BAKE_RESIST_CLAMP)))  # :130
    defense = snap.defense(preferred)  # :131
    health_regen = snap.total(StatType.HealthRegen)
    flat_regen = snap.total(StatType.LifestealFlat)
    evasion = calculate_specific_value(StatType.Evasion, snap.total(StatType.Evasion))  # :134 (no 0.8 clamp)
    block_chance = calculate_specific_value(StatType.BlockChance, snap.total(StatType.BlockChance))
    block_amount = calculate_specific_value(StatType.BlockAmount, snap.total(StatType.BlockAmount))
    enemies = f32(min(f32(MAX_SIMULTANEOUS_ATTACKERS), f32(f32(sp.count + 1) / f32(2.0))))  # :139
    raw_difficulty = f32(sp.difficulty)
    difficulty = f32(1.0) if raw_difficulty <= 0.0 else raw_difficulty  # :141
    battle_regen = get_battle_regen(health_regen, flat_regen, float(difficulty))  # :142
    base_health = snap.total(StatType.Health)  # :143
    needed_health = base_health * float(difficulty)  # :146
    needed_dps = ddiv(needed_health, float(sp.average_fight_time))  # :147
    cooldown = float(sp.attack_cooldown_base)  # :148
    needed_dps_per_unit = ddiv(battle_regen + needed_dps, float(enemies))  # :149
    frequency = ddiv(cooldown, 1.0 - evasion)  # :150
    block_multiplier = 1.0 - block_chance * block_amount  # :151
    needed_damage = ddiv(needed_dps_per_unit * frequency, block_multiplier)  # :152
    incoming_percent = 1.0 / (float(ability_ehp) + 1.0)  # :153
    new_damage_raw = solve_attack_damage_for_incoming(needed_damage, defense, incoming_percent)  # :154
    p_before = (float(ability_dps) + 1.0) * dps  # :155
    crit_dps = cd * cc * p_before  # :156
    player_dps = (1.0 - cc) * p_before + crit_dps  # :157
    new_health_raw = ddiv(player_dps * float(sp.average_fight_time), float(sp.count)) * float(f32(f32(1.0) - used_resist))  # :158
    new_damage = round_half_even(max(10.0, new_damage_raw) / 10.0) * 10.0  # :159
    new_health = round_half_even(new_health_raw)  # :160
    return SpawnerBakeReport(
        key=sp.key, location_number=sp.location_number, optimum_time=float(f32(optimum_time)),
        recalc_optimum_time=float(recalc_time), day=float(day),
        snapshot_time_chunk_index=snap.time_chunk_index, snapshot_playtime=float(snap.playtime),
        snapshot_source_index=snap.source_index,
        armor_type=chosen.type, armor_resist_raw=float(chosen.resist), armor_resist_used=float(used_resist),
        preferred_weapon_type=preferred, crit_chance=cc, crit_damage=cd, dps_by_armor_type=dps,
        player_dps=player_dps, defense=defense, battle_regen=battle_regen, evasion=evasion,
        block_multiplier=block_multiplier, enemies_count=float(enemies), difficulty_used=float(difficulty),
        need_total_hp_base_health=base_health, need_damage_from_one_unit=needed_damage,
        incoming_percent=incoming_percent, ability_dps=float(ability_dps), ability_ehp=float(ability_ehp),
        new_damage_raw=new_damage_raw, new_damage=new_damage, new_health_raw=new_health_raw, new_health=new_health,
        prev_health=sp.health_base, prev_damage=sp.damage_base, candidates=rows,
        name=sp.name, spawner_id=sp.spawner_id,
    )


@implements("simulation.bake_spawner_health", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/SimulationConfig.cs:103-106")
def bake_spawner(ctx: BakeContext, sp: SpawnerBakeInput) -> SpawnerBakeReport:
    """Public ``BuildSpawnerBakeReport(spawner)`` (SimulationConfig.cs:103-106)."""
    return build_spawner_bake_report(ctx, sp, sp.optimum_time, recalc_optimum_time(sp, ctx), sp.is_days)


def calculate_baked_spawner_health(ctx: BakeContext, sp: SpawnerBakeInput, optimum_day) -> float:
    """``CalculateBakedSpawnerHealth`` (SimulationConfig.cs:108-111): explicit day, day-mode, no early shift."""
    return build_spawner_bake_report(ctx, sp, optimum_day, optimum_day, True).new_health
