"""Config patches: parse, validate, apply to a :class:`~.config.GameConfig`, expand generator steps (design 6.1-6.2).

A patch (YAML, design 6.1)::

    name: island10_later
    base: other.yaml                       # optional: applied first (path relative to this file)
    overrides:
      - node: CombatBalanceConfig.IslandTargetDays   # data_dictionary node
        index: 9                                     # array element, or list row (int or "a:b", half-open)
        add: 2.0                                     # exactly one of set / add / mul
      - node: EnemySpawnerConfig.AverageFightTime
        where: {LocationNumber: 10, IsEliteBoss: 1}  # rows whose fields equal these values
        mul: 1.2
    regenerate: {optimum_time_from_relative: true, ...}
    simulate: {...}                                  # passed through (P12)
    report: {...}

Semantics shared with the C# patcher (C9; cases in ``tests/patch_cases``):

* Overrides apply in order, each to the result of the previous one. ``base`` overrides come first.
* Rows: a single-asset config needs no selector. Other tables need ``where`` (field equality; enums
  also match by member name, bools by 0/1; ``where: {}`` = every row) or, for list tables, ``index``
  (list positions). Zero matching rows is an error. On a typed field the equality is in its C# type:
  float fields compare as binary32, a string never matches a float/double field (C9).
* Array fields: ``index`` selects elements (int or ``"a:b"``); without ``index``, ``add``/``mul`` touch
  every element and ``set`` takes a whole list (length may change; added/removed elements are leaves
  with ``old``/``new`` null).
* Arithmetic is done in double and stored as the field's C# type: ``float`` fields
  ``(float)((double)old + arg)`` / ``(float)((double)old * arg)``, ``double`` fields in double, ``int``
  fields only when the result is integral (NaN/inf: ``bad_type``). ``set`` converts the value to the field
  type; an enum takes a member name or an integral number (not a bool). A result that
  is not finite as the field type (``1e39`` as a float, ``mul: .inf``) is an error; a stored inf stays
  inf. ``bool``, ``string``, ``ref`` and ``enum`` fields take ``set`` only (enums by int or member name;
  refs must name an existing asset; interpolation point lists must be ``;``-separated numbers).
* A node that is in ``data_dictionary`` but not a :mod:`.config` field is passed through to C# with a
  warning listing the formulas that read it. Its ``where`` is still resolved on the dictionary's source
  tables (unknown field, zero rows and a missing selector are errors) and its value checked against the
  stored cells; the report has no leaves for it. A patchable field that ``derive_static`` does not read
  (on that row, or under ``evolution_tables: frozen`` for the generator levers) warns. New values
  outside the dictionary's min/max warn.
* Python-only fields (the scene tool ``SpawnerRelativeOptimumTimeTool``: the game reads only the
  ``OptimumTime`` it wrote) have leaves in the report but never reach ``expanded``
  (:attr:`PatchReport.python_only`).

Generator steps (``regenerate``) recompute derived leaves from the patched inputs with the stage-3/4
rules (:mod:`.timeline`, :mod:`.evolution_gen`) and are expanded into explicit ``set`` overrides
(:attr:`PatchReport.expanded`), so the C# side never runs a rule. A derived leaf that did not follow its
rule in the base config (an exception, e.g. the 16 OT exceptions of G03) keeps its value, as does a
leaf the patch sets directly; both warn when the rule's result moves. A rule that has no value on the
patched inputs (``rule_error``) keeps the leaf. ``evolution_tables`` replaces the
generated rows wholesale (``replay_stored_inputs`` / ``fitted_h``); with ``SyncShieldValuesByLeader`` on,
``evolution_armor_sync`` either reports the armor cells whose types disagree (``report``, default) or
copies Value/CopyValue from a leader type.
"""

from __future__ import annotations

import json
import math
from dataclasses import dataclass, field, replace
from pathlib import Path
from typing import Any, Mapping, Sequence

import numpy as np
import yaml

from . import config as C
from . import evolution_gen as EG
from . import timeline as TL
from .enums import WeaponType
from .f32 import f32
from .registry import Provenance, implements

OPS = ("set", "add", "mul")
TOP_KEYS = ("name", "description", "base", "overrides", "regenerate", "simulate", "report")
OVERRIDE_KEYS = ("node", "set", "add", "mul", "index", "where", "note")
BOOL_STEPS = ("island_target_days_tail", "optimum_time_from_relative", "evolution_target_timings",
              "interpolation_timing_points", "bake")
CHOICE_STEPS = {"evolution_tables": EG.EVOLUTION_TABLE_MODES, "sim_autobalance": ("frozen", "regenerate"),
                "evolution_armor_sync": ("report", "Stabbing", "Cutting", "Buckshot")}
REGENERATE_DEFAULTS = {"island_target_days_tail": False, "optimum_time_from_relative": False,
                       "evolution_target_timings": False, "interpolation_timing_points": False,
                       "evolution_tables": "frozen", "evolution_armor_sync": "report", "sim_autobalance": "frozen",
                       "bake": False}
INT32 = (-2 ** 31, 2 ** 31 - 1)
ITD = "CombatBalanceConfig.IslandTargetDays"
POINT_LISTS = tuple(f"CombatBalanceConfig.Interpolations.Interpolations[].{c}"
                    for c in ("IslandPoints", "TimingPoints", "CoefPoints"))


class PatchError(ValueError):
    """A patch that cannot be applied. ``code``: unknown_node, bad_patch, bad_op, bad_type, bad_index,
    bad_where, bad_selector, no_match, missing_value, bad_ref."""

    def __init__(self, code: str, message: str, *, override: int | None = None, node: str | None = None):
        self.code, self.override, self.node = code, override, node
        where = f"override {override}" + (f" ({node})" if node else "") + ": " if override is not None else ""
        super().__init__(f"[{code}] {where}{message}")


# ---------------------------------------------------------------------------------------------
# Patch file


@dataclass(frozen=True)
class Override:
    node: str
    op: str
    arg: Any
    index: int | str | None = None
    where: Mapping[str, Any] | None = None
    source: str = "override"
    note: str | None = None

    def to_json(self) -> dict:
        out: dict = {"node": self.node}
        if self.index is not None:
            out["index"] = self.index
        if self.where is not None:
            out["where"] = dict(self.where)
        out[self.op] = list(self.arg) if isinstance(self.arg, tuple) else self.arg
        return out


