"""Balance interpolation curves (CombatBalanceConfig.Interpolations).

Ports, at commit 6af011f:

* :meth:`BalanceInterpolation.evaluate` / :meth:`BalanceInterpolation.evaluate_rounded` --
  ``Balance/BalanceInterpolation.cs:23-52`` (native ELF VA 0x1579ed4); piecewise-linear in binary32.
* :func:`parse_points` -- ``Balance/BalanceInterpolation.cs:58-64`` (VA 0x157a65c).
* :meth:`BalanceInterpolationsConfig.get_total_coefficient` / :meth:`~BalanceInterpolationsConfig.total_coefficient`
  -- ``Balance/BalanceInterpolationsConfig.cs:12-27`` (VA 0x1579dac): product of all enabled curves
  for types 1/3, sum for types 0/2/4/5/6/7.
* :meth:`BalanceInterpolationsConfig.get_interpolation_coefficient` --
  ``Balance/CombatBalanceConfig.cs:135-145`` (VA 0x157dd80 / 0x157dd98), default rarity Rare.

Rounding of the final lerp ``first + factor * (second - first)`` (design 3.6 item 1, G11, Q32):

* ``stepwise``    -- binary32 after every operation (what the C# source literally says);
* ``fma``         -- one fused multiply-add rounded once to binary32 (``MathF.FusedMultiplyAdd``);
* ``double_tail`` -- progress in binary32, lerp evaluated in binary64, then one rounding to binary32;
* ``double_all``  -- progress quotient taken in binary64 and rounded to binary32, then lerp as ``double_tail``
  (exactly the variant the W0 fixture ``interpolation_rounding.json`` measured).

The shipped Simulation Data rules out ``stepwise`` (K2 108/109, K3 105/109) but cannot tell the
other three apart (all 109/109). The default of the rounding-aware API is read from
``interpolation_default.json`` next to this file and nowhere else; unit C2 sets it to the variant
the editor's Mono shows in DP3.

Two entry points exist on purpose:

* ``evaluate`` / ``get_total_coefficient`` / ``get_interpolation_coefficient`` keep the stage-4
  behaviour, i.e. ``stepwise``. The immutable W0 fixture generator freezes numbers through them and
  swaps ``BalanceInterpolation.evaluate`` on the class to compare roundings, so their semantics and
  the ``get_total_coefficient -> it.evaluate`` dispatch must not change.
* ``evaluate_rounded`` / ``total_coefficient`` take ``rounding`` (``None`` = the JSON default);
  ``total_coefficient`` is cached by (type, rarity, timing, rounding). New oracle code uses these.
"""

from __future__ import annotations

import json
import math
from dataclasses import dataclass, field
from enum import Enum
from functools import lru_cache
from pathlib import Path

import numpy as np

from .enums import BalanceInterpolationType, Rarity
from .f32 import f32, fmaf_py, r32
from .registry import Provenance, implements

DEFAULT_FILE = Path(__file__).with_name("interpolation_default.json")
_F32 = f32  # analysis/bake_check.py (H9) swaps this module's ``f32`` for np.float64 to run in double


class Rounding(str, Enum):
    STEPWISE = "stepwise"
    FMA = "fma"
    DOUBLE_TAIL = "double_tail"
    DOUBLE_ALL = "double_all"


ROUNDINGS = tuple(Rounding)


@lru_cache(maxsize=1)
def default_rounding() -> Rounding:
    """The rounding used for ``rounding=None``: ``interpolation_default.json`` (owned by C2 after P2)."""
    data = json.loads(DEFAULT_FILE.read_text(encoding="utf-8"))
    return Rounding(data["rounding"])


def resolve_rounding(rounding: Rounding | str | None) -> Rounding:
    return default_rounding() if rounding is None else Rounding(rounding)


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


@lru_cache(maxsize=4096)
def _points(value: str | None) -> tuple[float, ...]:
    return tuple(float(x) for x in parse_points(value))


def _div(a: float, b: float) -> float:
    try:
        return a / b
    except ZeroDivisionError:  # unreachable for distinct binary32 points; IEEE result for safety
        return math.nan if a == 0.0 or math.isnan(a) else math.copysign(math.inf, a) * math.copysign(1.0, b)


def _evaluate(timings: tuple[float, ...], coefs: tuple[float, ...], timing: float, rounding: Rounding) -> float:
    """Body of ``Evaluate`` on binary32 values held in Python floats; ``timing`` is already binary32.

    Comparisons keep the C# NaN behaviour (ordered comparisons with NaN are false in both languages).
    """
    n = len(timings)
    if n < 2 or len(coefs) < 2 or n != len(coefs):
        return 0.0  # C# also logs an error
    if timings[0] >= timing:
        return coefs[0]
    for i in range(n - 1):
        left = timings[i]
        right = timings[i + 1]
        if right < timing:
            continue
        progress = 0.0
        if left != right:
            if rounding is Rounding.DOUBLE_ALL:
                raw = r32(_div(timing - left, right - left))
            else:
                raw = r32(_div(r32(timing - left), r32(right - left)))
            capped = 1.0 if raw > 1.0 else raw
            progress = 0.0 if raw < 0.0 else capped
        first = coefs[i]
        second = coefs[i + 1]
        factor = 0.0 if progress < 0.0 else progress
        if rounding is Rounding.STEPWISE:
            return r32(first + r32(factor * r32(second - first)))
        if rounding is Rounding.FMA:
            return fmaf_py(factor, r32(second - first), first)
        return r32(first + factor * (second - first))
    return coefs[-1]


