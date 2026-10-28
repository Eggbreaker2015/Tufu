"""Formula implementation registry.

Python side: oracle functions declare which catalog formula they implement::

    from tufu_balance.model.registry import Provenance, implements

    @implements("spawners.charges_limit", provenance=Provenance.PORT,
                code_ref="CodeBase/Enemy/EnemySpawner.cs:812", va="0x1234abc")
    def charges_limit(...): ...

Ids must exist in ``Balance/catalog/formulas.json`` (aliases resolve to their canonical id).
Rules that have no catalog entry yet use ``pending:<domain>.<name>``; they warn while
``Balance/tests/golden/registry.json`` has ``"pending_allowed": true`` and fail once Z1 sets it
to false after promoting them into the catalog.

C# side: :func:`scan_csharp` statically parses ``[SimulationRebuild(Provenance.X, "id")]``
annotations, for when no run output (``rebuild_coverage.json``) is at hand.
"""

from __future__ import annotations

import functools
import importlib
import json
import pkgutil
import re
import warnings
from collections import Counter
from dataclasses import dataclass, field
from enum import Enum
from pathlib import Path
from typing import Any, Callable, Iterable, Mapping

BALANCE = Path(__file__).resolve().parents[2]
REPO = BALANCE.parent
CATALOG = BALANCE / "catalog" / "formulas.json"
REGISTRY_JSON = BALANCE / "tests" / "golden" / "registry.json"
CSHARP_ROOTS = (REPO / "Recovery" / "StudyProject" / "Assets" / "Scripts",
                REPO / "Recovery" / "StudyProject" / "Assets" / "Editor")

PENDING_PREFIX = "pending:"
PENDING_ID = re.compile(r"^pending:[a-z0-9_]+(\.[a-z0-9_]+)+$")


class Provenance(str, Enum):
    """Where a Python oracle implementation comes from."""

    PORT = "port"  # ported from recovered native C# (cites CodeBase/<file>:<line> and the VA)
    RULE = "rule"  # generation rule reverse-engineered from the shipped data, exact on it
    FIT = "fit"  # fitted rule, approximate on the shipped data
    DATA = "data"  # identity or fact read off the shipped data
    INFERRED = "inferred"  # reconstructed behaviour with no native code and no exact data check


class UnknownFormulaId(ValueError):
    pass


class PendingFormulaWarning(UserWarning):
    pass


@dataclass(frozen=True)
class Implementation:
    formula_id: str  # as written in the decorator
    canonical_id: str  # alias resolved; equals formula_id for pending ids
    provenance: str
    module: str
    qualname: str
    code_ref: str | None = None
    va: str | None = None

    @property
    def pending(self) -> bool:
        return self.canonical_id.startswith(PENDING_PREFIX)


@dataclass(frozen=True)
class RebuildAnnotation:
    """One ``[SimulationRebuild]`` annotation found in C# source."""

    file: str  # relative to the repository root
    line: int
    type_name: str | None
    member: str | None
    provenance: str | None  # e.g. "Inferred"; None for a bare [SimulationRebuild]
    formula_id: str | None

    @property
    def pending(self) -> bool:
        return bool(self.formula_id) and self.formula_id.startswith(PENDING_PREFIX)


@functools.lru_cache(maxsize=4)
def load_catalog(path: Path = CATALOG) -> tuple[frozenset[str], Mapping[str, str]]:
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    return frozenset(f["id"] for f in data["formulas"]), dict(data.get("aliases", {}))


def load_config(path: Path = REGISTRY_JSON) -> dict:
    path = Path(path)
    if not path.exists():
        return {"pending_allowed": True}
    return json.loads(path.read_text(encoding="utf-8"))