@dataclass(frozen=True)
class Patch:
    name: str
    description: str = ""
    overrides: tuple[Override, ...] = ()
    regenerate: Mapping[str, Any] = field(default_factory=dict)
    simulate: Mapping[str, Any] | None = None
    report: Mapping[str, Any] | None = None
    bases: tuple[str, ...] = ()  # names of the stacked base patches, outermost first

    def regen(self, key: str):
        return self.regenerate.get(key, REGENERATE_DEFAULTS[key])


def _parse_override(i: int, raw: Any, source: str) -> Override:
    if not isinstance(raw, Mapping):
        raise PatchError("bad_patch", f"override must be a mapping, got {raw!r}", override=i)
    unknown = sorted(set(raw) - set(OVERRIDE_KEYS))
    if unknown:
        raise PatchError("bad_patch", f"unknown keys {unknown}", override=i, node=raw.get("node"))
    node = raw.get("node")
    if not isinstance(node, str) or not node:
        raise PatchError("bad_patch", "missing node", override=i)
    ops = [op for op in OPS if op in raw]
    if len(ops) != 1:
        raise PatchError("bad_op", f"exactly one of {OPS} required, got {ops}", override=i, node=node)
    where = raw.get("where")
    if where is not None and not isinstance(where, Mapping):
        raise PatchError("bad_where", f"where must be a mapping, got {where!r}", override=i, node=node)
    index = raw.get("index")
    if index is not None and (isinstance(index, bool) or not isinstance(index, (int, str))):
        raise PatchError("bad_index", f"index must be an int or 'a:b', got {index!r}", override=i, node=node)
    arg = raw[ops[0]]
    return Override(node, ops[0], tuple(arg) if isinstance(arg, list) else arg, index,
                    dict(where) if where is not None else None, source, raw.get("note"))


def parse_patch(data: Mapping, *, base_dir: str | Path | None = None, _seen: tuple = ()) -> Patch:
    if not isinstance(data, Mapping):
        raise PatchError("bad_patch", "a patch must be a mapping")
    unknown = sorted(set(data) - set(TOP_KEYS))
    if unknown:
        raise PatchError("bad_patch", f"unknown top-level keys {unknown}")
    name = str(data.get("name") or "patch")
    regen = dict(data.get("regenerate") or {})
    for k, v in regen.items():
        if k in BOOL_STEPS:
            if not isinstance(v, bool):
                raise PatchError("bad_patch", f"regenerate.{k} must be true/false, got {v!r}")
        elif k in CHOICE_STEPS:
            if v not in CHOICE_STEPS[k]:
                raise PatchError("bad_patch", f"regenerate.{k} must be one of {CHOICE_STEPS[k]}, got {v!r}")
        else:
            raise PatchError("bad_patch", f"unknown regenerate key {k!r}")
    raw = data.get("overrides") or []
    if not isinstance(raw, list):
        raise PatchError("bad_patch", "overrides must be a list")
    own = tuple(_parse_override(i, o, "override") for i, o in enumerate(raw))
    patch = Patch(name, str(data.get("description") or ""), own, regen, data.get("simulate"), data.get("report"))
    base_ref = data.get("base")
    if base_ref is None:
        return patch
    path = Path(base_dir or ".") / str(base_ref)
    if path.resolve() in _seen:
        raise PatchError("bad_patch", f"base cycle at {path}")
    base = load_patch(path, _seen=_seen)
    tag = f"base:{base.name}"
    inherited = tuple(replace(o, source=tag if o.source == "override" else o.source) for o in base.overrides)
    return Patch(name, patch.description, inherited + own, {**base.regenerate, **regen},
                 patch.simulate if patch.simulate is not None else base.simulate,
                 patch.report if patch.report is not None else base.report, (base.name,) + base.bases)


def load_patch(path: str | Path, *, _seen: tuple = ()) -> Patch:
    path = Path(path)
    data = yaml.safe_load(path.read_text(encoding="utf-8"))
    return parse_patch(data, base_dir=path.parent, _seen=_seen + (path.resolve(),))


# ---------------------------------------------------------------------------------------------
# Report


@dataclass(frozen=True)
class Leaf:
    node: str
    row: Mapping[str, Any]
    index: int | None
    old: Any
    new: Any

    def address(self) -> tuple:
        return (self.node, tuple(self.row.items()), self.index)

    def to_json(self) -> dict:
        return {"node": self.node, "row": dict(self.row), "index": self.index, "old": _json_value(self.old),
                "new": _json_value(self.new)}


def _json_value(v):
    if isinstance(v, float) and not math.isfinite(v):
        return repr(v)
    return v


def _sort_key(leaf: Leaf) -> tuple:
    return (leaf.node, tuple((k, str(type(v)), v) for k, v in leaf.row.items()),
            -1 if leaf.index is None else leaf.index)


@dataclass
class OpResult:
    source: str  # override | base:<name> | regenerate.<step>
    override: Override | None  # None for generated sets
    node: str
    rows: int  # rows matched
    leaves: list[Leaf]

    def to_json(self) -> dict:
        out = {"source": self.source, "node": self.node, "rows": self.rows,
               "leaves": [x.to_json() for x in sorted(self.leaves, key=_sort_key)]}
        if self.override is not None:
            out["override"] = self.override.to_json()
        return out


@dataclass
class PatchWarning:
    code: str  # out_of_range | not_python_patchable | not_modeled_static | exception_kept | override_kept |
    # armor_sync_broken | no_window | rule_error
    node: str | None
    message: str
    detail: Mapping[str, Any] = field(default_factory=dict)

    def to_json(self) -> dict:
        return {"code": self.code, "node": self.node, "message": self.message, "detail": dict(self.detail)}


@dataclass
class PatchReport:
    name: str
    ops: list[OpResult]
    leaves: list[Leaf]  # net: base config -> patched config, sorted
    warnings: list[PatchWarning]
    passthrough: list[Override]  # dictionary nodes the Python model does not hold (C# only)
    expanded: list[dict]  # the patch for C#: overrides as written + generated explicit sets (C# nodes only)
    regenerate: Mapping[str, Any]
    python_only: list[str] = field(default_factory=list)  # nodes of ``leaves`` that C# never sees

    def to_json(self) -> dict:
        return {"schema": "tufu.patch_report/1", "name": self.name, "regenerate": dict(self.regenerate),
                "ops": [o.to_json() for o in self.ops], "leaves": [x.to_json() for x in self.leaves],
                "warnings": [w.to_json() for w in self.warnings],
                "passthrough": [o.to_json() for o in self.passthrough], "python_only": list(self.python_only),
                "expanded": self.expanded}

    def leaf_map(self) -> dict[tuple, Leaf]:
        return {x.address(): x for x in self.leaves}

    def csharp_leaves(self) -> list[Leaf]:
        """The leaves C9's ``applied_patch.json`` must reproduce (Python-only nodes left out)."""
        return [x for x in self.leaves if x.node not in self.python_only]


