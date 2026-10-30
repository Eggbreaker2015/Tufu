"""Python port of the C# ``SimulationRandom`` (design 4.7; unit C3c).

Same streams, same bits: ``tests/test_random.py`` runs both on identical call sequences (the C# side under
.NET through ``Balance/tools/rebuildcheck``).

* ``Carry`` (``RollMode`` 0): ``roll(stream, x) = floor(x) + floor(carry + frac(x))``, the remainder stays in
  the stream's carry, so ``0 <= sum(x) - sum(n) < 1`` per stream (up to double rounding of the carry).
* ``Seeded`` (1): ``roll = floor(x) + (u < frac(x))`` with one SplitMix64 draw ``u`` per roll; the stream's
  start state is :func:`stream_seed` of the run seed and the stream name (FNV-1a 64 of its UTF-8 bytes).

Streams never share state, so the calls of one stream do not change any other stream's results.
"""

from __future__ import annotations

import math
import struct
from dataclasses import dataclass

CARRY, SEEDED = 0, 1
GOLDEN = 0x9E3779B97F4A7C15
MASK = (1 << 64) - 1
INT_MIN, INT_MAX = -(1 << 31), (1 << 31) - 1


def mix(z: int) -> int:
    z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & MASK
    z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & MASK
    return z ^ (z >> 31)


def stream_seed(seed: int, stream: str) -> int:
    h = 0xCBF29CE484222325
    for b in stream.encode("utf-8"):
        h = ((h ^ b) * 0x100000001B3) & MASK
    return mix((h + (seed & 0xFFFFFFFF) * GOLDEN) & MASK)


def _bits(x: float) -> str:
    return format(struct.unpack("<Q", struct.pack("<d", x))[0], "016x")


@dataclass
class _Stream:
    state: int
    carry: float = 0.0
    rolls: int = 0
    draws: int = 0


class SimulationRandom:
    def __init__(self, mode: int, seed: int):
        if mode not in (CARRY, SEEDED):
            raise ValueError(f"unknown RollMode {mode}")
        if not INT_MIN <= seed <= INT_MAX:
            raise ValueError("seed must be a C# int")
        self.mode, self.seed = mode, seed
        self._streams: dict[str, _Stream] = {}

    def _get(self, stream: str) -> _Stream:
        s = self._streams.get(stream)
        if s is None:
            s = self._streams[stream] = _Stream(stream_seed(self.seed, stream))
        return s

    @staticmethod
    def _uniform(s: _Stream) -> float:
        s.draws += 1
        s.state = (s.state + GOLDEN) & MASK
        return (mix(s.state) >> 11) * (1.0 / 9007199254740992.0)

    def roll(self, stream: str, x: float) -> int:
        if math.isnan(x) or math.isinf(x):
            raise ValueError(f"roll({stream}): x = {x}")
        s = self._get(stream)
        s.rolls += 1
        whole = float(math.floor(x))
        fraction = x - whole
        if self.mode == CARRY:
            carry = s.carry + fraction
            extra = float(math.floor(carry))
            s.carry = carry - extra
        else:
            extra = 1.0 if self._uniform(s) < fraction else 0.0
        n = whole + extra
        if not INT_MIN <= n <= INT_MAX:
            raise OverflowError(f"roll({stream}): {n} does not fit an int")
        return int(n)

    def next_double(self, stream: str) -> float:
        return self._uniform(self._get(stream))

    def carried(self, stream: str) -> float:
        s = self._streams.get(stream)
        return s.carry if s else 0.0

    def state_text(self) -> str:
        """``SimulationRandom.StateText()``: mode and seed, then per stream (ordinal name order) rolls, draws,
        carry bits and generator state."""
        lines = [f"mode={self.mode},seed={self.seed}"]
        for name in sorted(self._streams, key=lambda n: n.encode("utf-16-be")):
            s = self._streams[name]
            lines.append(f"{name}={s.rolls},{s.draws},{_bits(s.carry)},{s.state:016x}")
        return "\n".join(lines) + "\n"
