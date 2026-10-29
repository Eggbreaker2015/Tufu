"""Stat "specific value" curves (diminishing returns).

Port of ``Codebase.Unit.Stat`` at commit 6af011f:

* :func:`calculate_specific_value` -- ``Unit/Stat.cs:61-79`` (``Stat.CalculateSpecificValue``)
* :func:`clamp_specific_value` -- ``Unit/Stat.cs:51-59`` (cap 0.8 for Evasion/BlockChance/BlockAmount)
* :func:`specific_value_pipeline` -- ``Unit/Stat.cs:81-90`` (``Stat.UpdateValue``)

All arithmetic is ``double`` in C#; ``Math.Pow`` maps to C ``pow`` which Python's
``math.pow`` also calls.
"""

from __future__ import annotations

import math

from .enums import StatType

CAPPED_SPECIFIC_VALUE_LIMIT = 0.8  # Unit/Stat.cs:17


def _pow(x: float, y: float) -> float:
    # C pow(): negative base with non-integer exponent -> NaN (C# returns NaN too).
    try:
        return math.pow(x, y)
    except ValueError:
        return math.nan


def calculate_specific_value(stat_type: int, value: float) -> float:
    """``Stat.CalculateSpecificValue(StatType, double)`` -- Unit/Stat.cs:61-79."""
    v = float(value)
    t = int(stat_type)
    if t == StatType.MovementSpeed:
        return _pow(v, 0.115) * 4.6 if v < 10000.0 else _pow(v, 0.1) * 3.0 + 5.8
    if t == StatType.Evasion:
        return _pow(v, 0.12) * 0.06 if v < 700.0 else _pow(v, 0.065) * 0.05 + 0.06
    if t == StatType.CritChance:
        return _pow(v, 0.15) * 0.08 if v <= 700.0 else _pow(v, 0.1) * 0.07 + 0.082
    if t == StatType.CritDamagePercent:
        return _pow(v, 0.08) * 1.5
    if t == StatType.SuperCritDamagePercent:
        return 2.0
    if t in (StatType.GlobalAttack, StatType.GlobalDefence):
        return v if v <= 100.0 else _pow(v - 100.0, 0.75) + 100.0
    if t == StatType.BlockChance:
        return _pow(v, 0.1) * 0.08
    if t == StatType.BlockAmount:
        return _pow(v, 0.053) * 0.15
    return v


def is_specific_value_cappable(stat_type: int) -> bool:
    """Unit/Stat.cs:51-54."""
    return int(stat_type) in (StatType.Evasion, StatType.BlockChance, StatType.BlockAmount)


def clamp_specific_value(stat_type: int, value: float) -> float:
    """Unit/Stat.cs:56-59."""
    return min(value, CAPPED_SPECIFIC_VALUE_LIMIT) if is_specific_value_cappable(stat_type) else value


def specific_value_pipeline(stat_type: int, raw: float, post_additive: float = 0.0,
                            post_multiplicative: tuple[float, ...] = (), post_flat: float = 0.0) -> float:
    """``Stat.UpdateValue`` (Unit/Stat.cs:81-110) for a raw value plus post-formula modifiers.

    ``SpecificValue = Clamp(type, mult * (Calc(type, raw) + flat))`` with
    ``mult = prod(1 + m_i) * (1 + sum(additive))``. The spawner bake does NOT use this
    pipeline: it calls :func:`calculate_specific_value` directly on snapshot TotalValues.
    """
    mult = 1.0
    for m in post_multiplicative:
        mult *= 1.0 + m
    mult = mult * (post_additive + 1.0)
    return clamp_specific_value(stat_type, mult * (calculate_specific_value(stat_type, raw) + post_flat))
