"""Interpolation rounding (G11, design 3.6 item 1, Q32) and the correctly rounded ``fmaf``."""

from __future__ import annotations

import ast
import json
import math
import random
import re
import timeit
from fractions import Fraction
from pathlib import Path

import numpy as np
import pytest

from tufu_balance.golden import at_most, check, exact, same_set
from tufu_balance.model import data as MD
from tufu_balance.model import interpolation as I
from tufu_balance.model.f32 import f32, fmaf, fmaf_py, r32, ulp_diff32
from tufu_balance.model.interpolation import ROUNDINGS, Rounding

MODEL = Path(I.__file__).resolve().parent
MAIN = {6, 8, 12, 14, 16, 20, 1, 2, 22}
DIRECT = {1, 2, 22}
# Design 3.3 G11 (and fixtures/interpolation_rounding.json for the stepwise-vs-other grid counts).
EXPECTED = {
    "stepwise": {"k2": 108, "k3": 105, "sff": 972, "pm": 945, "tf": 972, "tv": 960},
    "fma": {"k2": 109, "k3": 109, "sff": 981, "pm": 981, "tf": 981, "tv": 981},
    "double_tail": {"k2": 109, "k3": 109, "sff": 981, "pm": 981, "tf": 981, "tv": 981},
    "double_all": {"k2": 109, "k3": 109, "sff": 981, "pm": 981, "tf": 981, "tv": 981},
}
GRID_EXPECTED = {("stepwise", "fma"): 842, ("stepwise", "double_tail"): 875, ("stepwise", "double_all"): 1015,
                 ("fma", "double_tail"): 123, ("fma", "double_all"): 263, ("double_tail", "double_all"): 140}


@pytest.fixture(scope="module")
def sd(db):
    return MD.load_simulation_data(db)


@pytest.fixture(scope="module")
def interp(db):
    return MD.load_interpolations(db)


def g11_counts(sd, interp, rounding):
    out = {"n": 0, "k2": 0, "k3": 0, "cells": 0, "sff": 0, "pm": 0, "tf": 0, "tv": 0, "other_n": 0, "other": 0}
    fails = []
    for lname, lst in (("time_chunk_snapshots", sd.time_chunk_snapshots), ("snapshots", sd.snapshots)):
        for i, s in enumerate(lst):
            out["n"] += 1
            d = f32(s.time_chunk_index)
            k2 = float(interp.total_coefficient(d, 2, rounding=rounding))
            k3 = float(interp.total_coefficient(d, 3, rounding=rounding))
            main = [st for st in s.stats if st.key in MAIN]
            a = all(st.stats_from_features_flat == k2 for st in main)
            b = all(st.percent_multiplier == k3 for st in main)
            out["k2"] += a
            out["k3"] += b
            if not (a and b):
                fails.append([lname, i])
            for st in s.stats:
                if st.key in MAIN:
                    tf = st.value * (1.0 + k2)
                    tv = tf * k3 if st.key in DIRECT else tf
                    out["cells"] += 1
                    out["sff"] += st.stats_from_features_flat == k2
                    out["pm"] += st.percent_multiplier == k3
                    out["tf"] += st.total_flat_value == tf
                    out["tv"] += st.total_value == tv
                else:
                    out["other_n"] += 1
                    out["other"] += (st.stats_from_features_flat == 0 and st.percent_multiplier == 1
                                     and st.total_flat_value == st.value and st.total_value == st.value)
    return out, fails