# ---------------------------------------------------------------------------------------------
# Applying overrides


def _is_number(x) -> bool:
    return isinstance(x, (int, float)) and not isinstance(x, bool)


def _integral(x) -> bool:
    """A number with an integer value (NaN and inf are not)."""
    return isinstance(x, int) or (math.isfinite(x) and x == math.floor(x))


def _ref_names(cfg: C.GameConfig, f: C.FieldSpec) -> set[str]:
    sql = {"ArmorConfig": "c_ArmorConfig__Armors", "ItemReward": "c_ItemConfig"}[f.column]
    return {Path(str(r["asset"])).stem for r in cfg.tables[sql].rows}


def _coerce(cfg: C.GameConfig, f: C.FieldSpec, value, i: int):
    t = f.type
    if t in ("float", "double"):
        if not _is_number(value) or not math.isfinite(float(value)):
            raise PatchError("bad_type", f"{t} field needs a finite number, got {value!r}", override=i, node=f.node)
        return _finite(f, _to_float(value) if t == "float" else float(value), value, i)
    if t == "int":
        if not _is_number(value) or not _integral(value):
            raise PatchError("bad_type", f"int field needs an integer, got {value!r}", override=i, node=f.node)
        v = int(value)
        if not INT32[0] <= v <= INT32[1]:
            raise PatchError("bad_type", f"{v} is outside int32", override=i, node=f.node)
        return v
    if t == "bool":
        if isinstance(value, bool) or value in (0, 1):
            return bool(value)
        raise PatchError("bad_type", f"bool field needs true/false, got {value!r}", override=i, node=f.node)
    if t == "enum":
        if not isinstance(value, str) and not (_is_number(value) and _integral(value)):
            raise PatchError("bad_type", f"enum field needs a member name or an integer, got {value!r}", override=i,
                             node=f.node)
        try:
            return C.enum_value(f, value)
        except (ValueError, OverflowError, C.ConfigError):
            raise PatchError("bad_type", f"{value!r} is not a {f.enum}", override=i, node=f.node) from None
    if value is not None and not isinstance(value, str):
        raise PatchError("bad_type", f"{t} field needs a string, got {value!r}", override=i, node=f.node)
    if t == "ref" and value is not None and value not in _ref_names(cfg, f):
        raise PatchError("bad_ref", f"no asset named {value!r} for {f.column}", override=i, node=f.node)
    if f.node in POINT_LISTS and value is not None:
        try:
            ok = all(math.isfinite(float(x)) for x in str(value).split(";"))
        except ValueError:
            ok = False
        if not ok:
            raise PatchError("bad_type", f"{f.column} needs ';'-separated finite numbers, got {value!r}",
                             override=i, node=f.node)
    return value


def _to_float(x) -> float:
    """``(float)x`` widened back to double (inf on overflow, rejected by :func:`_finite`)."""
    with np.errstate(over="ignore"):
        return float(f32(x))


def _finite(f: C.FieldSpec, v: float, arg, i: int) -> float:
    if not math.isfinite(v):
        raise PatchError("bad_type", f"{arg!r} gives {v!r} as a {f.type}", override=i, node=f.node)
    return v


def _arith(f: C.FieldSpec, op: str, old, arg, i: int):
    if f.type not in ("float", "double", "int"):
        raise PatchError("bad_op", f"{op} needs a numeric field, {f.node} is {f.type}", override=i, node=f.node)
    if not _is_number(arg) or not math.isfinite(float(arg)):
        raise PatchError("bad_type", f"{op} needs a finite number, got {arg!r}", override=i, node=f.node)
    if old is None:
        raise PatchError("missing_value", f"{op} on a missing value", override=i, node=f.node)
    r = float(old) + float(arg) if op == "add" else float(old) * float(arg)
    if f.type in ("float", "double"):
        r = _to_float(r) if f.type == "float" else r
        return _finite(f, r, f"{op} {arg!r}", i) if math.isfinite(float(old)) else r  # stored inf/NaN stay
    if not _integral(r) or not INT32[0] <= r <= INT32[1]:
        raise PatchError("bad_type", f"int field result {r!r} is not an int32", override=i, node=f.node)
    return int(r)


def _apply_value(cfg, f: C.FieldSpec, op: str, old, arg, i: int):
    return _coerce(cfg, f, arg, i) if op == "set" else _arith(f, op, old, arg, i)


def _parse_index(index, n: int, i: int, node: str) -> list[int]:
    """Positions selected by ``index`` among ``n`` (an int or ``"a:b"`` half-open; ``"a:"`` to the end)."""
    if isinstance(index, int):
        if not 0 <= index < n:
            raise PatchError("bad_index", f"index {index} out of range 0..{n - 1}", override=i, node=node)
        return [index]
    text = str(index).strip()
    if text.lstrip("-").isdigit():
        return _parse_index(int(text), n, i, node)
    a, sep, b = text.partition(":")
    try:
        lo = int(a) if a.strip() else 0
        hi = int(b) if b.strip() else n
    except ValueError:
        raise PatchError("bad_index", f"index {index!r} is not an int or 'a:b'", override=i, node=node) from None
    if not sep or not 0 <= lo < hi <= n:
        raise PatchError("bad_index", f"index {index!r} outside 0..{n}", override=i, node=node)
    return list(range(lo, hi))


