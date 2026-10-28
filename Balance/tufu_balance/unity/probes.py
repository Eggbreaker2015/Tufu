"""Differential probes: the game's C# formulas against the Python oracles (design 3.4 DP1-DP7; unit C2).

Python generates the input grids from a fixed seed (:func:`generate_inputs`), they are committed as
``Balance/tests/data/probes_in.json`` and replayed by every probe run. A probe job is an ``empty`` job
with a ``probes`` field (:func:`probe_job`); ``SimulationPlayModeDriver`` runs the edit-mode probes
(``ED/SimulationProbes.cs``: DP1-DP5) before the job enters Play and ``SimulationHarness`` runs the
Play-mode ones (``SimulationRuntimeProbes``: DP3 again, DP6, DP7) when it finishes. Results are
``<job>/probes_edit.json`` and ``<job>/probes_runtime.json``. Every number crosses the boundary as the
hex of its IEEE bits (doubles 16 hex digits, floats 8, ints 8, concatenated per grid), so nothing
depends on number formatting.

The ``compare_dp*`` functions take a probe output and return a plain dict of counts, mismatches and
(DP3) ulp histograms; ``tests/unity/test_differential.py`` asserts on them and freezes them in the
golden suite ``differential``. ``python -m tufu_balance.unity.probes [--write]`` checks (or rewrites)
the committed inputs.
"""

from __future__ import annotations

import argparse
import collections
import hashlib
import json
import math
import random
import sqlite3
import statistics
import struct
import sys
from pathlib import Path
from typing import Any, Iterable, Mapping, Sequence

import numpy as np

from . import request as req
from . import schema
from .runner import BALANCE, WORKTREE
from ..model import bake as B
from ..model import data as MD
from ..model import items as IT
from ..model import spawners as SP
from ..model.defense import (apply_damage_defense, get_battle_regen, solve_attack_damage_for_incoming,
                             weapon_type_to_defence)
from ..model.enums import Rarity, StatType, WeaponType
from ..model.f32 import bits32, f32, r32, ulp_diff32
from ..model.interpolation import ROUNDINGS, Rounding
from ..model.snapshots import SimulationData, SimulationSnapshot
from ..model.stats import calculate_specific_value, clamp_specific_value

SEED = 20261028
GENERATOR = "tufu_balance.unity.probes/1"
SCHEMA = "tufu.sim.probes/1"
INPUT_FILE = BALANCE / "tests" / "data" / "probes_in.json"
EDIT_FILE, RUNTIME_FILE = "probes_edit.json", "probes_runtime.json"
EDIT_PROBES = ("DP1", "DP2", "DP3", "DP4", "DP5")
PLAY_PROBES = ("DP3", "DP6", "DP7")
DP3_INTEGERS, DP3_FRACTIONAL = 801, 1000  # t = 0..800, then 1000 fractional points (design 3.4)

req.register_fields("job", {"probes": (dict, None)})
schema.register_manifest_keys({"probes": "object"})


# ---------------------------------------------------------------- hex codec (SimulationRuntimeProbes.D/F/I)

def dhex(x: float) -> str:
    return struct.pack(">d", float(x)).hex()


def fhex(x) -> str:
    return struct.pack(">f", float(np.float32(x))).hex()


def doubles(text: str) -> list[float]:
    return [struct.unpack(">d", bytes.fromhex(text[i:i + 16]))[0] for i in range(0, len(text), 16)]


def floats(text: str) -> list[float]:
    """binary32 values widened to Python floats."""
    return [struct.unpack(">f", bytes.fromhex(text[i:i + 8]))[0] for i in range(0, len(text), 8)]


def _int_hex(v: int) -> str:
    return format(int(v) & 0xFFFFFFFF, "08x")


def _int32(text: str) -> int:
    v = int(text, 16)
    return v - (1 << 32) if v >= 1 << 31 else v


def _same(a: float, b: float) -> bool:
    """Bit-equal, except that every NaN equals every NaN (payloads differ between Unity's YAML and Python)."""
    if math.isnan(a) or math.isnan(b):
        return math.isnan(a) and math.isnan(b)
    return struct.pack(">d", a) == struct.pack(">d", b)


def _same32(a: float, b: float) -> bool:
    if math.isnan(a) or math.isnan(b):
        return math.isnan(a) and math.isnan(b)
    return bits32(a) == bits32(b)


# ---------------------------------------------------------------- input generation

def _log_grid(lo: float, hi: float, n: int) -> list[float]:
    return [10.0 ** (lo + (hi - lo) * i / (n - 1)) for i in range(n)]


def _near(x: float) -> list[float]:
    return [math.nextafter(x, -math.inf), x, math.nextafter(x, math.inf)]


def _u(rng: random.Random, lo: float, hi: float) -> float:
    return lo + (hi - lo) * rng.random()


def _i(rng: random.Random, lo: int, hi: int) -> int:
    """Uniform integer in [lo, hi] from ``random()`` only (stable across Python versions)."""
    return lo + min(hi - lo, int(rng.random() * (hi - lo + 1)))


def _dp1_inputs() -> dict:
    special = [0.0, -0.0, -1.0, 1.0, 5e-324, math.nan, math.inf, -math.inf]
    for b in (100.0, 700.0, 10000.0):
        special += _near(b)
    x = _log_grid(-4.0, 8.0, 200) + special
    return {"types": [int(t) for t in StatType], "x": "".join(map(dhex, x)), "n_grid": 200}