def test_g11_all_roundings(sd, interp, load_fixture):
    fx = load_fixture("interpolation_rounding")["roundings"]
    results = {}
    for r in ROUNDINGS:
        c, fails = g11_counts(sd, interp, r)
        exp = EXPECTED[r.value]
        assert (c["n"], c["cells"], c["other_n"]) == (109, 981, 2289)
        f = fx[r.value]
        assert (c["k2"], c["k3"]) == (f["snapshots"]["k2_ok"], f["snapshots"]["k3_ok"])
        assert [c[k] for k in ("sff", "pm", "tf", "tv")] == [f["main_cells"][k] for k in ("sff", "pm", "tf", "tv")]
        assert c["other"] == f["other_cells"]["ok"] == 2289
        assert fails == [[x["list"], x["index"]] for x in f["failing_snapshots"]]
        for k in ("k2", "k3", "sff", "pm", "tf", "tv"):
            results[f"G11.{r.value}.{k}"] = exact(c[k], source="G11", original=exp[k])
        results[f"G11.{r.value}.other_cells"] = exact(c["other"], source="G11", original=2289)
        results[f"G11.{r.value}.failing_snapshots"] = same_set(
            fails, source="G11", original=[[x["list"], x["index"]] for x in f["failing_snapshots"]])
    c, _ = g11_counts(sd, interp, None)
    for k in ("k2", "k3", "sff", "pm", "tf", "tv"):
        results[f"G11.default.{k}"] = exact(c[k], source="G11", note="interpolation_default.json rounding",
                                            original=EXPECTED["fma"][k])
    check("core", results)


def test_g11_grid_differences(interp, load_fixture):
    grid = {r: [float(it.evaluate_rounded(f32(t / 4), r)) for it in interp.interpolations for t in range(3200)]
            for r in ROUNDINGS}
    assert len(grid[Rounding.FMA]) == 60800
    fx = {(p["a"], p["b"]): p["differ"] for p in load_fixture("interpolation_rounding")["grid_differences"]}
    results = {}
    for i, a in enumerate(ROUNDINGS):
        for b in ROUNDINGS[i + 1:]:
            n = sum(x != y for x, y in zip(grid[a], grid[b]))
            assert n == fx[(a.value, b.value)]
            results[f"G11.grid_diff.{a.value}.{b.value}"] = exact(n, source="G11", note="of 60800 grid points",
                                                                  original=GRID_EXPECTED[(a.value, b.value)])
    # The differences are single-ulp nudges: record the ulp spread per pair (DP3 compares the editor the same way).
    spread = max(abs(ulp_diff32(x, y)) for a in ROUNDINGS for b in ROUNDINGS for x, y in zip(grid[a], grid[b]))
    results["G11.grid_max_ulp_between_roundings"] = exact(spread, source="G11", note="measured on first run")
    check("core", results)


def test_k_at_day_405(interp, sd):
    last = sd.time_chunk_snapshots[-1]
    assert last.time_chunk_index == 405
    for r in ROUNDINGS:
        k2 = interp.total_coefficient(405, 2, rounding=r)
        k3 = interp.total_coefficient(405, 3, rounding=r)
        assert k2 == np.float32(3.0000002)
        # Design text "4.9075f" is this value to 5 significant digits; f32(4.9075) itself is 1 ulp-ish lower.
        assert k3 == np.float32(4.90752) and abs(float(k3) - 4.9075) < 5e-5
        assert float(k2) == last.stat(1).stats_from_features_flat and float(k3) == last.stat(1).percent_multiplier
    check("core", {
        "G11.k2_405": exact(float(interp.total_coefficient(405, 2)), source="G11/P2", original=float(np.float32(3.0000002))),
        "G11.k3_405": exact(float(interp.total_coefficient(405, 3)), source="G11/P2", original=float(np.float32(4.90752))),
    })


# ----------------------------------------------------------------------------- default and entry points
def test_default_comes_only_from_json():
    data = json.loads((MODEL / "interpolation_default.json").read_text(encoding="utf-8"))
    assert I.DEFAULT_FILE == MODEL / "interpolation_default.json"
    assert I.default_rounding() == Rounding(data["rounding"])
    assert set(data["allowed"]) == {r.value for r in Rounding}
    assert I.resolve_rounding(None) is I.default_rounding()
    # No other module names a rounding as a default.
    for path in MODEL.glob("*.py"):
        if path.name == "interpolation.py":
            continue
        assert not re.search(r"\bRounding\.(STEPWISE|FMA|DOUBLE_TAIL|DOUBLE_ALL)\b|\brounding=[\"']",
                             path.read_text(encoding="utf-8")), path.name


