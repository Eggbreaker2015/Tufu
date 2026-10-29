"""Flat-defence curve, its inverse, and battle regen.

Ports of ``Services/Damage/DamageCalculator.cs`` at commit 6af011f:

* :func:`apply_damage_defense` -- lines 24-29: ``incoming = A * exp(-0.75 D/A) * max(0, 1 - 0.25 D/A)``.
* :func:`solve_attack_damage` -- lines 31-52: doubling search from 1 then bisection
  from lower=0 until ``upper - lower <= 1``; returns ``upper``.
* :func:`solve_attack_damage_for_incoming` -- lines 54-57.
* :func:`get_battle_regen` -- lines 59-62.
* :func:`weapon_type_to_defence` -- lines 269-280.
"""

from __future__ import annotations

import math
from typing import Callable

from .enums import StatType, WeaponType


def apply_damage_defense(attack_damage: float, flat_defense: float) -> float:
    """``DamageCalculator.ApplyDamageDefense`` (DamageCalculator.cs:24-29)."""
    a = float(attack_damage)
    if not (a > 0.0):
        return 0.0
    ratio = float(flat_defense) / a
    return math.exp(ratio * -0.75) * a * max(0.0, ratio * -0.25 + 1.0)


def solve_attack_damage(target_incoming: float, forward: Callable[[float], float],
                        max_iter: int = 2_147_483_647) -> float:
    """``DamageCalculator.SolveAttackDamage`` (DamageCalculator.cs:31-52).

    ``max_iter`` mirrors the ``int.MaxValue`` loop guard; with a monotone forward the loop
    exits long before. A NaN target never satisfies ``<=`` so the doubling would run until
    ``upper`` overflows to inf; we stop there (C# would spin to the guard and log).
    """
    target = float(target_incoming)
    upper = 1.0
    remaining = max_iter
    while True:
        if target <= forward(upper):
            break
        upper += upper
        if math.isinf(upper):
            break
        remaining -= 1
        if remaining == 0:
            break
    lower = 0.0
    remaining = max_iter
    while True:
        if upper - lower <= 1.0:
            return upper
        middle = (upper + lower) * 0.5
        if target <= forward(middle):
            upper = middle
        else:
            lower = middle
        remaining -= 1
        if remaining == 0 or math.isinf(upper):
            return upper


def solve_attack_damage_for_incoming(target_incoming: float, flat_defense: float, percent_multiplier: float) -> float:
    """``DamageCalculator.SolveAttackDamageForIncomingDamage`` (DamageCalculator.cs:54-57).

    Smallest attack (to within 1, rounded up) whose post-defence, post-percent damage reaches the target.
    """
    d = float(flat_defense)
    p = float(percent_multiplier)
    return solve_attack_damage(target_incoming, lambda a: apply_damage_defense(a, d) * p)


def get_battle_regen(health_regen: float, lifesteal_flat: float, difficulty: float) -> float:
    """``DamageCalculator.GetBattleRegen`` (DamageCalculator.cs:59-62)."""
    return (difficulty + 1.0) * health_regen + (difficulty + difficulty + 3.0) * lifesteal_flat


_DEF_MAP = {
    WeaponType.Stabbing: (StatType.StabDefenceFlat, StatType.StabDefencePercent),
    WeaponType.Cutting: (StatType.CutDefenceFlat, StatType.CutDefencePercent),
    WeaponType.Magic: (StatType.MagicDefenceFlat, StatType.MagicDefencePercent),
    WeaponType.Buckshot: (StatType.BuckshotDefenceFlat, StatType.BuckshotDefencePercent),
    WeaponType.Universal: (StatType.UniversalDefenceFlat, StatType.UniversalDefencePercent),
}


def weapon_type_to_defence(weapon_type: int, percent: bool) -> StatType:
    """``DamageCalculator.WeaponTypeToDefence`` (DamageCalculator.cs:269-280)."""
    try:
        flat, pct = _DEF_MAP[WeaponType(int(weapon_type))]
    except (KeyError, ValueError):
        raise ValueError(f"WeaponType out of range: {weapon_type}")
    return pct if percent else flat