def _evaluate_generic(it: "BalanceInterpolation", timing):
    """Stage-4 body of ``evaluate`` through the module-level ``f32``; used only while ``f32`` is replaced."""
    timing = f32(timing)
    timings = parse_points(it.timing_points)
    coefs = parse_points(it.coef_points)
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


@dataclass(frozen=True)
class BalanceInterpolation:
    use: bool
    name: str
    type: int
    timing_points: str
    coef_points: str
    island_points: str = ""

    @implements("balance.interpolation_evaluate", provenance=Provenance.PORT,
                code_ref="CodeBase/Balance/BalanceInterpolation.cs:23-64", va="0x1579ed4")
    def evaluate(self, timing) -> np.float32:
        """``BalanceInterpolation.Evaluate(float)`` with ``stepwise`` rounding (stage-4 entry point)."""
        if f32 is not _F32:
            return _evaluate_generic(self, timing)
        return np.float32(_evaluate(_points(self.timing_points), _points(self.coef_points),
                                    float(f32(timing)), Rounding.STEPWISE))

    @implements("balance.interpolation_evaluate", provenance=Provenance.PORT,
                code_ref="CodeBase/Balance/BalanceInterpolation.cs:23-64", va="0x1579ed4")
    def evaluate_rounded(self, timing, rounding: Rounding | str | None = None) -> np.float32:
        """``Evaluate`` with the given rounding of the final lerp (``None`` = :func:`default_rounding`)."""
        return np.float32(_evaluate(_points(self.timing_points), _points(self.coef_points),
                                    float(f32(timing)), resolve_rounding(rounding)))


_ADDITIVE_MASK = 0xF5  # bits 0,2,4,5,6,7
_CACHE_LIMIT = 200_000


def _effective_type(type_: int, rarity: int) -> tuple[int, bool]:
    t = int(type_)
    if int(rarity) == Rarity.Legendary and t == BalanceInterpolationType.ItemEmission:
        t = int(BalanceInterpolationType.ItemLegendaryEmission)
    multiply = not (0 <= t < 8 and ((1 << t) & _ADDITIVE_MASK) != 0)
    return t, multiply


@dataclass
class BalanceInterpolationsConfig:
    interpolations: list[BalanceInterpolation] = field(default_factory=list)

    @implements("balance.total_coefficient", provenance=Provenance.PORT,
                code_ref="CodeBase/Balance/BalanceInterpolationsConfig.cs:12-27", va="0x1579dac")
    def get_total_coefficient(self, timing, type_: int, rarity: int = Rarity.Rare) -> np.float32:
        """``BalanceInterpolationsConfig.GetTotalCoefficient`` with ``stepwise`` rounding (stage-4 entry point)."""
        t, multiply = _effective_type(type_, rarity)
        total = f32(1.0 if multiply else 0.0)
        for it in self.interpolations:
            if int(it.type) != t or not it.use:
                continue
            v = it.evaluate(timing)
            total = f32(total * v) if multiply else f32(total + v)
        return total

    def get_interpolation_coefficient(self, timing, type_: int, rarity: int = Rarity.Rare) -> np.float32:
        """``CombatBalanceConfig.GetInterpolationCoefficient`` -- CombatBalanceConfig.cs:135-145 (``stepwise``)."""
        return self.get_total_coefficient(timing, type_, rarity)

    @implements("balance.total_coefficient", provenance=Provenance.PORT,
                code_ref="CodeBase/Balance/BalanceInterpolationsConfig.cs:12-27", va="0x1579dac")
    def total_coefficient(self, timing, type_: int, rarity: int = Rarity.Rare,
                          rounding: Rounding | str | None = None) -> np.float32:
        """``GetTotalCoefficient`` / ``GetInterpolationCoefficient`` with a rounding (``None`` = default).

        Cached by (type, rarity, timing, rounding). The cache is dropped when the curve list changes;
        the list is compared by value, so replacing a curve inside ``interpolations`` is seen.
        """
        r = default_rounding() if rounding is None else Rounding(rounding)
        key = (int(type_), int(rarity), float(f32(timing)), r)
        cache = self.__dict__.get("_cache")
        current = tuple(self.interpolations)
        if cache is None or cache[0] != current:
            cache = (current, {}, {})
            self.__dict__["_cache"] = cache
        values = cache[1]
        v = values.get(key)
        if v is None:
            v = self._compute_total(key, cache[2])
            if len(values) >= _CACHE_LIMIT:
                values.clear()
            values[key] = v
        return v

    def _compute_total(self, key, groups) -> np.float32:
        type_, rarity, timing, rounding = key
        t, multiply = _effective_type(type_, rarity)
        curves = groups.get(t)
        if curves is None:
            curves = groups[t] = tuple((_points(it.timing_points), _points(it.coef_points))
                                       for it in self.interpolations if int(it.type) == t and it.use)
        total = 1.0 if multiply else 0.0
        for timings, coefs in curves:
            v = _evaluate(timings, coefs, timing, rounding)
            total = r32(total * v) if multiply else r32(total + v)
        return np.float32(total)