def _dp2_inputs(rng: random.Random) -> dict:
    attack = [0.0, -5.0, math.nan] + _log_grid(-2.0, 8.0, 30)
    defense = [0.0] + _log_grid(-2.0, 8.0, 30)
    pairs = [(a, d) for a in attack for d in defense]
    solve = [(0.0, 10.0, 0.5), (-3.0, 0.0, 1.0), (1.0, 0.0, 1.0), (0.5, 0.0, 1.0)]
    for _ in range(400):
        target = 10.0 ** _u(rng, -1.0, 7.0)
        d = 0.0 if rng.random() < 0.2 else 10.0 ** _u(rng, -1.0, 7.0)
        solve.append((target, d, _u(rng, 0.05, 1.0)))
    regen = [(0.0, 0.0, 0.0), (1.0, 1.0, 1.0)]
    for _ in range(300):
        regen.append((10.0 ** _u(rng, -2.0, 6.0), 0.0 if rng.random() < 0.3 else 10.0 ** _u(rng, -2.0, 6.0),
                      1.0 if rng.random() < 0.3 else _u(rng, 0.0, 5.0)))
    return {
        "apply": {"attack": "".join(dhex(a) for a, _ in pairs), "defense": "".join(dhex(d) for _, d in pairs)},
        "solve": {k: "".join(dhex(t[i]) for t in solve) for i, k in enumerate(("target", "defense", "percent"))},
        "regen": {k: "".join(dhex(t[i]) for t in regen) for i, k in enumerate(("health_regen", "lifesteal_flat", "difficulty"))},
        "weapon_types": list(range(-1, max(WeaponType) + 3)),
    }


def _dp3_inputs(rng: random.Random) -> dict:
    t = [float(i) for i in range(DP3_INTEGERS)]
    t += [r32(_u(rng, 0.0, 800.0)) for _ in range(DP3_FRACTIONAL)]
    t += [-1.0, -0.0, 800.5, 1.0e6, math.nan, math.inf, -math.inf]
    return {"t": "".join(map(fhex, t)), "n_integers": DP3_INTEGERS, "n_fractional": DP3_FRACTIONAL,
            "types": list(range(0, 9)), "rarities": [int(r) for r in Rarity]}


def _dp4_inputs(rng: random.Random, sd: SimulationData) -> dict:
    tc = [float(i) for i in range(-2, 411)] + [r32(_u(rng, -1.0, 410.0)) for _ in range(400)]
    tc += [math.nan, math.inf, -math.inf]
    early = [-1.0 + 0.25 * i for i in range(25)] + [r32(_u(rng, -0.5, 4.5)) for _ in range(50)] + [math.nan]
    pts = sorted({float(s.playtime) for s in sd.snapshots})
    play = list(pts) + [float(f32(f32(a + b) / f32(2.0))) for a, b in zip(pts, pts[1:])]
    play += [r32(_u(rng, -10.0, 260.0)) for _ in range(300)] + [-1.0e6, 1.0e6, math.nan]
    return {"tc": "".join(map(fhex, tc)), "tc_early": "".join(map(fhex, early)), "playtime": "".join(map(fhex, play))}


def g18_samples(items: Mapping[str, IT.ItemDef], tables: IT.ItemTables) -> list[tuple[str, int, int, int]]:
    """(key, Level, Copies, Evolved) of both sides of every G18 jump, in ``evolve_jumps`` order."""
    out = []
    for j in IT.evolve_jumps(IT.g18_items(items), tables):
        it = items[j.key]
        e = j.evolution_level
        before = IT.ItemState(False, 1, j.shards) if e == 1 else IT.ItemState(True, e - 1, j.shards)
        after = IT.evolve(it, before, tables)
        for st in (before, after):
            out.append((j.key, st.level, st.copies, int(st.evolved)))
    return out


def _dp7_inputs(rng: random.Random, items: Mapping[str, IT.ItemDef], tables: IT.ItemTables) -> dict:
    samples = list(dict.fromkeys(g18_samples(items, tables)))
    top = tables.max_evolution_level + 2
    for key in sorted(items):
        samples.append((key, 1, 1, 0))
        for _ in range(6):
            samples.append((key, _i(rng, 1, top), _i(rng, 0, 60), int(rng.random() < 0.5)))
    return {"samples": [list(s) for s in samples]}


def generate_inputs(con: sqlite3.Connection) -> dict:
    """The probe input grids; deterministic for a database (each DP draws from its own seeded stream)."""
    sd = MD.load_simulation_data(con)
    items, tables = IT.load_item_defs(con), IT.load_item_tables(con)
    return {
        "schema": "tufu.sim.probes_in/1",
        "generator": GENERATOR,
        "seed": SEED,
        "dp1": _dp1_inputs(),
        "dp2": _dp2_inputs(random.Random(SEED + 2)),
        "dp3": _dp3_inputs(random.Random(SEED + 3)),
        "dp4": _dp4_inputs(random.Random(SEED + 4), sd),
        "dp7": _dp7_inputs(random.Random(SEED + 7), items, tables),
    }


def dumps_inputs(inputs: Mapping) -> bytes:
    return (json.dumps(inputs, indent=1, sort_keys=True) + "\n").encode()


def input_ref(path: Path = INPUT_FILE) -> tuple[str, str]:
    """(path relative to the repository root, sha256) for the request."""
    data = Path(path).read_bytes()
    return str(Path(path).resolve().relative_to(WORKTREE)), hashlib.sha256(data).hexdigest()


def load_inputs(path: Path = INPUT_FILE) -> dict:
    return json.loads(Path(path).read_text())


