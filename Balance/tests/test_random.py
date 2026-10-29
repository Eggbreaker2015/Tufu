"""SimulationRandom (design 4.7, 7.3 C3c): Carry keeps every stream's integer total within 1 of its expected
total; Seeded streams are separated by name, so changing one stream leaves the others unchanged. The C# class
runs under .NET (tools/rebuildcheck) on the same calls as the Python port and must agree bit for bit."""

from __future__ import annotations

import math
import random
import struct
from fractions import Fraction

import pytest

from tufu_balance.unity import rebuildcheck, simrandom
from tufu_balance.unity.simrandom import CARRY, SEEDED, SimulationRandom

needs_dotnet = pytest.mark.skipif(not rebuildcheck.available(), reason="needs the .NET 8 SDK")
RUNTIMES = [pytest.param(rt, marks=pytest.mark.skipif(not rebuildcheck.available(rt), reason=f"needs {rt}"))
            for rt in rebuildcheck.RUNTIMES]

# Mixed case: StateText orders streams by ordinal (upper case before lower case), like real item Keys.
STREAMS = ("knife", "weapon_1", "armor_helmet_3", "Ω-stream", "", "Weapon_A", "armor_b", "Zeta", "ZZ_upper")


def _sequences(seed: int, n: int):
    """Adversarial expected counts: tiny and near-one fractions, integers, large values, negatives."""
    rng = random.Random(seed)
    makers = [lambda: rng.random(), lambda: 1 - 2 ** -rng.randint(20, 52), lambda: 2 ** -rng.randint(20, 60),
              lambda: float(rng.randint(0, 9)), lambda: rng.randint(0, 50) + rng.random(), lambda: rng.uniform(1e5, 1e6),
              lambda: 1 / 3, lambda: 0.1, lambda: -rng.random() * 3]
    return [(rng.choice(STREAMS), rng.choice(makers)()) for _ in range(n)]


def _run_py(mode, seed, calls):
    r = SimulationRandom(mode, seed)
    out = [r.roll(s, x) if x is not None else repr(r.next_double(s)) for s, x in calls]
    return out, r


@pytest.mark.parametrize("seed", [1, 2, 3])
def test_carry_keeps_every_stream_within_one(seed):
    calls = _sequences(seed, 20000)
    out, r = _run_py(CARRY, 1, calls)
    for stream in STREAMS:
        xs = [Fraction(x) for (s, x) in calls if s == stream]
        ns = [n for (s, _), n in zip(calls, out) if s == stream]
        diff = sum(xs) - sum(ns)
        assert -Fraction(1, 10 ** 9) < diff < 1, (stream, float(diff))
        assert abs(float(diff) - r.carried(stream)) < 1e-9
        assert all(isinstance(n, int) for n in ns)
    # Every prefix too: the carry never leaves [0, 1).
    carry = {s: Fraction(0) for s in STREAMS}
    for (s, x), n in zip(calls, out):
        carry[s] += Fraction(x) - n
        assert -Fraction(1, 10 ** 9) < carry[s] < 1


def test_carry_is_exact_for_simple_fractions():
    r = SimulationRandom(CARRY, 1)
    assert [r.roll("a", 0.25) for _ in range(8)] == [0, 0, 0, 1, 0, 0, 0, 1]
    assert [r.roll("b", 2.5) for _ in range(4)] == [2, 3, 2, 3]
    assert r.roll("c", 7.0) == 7 and r.carried("c") == 0.0


def test_seeded_is_unbiased_and_reproducible():
    a, b = SimulationRandom(SEEDED, 7), SimulationRandom(SEEDED, 7)
    xs = [0.3] * 20000
    na = [a.roll("item", x) for x in xs]
    assert na == [b.roll("item", x) for x in xs]
    assert abs(sum(na) / len(na) - 0.3) < 0.02
    other = SimulationRandom(SEEDED, 8)
    assert [other.roll("item", x) for x in xs] != na
    assert all(n in (0, 1) for n in na)