@dataclass
class Registry:
    """Formula ids known to the catalog plus the implementations registered against them."""

    ids: frozenset[str] | None = None
    aliases: Mapping[str, str] | None = None
    pending_allowed: bool | None = None
    implementations: list[Implementation] = field(default_factory=list)

    def _catalog(self) -> tuple[frozenset[str], Mapping[str, str]]:
        if self.ids is None:
            self.ids, aliases = load_catalog()
            if self.aliases is None:
                self.aliases = aliases
        return self.ids, self.aliases or {}

    def allows_pending(self) -> bool:
        if self.pending_allowed is None:
            return bool(load_config().get("pending_allowed", False))
        return self.pending_allowed

    def resolve(self, formula_id: str, *, stacklevel: int = 2) -> str:
        """Canonical id; raises :class:`UnknownFormulaId`, warns for pending ids."""
        if not isinstance(formula_id, str) or not formula_id:
            raise UnknownFormulaId(f"formula id must be a non-empty string, got {formula_id!r}")
        if formula_id.startswith(PENDING_PREFIX):
            if not PENDING_ID.match(formula_id):
                raise UnknownFormulaId(f"malformed pending id {formula_id!r}: use 'pending:<domain>.<name>'")
            if not self.allows_pending():
                raise UnknownFormulaId(f"pending id {formula_id!r} is no longer allowed (registry.json "
                                       f"pending_allowed is false): promote it into catalog/formulas.json")
            warnings.warn(PendingFormulaWarning(f"formula id {formula_id!r} is pending (not in the catalog yet)"),
                          stacklevel=stacklevel + 1)
            return formula_id
        ids, aliases = self._catalog()
        if formula_id in ids:
            return formula_id
        if formula_id in aliases:
            return aliases[formula_id]
        raise UnknownFormulaId(f"unknown formula id {formula_id!r}: not in {CATALOG.name} (ids or aliases); "
                               f"use 'pending:<domain>.<name>' for rules without a catalog entry")

    def implements(self, formula_id: str, *, provenance: Provenance | str, code_ref: str | None = None,
                   va: str | None = None) -> Callable[[Any], Any]:
        """Decorator registering the decorated function or class as an implementation of a formula."""
        try:
            prov = Provenance(provenance).value
        except ValueError:
            raise ValueError(f"unknown provenance {provenance!r}; expected one of "
                             f"{[p.value for p in Provenance]}") from None
        canonical = self.resolve(formula_id, stacklevel=2)

        def decorate(obj):
            impl = Implementation(formula_id, canonical, prov, obj.__module__, obj.__qualname__, code_ref, va)
            if impl not in self.implementations:
                self.implementations.append(impl)
            obj.__implements__ = tuple(getattr(obj, "__implements__", ())) + (impl,)
            return obj

        return decorate

    def implementers(self, formula_id: str) -> list[Implementation]:
        key = formula_id if formula_id.startswith(PENDING_PREFIX) else self._catalog()[1].get(formula_id, formula_id)
        return [i for i in self.implementations if i.canonical_id == key]

    def coverage(self, csharp: Iterable[RebuildAnnotation | Mapping[str, Any]] | None = None,
                 import_models: bool = True) -> dict:
        """Counts by provenance on both sides, and catalog formulas implemented by either side.

        ``csharp`` defaults to :func:`scan_csharp`; pass entries from a run's ``rebuild_coverage.json``
        (mappings with ``provenance`` and ``formula_id``) to use the run's list instead.
        """
        if import_models:
            import_model_modules()
        ids, aliases = self._catalog()
        cs = [_as_annotation(a) for a in (scan_csharp() if csharp is None else csharp)]
        py_ids = {i.canonical_id for i in self.implementations if not i.pending}
        cs_ids = {aliases.get(a.formula_id, a.formula_id) for a in cs if a.formula_id and not a.pending}
        return {
            "python": {
                "implementations": len(self.implementations),
                "by_provenance": dict(sorted(Counter(i.provenance for i in self.implementations).items())),
                "formulas": len(py_ids),
                "pending": sorted({i.canonical_id for i in self.implementations if i.pending}),
            },
            "csharp": {
                "annotations": len(cs),
                "by_provenance": dict(sorted(Counter(a.provenance or "unspecified" for a in cs).items())),
                "formulas": len(cs_ids & ids),
                "pending": sorted({a.formula_id for a in cs if a.pending}),
                "unknown_ids": sorted(cs_ids - ids),
            },
            "catalog": {
                "formulas": len(ids),
                "python": len(py_ids & ids),
                "csharp": len(cs_ids & ids),
                "either": len((py_ids | cs_ids) & ids),
            },
        }


REGISTRY = Registry()


def implements(formula_id: str, *, provenance: Provenance | str, code_ref: str | None = None,
               va: str | None = None) -> Callable[[Any], Any]:
    return REGISTRY.implements(formula_id, provenance=provenance, code_ref=code_ref, va=va)


def coverage(csharp: Iterable[RebuildAnnotation | Mapping[str, Any]] | None = None,
             import_models: bool = True) -> dict:
    return REGISTRY.coverage(csharp, import_models)


def import_model_modules() -> list[str]:
    """Import every ``tufu_balance.model`` submodule so that their decorators run."""
    package = importlib.import_module(__package__)
    names = sorted(m.name for m in pkgutil.iter_modules(package.__path__) if not m.name.startswith("_"))
    for name in names:
        importlib.import_module(f"{__package__}.{name}")
    return names


def _as_annotation(entry: RebuildAnnotation | Mapping[str, Any]) -> RebuildAnnotation:
    if isinstance(entry, RebuildAnnotation):
        return entry
    return RebuildAnnotation(entry.get("file", ""), int(entry.get("line", 0)), entry.get("type"),
                             entry.get("member"), entry.get("provenance"), entry.get("formula_id"))


# ---------------------------------------------------------------------------------------------
# C# [SimulationRebuild] annotations