def probe_job(job_id: str = "probes", *, edit: Sequence[str] = EDIT_PROBES, runtime: Sequence[str] = PLAY_PROBES,
              path: Path = INPUT_FILE, frames: int = 30) -> dict:
    """An ``empty`` job that runs the probes (no simulation)."""
    rel, sha = input_ref(path)
    job = req.empty_job(job_id, frames=frames)
    job["probes"] = {"input": rel, "input_sha256": sha, "edit": list(edit), "runtime": list(runtime)}
    validate_probe_options(job["probes"])
    return job


def validate_probe_options(p: Mapping) -> None:
    if set(p) - {"input", "input_sha256", "edit", "runtime"} or not {"input", "input_sha256"} <= set(p):
        raise req.RequestError(f"probes: fields must be input, input_sha256[, edit, runtime], got {sorted(p)}")
    if Path(p["input"]).is_absolute() or ".." in Path(p["input"]).parts:
        raise req.RequestError("probes.input must be relative to the repository root")
    if not set(p.get("edit", ())) <= set(EDIT_PROBES) or not set(p.get("runtime", ())) <= set(PLAY_PROBES):
        raise req.RequestError(f"probes: edit within {EDIT_PROBES}, runtime within {PLAY_PROBES}")


# ---------------------------------------------------------------- DP1, DP2

def compare_dp1(out: Mapping, inputs: Mapping) -> dict:
    x = doubles(inputs["x"])
    res = {"values": 0, "specific_equal": 0, "clamped_equal": 0, "types": len(inputs["types"]), "mismatches": []}
    for t in inputs["types"]:
        cs, cc = doubles(out["specific"][str(t)]), doubles(out["clamped"][str(t)])
        for v, a, b in zip(x, cs, cc):
            ps = calculate_specific_value(t, v)
            pc = clamp_specific_value(t, ps)
            res["values"] += 1
            res["specific_equal"] += _same(a, ps)
            res["clamped_equal"] += _same(b, pc)
            if not (_same(a, ps) and _same(b, pc)) and len(res["mismatches"]) < 20:
                res["mismatches"].append({"type": t, "x": v, "csharp": [a, b], "oracle": [ps, pc]})
    return res


def _err(f):
    try:
        return f()
    except (ValueError, KeyError, IndexError, IT.MissingTableRow, B.BakeError) as e:
        return "error:" + type(e).__name__


def compare_dp2(out: Mapping, inputs: Mapping) -> dict:
    res: dict[str, Any] = {"mismatches": []}

    def tally(name, csharp, oracle, args):
        eq = [_same(a, b) for a, b in zip(csharp, oracle)]
        res[name] = {"n": len(eq), "equal": sum(eq)}
        res["mismatches"] += [{"probe": name, "args": a, "csharp": c, "oracle": o}
                              for a, c, o, e in zip(args, csharp, oracle, eq) if not e][:10]

    a, d = doubles(inputs["apply"]["attack"]), doubles(inputs["apply"]["defense"])
    tally("apply", doubles(out["apply"]), [apply_damage_defense(*p) for p in zip(a, d)], list(zip(a, d)))
    s = [doubles(inputs["solve"][k]) for k in ("target", "defense", "percent")]
    tally("solve", doubles(out["solve"]), [solve_attack_damage_for_incoming(*p) for p in zip(*s)], list(zip(*s)))
    r = [doubles(inputs["regen"][k]) for k in ("health_regen", "lifesteal_flat", "difficulty")]
    tally("regen", doubles(out["regen"]), [get_battle_regen(*p) for p in zip(*r)], list(zip(*r)))
    eq = 0
    for row in out["defence"]:
        wt = row[0]
        oracle = [_err(lambda p=p: int(weapon_type_to_defence(wt, p))) for p in (False, True)]
        csharp = [v if isinstance(v, int) else "error:" for v in row[1:]]
        ok = all((isinstance(c, int) and c == o) or (isinstance(c, str) and isinstance(o, str)) for c, o in zip(csharp, oracle))
        eq += ok
        if not ok:
            res["mismatches"].append({"probe": "defence", "weapon_type": wt, "csharp": row[1:], "oracle": oracle})
    res["defence"] = {"n": len(out["defence"]), "equal": eq}
    return res


# ---------------------------------------------------------------- DP3

def _histogram(diffs: Iterable[int]) -> dict[str, int]:
    c = collections.Counter(diffs)
    return {str(k): c[k] for k in sorted(c)}


def _ulp(a: float, b: float) -> int | str:
    if math.isnan(a) or math.isnan(b):
        return 0 if math.isnan(a) and math.isnan(b) else "nan"
    if math.isinf(a) or math.isinf(b):
        return 0 if a == b else "inf"
    return ulp_diff32(b, a)  # csharp - oracle, in ulps