def _matches(row: Mapping, column: str, want, f: C.FieldSpec | None = None) -> bool:
    """``where`` equality of one cell. On a typed field (``f``) the comparison is in the field's C# type, as the C#
    patcher compares the live value: float fields as binary32 (``f32(cell) == f32(want)``), enum fields also by
    member name; a string never matches a float/double field."""
    cell = row.get(column)
    kind = f.type if f is not None and not f.is_array else None
    if isinstance(want, str):
        if f"{column}__name" in row and not isinstance(cell, str):
            return row[f"{column}__name"] == want
        if kind == "enum" and cell is not None:
            member = C.ENUMS[f.enum].__members__.get(want)
            return member is not None and int(cell) == int(member)
        if kind in ("float", "double"):
            return False
    if want is None or cell is None:
        return want is None and cell is None
    if _is_number(want) or isinstance(want, bool):
        try:
            if kind == "float":
                return _to_float(float(cell)) == _to_float(float(want))
            return float(cell) == float(want)
        except (TypeError, ValueError):
            return False
    return str(cell) == str(want)


def _column_field(sql: str, column: str) -> C.FieldSpec | None:
    spec = C.TABLE_BY_SQL.get(sql)
    if spec is not None:
        return next((f for f in spec.fields if f.column == column), None)
    return C.FIELDS.get(C.dictionary_node(sql, column) or "")


def _select_rows(spec: C.TableSpec, rows: Sequence[dict], ov: Override, f: C.FieldSpec, i: int) -> list[int]:
    if spec.singleton:
        if ov.where:
            raise PatchError("bad_selector", "a single-asset config takes no where", override=i, node=ov.node)
        if ov.index is not None and not f.is_array:
            raise PatchError("bad_index", "index on a scalar field of a single-asset config", override=i, node=ov.node)
        return [0]
    selected = list(range(len(rows)))
    row_index = ov.index is not None and not f.is_array
    if ov.where is None and not row_index:
        raise PatchError("bad_selector", f"{spec.prefix} has {len(rows)} rows: give where (where: {{}} for all)"
                         + (" or index" if "idx" in spec.key_names() else ""), override=i, node=ov.node)
    if row_index:
        if spec.key_names() != ("idx",):
            raise PatchError("bad_index", f"{spec.prefix} rows are not a list; use where", override=i, node=ov.node)
        positions = set(_parse_index(ov.index, len(rows), i, ov.node))
        selected = [p for p in selected if p in positions]
    for name, want in (ov.where or {}).items():
        column = spec.column_of(str(name))
        if column is None:
            raise PatchError("bad_where", f"{spec.prefix} has no field {name!r}", override=i, node=ov.node)
        typed = _column_field(spec.sql, column)
        selected = [p for p in selected if _matches(rows[p], column, want, typed)]
    if not selected:
        raise PatchError("no_match", f"selector {ov.to_json()} matches no {spec.prefix} row", override=i, node=ov.node)
    return selected


class _State:
    """Working copy of the tables while a patch is applied."""

    def __init__(self, cfg: C.GameConfig, evolution_tables: str = "frozen"):
        self.cfg = cfg
        self.evolution_tables = evolution_tables
        self.rows: dict[str, list[dict]] = {}
        self.dirty: set[str] = set()
        self.touched: set[tuple] = set()  # leaf addresses set by overrides (protected from generators)
        self.warnings: list[PatchWarning] = []
        self.version = 0
        self._config, self._config_key = cfg, ()

    def table_rows(self, sql: str) -> list[dict]:
        if sql not in self.rows:
            self.rows[sql] = list(self.cfg.tables[sql].rows)
        return self.rows[sql]

    def config(self) -> C.GameConfig:
        key = tuple(sorted((s, self.version) for s in self.dirty))
        if self._config_key != key:
            self._config = self.cfg.with_tables({s: C.ConfigTable(self.cfg.tables[s].spec, tuple(self.rows[s]))
                                                 for s in self.dirty})
            self._config_key = key
        return self._config

    def write(self, f: C.FieldSpec, pos: int, value) -> None:
        rows = self.table_rows(f.sql)
        row = dict(rows[pos])
        row[f.column] = f.encode(value)
        if f.type == "enum" and f"{f.column}__name" in row:
            row[f"{f.column}__name"] = C.ENUMS[f.enum](int(value)).name
        rows[pos] = row
        self.dirty.add(f.sql)
        self.version += 1

    def warn(self, code: str, node: str | None, message: str, **detail) -> None:
        self.warnings.append(PatchWarning(code, node, message, detail))


def _range_check(st: _State, f: C.FieldSpec, values: list, i: int) -> None:
    if f.array == "key_data":
        return  # dictionary keys, not values (and the legacy keyData cells have a meaningless min/max)
    entry = st.cfg.dictionary.get(f.node)
    nums = [v for v in values if _is_number(v)]
    if entry is None or not nums or (entry.min is None and entry.max is None):
        return
    out = [v for v in nums if (entry.min is not None and v < entry.min) or (entry.max is not None and v > entry.max)]
    if out:
        st.warn("out_of_range", f.node, f"override {i}: {len(out)} new value(s) outside the dictionary range "
                f"[{entry.min}, {entry.max}]", count=len(out), example=out[0], min=entry.min, max=entry.max)


def _apply_override(st: _State, ov: Override, i: int) -> OpResult:
    f = C.field_for(ov.node)
    spec = C.TABLE_BY_SQL[f.sql]
    rows = st.table_rows(f.sql)
    positions = _select_rows(spec, rows, ov, f, i)
    leaves: list[Leaf] = []
    for pos in positions:
        row_id = spec.row_id(rows[pos])
        old = f.decode(rows[pos])
        if not f.is_array:
            new = _apply_value(st.cfg, f, ov.op, old, ov.arg, i)
            leaves.append(Leaf(ov.node, row_id, None, old, new))
            st.write(f, pos, new)
            continue
        arr = list(old)
        if ov.index is None and ov.op == "set":
            if not isinstance(ov.arg, (list, tuple)):
                raise PatchError("bad_type", "set on a whole array needs a list", override=i, node=ov.node)
            new_arr = [_coerce(st.cfg, f, v, i) for v in ov.arg]
            for k in range(max(len(arr), len(new_arr))):
                leaves.append(Leaf(ov.node, row_id, k, arr[k] if k < len(arr) else None,
                                   new_arr[k] if k < len(new_arr) else None))
        else:
            if isinstance(ov.arg, (list, tuple)):
                raise PatchError("bad_type", "a list value needs set without index", override=i, node=ov.node)
            idx = range(len(arr)) if ov.index is None else _parse_index(ov.index, len(arr), i, ov.node)
            new_arr = list(arr)
            for k in idx:
                new_arr[k] = _apply_value(st.cfg, f, ov.op, arr[k], ov.arg, i)
                leaves.append(Leaf(ov.node, row_id, k, arr[k], new_arr[k]))
        st.write(f, pos, tuple(new_arr))
    for x in leaves:
        st.touched.add(x.address())
    _range_check(st, f, [x.new for x in leaves], i)
    unmodeled = [p for p in positions if not f.modeled_for(rows[p], st.evolution_tables)]
    if unmodeled:
        why = (" with evolution_tables=frozen (it only feeds the evolution generators)"
               if f.modeled == "evolution_regenerated" else
               " on curves of types other than " + "/".join(map(str, C.STATIC_INTERPOLATION_TYPES))
               if f.modeled == "static_interpolation" else "")
        st.warn("not_modeled_static", ov.node, f"derive_static does not read this field{why}; only the C# "
                "simulation reacts to it", formulas=formulas_reading(st.cfg, ov.node),
                rows=[spec.row_id(rows[p]) for p in unmodeled] if not spec.singleton else [])
    return OpResult(ov.source, ov, ov.node, len(positions), leaves)


