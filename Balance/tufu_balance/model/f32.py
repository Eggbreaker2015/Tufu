"""IEEE helpers that reproduce C#/IL2CPP numeric semantics in Python.

C# ``float`` arithmetic on ARM64 IL2CPP is IEEE binary32; ``double`` is binary64.
Every function in this package that ports a C# ``float`` expression goes through
:func:`f32` after *each* operation, so intermediate rounding matches the original.

``round_half_even`` is .NET ``Math.Round(double)`` (MidpointRounding.ToEven),
which Python's built-in ``round`` already implements on the exact binary value.

``r32`` rounds a Python float to the nearest binary32 value and returns it as a Python float;
binary32 ``+ - * /`` computed in binary64 and then passed through ``r32`` are exact emulations
(binary64 has more than 2*24+2 bits, so the double rounding is innocuous). That does NOT hold for
a fused multiply-add: :func:`fmaf` handles it. ``fmaf`` is used only by the ``fma`` rounding of
``BalanceInterpolation.Evaluate`` (design 1.5, Python rule 3).
"""

from __future__ import annotations

import math
import struct

import numpy as np

F32 = np.float32
_F32 = struct.Struct("<f")
_U32 = struct.Struct("<I")


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


def r32(x: float) -> float:
    """Round a binary64 value to binary32 (nearest, ties to even) and return it as a Python float."""
    try:
        return _F32.unpack(_F32.pack(x))[0]
    except OverflowError:  # older Pythons refuse to pack out-of-range values
        with np.errstate(over="ignore"):
            return float(np.float32(x))


def fmaf(a, b, c) -> np.float32:
    """C# ``MathF.FusedMultiplyAdd(a, b, c)``: ``a*b + c`` on binary32 inputs, rounded once to binary32."""
    return np.float32(fmaf_py(float(np.float32(a)), float(np.float32(b)), float(np.float32(c))))


def fmaf_py(a: float, b: float, c: float) -> float:
    """:func:`fmaf` on Python floats that already hold binary32 values (no conversion, faster).

    ``math.fma`` gives the correctly rounded binary64 value ``s`` of ``a*b + c``. Rounding ``s`` again
    to binary32 is wrong exactly when ``s`` lies on a binary32 midpoint (a tie) while the exact value
    does not. ``a*b`` of two binary32 values is exact in binary64 (48 significant bits, no under- or
    overflow), so TwoSum(``a*b``, ``c``) gives the exact residual ``e`` with ``s + e == a*b + c``; at a
    tie the sign of ``e`` picks the side. Only the ``fma`` rounding of ``Evaluate`` may call this.
    """
    p = a * b
    if not (math.isfinite(p) and math.isfinite(c)):
        return r32(p + c)  # inf/nan propagate as in IEEE fma (a*b is exact)
    s = math.fma(a, b, c)
    if s != 0.0:
        # binary32 quantum at |s| is 2**q; s is a tie iff s / 2**(q-1) is an odd integer.
        q = math.frexp(s)[1] - 24
        if q < -149:
            q = -149
        x = math.ldexp(s, 1 - q)
        if x.is_integer() and int(x) & 1:
            t = s - p
            e = (p - (s - t)) + (c - t)
            if e != 0.0:
                s = math.nextafter(s, math.inf if e > 0.0 else -math.inf)
    return r32(s)


def bits32(x) -> int:
    """Raw IEEE bits of a binary32 value."""
    return _U32.unpack(_F32.pack(float(np.float32(x))))[0]


def ulp_diff32(a, b) -> int:
    """Signed distance in binary32 ulps from ``a`` to ``b`` (both finite; +0 and -0 are the same point)."""
    def ordered(x):
        u = bits32(x)
        return u if u < 0x80000000 else 0x80000000 - u
    return ordered(b) - ordered(a)