def compare_dp3(out: Mapping, inputs: Mapping, interpolations) -> dict:
    """ulp histograms (C# minus oracle) of every curve and every coefficient against the four roundings.

    ``main`` counts the 1801 design points (t = 0..800 and 1000 fractional); the specials (negative,
    beyond the last point, NaN, infinities) are listed separately.
    """
    t = floats(inputs["t"])
    n_main = inputs["n_integers"] + inputs["n_fractional"]
    curves = interpolations.interpolations
    if [(c["name"], c["type"], c["use"]) for c in out["curves"]] != [(c.name, c.type, c.use) for c in curves]:
        raise AssertionError("DP3: the editor's curve list differs from the database's")
    res: dict[str, Any] = {"points": len(t), "main_points": n_main, "curves": len(curves), "by_rounding": {}}
    for r in ROUNDINGS:
        per: dict[str, Any] = {"curve_values": 0, "curve_equal": 0, "coef_values": 0, "coef_equal": 0,
                               "special_equal": True, "curve_hist": collections.Counter(), "coef_hist": collections.Counter()}
        for c, oc in zip(out["curves"], curves):
            cs = floats(c["values"])
            for i, (x, v) in enumerate(zip(t, cs)):
                o = float(oc.evaluate_rounded(x, r))
                if i < n_main:
                    per["curve_values"] += 1
                    per["curve_equal"] += _same32(v, o)
                    per["curve_hist"][_ulp(v, o)] += 1
                elif not _same32(v, o):
                    per["special_equal"] = False
        coef_sets = [(int(k), int(Rarity.Rare), v) for k, v in out["coef"].items()]
        coef_sets += [(int(k.split(":")[0]), int(k.split(":")[1]), v) for k, v in out["coef_rarity"].items()]
        for type_, rarity, text in coef_sets:
            for i, (x, v) in enumerate(zip(t, floats(text))):
                o = float(interpolations.total_coefficient(x, type_, rarity, rounding=r))
                if i < n_main:
                    per["coef_values"] += 1
                    per["coef_equal"] += _same32(v, o)
                    per["coef_hist"][_ulp(v, o)] += 1
                elif not _same32(v, o):
                    per["special_equal"] = False
        per["curve_hist"] = _histogram_any(per["curve_hist"])
        per["coef_hist"] = _histogram_any(per["coef_hist"])
        per["bit_equal"] = (per["curve_equal"] == per["curve_values"] and per["coef_equal"] == per["coef_values"]
                            and per["special_equal"])
        res["by_rounding"][r.value] = per
    res["variants_bit_equal"] = [r for r, p in res["by_rounding"].items() if p["bit_equal"]]
    return res


def _histogram_any(c: collections.Counter) -> dict[str, int]:
    return {str(k): c[k] for k in sorted(c, key=lambda k: (isinstance(k, str), k if not isinstance(k, str) else 0, str(k)))}


def k2k3_from_csharp(out: Mapping, inputs: Mapping) -> dict[int, tuple[float, float]]:
    """C# K2/K3 at the integer days 0..800 (``GetInterpolationCoefficient(d, 2|3)``)."""
    t = floats(inputs["t"])[:inputs["n_integers"]]
    k2, k3 = floats(out["coef"]["2"]), floats(out["coef"]["3"])
    return {int(x): (k2[i], k3[i]) for i, x in enumerate(t)}


MAIN_STATS = {6, 8, 12, 14, 16, 20, 1, 2, 22}  # SimulationSystem.MainInterpolationStats


def g11_on_csharp(sd: SimulationData, k: Mapping[int, tuple[float, float]]) -> dict:
    """Snapshots whose main stats' SFF/PM equal the C# K2/K3 of their day (design 3.4 DP3 anchor)."""
    n = k2 = k3 = 0
    fails = []
    for lname, lst in (("time_chunk_snapshots", sd.time_chunk_snapshots), ("snapshots", sd.snapshots)):
        for i, s in enumerate(lst):
            n += 1
            a2, a3 = k[s.time_chunk_index]
            main = [st for st in s.stats if st.key in MAIN_STATS]
            ok2 = all(st.stats_from_features_flat == a2 for st in main)
            ok3 = all(st.percent_multiplier == a3 for st in main)
            k2 += ok2
            k3 += ok3
            if not (ok2 and ok3):
                fails.append([lname, i])
    return {"n": n, "k2": k2, "k3": k3, "failing_snapshots": fails}


# ---------------------------------------------------------------- the editor's float arithmetic (diagnosis)
#
# DP3 shows the editor's Mono JIT evaluating float expressions in binary64 and rounding to binary32 only
# where a value is stored (a local the compiler keeps, a field, a float return value, a float argument):
# that is exactly the ``double_all`` rounding of ``Evaluate``. The other float paths of the oracles round
# after every operation (design 1.5, Python rule 3), so a few probe values differ. These functions
# evaluate the three affected C# lines the editor's way; they only classify differences (the
# comparisons report how many differences they explain) and are not oracles.

def mono_total_playtime(left: SimulationSnapshot, right: SimulationSnapshot, x: float) -> float:
    """``InterpolateSnapshot`` TotalPlaytime (SimulationData.cs:76): t is a float argument (rounded once),
    ``a + (b - a) * t`` in binary64, rounded once on the store to the float field."""
    t = r32((float(x) - left.time_chunk_index) / (right.time_chunk_index - left.time_chunk_index))
    a, b = float(left.total_playtime), float(right.total_playtime)
    return r32(a + (b - a) * t)


def mono_new_health_raw(o: B.SpawnerBakeReport, sp: B.SpawnerBakeInput) -> float:
    """``newHealthRaw`` (SimulationConfig.cs:158): ``(double)(1f - usedResist)`` is never rounded to float."""
    from ..model.f32 import ddiv
    return ddiv(o.player_dps * float(sp.average_fight_time), float(sp.count)) * (1.0 - float(o.armor_resist_used))


