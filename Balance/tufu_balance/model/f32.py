"""IEEE helpers that reproduce C#/IL2CPP numeric semantics in Python.

C# ``float`` arithmetic on ARM64 IL2CPP is IEEE binary32; ``double`` is binary64.
Every function in this package that ports a C# ``float`` expression goes through
:func:`f32` after *each* operation, so intermediate rounding matches the original.

``round_half_even`` is .NET ``Math.Round(double)`` (MidpointRounding.ToEven),
which Python's built-in ``round`` already implements on the exact binary value.
"""

from __future__ import annotations

import math

import numpy as np

F32 = np.float32


def f32(x) -> np.float32:
    """Round a value to binary32 (C# ``(float)x``)."""
    return np.float32(x)


def fadd(a, b) -> np.float32:
    return np.float32(np.float32(a) + np.float32(b))


def fsub(a, b) -> np.float32:
    return np.float32(np.float32(a) - np.float32(b))


def fmul(a, b) -> np.float32:
    return np.float32(np.float32(a) * np.float32(b))


def fdiv(a, b) -> np.float32:
    a = np.float32(a)
    b = np.float32(b)
    with np.errstate(divide="ignore", invalid="ignore"):
        return np.float32(a / b)


def ddiv(a: float, b: float) -> float:
    """C# ``double / double`` with IEEE semantics (x/0 -> +-inf, 0/0 -> NaN)."""
    a = float(a)
    b = float(b)
    if b == 0.0:
        if a == 0.0 or math.isnan(a):
            return math.nan
        return math.copysign(math.inf, a) * math.copysign(1.0, b)
    return a / b


def round_half_even(x: float) -> float:
    """.NET ``Math.Round(double)``: banker's rounding; inf/NaN pass through."""
    x = float(x)
    if math.isinf(x) or math.isnan(x):
        return x
    return float(round(x))