_TOKENS = re.compile(r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\\n])*"|\'(?:\\.|[^\'\\\n])*\'|//[^\n]*|/\*.*?\*/', re.S)
_SECTION = re.compile(r"\[\s*(?:(?:method|property|field|return)\s*:\s*)?((?:[^\[\]\"]|\"(?:\\.|[^\"\\])*\")*)\]")
_ATTR_NAME = re.compile(r"^(?:global::)?(?:[A-Za-z_]\w*\.)*SimulationRebuild(?:Attribute)?$")
_TYPE_DECL = re.compile(r"\b(?:class|struct|interface|record)\s+([A-Za-z_]\w*)")
_PROVENANCE_ARG = re.compile(r"^(?:global::)?(?:[A-Za-z_]\w*\.)*Provenance\.([A-Za-z_]\w*)$")
_STRING = re.compile(r'^@?"((?:\\.|[^"\\])*)"$')
_KEYWORDS = {"if", "for", "foreach", "while", "switch", "using", "lock", "catch", "return", "new", "typeof",
             "nameof", "sizeof", "default", "base", "this"}


def _strip_comments(text: str) -> str:
    """Blank out comments (keeping newlines and string literals) so offsets and lines stay valid."""
    def repl(m):
        s = m.group(0)
        return s if s[0] in "\"'@" else re.sub(r"[^\n]", " ", s)
    return _TOKENS.sub(repl, text)


def _split_top(text: str, sep: str = ",") -> list[str]:
    parts, depth, cur, quote = [], 0, [], False
    i = 0
    while i < len(text):
        c = text[i]
        if quote:
            cur.append(c)
            if c == "\\":
                cur.append(text[i + 1:i + 2])
                i += 1
            elif c == '"':
                quote = False
        elif c == '"':
            quote = True
            cur.append(c)
        elif c in "([{<":
            depth += 1
            cur.append(c)
        elif c in ")]}>":
            depth -= 1
            cur.append(c)
        elif c == sep and depth == 0:
            parts.append("".join(cur).strip())
            cur = []
        else:
            cur.append(c)
        i += 1
    if "".join(cur).strip():
        parts.append("".join(cur).strip())
    return parts


def _member_after(text: str, pos: int) -> str | None:
    """Name of the member declared after the attribute sections that end at ``pos``."""
    while True:
        m = re.compile(r"\s*").match(text, pos)
        pos = m.end()
        if pos < len(text) and text[pos] == "[":
            sec = _SECTION.match(text, pos)
            if not sec:
                return None
            pos = sec.end()
            continue
        break
    end = len(text)
    for stop in ("{", ";", "=>", "="):
        i = text.find(stop, pos)
        if i != -1:
            end = min(end, i)
    header = text[pos:end]
    m = re.search(r"([A-Za-z_]\w*)\s*(?:<[^()]*>)?\s*\(", header)
    if m and m.group(1) not in _KEYWORDS:
        return m.group(1)
    names = re.findall(r"[A-Za-z_]\w*", header)
    return names[-1] if names else None


def parse_rebuild_annotations(text: str, file: str = "<string>") -> list[RebuildAnnotation]:
    """All ``[SimulationRebuild]`` annotations in one C# source text."""
    if "SimulationRebuild" not in text:
        return []
    code = _strip_comments(text)
    types = [(m.start(), m.group(1)) for m in _TYPE_DECL.finditer(code)]
    out = []
    for sec in _SECTION.finditer(code):
        for attr in _split_top(sec.group(1)):
            name, _, rest = attr.partition("(")
            if not _ATTR_NAME.match(name.strip()):
                continue
            args = _split_top(rest.rsplit(")", 1)[0]) if rest else []
            provenance = formula_id = None
            for arg in args:
                value = arg.split("=", 1)[1].strip() if re.match(r"^\w+\s*=[^=]", arg) else arg
                if m := _PROVENANCE_ARG.match(value):
                    provenance = m.group(1)
                elif m := _STRING.match(value):
                    formula_id = m.group(1)
            type_name = next((n for p, n in reversed(types) if p < sec.start()), None)
            out.append(RebuildAnnotation(file, code.count("\n", 0, sec.start()) + 1, type_name,
                                         _member_after(code, sec.end()), provenance, formula_id))
    return out


def scan_csharp(roots: Iterable[Path] = CSHARP_ROOTS, repo: Path = REPO) -> list[RebuildAnnotation]:
    """Annotations in every ``*.cs`` under ``roots``, sorted by file and line."""
    out = []
    for root in roots:
        for path in sorted(Path(root).rglob("*.cs")):
            data = path.read_bytes()
            if b"SimulationRebuild" not in data:
                continue
            rel = path.relative_to(repo).as_posix() if path.is_relative_to(repo) else str(path)
            out.extend(parse_rebuild_annotations(data.decode("utf-8-sig", errors="replace"), rel))
    return sorted(out, key=lambda a: (a.file, a.line))