def mono_item_values(it: IT.ItemDef, st: IT.ItemState, tables: IT.ItemTables) -> list:
    """``CalcDamage``/``CalcArmor`` (ItemModel.cs:475-518): the float locals are compiled away, so the
    whole sum is evaluated in binary64 and returned as double; DPS divides that by the float cooldown."""
    def value(armor: bool) -> float:
        lvl = st.effective_level
        if st.evolved:
            row = tables.evolution_row(armor, it.damage_type, it.rarity, lvl)
            v = float(row.value) + float(row.copy_value) * float(st.copies)
        elif armor:
            v = float(tables.armor_base_value(it.damage_type, it.rarity)) + float(tables.armor_growth_value(it.damage_type, it.rarity)) * lvl
        else:
            v = float(tables.damage_base(it.damage_type, it.rarity)) + float(tables.damage_growth(it.damage_type, it.rarity)) * lvl
        return v if armor else v + float(it.flat_damage)
    damage = _err(lambda: value(False))
    return [damage, _err(lambda: value(True)),
            damage if isinstance(damage, str) else damage / float(it.attack_cooldown),
            _err(lambda: IT.levels_to_evolve(it, st, tables))]


def f32_ulps(csharp: float, oracle: float) -> str:
    """``csharp - oracle`` in binary32 ulps of the oracle value (C# may hold an unrounded double)."""
    if not (math.isfinite(csharp) and math.isfinite(oracle)):
        return "0" if _same(csharp, oracle) else "nonfinite"
    return format((csharp - oracle) / float(np.spacing(np.float32(abs(oracle)))), "g")


# ---------------------------------------------------------------- DP4

def encode_snapshot(s: SimulationSnapshot) -> dict:
    """The fields of SimulationRuntimeProbes.Snapshot, from an oracle snapshot."""
    return {
        "p": fhex(s.playtime), "tp": fhex(s.total_playtime), "loc": int(s.location), "tci": int(s.time_chunk_index),
        "stats": [(st.key, st.value, st.stats_from_features_flat, st.percent_multiplier, st.total_flat_value,
                   st.total_value, st.current_gpm) for st in s.stats],
        "dps": list(s.dps_data), "def": list(s.defense_data),
        "items": [[it.key, it.level, it.copies] for it in s.items],
    }


def _decode_pairs(text: str) -> list[tuple[int, float]]:
    return [(_int32(text[i:i + 8]), doubles(text[i + 8:i + 24])[0]) for i in range(0, len(text), 24)]


def _decode_stats(text: str) -> list[tuple]:
    step = 8 + 6 * 16
    return [(_int32(text[i:i + 8]), *doubles(text[i + 8:i + step])) for i in range(0, len(text), step)]


def _snapshot_diffs(c: Mapping, o: Mapping) -> list[str]:
    diffs = [k for k in ("p", "tp") if not _same32(floats(c[k])[0], floats(o[k])[0])]
    diffs += [k for k in ("loc", "tci") if c[k] != o[k]]
    cs, os_ = _decode_stats(c["stats"]), o["stats"]
    if len(cs) != len(os_) or any(a[0] != b[0] or not all(_same(x, y) for x, y in zip(a[1:], b[1:])) for a, b in zip(cs, os_)):
        diffs.append("stats")
    for k in ("dps", "def"):
        cp, op = _decode_pairs(c[k]), o[k]
        if len(cp) != len(op) or any(a[0] != b[0] or not _same(a[1], b[1]) for a, b in zip(cp, op)):
            diffs.append(k)
    if [list(x) for x in c["items"]] != o["items"]:
        diffs.append("items")
    return diffs


def _expected_source(result: SimulationSnapshot, x: float, lst: list, tag: str) -> tuple:
    """(src, items_src) the C# probe reports: the stored element, or None plus the left neighbour's items."""
    if result.source_index is not None:
        return f"{tag}:{result.source_index}", f"{tag}:{result.source_index}"
    return None, f"{tag}:{_neighbours(lst, x)[0].source_index}"


def compare_dp4(out: Mapping, inputs: Mapping, sd: SimulationData) -> dict:
    res: dict[str, Any] = {"mismatches": [], "tp_ulp_hist": collections.Counter(), "tp_explained_by_mono": 0}
    cases = (("tc", lambda x: sd.get_by_time_chunk_index(x), sd.time_chunk_snapshots, "t"),
             ("tc_early", lambda x: sd.get_by_time_chunk_index(x, sd.snapshots), sd.snapshots, "s"),
             ("playtime", lambda x: sd.get_by_playtime(x), sd.snapshots, "s"))
    for name, lookup, lst, tag in cases:
        xs = floats(inputs[name])
        eq = 0
        for x, c in zip(xs, out[name]):
            o_snap = lookup(x)
            diffs = _snapshot_diffs(c, encode_snapshot(o_snap))
            if name != "playtime":
                src, items_src = _expected_source(o_snap, x, lst, tag)
            else:
                src = items_src = f"s:{o_snap.source_index}"
            if c["src"] != src:
                diffs.append("src")
            if c["items_src"] != items_src:
                diffs.append("items_src")
            if diffs == ["tp"] and o_snap.source_index is None:
                ctp = floats(c["tp"])[0]
                res["tp_ulp_hist"][ulp_diff32(float(o_snap.total_playtime), ctp)] += 1
                left, right = _neighbours(lst, x)
                res["tp_explained_by_mono"] += _same32(ctp, mono_total_playtime(left, right, x))
            eq += not diffs
            if diffs:
                res["mismatches"].append({"lookup": name, "x": x, "fields": diffs, "csharp_src": c["src"], "oracle_src": src})
        res[name] = {"n": len(xs), "equal": eq}
    res["mismatch_fields"] = dict(collections.Counter(f for m in res["mismatches"] for f in m["fields"]))
    res["mismatch_count"] = sum(v["n"] - v["equal"] for k, v in res.items() if k in ("tc", "tc_early", "playtime"))
    res["tp_ulp_hist"] = _histogram(res["tp_ulp_hist"].elements())
    return res


