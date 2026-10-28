"""Parameter layers and ``params_hash`` (design 4.8, 5.3; unit P10).

Layers, merged in this order (later wins):

1. ``Balance/sim/params/defaults/<module>.json``: initial values, one file per module unit (C3b engine, C5
   fasttick, C6 inventory, C7 slow_policy). A field may appear in one module file only; the union of the
   files is the complete parameter object and fixes each field's type.
2. ``open.json``, ``slow.json``, ``calibrated.json`` (K1, K2, K3) and any further named layer or mapping.
3. Per-run overrides.

Later layers may only set fields the defaults define. Enum fields are integers in JSON (as in the C#
``SimulationRebuildParams``); a layer may also name the member (``"KillState": "AfterPass"``) when the
C# enum declarations are at hand, and :func:`resolve` turns it into the integer. Integers given for a
float field become floats, so the canonical JSON and :func:`params_hash` do not depend on how a layer
spelled a number. Keys starting with ``_`` are comments.

The C# side is the authority on field names, types and enum values: :func:`read_csharp_params` parses
the partial ``SimulationRebuildParams*.cs`` files and the enums under ``CodeBase/Simulation`` with the
extractor's enum parser, and :func:`check_against_csharp` compares a resolved object with them.
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Mapping, Sequence

from ..csharp_enums import parse_enums
from ..unity import request as req

BALANCE = Path(__file__).resolve().parents[2]
PARAMS_ROOT = BALANCE / "sim" / "params"
SIMULATION_SCRIPTS = (BALANCE.parent / "Recovery" / "StudyProject" / "Assets" / "Scripts" / "Assembly-CSharp"
                      / "CodeBase" / "Simulation")
CALIBRATION_LAYERS = ("open", "slow", "calibrated")
PARAMS_CLASS = "SimulationRebuildParams"


class ParamsError(ValueError):
    pass


@dataclass(frozen=True)
class Layer:
    name: str
    values: Mapping[str, Any]
    files: tuple[str, ...] = ()


@dataclass
class Resolved:
    params: dict[str, Any]  # complete object, keys sorted
    params_hash: str
    source: dict[str, str]  # field -> layer that set its final value
    layers: tuple[str, ...]

    def canonical(self) -> str:
        return req.canonical_json(self.params)


def _strip(values: Mapping[str, Any], where: str) -> dict[str, Any]:
    if not isinstance(values, Mapping):
        raise ParamsError(f"{where}: a layer must be a JSON object")
    return {k: v for k, v in values.items() if not str(k).startswith("_")}


def _read_json(path: Path) -> dict:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        raise ParamsError(f"{path}: {e}") from None


def load_defaults(root: str | Path = PARAMS_ROOT) -> Layer:
    """The union of ``defaults/*.json`` (file name order); a field defined by two modules is an error."""
    files = sorted((Path(root) / "defaults").glob("*.json"))
    if not files:
        raise ParamsError(f"no default parameter files under {Path(root) / 'defaults'}")
    values: dict[str, Any] = {}
    owner: dict[str, str] = {}
    for f in files:
        for k, v in _strip(_read_json(f), str(f)).items():
            if k in owner:
                raise ParamsError(f"parameter {k} defined by both {owner[k]} and {f.name}")
            owner[k], values[k] = f.name, v
    return Layer("defaults", values, tuple(f.name for f in files))


def load_layer(layer: str | Path | Mapping[str, Any] | Layer, root: str | Path = PARAMS_ROOT) -> Layer:
    if isinstance(layer, Layer):
        return layer
    if isinstance(layer, Mapping):
        return Layer("mapping", _strip(layer, "mapping"))
    if str(layer) == "defaults":
        return load_defaults(root)
    path = Path(layer)
    if path.suffix != ".json":
        path = Path(root) / f"{layer}.json"
    return Layer(path.stem, _strip(_read_json(path), str(path)), (path.name,))


# ---------------------------------------------------------------- C# declarations

@dataclass
class CsParams:
    """Fields of the C# ``SimulationRebuildParams`` (all partial files) and the enums they use."""

    fields: dict[str, str]  # name -> C# type text ("int", "float[]", "RollMode", ...)
    defaults: dict[str, str]  # name -> initializer text
    enums: dict[str, dict[str, int]]  # enum -> member -> value
    files: tuple[str, ...] = ()

    def enum_of(self, name: str) -> str | None:
        t = self.fields.get(name, "").rstrip("[]")
        return t if t in self.enums else None


_CLASS = re.compile(r"\bpartial\s+class\s+" + PARAMS_CLASS + r"\b[^{]*\{")
_FIELD = re.compile(r"^\s*public\s+(?!static|const|class|enum|partial)([\w.]+(?:\[\])?)\s+(\w+)\s*(?:=\s*([^;]+))?;",
                    re.M)
_COMMENT = re.compile(r"//[^\n]*|/\*.*?\*/", re.S)


