"""Executable numeric model of the original game (stage 4 core, extended in stage 5).

Every function ports a specific C# method of the ORIGINAL code (git commit 6af011f, before the
optional pets feature); docstrings cite ``CodeBase/<file>:<lines>``. Float/double semantics follow
the C# declarations (see :mod:`.f32`).

Modules:

* :mod:`.stats` -- ``Stat.CalculateSpecificValue`` curves and cap.
* :mod:`.interpolation` -- ``BalanceInterpolation.Evaluate`` and ``GetTotalCoefficient``.
* :mod:`.defense` -- ``ApplyDamageDefense``, its bisection inverse, battle regen.
* :mod:`.snapshots` -- Simulation Data snapshots, ``GetByPlayTime``, ``GetByTimeChunkIndex``.
* :mod:`.bake` -- ``GetReCalcOptimumTime`` and ``BuildSpawnerBakeReport`` (spawner Health/Damage).
* :mod:`.data` -- loaders from ``Balance/out/balance.sqlite``.
"""

from .bake import (ArmorEntry, BakeContext, SpawnerBakeInput, SpawnerBakeReport, bake_spawner,
                   build_spawner_bake_report, calculate_baked_spawner_health, recalc_optimum_time)
from .defense import (apply_damage_defense, get_battle_regen, solve_attack_damage,
                      solve_attack_damage_for_incoming, weapon_type_to_defence)
from .enums import BalanceInterpolationType, Rarity, StatType, WeaponType
from .interpolation import BalanceInterpolation, BalanceInterpolationsConfig, parse_points
from .snapshots import SimulationData, SimulationSnapshot, SimulationStat, interpolate_snapshot
from .stats import calculate_specific_value, clamp_specific_value, specific_value_pipeline

__all__ = [
    "ArmorEntry", "BakeContext", "SpawnerBakeInput", "SpawnerBakeReport", "bake_spawner",
    "build_spawner_bake_report", "calculate_baked_spawner_health", "recalc_optimum_time",
    "apply_damage_defense", "get_battle_regen", "solve_attack_damage", "solve_attack_damage_for_incoming",
    "weapon_type_to_defence", "BalanceInterpolationType", "Rarity", "StatType", "WeaponType",
    "BalanceInterpolation", "BalanceInterpolationsConfig", "parse_points", "SimulationData",
    "SimulationSnapshot", "SimulationStat", "interpolate_snapshot", "calculate_specific_value",
    "clamp_specific_value", "specific_value_pipeline",
]