def _neighbours(lst: list, x: float) -> tuple[SimulationSnapshot, SimulationSnapshot]:
    """The pair ``GetByTimeChunkIndex`` interpolates between (first i with left <= x <= right)."""
    ordered = sorted(lst, key=lambda s: s.time_chunk_index)
    for a, b in zip(ordered, ordered[1:]):
        if a.time_chunk_index <= x <= b.time_chunk_index:
            return a, b
    raise ValueError(f"{x} is not inside the list")


def g19_on_csharp(out: Mapping, inputs: Mapping, sd: SimulationData) -> dict:
    """The day-snapshot sequence the C# lookup gives at the stored days (design 3.4 DP4 anchor, G19).

    At a stored day inside the list, ``GetByTimeChunkIndex`` interpolates with t = 1 (it does not return
    the stored element), so the anchor is the TimeChunkIndex and Location sequence."""
    xs = floats(inputs["tc"])
    got = [out["tc"][xs.index(float(s.time_chunk_index))] for s in sd.time_chunk_snapshots]
    return {"days": [c["tci"] for c in got],
            "locations_equal": [c["loc"] for c in got] == [s.location for s in sd.time_chunk_snapshots]}


# ---------------------------------------------------------------- DP5

DP5_FIELDS = {  # oracle attribute -> (C# report field, kind); kind f = float32, d = double
    "recalc_optimum_time": ("ReCalcOptimumTime", "f"), "optimum_time": ("OptimumTime", "f"),
    "snapshot_playtime": ("Playtime", "f"), "armor_resist_raw": ("ArmorResistRaw", "f"),
    "armor_resist_used": ("ArmorResistUsed", "f"), "enemies_count": ("EnemiesCount", "f"),
    "difficulty_used": ("DifficultyUsed", "f"), "crit_chance": ("CritChance", "d"), "crit_damage": ("CritDamage", "d"),
    "dps_by_armor_type": ("DpsByArmorType", "d"), "player_dps": ("PlayerDps", "d"), "defense": ("Defense", "d"),
    "battle_regen": ("BattleRegen", "d"), "evasion": ("PlayerEvasion", "d"),
    "block_multiplier": ("BlockDamageMultiplier", "d"), "need_total_hp_base_health": ("NeedTotalHpBaseHealth", "d"),
    "need_damage_from_one_unit": ("NeedDamageFromOneUnit", "d"),
    "incoming_percent": ("IncomingDamagePercentMultiplier", "d"), "new_damage_raw": ("NewDamageRaw", "d"),
    "new_health_raw": ("NewHealthRaw", "d"), "prev_health": ("PrevHealth", "d"), "prev_damage": ("PrevDamage", "d"),
}
DP5_INTS = {"armor_type": "ArmorType", "preferred_weapon_type": "PreferredWeaponType",
            "location_number": "LocationNumber", "snapshot_time_chunk_index": "TimeChunkIndex"}


def _rel_ok(a: float, b: float, tol: float = 1e-12) -> bool:
    if _same(a, b) or a == b:
        return True
    if not (math.isfinite(a) and math.isfinite(b)):
        return False
    return abs(a - b) <= tol * max(abs(a), abs(b))