def _class_body(text: str, start: int) -> str:
    depth, i = 1, start
    while i < len(text) and depth:
        depth += {"{": 1, "}": -1}.get(text[i], 0)
        i += 1
    return text[start:i - 1]


def read_csharp_params(scripts_root: str | Path = SIMULATION_SCRIPTS) -> CsParams | None:
    """Parse the C# parameter class; None when no ``partial class SimulationRebuildParams`` exists yet."""
    root = Path(scripts_root)
    fields: dict[str, str] = {}
    defaults: dict[str, str] = {}
    files = []
    for path in sorted(root.rglob("*.cs")):
        text = _COMMENT.sub("", path.read_text(encoding="utf-8", errors="replace"))
        for m in _CLASS.finditer(text):
            files.append(path.relative_to(root).as_posix())
            body = _class_body(text, m.end())
            for f in _FIELD.finditer(body):
                ctype, name, init = f.group(1), f.group(2), (f.group(3) or "").strip()
                if name in fields:
                    raise ParamsError(f"C# field {name} declared twice ({path.name})")
                fields[name], defaults[name] = ctype, init
    if not files:
        return None
    enums: dict[str, dict[str, int]] = {}
    for enum, _ns, member, value, _file in parse_enums(root):
        enums.setdefault(enum, {})[member] = value
    return CsParams(fields=fields, defaults=defaults, enums=enums, files=tuple(sorted(set(files))))


_NUMBER = re.compile(r"^-?(\d+(\.\d*)?|\.\d+)([eE][-+]?\d+)?[fFdDmM]?$")


def csharp_default_value(cs: CsParams, name: str) -> Any:
    """The JSON value of a field's C# initializer (enums as integers); raises for forms it cannot read."""
    ctype, init = cs.fields[name], cs.defaults[name]
    base = ctype.rstrip("[]")
    if not init:
        return [] if ctype.endswith("[]") else {"bool": False, "string": None}.get(base, 0)

    def scalar(text: str) -> Any:
        text = text.strip()
        if base == "bool":
            return {"true": True, "false": False}[text]
        if base == "string":
            return json.loads(text) if text != "null" else None
        if base in cs.enums:
            enum, _, member = text.rpartition(".")
            if enum.split(".")[-1] != base or member not in cs.enums[base]:
                raise ParamsError(f"{name}: initializer {text} is not a {base} member")
            return cs.enums[base][member]
        if not _NUMBER.match(text):
            raise ParamsError(f"{name}: cannot read initializer {text!r}")
        number = text.rstrip("fFdDmM")
        return float(number) if base in ("float", "double") else int(number)

    if ctype.endswith("[]"):
        inner = re.sub(r"^new\s+[\w.]+\s*\[\s*\]\s*", "", init).strip()
        if not (inner.startswith("{") and inner.endswith("}")):
            raise ParamsError(f"{name}: cannot read array initializer {init!r}")
        items = [x for x in inner[1:-1].split(",") if x.strip()]
        return [scalar(x) for x in items]
    return scalar(init)


def check_against_csharp(params: Mapping[str, Any], cs: CsParams, *, complete: bool = True) -> list[str]:
    """Problems of a parameter object against the C# class: unknown or (with ``complete``) missing fields,
    JSON types that the C# field cannot take, enum integers that are not members."""
    problems = []
    for name in sorted(params):
        if name not in cs.fields:
            problems.append(f"{name}: no such field in {PARAMS_CLASS}")
            continue
        problems += [f"{name}: {p}" for p in _type_problems(params[name], cs.fields[name], cs)]
    if complete:
        problems += [f"{name}: missing (the request carries the complete object)"
                     for name in sorted(set(cs.fields) - set(params))]
    return problems