# ---------------------------------------------------------------------------------------------
# Passthrough nodes (C# only): validated against the dictionary's source tables


def _source_table(cfg: C.GameConfig, sql: str) -> tuple[list[str], list[dict]]:
    def read():
        try:
            cur = cfg.source.execute(f"SELECT * FROM {C._quote(sql)}")
        except Exception:  # noqa: BLE001  (a dictionary table that is not in this database)
            return [], []
        cols = [d[0] for d in cur.description]
        return cols, [dict(zip(cols, r)) for r in cur]
    return cfg.base.cached(("source_table", sql), read)


def _passthrough_kind(values: list) -> str | None:
    """What the stored cells of a passthrough column look like: int, bool, number, array, string (None: empty)."""
    if not values:
        return None
    if all(_is_number(v) for v in values):
        if all(isinstance(v, int) for v in values):
            return "bool" if set(values) <= {0, 1} else "int"
        return "number"
    if all(isinstance(v, str) and v.startswith("[") for v in values):
        return "array"
    return "string"


def _check_passthrough_value(ov: Override, kind: str | None, i: int) -> None:
    def bad(msg):
        raise PatchError("bad_type", msg, override=i, node=ov.node)

    def number(x):
        return _is_number(x) and math.isfinite(float(x))

    if kind is None:
        return
    if ov.op != "set":
        if kind == "string" or (kind == "bool" and ov.index is None):
            raise PatchError("bad_op", f"{ov.op} needs a numeric field (stored cells are {kind})", override=i,
                             node=ov.node)
        if not number(ov.arg):
            bad(f"{ov.op} needs a finite number, got {ov.arg!r}")
        return
    v = ov.arg
    if kind == "array" and ov.index is None:
        if not isinstance(v, (list, tuple)):
            bad(f"set on a whole array needs a list, got {v!r}")
        return
    if kind in ("int", "bool", "number") and not (number(v) or isinstance(v, bool)):
        bad(f"stored cells are {kind}s, got {v!r}")
    if kind == "int" and not isinstance(v, bool) and float(v) != int(v):
        bad(f"stored cells are ints, got {v!r}")
    if kind == "bool" and not (isinstance(v, bool) or v in (0, 1)):
        bad(f"stored cells are bools, got {v!r}")
    if kind == "string" and v is not None and not isinstance(v, str):
        bad(f"stored cells are strings, got {v!r}")


def check_passthrough(cfg: C.GameConfig, ov: Override, i: int) -> int | None:
    """Rows of the dictionary's source tables that a passthrough override selects (None: no source column to
    check). ``where`` is resolved like a Python field (unknown field: bad_where, no row: no_match, several rows
    and no selector: bad_selector) and the value is checked against the stored cells."""
    entry = cfg.dictionary[ov.node]
    tables = [t.strip() for t in str(entry.tables or "").split(",") if t.strip()]
    hits, found, known, values = 0, False, set(), []
    for sql in tables:
        cols, rows = _source_table(cfg, sql)
        column = next((c for c in cols if C.dictionary_node(sql, c) == ov.node), None)
        if column is None:
            continue
        found = True
        prefix = C.dictionary_node(sql, "x")[:-2]
        names = {c: c for c in cols}
        for c in cols:
            n = C.dictionary_node(sql, c)
            if n and n.startswith(prefix + "."):
                names.setdefault(n[len(prefix) + 1:], c)
        values += [r[column] for r in rows if r[column] is not None]
        if ov.where is None:
            if len(rows) > 1 and ov.index is None:
                raise PatchError("bad_selector", f"{sql} has {len(rows)} rows: give where (where: {{}} for all)",
                                 override=i, node=ov.node)
            hits += len(rows) if ov.index is None else 1
            continue
        selected = rows
        for name, want in ov.where.items():
            c = names.get(str(name))
            if c is None:
                selected = []
                continue
            known.add(str(name))
            typed = _column_field(sql, c)
            selected = [r for r in selected if _matches(r, c, want, typed)]
        hits += len(selected)
    if not found:
        return None
    unknown = sorted(set(map(str, ov.where or {})) - known)
    if unknown:
        raise PatchError("bad_where", f"no source table of {ov.node} has field(s) {unknown}", override=i, node=ov.node)
    if not hits:
        raise PatchError("no_match", f"selector {ov.to_json()} matches no row of {', '.join(tables)}", override=i,
                         node=ov.node)
    _check_passthrough_value(ov, _passthrough_kind(values), i)
    return hits


# ---------------------------------------------------------------------------------------------
# Formulas reading a node (warnings)

_GRAPH: dict = {}


def _graph(cfg: C.GameConfig):
    if "g" not in _GRAPH:
        from .. import graph as G  # noqa: PLC0415  (only for warnings)
        catalog = json.loads(G.CATALOG.read_text(encoding="utf-8"))
        values = json.loads(G.VALUES.read_text(encoding="utf-8")) if G.VALUES.exists() else {"values": []}
        _GRAPH["g"] = G.Graph(catalog, values, set(cfg.dictionary))
    return _GRAPH["g"]


def _ancestors(node: str) -> list[str]:
    out, cur = [node], node
    while True:
        if cur.endswith("[]"):
            cur = cur[:-2]
        elif "." in cur:
            cur = cur.rsplit(".", 1)[0]
            if cur.endswith("[]"):
                out.append(cur)
                cur = cur[:-2]
        else:
            break
        out.append(cur)
    return list(dict.fromkeys(out))