def compare_dp5(out: Mapping, con: sqlite3.Connection, rounding: Rounding | str | None = None) -> dict:
    """NewHealth/NewDamage bit for bit, intermediates to 1e-12 relative, for every spawner of the scene
    (the ones that never register at runtime are listed apart); BossTower (Count 0, Health Inf) too.
    ``hp_exact_minute`` is G13's set on the C# report: minute-mode spawners whose stored Health equals
    the C# NewHealth."""
    from .spawner_map import load_identity_map

    identity = load_identity_map(con)
    ctx = MD.load_bake_context(con, rounding=rounding)
    armors = MD.load_armor_configs(con)
    rows = {(str(r["scene"]), int(r["file_id"])): r for r in MD.load_spawner_rows(con)}
    res: dict[str, Any] = {"csharp_spawners": len(out["spawners"]), "unknown": [], "registered": 0, "unregistered": [],
                           "new_bits_equal": 0, "intermediate_rel_ok": 0, "intermediate_bits_equal": 0,
                           "intermediate_values": 0, "errors_both": [], "boss_tower": [], "mismatches": [],
                           "hp_exact_minute": [], "minute_n": 0, "snapshot_source_equal": 0,
                           "new_health_raw_explained_by_mono": 0, "new_health_raw_rel_diff_max": 0.0}
    seen = set()
    for c in out["spawners"]:
        e = identity.by_runtime_id.get(c["id"])
        if e is None:
            res["unknown"].append(c["id"])
            continue
        sid = e.spawner_id
        seen.add(sid)
        if not e.registers:
            res["unregistered"].append(f"{sid[0]}:{sid[1]}")
        else:
            res["registered"] += 1
        sp = MD.spawner_input_from_row(rows[sid], armors)
        try:
            o = B.bake_spawner(ctx, sp)
        except B.BakeError as err:
            o = err
        if "error" in c or isinstance(o, B.BakeError):
            if "error" in c and isinstance(o, B.BakeError):
                res["errors_both"].append(f"{sid[0]}:{sid[1]}")
            else:
                res["mismatches"].append({"id": f"{sid[0]}:{sid[1]}", "csharp": c.get("error"), "oracle": str(o) if isinstance(o, B.BakeError) else "ok"})
            continue
        rep = c["report"]
        ch, cd = doubles(rep["NewHealth"])[0], doubles(rep["NewDamage"])[0]
        new_ok = _same(ch, o.new_health) and _same(cd, o.new_damage)
        res["new_bits_equal"] += new_ok
        bad = []
        for attr, (field, kind) in DP5_FIELDS.items():
            cv = (floats if kind == "f" else doubles)(rep[field])[0]
            ov = float(getattr(o, attr))
            res["intermediate_values"] += 1
            res["intermediate_bits_equal"] += _same(cv, ov)
            ok = _rel_ok(cv, ov)
            res["intermediate_rel_ok"] += ok
            if not ok:
                bad.append(field)
                if field == "NewHealthRaw" and _same(cv, mono_new_health_raw(o, sp)):
                    res["new_health_raw_explained_by_mono"] += 1
                    res["new_health_raw_rel_diff_max"] = max(res["new_health_raw_rel_diff_max"], abs(cv - ov) / abs(cv))
        for attr, field in DP5_INTS.items():
            res["intermediate_values"] += 1
            ok = int(getattr(o, attr)) == int(rep[field])
            res["intermediate_bits_equal"] += ok
            res["intermediate_rel_ok"] += ok
            if not ok:
                bad.append(field)
        for field, ov in (("PlayerAbilityDpsMultiplier", o.ability_dps + 1.0), ("PlayerAblityEhpMultiplier", o.ability_ehp + 1.0)):
            cv = doubles(rep[field])[0]
            res["intermediate_values"] += 1
            res["intermediate_bits_equal"] += _same(cv, ov)
            res["intermediate_rel_ok"] += _rel_ok(cv, ov)
            if not _rel_ok(cv, ov):
                bad.append(field)
        src = rep.get("SnapshotSource")
        o_src = f"{'t' if sp.is_days else 's'}:{o.snapshot_source_index}" if o.snapshot_source_index is not None else None
        res["snapshot_source_equal"] += src == o_src
        if not new_ok or bad or src != o_src:
            res["mismatches"].append({"id": f"{sid[0]}:{sid[1]}", "new_ok": new_ok, "fields": bad,
                                      "csharp_src": src, "oracle_src": o_src})
        if math.isinf(ch) or str(rows[sid]["go_name"]) == "BossTower":
            res["boss_tower"].append({"id": f"{sid[0]}:{sid[1]}", "name": str(rows[sid]["go_name"]), "count": sp.count,
                                      "csharp_new_health": ch, "oracle_new_health": o.new_health,
                                      "csharp_new_health_raw": doubles(rep["NewHealthRaw"])[0]})
        if not sp.is_days:
            res["minute_n"] += 1
            if sp.health_base == ch:
                res["hp_exact_minute"].append(f"{sid[0]}:{sid[1]}")
    res["missing"] = sorted(f"{s[0]}:{s[1]}" for s in rows if s not in seen and identity.by_spawner_id[s].registers)
    res["hp_exact_minute"].sort()
    res["errors_both"].sort()
    res["unregistered"].sort()
    return res


# ---------------------------------------------------------------- DP6

def compare_dp6(out: Mapping, con: sqlite3.Connection, variant: str = "A") -> dict:
    from .spawner_map import load_identity_map

    identity = load_identity_map(con)
    expected = SP.expected_ready_spawners(variant, con=con)
    cols = out["columns"]
    res: dict[str, Any] = {"rows": len(out["rows"]), "equal": 0, "unknown": [], "mismatches": [], "duplicates": []}
    seen = set()
    for row in out["rows"]:
        r = dict(zip(cols, row))
        e = identity.by_runtime_id.get(r["id"])
        if e is None or e.spawner_id not in expected:
            res["unknown"].append(r["id"])
            continue
        if e.spawner_id in seen:
            res["duplicates"].append(r["id"])
        seen.add(e.spawner_id)
        x = expected[e.spawner_id]
        got = (r["charges_total"], floats(r["base_cooldown"])[0], r["charges_limit"], r["uses_ab"])
        want = (x.charges_total, x.base_cooldown, x.charges_limit, x.uses_ab)
        ok = bool(got[0] == want[0] and _same32(got[1], want[1]) and got[2] == want[2] and got[3] == want[3])
        res["equal"] += ok
        if not ok:
            res["mismatches"].append({"id": r["id"], "csharp": list(got), "oracle": list(want)})
    res["missing"] = sorted(f"{s[0]}:{s[1]}" for s in expected if s not in seen)
    return res


def g17b_on_csharp(out: Mapping) -> dict:
    cols = out["columns"]
    rows = [dict(zip(cols, r)) for r in out["rows"]]
    items = [r for r in rows if r["item_rarity"] >= 0]
    by = collections.Counter((Rarity(r["item_rarity"]).name, r["charges_total"]) for r in items)
    ab = out["ab"]
    mins = {k: [floats(v)[0] for v in ab[k]["cooldown_minutes"]] for k in ("non_elite", "elite")}
    # the AB table as the spawners that use it see it: BaseCooldown / 60 per (elite, island)
    seen = collections.defaultdict(set)
    for r in rows:
        if r["uses_ab"]:
            seen[(bool(r["is_elite"]), r["location"])].add(floats(r["base_cooldown"])[0] / 60.0)
    return {
        "item_spawners": len(items),
        "item_charges_by_rarity": sorted([r, c, n] for (r, c), n in by.items()),
        "non_elite_cooldown_minutes": mins["non_elite"][:13],
        "elite_cooldown_minutes": mins["elite"][:4],
        "cooldown_defaults": [mins["non_elite"][13], mins["elite"][4]],
        "charges_per_unit": [ab["non_elite"]["charges_per_unit"][0], ab["elite"]["charges_per_unit"][0]],
        "fast_rpm": [floats(v)[0] for v in ab["fast_rpm"]],
        "spawner_minutes_agree": all(len(v) == 1 and next(iter(v)) == mins["elite" if el else "non_elite"][loc - 1]
                                     for (el, loc), v in seen.items() if loc <= 20),
    }