def test_default_rounding_switch(monkeypatch, tmp_path, interp):
    for r in ROUNDINGS:
        p = tmp_path / f"{r.value}.json"
        p.write_text(json.dumps({"rounding": r.value}), encoding="utf-8")
        monkeypatch.setattr(I, "DEFAULT_FILE", p)
        I.default_rounding.cache_clear()
        try:
            assert I.default_rounding() is r
            for d in (0, 33, 94, 154, 405):
                assert interp.total_coefficient(d, 3) == interp.total_coefficient(d, 3, rounding=r)
        finally:
            I.default_rounding.cache_clear()


def _stage4_evaluate(it, timing):
    """BalanceInterpolation.evaluate as of 7da12da (stage 4), numpy binary32 per operation."""
    timing = f32(timing)
    timings = I.parse_points(it.timing_points)
    coefs = I.parse_points(it.coef_points)
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


def test_stage4_entry_points_unchanged(interp):
    ts = [t / 4 for t in range(-8, 3300)] + [math.inf, -math.inf, 1e30]
    for it in interp.interpolations:
        for t in ts:
            a, b = it.evaluate(t), _stage4_evaluate(it, t)
            assert a.tobytes() == b.tobytes()
            assert it.evaluate_rounded(t, "stepwise").tobytes() == a.tobytes()
        assert math.isnan(it.evaluate(math.nan)) and math.isnan(_stage4_evaluate(it, math.nan))
    for ty in range(8):
        for rarity in (2, 4):
            for d in range(0, 451):
                assert interp.get_total_coefficient(d, ty, rarity) == interp.total_coefficient(d, ty, rarity, "stepwise")
    bad = I.BalanceInterpolation(True, "bad", 2, "1; 2", "3")
    assert bad.evaluate(1.5) == 0.0 and bad.evaluate_rounded(1.5, "fma") == 0.0


def test_total_coefficient_dispatches_through_evaluate(interp):
    """The W0 generator swaps ``evaluate`` on the class; ``get_total_coefficient`` must follow it."""
    cls = I.BalanceInterpolation
    orig = cls.evaluate
    try:
        cls.evaluate = lambda self, timing: f32(2.0)
        assert interp.get_total_coefficient(100, 3) == f32(2.0) ** sum(1 for it in interp.interpolations if it.type == 3)
    finally:
        cls.evaluate = orig


def test_cache_follows_curve_edits(interp):
    cfg = I.BalanceInterpolationsConfig(list(interp.interpolations))
    before = cfg.total_coefficient(100, 3)
    i = next(i for i, it in enumerate(cfg.interpolations) if it.type == 3)
    from dataclasses import replace
    cfg.interpolations[i] = replace(cfg.interpolations[i], coef_points="1; 1")
    after = cfg.total_coefficient(100, 3)
    assert after != before and after == cfg.get_total_coefficient(100, 3)


@pytest.mark.perf
def test_evaluate_cost_after_caching(interp):
    it = next(it for it in interp.interpolations if it.name == "EventsMult")
    interp.total_coefficient(300.0, 3)
    it.evaluate_rounded(300.0, "fma")
    n = 5000
    cached = min(timeit.repeat(lambda: interp.total_coefficient(300.0, 3), number=n, repeat=5)) / n
    curve = min(timeit.repeat(lambda: it.evaluate_rounded(300.0, "fma"), number=n, repeat=5)) / n
    legacy = min(timeit.repeat(lambda: it.evaluate(300.0), number=n, repeat=5)) / n
    assert cached <= 2e-6, cached
    assert curve <= 2e-6, curve
    assert legacy <= 2e-6, legacy


# ----------------------------------------------------------------------------- fmaf
def _round_fraction(x: Fraction, mant_bits: int, emin: int, emax: int) -> float:
    """Exact round-to-nearest-even of a rational to a binary format with ``mant_bits`` (incl. hidden bit)."""
    if x == 0:
        return 0.0
    sign = -1.0 if x < 0 else 1.0
    x = abs(x)
    e = x.numerator.bit_length() - x.denominator.bit_length()
    if Fraction(2) ** e > x:
        e -= 1
    q = max(e, emin) - (mant_bits - 1)  # quantum exponent
    scaled = x / Fraction(2) ** q
    n = scaled.numerator // scaled.denominator
    rem = scaled - n
    if rem > Fraction(1, 2) or (rem == Fraction(1, 2) and n % 2 == 1):
        n += 1
    if n * Fraction(2) ** q >= Fraction(2) ** (emax + 1):
        return sign * math.inf
    return sign * math.ldexp(float(n), q)