def formulas_reading(cfg: C.GameConfig, node: str) -> list[str]:
    """Catalog formulas with ``config:<node>`` (or a list/struct containing it) as an input."""
    g = _graph(cfg)
    hits = set()
    for n in _ancestors(node):
        for target, _ in g.out_edges.get(f"config:{n}", []):
            if target.startswith("formula:"):
                hits.add(target.removeprefix("formula:"))
    return sorted(hits)


# ---------------------------------------------------------------------------------------------
# Generator steps


class _Generator:
    """Emit explicit sets for derived leaves of one step (see module doc for which leaves are kept)."""

    def __init__(self, st: _State, base: C.GameConfig, step: str, *, exceptions: bool = True):
        self.st, self.base, self.step, self.exceptions = st, base, step, exceptions
        self.ops: dict[str, OpResult] = {}
        self.kept: dict[str, list] = {}
        self.errors: list[dict] = []

    def rule(self, fn, *args, row=None):
        """``fn(*args)``, or None when the rule cannot be evaluated on the patched inputs (warned in finish)."""
        try:
            return fn(*args)
        except ValueError as e:
            if row is not None:
                self.errors.append({"row": dict(row), "error": str(e)})
            return None

    def leaf(self, f: C.FieldSpec, pos: int, index: int | None, rule, base_rule=None, base_stored=None) -> None:
        rows = self.st.table_rows(f.sql)
        spec = C.TABLE_BY_SQL[f.sql]
        row_id = spec.row_id(rows[pos])
        cur = f.decode(rows[pos])
        current = cur if index is None else cur[index]
        address = (f.node, tuple(row_id.items()), index)
        if self.exceptions and base_rule != base_stored:
            if rule != base_rule:
                self.kept.setdefault("exception_kept", []).append((f.node, row_id, index, current, rule))
            return
        if address in self.st.touched:
            if rule != current:
                self.kept.setdefault("override_kept", []).append((f.node, row_id, index, current, rule))
            return
        if rule == current:
            return
        new = rule if index is None else tuple(rule if k == index else v for k, v in enumerate(cur))
        self.st.write(f, pos, new)
        op = self.ops.setdefault(f.node, OpResult(f"regenerate.{self.step}", None, f.node, 0, []))
        op.leaves.append(Leaf(f.node, row_id, index, current, rule))

    def finish(self) -> list[OpResult]:
        if self.errors:
            self.st.warn("rule_error", None, f"regenerate.{self.step}: the rule cannot be evaluated for "
                         f"{len(self.errors)} row(s); their derived leaves keep their values", rows=self.errors)
        for code, items in self.kept.items():
            what = "base exception (rule != stored in the base config)" if code == "exception_kept" \
                else "leaf set by an override"
            self.st.warn(code, items[0][0], f"regenerate.{self.step}: {len(items)} {what} kept although the rule "
                         "now gives another value",
                         leaves=[{"node": n, "row": dict(r), "index": k, "kept": c, "rule": v}
                                 for n, r, k, c, v in items])
        for op in self.ops.values():
            op.rows = len({x.address()[1] for x in op.leaves})
        return list(self.ops.values())


def _itd(cfg: C.GameConfig) -> tuple:
    return cfg.value(ITD)


def _regen_itd_tail(st: _State, base: C.GameConfig) -> list[OpResult]:
    # P3's tail is a closed form of the island index alone (no anchor on the hand-filled head) and the shipped
    # tail already matches it; tail cells a patch writes are kept, so this step never emits a set (Z1).
    g = _Generator(st, base, "island_target_days_tail")
    f = C.field_for(ITD)
    cur_itd, base_itd = _itd(st.config()), _itd(base)
    for island in range(TL.TAIL_FIRST_ISLAND, len(cur_itd) + 1):
        rule = float(TL.island_target_days_closed_form(island))
        stored = base_itd[island - 1] if island <= len(base_itd) else None
        g.leaf(f, 0, island - 1, rule, rule, stored)
    return g.finish()


def _node(sql: str, column: str) -> C.FieldSpec:
    return C.FIELDS[C.dictionary_node(sql, column)]


def _regen_optimum_time(st: _State, base: C.GameConfig) -> list[OpResult]:
    g = _Generator(st, base, "optimum_time_from_relative")
    tool = "s_SpawnerRelativeOptimumTimeTool__Islands"
    f_min, f_max, f_days = _node(tool, "MinOptimumTime"), _node(tool, "MaxOptimumTime"), _node(tool, "IsDays")
    itd, base_itd = _itd(st.config()), _itd(base)
    base_tool = {int(r["LocationNumber"]): r for r in base.tables[tool].rows}
    for pos, r in enumerate(st.table_rows(tool)):
        L = int(r["LocationNumber"])
        window = g.rule(TL.relative_window, itd, L, row={"LocationNumber": L})
        if window is None:
            continue
        lo, hi, days = window
        blo, bhi, bdays = TL.relative_window(base_itd, L)
        b = base_tool.get(L)
        for f, rule, brule in ((f_min, float(lo), float(blo)), (f_max, float(hi), float(bhi)),
                               (f_days, bool(days), bool(bdays))):
            g.leaf(f, pos, None, rule, brule, f.decode(b) if b is not None else None)
    windows = {int(r["LocationNumber"]): (f_min.decode(r), f_max.decode(r)) for r in st.table_rows(tool)}
    base_windows = {L: (f_min.decode(r), f_max.decode(r)) for L, r in base_tool.items()}
    f_ot, f_rel = _node(C.SPAWNERS, "OptimumTime"), _node(C.SPAWNERS, "RelativeOptimumTime")
    f_loc, f_isd = _node(C.SPAWNERS, "LocationNumber"), _node(C.SPAWNERS, "IsDays")
    base_rows = base.tables[C.SPAWNERS]
    missing = []
    for pos, r in enumerate(st.table_rows(C.SPAWNERS)):
        L = f_loc.decode(r)
        if L not in windows:
            missing.append(C.TABLE_BY_SQL[C.SPAWNERS].row_id(r))
            continue
        rule = float(TL.optimum_time_from_relative(f_rel.decode(r), *windows[L], f_isd.decode(r)))
        b = base_rows.row(C.TABLE_BY_SQL[C.SPAWNERS].row_key(r))
        bL = f_loc.decode(b)
        brule = (float(TL.optimum_time_from_relative(f_rel.decode(b), *base_windows[bL], f_isd.decode(b)))
                 if bL in base_windows else None)
        g.leaf(f_ot, pos, None, rule, brule, f_ot.decode(b))
    if missing:
        st.warn("no_window", f_ot.node, f"{len(missing)} spawner(s) on islands without a relative window kept "
                "their OptimumTime", spawners=missing)
    return g.finish()