# ---------------------------------------------------------------- DP7

def _oracle_item(it: IT.ItemDef, st: IT.ItemState, tables: IT.ItemTables) -> list:
    return [_err(lambda: IT.damage(it, st, tables)), _err(lambda: IT.armor(it, st, tables)),
            _err(lambda: IT.dps(it, st, tables)), _err(lambda: IT.levels_to_evolve(it, st, tables))]


def _decode_item_row(row: Sequence[str]) -> list:
    out = []
    for i, v in enumerate(row):
        if v.startswith("error:") or v == "missing":
            out.append(v)
        else:
            out.append(int(v) if i == 3 else doubles(v)[0])
    return out


def compare_dp7(out: Mapping, inputs: Mapping, con: sqlite3.Connection) -> dict:
    """Every (sample, column) against the binary32 oracle; differences are binned in binary32 ulps of
    the oracle value and checked against the editor's evaluation (:func:`mono_item_values`)."""
    items, tables = IT.load_item_defs(con), IT.load_item_tables(con)
    res: dict[str, Any] = {"samples": len(inputs["samples"]), "values": 0, "equal": 0, "errors_both": 0,
                           "missing_keys": out["missing_keys"], "mismatches": [], "explained_by_mono": 0,
                           "ulp_hist": collections.Counter(), "mismatch_columns": collections.Counter()}
    for (key, level, copies, evolved), row in zip(inputs["samples"], out["rows"]):
        c = _decode_item_row(row)
        st = IT.ItemState(bool(evolved), level, copies)
        o = _oracle_item(items[key], st, tables)
        m = None
        for i, (name, a, b) in enumerate(zip(out["columns"], c, o)):
            res["values"] += 1
            if isinstance(a, str) or isinstance(b, str):
                ok = isinstance(a, str) and isinstance(b, str) and a != "missing"
                res["errors_both"] += ok
            else:
                ok = a == b if name == "levels_to_evolve" else _same(a, b)
            res["equal"] += ok
            if ok:
                continue
            m = m or mono_item_values(items[key], st, tables)
            explained = not isinstance(a, str) and not isinstance(m[i], str) and _same(a, m[i])
            res["explained_by_mono"] += explained
            res["mismatch_columns"][name] += 1
            if not isinstance(a, str) and not isinstance(b, str):
                res["ulp_hist"][f32_ulps(a, b)] += 1
            res["mismatches"].append({"sample": [key, level, copies, evolved], "field": name, "csharp": a, "oracle": b,
                                      "explained_by_mono": explained})
    res["mismatch_count"] = len(res["mismatches"])
    res["ulp_hist"] = dict(sorted(res["ulp_hist"].items(), key=lambda kv: float(kv[0]) if kv[0] != "nonfinite" else math.inf))
    res["mismatch_columns"] = dict(sorted(res["mismatch_columns"].items()))
    return res


def g18_on_csharp(out: Mapping, inputs: Mapping, con: sqlite3.Connection) -> dict:
    """G18's jump ratios with the C# item values on both sides of each jump (design 3.4 DP7 anchor)."""
    items, tables = IT.load_item_defs(con), IT.load_item_tables(con)
    values = {tuple(s): _decode_item_row(r) for s, r in zip(inputs["samples"], out["rows"])}
    samples = g18_samples(items, tables)
    jumps = IT.evolve_jumps(IT.g18_items(items), tables)
    ratios, ladder = [], []
    for j, before, after in zip(jumps, samples[0::2], samples[1::2]):
        col = 1 if items[j.key].is_armor else 0
        b, a = values[before][col], values[after][col]
        ratios.append((a / b, j, b, a))
        ladder.append([j.key, j.evolution_level, j.shards, b, a])
    rs = [r[0] for r in ratios]
    top = max(ratios, key=lambda r: r[0])
    return {"n": len(rs), "min_3dp": round(min(rs), 3), "median_3dp": round(statistics.median(rs), 3),
            "max": max(rs), "argmax": {"key": top[1].key, "evolution_level": top[1].evolution_level,
                                       "before": top[2], "after": top[3]},
            "ladder_sha256": hashlib.sha256(json.dumps(ladder).encode()).hexdigest()}


# ---------------------------------------------------------------- CLI

def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(prog="python -m tufu_balance.unity.probes", description=__doc__.split("\n\n")[0])
    ap.add_argument("--write", action="store_true", help=f"rewrite {INPUT_FILE.relative_to(BALANCE)}")
    ap.add_argument("--db", help="extracted database (default $TUFU_BALANCE_DB or Balance/out/balance.sqlite)")
    a = ap.parse_args(argv)
    from .launcher import balance_db

    con = MD.connect(a.db or balance_db())
    data = dumps_inputs(generate_inputs(con))
    if a.write:
        INPUT_FILE.parent.mkdir(parents=True, exist_ok=True)
        INPUT_FILE.write_bytes(data)
        print(f"wrote {INPUT_FILE} ({len(data)} bytes, sha256 {hashlib.sha256(data).hexdigest()})")
        return 0
    same = INPUT_FILE.exists() and INPUT_FILE.read_bytes() == data
    print("identical" if same else f"DIFFERENT from {INPUT_FILE}")
    return 0 if same else 1


if __name__ == "__main__":
    sys.exit(main())