def exact_f32(x: Fraction) -> float:
    return _round_fraction(x, 24, -126, 127)


def exact_f64(x: Fraction) -> float:
    return _round_fraction(x, 53, -1022, 1023)


def exact_fma(a, b, c) -> Fraction:
    return Fraction(float(a)) * Fraction(float(b)) + Fraction(float(c))


def test_round_fraction_helper_matches_hardware():
    rng = random.Random(1)
    for _ in range(2000):
        x = rng.uniform(-1e6, 1e6) * 2.0 ** rng.randint(-160, 120)
        assert exact_f32(Fraction(x)) == r32(x) or (math.isinf(r32(x)) and math.isinf(exact_f32(Fraction(x))))
        y = Fraction(rng.getrandbits(80), rng.getrandbits(40) | 1)
        assert exact_f64(y) == float(y)


def test_math_fma_is_correctly_rounded_binary64():
    rng = random.Random(2)
    naive_wrong = 0
    for _ in range(3000):
        a = rng.uniform(-2, 2) * 2.0 ** rng.randint(-60, 60)
        b = rng.uniform(-2, 2) * 2.0 ** rng.randint(-60, 60)
        c = -a * b * (1 + rng.uniform(-1e-9, 1e-9)) if rng.random() < 0.5 else rng.uniform(-2, 2) * 2.0 ** rng.randint(-60, 60)
        want = exact_f64(exact_fma(a, b, c))
        assert math.fma(a, b, c) == want
        naive_wrong += (a * b + c) != want
    # Cancellation cases where an unfused a*b+c is wrong, so the check above has teeth.
    assert naive_wrong > 100
    x = 1 + 2.0 ** -30
    assert math.fma(x, x, -1 - 2.0 ** -29) == 2.0 ** -60 != x * x - 1 - 2.0 ** -29


def _tie_cases():
    """(a, b, c) binary32 triples whose binary64 fma lands exactly on a binary32 midpoint while the exact
    value does not: ``a*b = 2^s (1 - 2^-2j)`` or ``-2^s (1 - 2^-2j)``, ``c`` one half-ulp away."""
    cases = []
    for j in (13, 14, 15, 17, 20, 23):
        a = 1.0 + 2.0 ** -j
        for s in (-24, -23, -30, 0, 10, -140, -146, 100):
            b = (1.0 - 2.0 ** -j) * 2.0 ** s
            for sign in (1.0, -1.0):
                # c's binary32 ulp is 2^(s+1): its exponent is s+24 (or the subnormal floor).
                for cm in (1.0, 1.0 + 2.0 ** -23, 1.0 + 3 * 2.0 ** -23, 1.5, 1.9999998807907104):
                    c = cm * 2.0 ** (s + 24)
                    if not (math.isfinite(c) and r32(c) == c and r32(a) == a and r32(b) == b):
                        continue
                    cases.append((sign * a, b, sign * c))
                    cases.append((sign * a, b, -sign * c + sign * 2.0 ** (s + 25)))
    # subnormal: c = 0, product just below/above the smallest subnormal's half
    for j in (13, 20, 23):
        a = 1.0 + 2.0 ** -j
        b = (1.0 - 2.0 ** -j) * 2.0 ** -150
        if r32(b) == b:
            cases.append((a, b, 0.0))
            cases.append((a, b, 2.0 ** -149))
    return cases


def test_fmaf_matches_exact_rounding_on_tie_cases():
    cases = _tie_cases()
    assert len(cases) > 100
    naive_wrong = 0
    for a, b, c in cases:
        want = exact_f32(exact_fma(a, b, c))
        got = fmaf(a, b, c)
        assert float(got) == want, (a, b, c)
        naive = r32(math.fma(a, b, c))
        naive_wrong += naive != want
    # A plain (float)fma(double) double-rounds and is wrong on a good share of these constructed ties.
    assert naive_wrong >= 40, naive_wrong
    # The textbook case: 1 + 2^-23 + 2^-24 - 2^-58 must round down to 1 + 2^-23.
    a, b, c = 1.0 + 2.0 ** -17, 2.0 ** -24 - 2.0 ** -41, 1.0 + 2.0 ** -23
    assert float(fmaf(a, b, c)) == 1.0 + 2.0 ** -23 != r32(math.fma(a, b, c))