def _target_array(row: Mapping) -> str:
    kind = "Armor" if str(row["Name"]).startswith("Armor") else ""
    return f"{row['Rarity__name']}{kind}EvolutionTargetTimings"


def _regen_evolution_target_timings(st: _State, base: C.GameConfig) -> list[OpResult]:
    g = _Generator(st, base, "evolution_target_timings")
    fill, timing = f"{C.CBC}__EvolutionIslandFillRuleRows", f"{C.CBC}__EvolutionIslandTimingRows"
    f_start, f_step = _node(fill, "StartValue"), _node(fill, "StepPerEvolution")
    e_fields = [_node(timing, f"E{k}") for k in range(1, TL.EVOLUTION_LEVELS + 1)]

    def rule_rows(cfg_rows):
        return {(int(r["Rarity"]), str(r["Name"])): [float(x) for x in TL.evolution_islands(f_start.decode(r),
                                                                                              f_step.decode(r))]
                for r in cfg_rows}

    cur_rule, base_rule = rule_rows(st.table_rows(fill)), rule_rows(base.tables[fill].rows)
    base_timing = {(int(r["Rarity"]), str(r["Name"])): r for r in base.tables[timing].rows}
    for pos, r in enumerate(st.table_rows(timing)):
        k = (int(r["Rarity"]), str(r["Name"]))
        if k not in cur_rule:
            continue
        b = base_timing.get(k)
        for e, f in enumerate(e_fields):
            g.leaf(f, pos, None, cur_rule[k][e], base_rule.get(k, [None] * 15)[e],
                   f.decode(b) if b is not None else None)
    itd, base_itd = _itd(st.config()), _itd(base)
    for r in st.table_rows(timing):
        f = C.FIELDS[f"CombatBalanceConfig.{_target_array(r)}"]
        islands = [x.decode(r) for x in e_fields]
        b = base_timing.get((int(r["Rarity"]), str(r["Name"])))
        b_islands = [x.decode(b) for x in e_fields] if b is not None else None
        rule = g.rule(TL.evolution_target_timings, islands, itd, row=C.TABLE_BY_SQL[timing].row_id(r))
        if rule is None:
            continue
        rule = [float(x) for x in rule]
        brule = [float(x) for x in TL.evolution_target_timings(b_islands, base_itd)] if b_islands else None
        stored = f.decode(base.tables[C.CBC].rows[0])
        for k, v in enumerate(rule):
            if k < len(f.decode(st.table_rows(C.CBC)[0])):
                g.leaf(f, 0, k, v, brule[k] if brule else None, stored[k] if k < len(stored) else None)
    return g.finish()


def _island_points(text) -> list[float] | None:
    parts = [x for x in str(text or "").split(";") if x.strip()]
    return [float(x) for x in parts] or None


def _regen_interpolation_points(st: _State, base: C.GameConfig) -> list[OpResult]:
    g = _Generator(st, base, "interpolation_timing_points")
    sql = f"{C.CBC}__Interpolations.Interpolations"
    f_tp, f_ip = _node(sql, "TimingPoints"), _node(sql, "IslandPoints")
    itd, base_itd = _itd(st.config()), _itd(base)
    base_rows = base.tables[sql]
    for pos, r in enumerate(st.table_rows(sql)):
        islands = _island_points(f_ip.decode(r))
        if islands is None:
            continue
        points = g.rule(TL.interpolation_timing_points, islands, itd, row=C.TABLE_BY_SQL[sql].row_id(r))
        if points is None:
            continue
        rule = TL.format_points(points)
        b = base_rows.row(C.TABLE_BY_SQL[sql].row_key(r))
        b_islands = _island_points(f_ip.decode(b))
        brule = TL.format_points(TL.interpolation_timing_points(b_islands, base_itd)) if b_islands else None
        g.leaf(f_tp, pos, None, rule, brule, f_tp.decode(b))
    return g.finish()


def armor_sync_cells(rows: Sequence[EG.EvolutionValueRow], levels: int) -> list[tuple[int, int]]:
    """(rarity, level) cells of the timed armor rows whose Value or CopyValue differ across the used types."""
    by = {r.key: r for r in rows}
    out = []
    for r in EG.TIMED_RARITIES:
        for L in range(1, levels + 1):
            cells = [by[("armor", int(t), int(r), L)] for t in EG.USED_TYPES]
            if len({(float(x.value), float(x.copy_value)) for x in cells}) > 1:
                out.append((int(r), L))
    return out


@implements("pending:balance.evolution_armor_sync_by_leader", provenance=Provenance.INFERRED)
def sync_armor_by_leader(rows: Sequence[EG.EvolutionValueRow], leader: WeaponType,
                         levels: int) -> list[EG.EvolutionValueRow]:
    """``SyncShieldValuesByLeader``: the other used types copy the leader's Value and CopyValue (the shards and
    H are already synced in the stored tables); ReferenceStatPercent follows each row's own stat value."""
    by = {r.key: r for r in rows}
    out = []
    for row in rows:
        if row.kind == "armor" and row.damage_type in EG.USED_TYPES and row.damage_type != leader \
                and row.rarity in EG.TIMED_RARITIES and 1 <= row.evolution_level <= levels:
            lead = by[("armor", int(leader), row.rarity, row.evolution_level)]
            row = replace(row, value=lead.value, copy_value=lead.copy_value,
                          reference_stat_percent=EG.reference_stat_percent(lead.value, row.reference_stat_value))
        out.append(row)
    return out