@pytest.mark.parametrize("mode", [CARRY, SEEDED])
def test_streams_are_independent(mode):
    base = _sequences(11, 3000)
    changed = []
    rng = random.Random(5)
    for s, x in base:  # stream "knife": other values, extra and dropped calls, extra uniform draws
        if s != "knife":
            changed.append((s, x))
            continue
        roll = rng.random()
        if roll < 0.2:
            continue
        changed.append((s, x * 1.7 + 0.13))
        if roll > 0.8:
            changed.append((s, None))
            changed.append((s, 0.5))
    out_a, ra = _run_py(mode, 3, base)
    out_b, rb = _run_py(mode, 3, changed)
    for stream in STREAMS:
        if stream == "knife":
            continue
        a = [n for (s, _), n in zip(base, out_a) if s == stream]
        b = [n for (s, _), n in zip(changed, out_b) if s == stream]
        assert a == b, stream
    lines = lambda r: {l.split("=")[0]: l for l in r.state_text().splitlines()[1:]}
    la, lb = lines(ra), lines(rb)
    assert {k: v for k, v in la.items() if k != "knife"} == {k: v for k, v in lb.items() if k != "knife"}
    if mode == SEEDED:
        assert la["knife"] != lb["knife"]


def test_stream_seed_depends_on_seed_and_name():
    seeds = {simrandom.stream_seed(seed, name) for seed in (1, 2, -1) for name in STREAMS}
    assert len(seeds) == 3 * len(STREAMS)


def test_bad_inputs():
    r = SimulationRandom(CARRY, 1)
    for bad in (math.nan, math.inf):
        with pytest.raises(ValueError):
            r.roll("a", bad)
    with pytest.raises(OverflowError):
        r.roll("a", 3e9)
    with pytest.raises(ValueError):
        SimulationRandom(2, 1)


# ---------------------------------------------------------------- the C# class, same calls

@pytest.mark.parametrize("runtime", RUNTIMES)
@pytest.mark.parametrize("mode,seed", [(CARRY, 1), (SEEDED, 1), (SEEDED, -123456789), (SEEDED, 2147483647)])
def test_csharp_matches_the_port_bit_for_bit(mode, seed, runtime):
    calls = _sequences(seed & 0xFFFF, 4000)
    rng = random.Random(seed)
    calls = [(s, None) if rng.random() < 0.1 else (s, x) for s, x in calls]
    out, r = _run_py(mode, seed, calls)
    [cs] = rebuildcheck.run([{"op": "random", "mode": mode, "seed": seed, "calls": [list(c) for c in calls]}], runtime)
    py = [n if isinstance(n, int) else n for n in out]
    got = [n if isinstance(n, int) else repr(float(n)) for n in cs["results"]]
    assert got == py
    assert cs["state"] == r.state_text()
    names = [line.split("=")[0] for line in cs["state"].splitlines()[1:]]
    assert names == sorted(names, key=lambda n: n.encode("utf-16-be")) != sorted(names, key=str.lower)
    for stream, bits in cs["carried"].items():
        assert bits == format(struct.unpack("<Q", struct.pack("<d", r.carried(stream)))[0], "016x")


@needs_dotnet
def test_csharp_streams_are_independent():
    base = [("a", 0.37), ("b", 0.61), ("a", 1.2), ("b", 0.05), ("c", None), ("b", 2.99)]
    changed = [("a", 0.9), ("a", None), ("b", 0.61), ("b", 0.05), ("c", None), ("a", 4.4), ("b", 2.99)]
    for mode in (CARRY, SEEDED):
        x, y = rebuildcheck.run([{"op": "random", "mode": mode, "seed": 9, "calls": [list(c) for c in seq]}
                                 for seq in (base, changed)])
        pick = lambda seq, res, s: [v for (t, _), v in zip(seq, res["results"]) if t == s]
        for stream in ("b", "c"):
            assert pick(base, x, stream) == pick(changed, y, stream)