def test_fmaf_matches_exact_rounding_random_and_special():
    rng = random.Random(3)
    for _ in range(20000):
        a = r32(rng.uniform(-4, 4) * 2.0 ** rng.randint(-30, 30))
        b = r32(rng.uniform(-4, 4) * 2.0 ** rng.randint(-30, 30))
        c = r32(-a * b * (1 + rng.uniform(-1e-6, 1e-6))) if rng.random() < 0.3 else r32(rng.uniform(-4, 4) * 2.0 ** rng.randint(-60, 60))
        assert float(fmaf(a, b, c)) == exact_f32(exact_fma(a, b, c))
    big = float(np.finfo(np.float32).max)
    ulp = 2.0 ** 104
    # Overflow boundary: exact value just below / at FLT_MAX + half an ulp.
    assert fmaf(big, 1.0, ulp / 2 * (1 - 2.0 ** -24)) == np.float32(big)
    assert math.isinf(fmaf(big, 1.0, ulp / 2)) and math.isinf(fmaf(big, 2.0, 0.0))
    assert fmaf(-0.0, 1.0, -0.0).tobytes() == np.float32(-0.0).tobytes()
    assert math.isnan(fmaf(math.inf, 0.0, 1.0)) and math.isnan(fmaf(math.nan, 1.0, 1.0))
    assert fmaf(math.inf, 2.0, 1.0) == np.float32(math.inf)
    assert fmaf_py(2.0, 3.0, 1.0) == 7.0


def test_fmaf_only_called_from_evaluate():
    """Design 1.5 Python rule 3: other float paths round stepwise; only Evaluate's fma variant fuses."""
    allowed = {("f32.py", "fmaf"), ("interpolation.py", "_evaluate")}
    seen = set()
    for path in sorted((MODEL.parent).rglob("*.py")):
        tree = ast.parse(path.read_text(encoding="utf-8"))
        for fn in [n for n in ast.walk(tree) if isinstance(n, (ast.FunctionDef, ast.AsyncFunctionDef))]:
            for node in ast.walk(fn):
                if not isinstance(node, ast.Call):
                    continue
                f = node.func
                name = f.id if isinstance(f, ast.Name) else f.attr if isinstance(f, ast.Attribute) else None
                if name in ("fmaf", "fmaf_py"):
                    seen.add((path.name, fn.name))
                if name == "fma":
                    assert path.name == "f32.py", f"math.fma used in {path}"
    assert seen <= allowed | {("f32.py", "fmaf_py")}, seen
    assert ("interpolation.py", "_evaluate") in seen


def test_parse_points_are_correctly_rounded(interp):
    """``float.Parse`` rounds decimal -> binary32 once; ``f32(float(s))`` rounds twice. Equal on every stored point."""
    for it in interp.interpolations:
        for text in (it.timing_points, it.coef_points, it.island_points):
            for part in (p.strip() for p in (text or "").split(";")):
                if part:
                    assert r32(float(part)) == exact_f32(Fraction(part)), part


def test_stage4_double_patch_still_works(monkeypatch, interp):
    """analysis/bake_check.py (H9) replaces the module's ``f32`` with np.float64 to evaluate in double."""
    it = next(it for it in interp.interpolations if it.name == "AmuletMult")
    single = it.evaluate(100.3)
    monkeypatch.setattr(I, "f32", np.float64)
    double = it.evaluate(100.3)
    assert isinstance(double, np.float64) and double == I._evaluate_generic(it, 100.3)
    assert abs(float(double) - float(single)) < 1e-6
    t, l, r = 100.3, 56.0, 130.0
    assert double == 1.0 + (t - l) / (r - l) * (1.2 - 1.0)