def regenerated_evolution_rows(cfg: C.GameConfig, mode: str, armor_sync: str = "report"
                               ) -> tuple[list[EG.EvolutionValueRow], list[tuple[int, int]], EG.EvolutionConfig]:
    """Both evolution tables under ``mode`` (and the leader sync), plus the armor cells left unsynced."""
    evo = evolution_config(cfg)
    rows = EG.evolution_tables(evo, mode)
    levels = evo.levers.max_evolution_level
    if mode != "frozen" and evo.levers.sync_shield_values_by_leader and armor_sync != "report":
        rows = sync_armor_by_leader(rows, WeaponType[armor_sync], levels)
    broken = armor_sync_cells(rows, levels) if evo.levers.sync_shield_values_by_leader else []
    return rows, broken, evo


def _regen_evolution_tables(st: _State, base: C.GameConfig, mode: str, armor_sync: str) -> list[OpResult]:
    g = _Generator(st, base, "evolution_tables", exceptions=False)
    rows, broken, evo = regenerated_evolution_rows(st.config(), mode, armor_sync)
    if broken and armor_sync == "report":
        st.warn("armor_sync_broken", "CombatBalanceConfig.SyncShieldValuesByLeader",
                f"evolution_tables={mode} generates armor per type: {len(broken)} of "
                f"{len(EG.TIMED_RARITIES) * evo.levers.max_evolution_level} (rarity, level) cells differ across "
                "types although SyncShieldValuesByLeader is on; set regenerate.evolution_armor_sync to a leader type "
                "to sync them", cells=[list(c) for c in broken])
    by_kind = {"weapon": [r for r in rows if r.kind == "weapon"], "armor": [r for r in rows if r.kind == "armor"]}
    for kind, sql in (("weapon", f"{C.CBC}__EvolutionDamageValues"), ("armor", f"{C.CBC}__EvolutionArmorValues")):
        fv, fcv, frsp = _node(sql, "Value"), _node(sql, "CopyValue"), _node(sql, "ReferenceStatPercent")
        table_rows = st.table_rows(sql)
        if len(table_rows) != len(by_kind[kind]):
            raise PatchError("bad_patch", f"{sql}: {len(table_rows)} rows but the generator gave {len(by_kind[kind])}")
        for pos, gen in enumerate(by_kind[kind]):
            g.leaf(fv, pos, None, float(gen.value))
            g.leaf(fcv, pos, None, float(gen.copy_value))
            g.leaf(frsp, pos, None, int(gen.reference_stat_percent))
    return g.finish()


# ---------------------------------------------------------------------------------------------
# Domain views of a config (shared with pipeline)


def evolution_config(cfg: C.GameConfig) -> EG.EvolutionConfig:
    return cfg.cached("evolution_config", lambda: EG.load_evolution_config(cfg.connection()))


# ---------------------------------------------------------------------------------------------
# apply


def _net_leaves(before: C.GameConfig, after: C.GameConfig) -> list[Leaf]:
    out: list[Leaf] = []
    for sql in after.changed_tables():
        if after.tables[sql] is before.tables.get(sql):
            continue
        spec = C.TABLE_BY_SQL[sql]
        old_t, new_t = before.tables[sql], after.tables[sql]
        for new_row in new_t.rows:
            old_row = old_t.row(spec.row_key(new_row))
            for f in spec.fields:
                a, b = f.decode(old_row), f.decode(new_row)
                if a == b and type(a) is type(b):
                    continue
                rid = spec.row_id(new_row)
                if f.is_array:
                    for k in range(max(len(a), len(b))):
                        x = a[k] if k < len(a) else None
                        y = b[k] if k < len(b) else None
                        if x != y:
                            out.append(Leaf(f.node, rid, k, x, y))
                else:
                    out.append(Leaf(f.node, rid, None, a, b))
    return sorted(out, key=_sort_key)


def _explicit_set(leaf: Leaf) -> dict:
    f = C.FIELDS[leaf.node]
    spec = C.TABLE_BY_SQL[f.sql]
    out: dict = {"node": leaf.node}
    if not spec.singleton:
        if spec.key_names() == ("idx",) and not f.is_array:
            out["index"] = leaf.row["idx"]
        else:
            out["where"] = dict(leaf.row)
    if f.is_array:
        out["index"] = leaf.index
    out["set"] = leaf.new
    return out


def csharp_node(node: str) -> bool:
    """Whether the C# patcher receives this node (passthrough nodes included; Python-only fields not)."""
    f = C.field_for(node)
    return f is None or f.csharp


def apply(cfg: C.GameConfig, patch: Patch) -> tuple[C.GameConfig, PatchReport]:
    """Apply ``patch`` to ``cfg``: overrides in order, then the enabled generator steps."""
    st = _State(cfg, patch.regen("evolution_tables"))
    ops: list[OpResult] = []
    passthrough: list[Override] = []
    for i, ov in enumerate(patch.overrides):
        if ov.node not in cfg.dictionary:
            raise PatchError("unknown_node", f"{ov.node!r} is not a data_dictionary node", override=i, node=ov.node)
        if C.field_for(ov.node) is None:
            rows = check_passthrough(cfg, ov, i)
            passthrough.append(ov)
            st.warn("not_python_patchable", ov.node, "the Python model does not hold this field; it is passed to "
                    "the C# patcher only and derive_static will not react (no leaves in this report)",
                    formulas=formulas_reading(cfg, ov.node), rows=rows)
            continue
        ops.append(_apply_override(st, ov, i))
    if patch.regen("island_target_days_tail"):
        ops += _regen_itd_tail(st, cfg)
    if patch.regen("optimum_time_from_relative"):
        ops += _regen_optimum_time(st, cfg)
    if patch.regen("evolution_target_timings"):
        ops += _regen_evolution_target_timings(st, cfg)
    if patch.regen("interpolation_timing_points"):
        ops += _regen_interpolation_points(st, cfg)
    if patch.regen("evolution_tables") != "frozen":
        ops += _regen_evolution_tables(st, cfg, patch.regen("evolution_tables"), patch.regen("evolution_armor_sync"))
    out = st.config()
    leaves = _net_leaves(cfg, out)
    generated = [x for op in ops if op.override is None for x in sorted(op.leaves, key=_sort_key)]
    expanded = ([o.to_json() for o in patch.overrides if csharp_node(o.node)]
                + [_explicit_set(x) for x in generated if csharp_node(x.node)])
    python_only = sorted({x.node for x in leaves if not csharp_node(x.node)})
    report = PatchReport(patch.name, ops, leaves, st.warnings, passthrough, expanded,
                         {k: patch.regen(k) for k in REGENERATE_DEFAULTS}, python_only)
    return out, report