def _type_problems(value: Any, ctype: str, cs: CsParams) -> list[str]:
    if ctype.endswith("[]"):
        if not isinstance(value, list):
            return [f"expected an array for {ctype}"]
        return [p for v in value for p in _type_problems(v, ctype[:-2], cs)]
    if ctype in cs.enums:
        if isinstance(value, bool) or not isinstance(value, int):
            return [f"enum {ctype} must be an integer in JSON, got {value!r}"]
        if value not in cs.enums[ctype].values():
            return [f"{value} is not a member of {ctype} {sorted(cs.enums[ctype].items(), key=lambda x: x[1])}"]
        return []
    ok = {
        "int": lambda v: isinstance(v, int) and not isinstance(v, bool),
        "long": lambda v: isinstance(v, int) and not isinstance(v, bool),
        "float": lambda v: isinstance(v, (int, float)) and not isinstance(v, bool),
        "double": lambda v: isinstance(v, (int, float)) and not isinstance(v, bool),
        "bool": lambda v: isinstance(v, bool),
        "string": lambda v: v is None or isinstance(v, str),
    }.get(ctype)
    if ok is None:
        return [f"unsupported C# type {ctype} (design 4.8: int, float, double, bool, string, enums, arrays)"]
    return [] if ok(value) else [f"{value!r} does not fit {ctype}"]


def csharp_defaults(cs: CsParams) -> dict[str, Any]:
    return {name: csharp_default_value(cs, name) for name in sorted(cs.fields)}


# ---------------------------------------------------------------- merge

def _coerce(name: str, value: Any, base: Any, enum_members: Mapping[str, int] | None, where: str) -> Any:
    if isinstance(value, str) and enum_members is not None and not isinstance(base, str):
        if value not in enum_members:
            raise ParamsError(f"{where}: {name} = {value!r} is not a member ({', '.join(sorted(enum_members))})")
        return enum_members[value]
    if isinstance(base, bool) or isinstance(value, bool):
        if not (isinstance(base, bool) and isinstance(value, bool)):
            raise ParamsError(f"{where}: {name} = {value!r} does not match the default's type ({base!r})")
        return value
    if isinstance(base, float):
        if not isinstance(value, (int, float)):
            raise ParamsError(f"{where}: {name} = {value!r} must be a number")
        return float(value)
    if isinstance(base, int):
        if isinstance(value, float) and value.is_integer():
            return int(value)
        if not isinstance(value, int):
            raise ParamsError(f"{where}: {name} = {value!r} must be an integer")
        return value
    if isinstance(base, list):
        if not isinstance(value, list):
            raise ParamsError(f"{where}: {name} must be an array")
        proto = base[0] if base else None
        return [v if proto is None else _coerce(name, v, proto, enum_members, where) for v in value]
    if base is None or isinstance(base, str):
        if value is not None and not isinstance(value, str):
            raise ParamsError(f"{where}: {name} = {value!r} must be a string")
        return value
    raise ParamsError(f"{where}: {name}: unsupported default {base!r}")


def resolve(layers: Sequence[str | Path | Mapping[str, Any] | Layer] = ("defaults",),
            overrides: Mapping[str, Any] | None = None, *, root: str | Path = PARAMS_ROOT,
            csharp: CsParams | None = None) -> Resolved:
    """Merge ``layers`` (the first must be the defaults) and ``overrides`` into the complete object."""
    loaded = [load_layer(x, root) for x in layers]
    if overrides:
        loaded.append(Layer("overrides", _strip(overrides, "overrides")))
    if not loaded:
        raise ParamsError("no layers")
    base = loaded[0]
    params = dict(base.values)
    if csharp is not None:  # a float field written as 1 in the defaults is 1.0
        for k, v in params.items():
            ctype = csharp.fields.get(k, "").rstrip("[]")
            if ctype in ("float", "double"):
                params[k] = [float(x) for x in v] if isinstance(v, list) else float(v)
    source = {k: base.name for k in params}
    for layer in loaded[1:]:
        for k, v in layer.values.items():
            if k not in params:
                raise ParamsError(f"layer {layer.name}: unknown parameter {k} (not in {base.name})")
            members = csharp.enums.get(csharp.enum_of(k) or "") if csharp else None
            params[k] = _coerce(k, v, params[k], members, f"layer {layer.name}")
            source[k] = layer.name
    ordered = {k: params[k] for k in sorted(params)}
    return Resolved(params=ordered, params_hash=params_hash(ordered), source={k: source[k] for k in sorted(source)},
                    layers=tuple(x.name for x in loaded))


def params_hash(params: Mapping[str, Any]) -> str:
    """sha256 of the canonical JSON (sorted keys, no spaces): the request's ``params_hash``."""
    return req.params_hash(params)


def enum_names(cs: CsParams, params: Mapping[str, Any]) -> dict[str, str]:
    """``{field: member name}`` of the enum fields of a resolved object (for reports)."""
    out = {}
    for name, value in params.items():
        enum = cs.enum_of(name)
        if enum and isinstance(value, int):
            out[name] = next((m for m, v in cs.enums[enum].items() if v == value), f"?{value}")
    return out
