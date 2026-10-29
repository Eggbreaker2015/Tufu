"""Balance interpolation curves (CombatBalanceConfig.Interpolations).

Ports, at commit 6af011f:

* :meth:`BalanceInterpolation.evaluate` -- ``Balance/BalanceInterpolation.cs:23-52``
  (native ELF VA 0x1579ed4); piecewise-linear in binary32.
* :func:`parse_points` -- ``Balance/BalanceInterpolation.cs:58-64`` (VA 0x157a65c).
* :meth:`BalanceInterpolationsConfig.get_total_coefficient` --
  ``Balance/BalanceInterpolationsConfig.cs:12-27`` (VA 0x1579dac): product of all
  enabled curves for types 1/3, sum for types 0/2/4/5/6/7.
* :meth:`BalanceInterpolationsConfig.get_interpolation_coefficient` --
  ``Balance/CombatBalanceConfig.cs:135-145`` (VA 0x157dd80 / 0x157dd98), default rarity Rare.
"""

from __future__ import annotations

from dataclasses import dataclass, field

import numpy as np

from .enums import BalanceInterpolationType, Rarity
from .f32 import f32


def parse_points(value: str | None) -> list[np.float32]:
    """``BalanceInterpolation.ParsePoints``: split on ';', trim, drop empties, float.Parse."""
    if value is None or str(value).strip() == "":
        return []
    out = []
    for part in str(value).split(";"):
        part = part.strip()
        if part:
            out.append(f32(float(part)))
    return out


@dataclass(frozen=True)
class BalanceInterpolation:
    use: bool
    name: str
    type: int
    timing_points: str
    coef_points: str
    island_points: str = ""

    def evaluate(self, timing) -> np.float32:
        """``BalanceInterpolation.Evaluate(float)`` -- BalanceInterpolation.cs:23-52."""
        timing = f32(timing)
        timings = parse_points(self.timing_points)
        coefs = parse_points(self.coef_points)
        if len(timings) < 2 or len(coefs) < 2 or len(timings) != len(coefs):
            return f32(0.0)
        if timings[0] >= timing:
            return coefs[0]
        for i in range(len(timings) - 1):
            left = timings[i]
            right = timings[i + 1]
            if right < timing:
                continue
            progress = f32(0.0)
            if left != right:
                raw = f32(f32(timing - left) / f32(right - left))
                capped = f32(1.0) if raw > 1.0 else raw
                progress = f32(0.0) if raw < 0.0 else capped
            first = coefs[i]
            second = coefs[i + 1]
            factor = f32(0.0) if progress < 0.0 else progress
            return f32(first + f32(factor * f32(second - first)))
        return coefs[-1]


_ADDITIVE_MASK = 0xF5  # bits 0,2,4,5,6,7


@dataclass
class BalanceInterpolationsConfig:
    interpolations: list[BalanceInterpolation] = field(default_factory=list)

    def get_total_coefficient(self, timing, type_: int, rarity: int = Rarity.Rare) -> np.float32:
        """``BalanceInterpolationsConfig.GetTotalCoefficient`` -- BalanceInterpolationsConfig.cs:12-27."""
        t = int(type_)
        if int(rarity) == Rarity.Legendary and t == BalanceInterpolationType.ItemEmission:
            t = int(BalanceInterpolationType.ItemLegendaryEmission)
        multiply = not (0 <= t < 8 and ((1 << t) & _ADDITIVE_MASK) != 0)
        total = f32(1.0 if multiply else 0.0)
        for it in self.interpolations:
            if int(it.type) != t or not it.use:
                continue
            v = it.evaluate(timing)
            total = f32(total * v) if multiply else f32(total + v)
        return total

    def get_interpolation_coefficient(self, timing, type_: int, rarity: int = Rarity.Rare) -> np.float32:
        """``CombatBalanceConfig.GetInterpolationCoefficient`` -- CombatBalanceConfig.cs:135-145."""
        return self.get_total_coefficient(timing, type_, rarity)
