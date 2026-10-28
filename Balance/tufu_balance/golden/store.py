"""Two-layer golden baselines (design section 3.2).

Layout under ``Balance/tests/golden/``::

    original/<suite>.json      ORIGINAL baseline: today's numbers on the shipped data (frozen)
    current/<suite>.json       CURRENT baseline: what the tests compare against (changed only by accept)
    current/<suite>/<file>     byte-compared files of a file suite (e.g. current/sim_run/*.csv)
    loosening/<suite>.jsonl    registered loosenings {key, from, to, kind, better, reason, date, git_rev}
    accepted/<suite>.jsonl     one record per accept / rebaseline-original

Every expectation is ``{"kind": "exact"|"min"|"max"|"set", "value": ..., "source": ..., "note": ...}``
plus an optional ``"better"`` that says which direction counts as tighter:

* ``exact`` -- ``"higher"`` or ``"lower"``; without it every change is a loosening.
* ``set``   -- ``"superset"`` or ``"subset"``; without it every change is a loosening.
* ``min``   -- the measured value must be ``>=`` the bound; a higher bound is tighter.
* ``max``   -- the measured value must be ``<=`` the bound; a lower bound is tighter.

Tests call :meth:`GoldenStore.check`; it records the measured values under
``Balance/out/golden/latest/`` (not tracked) and compares them strictly with CURRENT. ``accept``
copies recorded values into CURRENT.
"""

from __future__ import annotations

import datetime
import hashlib
import json
import math
import os
import subprocess
import warnings
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable, Mapping

BALANCE = Path(__file__).resolve().parents[2]
GOLDEN_DIR = BALANCE / "tests" / "golden"
LATEST_DIR = BALANCE / "out" / "golden" / "latest"

SCHEMA = "tufu.golden/1"
KINDS = ("exact", "min", "max", "set")
BETTER = {"exact": (None, "higher", "lower"), "set": (None, "superset", "subset"), "min": (None,), "max": (None,)}
LAYERS = ("original", "current")
TIGHTEN_REL = 1e-9  # ratchet improvements below this relative size are not reported


class GoldenError(Exception):
    """Bad input to the store (unknown suite or key, malformed file, missing reason)."""


class GoldenMismatch(AssertionError):
    """Measured values do not satisfy the CURRENT baseline."""


class AcceptRefused(GoldenError):
    """accept would loosen a key beyond ORIGINAL without ``--loosen``."""

    def __init__(self, message: str, keys: list[str]):
        super().__init__(message)
        self.keys = keys


class GoldenTightenWarning(UserWarning):
    """A ratchet improved noticeably: tighten it with ``golden accept``."""


# ---------------------------------------------------------------------------------------------
# Values


def _plain(value: Any) -> Any:
    """JSON-shaped copy: tuples become lists, numpy scalars become Python numbers."""
    if isinstance(value, bool) or value is None or isinstance(value, str):
        return value
    if isinstance(value, int):
        return int(value)
    if isinstance(value, float):
        return float(value)
    if isinstance(value, Mapping):
        return {str(k): _plain(v) for k, v in value.items()}
    if isinstance(value, (list, tuple)):
        return [_plain(v) for v in value]
    if isinstance(value, (set, frozenset)):
        return _sorted_set(value)
    if hasattr(value, "item"):  # numpy scalar
        return _plain(value.item())
    if hasattr(value, "tolist"):  # numpy array
        return _plain(value.tolist())
    raise GoldenError(f"value of type {type(value).__name__} is not JSON-serialisable: {value!r}")


def canonical(value: Any) -> str:
    return json.dumps(value, sort_keys=True, ensure_ascii=False, allow_nan=True, separators=(",", ":"))


def _sorted_set(values: Iterable[Any]) -> list[Any]:
    items = {canonical(v): v for v in (_plain(v) for v in values)}
    return [items[k] for k in sorted(items)]


def same(a: Any, b: Any) -> bool:
    """Structural equality; ``1 == 1.0``, ``NaN == NaN``, bools are not numbers."""
    if isinstance(a, bool) or isinstance(b, bool):
        return type(a) is type(b) and a == b
    if isinstance(a, (int, float)) and isinstance(b, (int, float)):
        if isinstance(a, float) and isinstance(b, float) and math.isnan(a) and math.isnan(b):
            return True
        return a == b
    if isinstance(a, list) and isinstance(b, list):
        return len(a) == len(b) and all(same(x, y) for x, y in zip(a, b))
    if isinstance(a, dict) and isinstance(b, dict):
        return a.keys() == b.keys() and all(same(a[k], b[k]) for k in a)
    return type(a) is type(b) and a == b


def _number(value: Any, key: str) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise GoldenError(f"{key}: min/max expectations need a number, got {value!r}")
    return value


@dataclass(frozen=True)
class Expectation:
    kind: str
    value: Any
    source: str = ""
    note: str = ""
    better: str | None = None

    def __post_init__(self):
        if self.kind not in KINDS:
            raise GoldenError(f"unknown expectation kind {self.kind!r}; expected one of {KINDS}")
        if self.better not in BETTER[self.kind]:
            raise GoldenError(f"kind {self.kind!r} does not take better={self.better!r}")
        value = _sorted_set(self.value) if self.kind == "set" else _plain(self.value)
        if self.kind in ("min", "max"):
            _number(value, "expectation")
        object.__setattr__(self, "value", value)

    def to_json(self) -> dict:
        out = {"kind": self.kind, "value": self.value, "source": self.source, "note": self.note}
        if self.better:
            out["better"] = self.better
        return out

    @classmethod
    def from_json(cls, data: Mapping[str, Any]) -> "Expectation":
        unknown = set(data) - {"kind", "value", "source", "note", "better"}
        if unknown:
            raise GoldenError(f"unknown expectation fields {sorted(unknown)}")
        return cls(data["kind"], data["value"], data.get("source", ""), data.get("note", ""), data.get("better"))

    def same_as(self, other: "Expectation") -> bool:
        return (self.kind, self.source, self.note, self.better) == (other.kind, other.source, other.note,
                                                                    other.better) and same(self.value, other.value)


def exact(value: Any, source: str = "", note: str = "", better: str | None = None) -> Expectation:
    return Expectation("exact", value, source, note, better)


def at_least(value: float, source: str = "", note: str = "") -> Expectation:
    """Ratchet: measured value must stay ``>=`` the accepted bound."""
    return Expectation("min", value, source, note)


def at_most(value: float, source: str = "", note: str = "") -> Expectation:
    """Ratchet: measured value must stay ``<=`` the accepted bound."""
    return Expectation("max", value, source, note)


def same_set(values: Iterable[Any], source: str = "", note: str = "", better: str | None = None) -> Expectation:
    return Expectation("set", list(values), source, note, better)


def _as_expectation(value: Any) -> Expectation:
    return value if isinstance(value, Expectation) else exact(value)


# ---------------------------------------------------------------------------------------------
# Comparisons


def is_looser(base: Expectation, new: Expectation | None) -> bool:
    """True when ``new`` accepts something ``base`` rejects (dropping the key counts too)."""
    if new is None:
        return True
    if new.kind != base.kind or new.better != base.better:
        return True
    if base.kind == "min":
        return not new.value >= base.value
    if base.kind == "max":
        return not new.value <= base.value
    if same(base.value, new.value):
        return False
    if base.kind == "exact":
        if base.better == "higher" and isinstance(new.value, (int, float)):
            return not new.value > base.value
        if base.better == "lower" and isinstance(new.value, (int, float)):
            return not new.value < base.value
        return True
    old, cur = {canonical(v) for v in base.value}, {canonical(v) for v in new.value}
    if base.better == "superset":
        return not cur >= old
    if base.better == "subset":
        return not cur <= old
    return True


def _improved(base: Expectation, value: Any) -> bool:
    margin = TIGHTEN_REL * max(1.0, abs(base.value))
    if base.kind == "min":
        return value - base.value > margin
    if base.kind == "max":
        return base.value - value > margin
    return False


def compare(key: str, base: Expectation, measured: Expectation) -> tuple[str | None, str | None]:
    """Compare a measured value with its CURRENT expectation: ``(failure, tighten_hint)``."""
    if measured.kind != base.kind:
        return f"{key}: kind is {measured.kind!r} in the test but {base.kind!r} in current", None
    value = measured.value
    if base.kind == "exact":
        if same(value, base.value):
            return None, None
        return f"{key}: expected exactly {_short(base.value)}, measured {_short(value)}", None
    if base.kind == "set":
        if same(value, base.value):
            return None, None
        old, cur = {canonical(v): v for v in base.value}, {canonical(v): v for v in value}
        added = [cur[k] for k in sorted(cur.keys() - old.keys())]
        removed = [old[k] for k in sorted(old.keys() - cur.keys())]
        return (f"{key}: set changed ({len(base.value)} -> {len(value)}); added {_short(added)}; "
                f"removed {_short(removed)}"), None
    v = _number(value, key)
    ok = v >= base.value if base.kind == "min" else v <= base.value
    if not ok:
        op = ">=" if base.kind == "min" else "<="
        return f"{key}: ratchet {op} {base.value!r} broken, measured {v!r}", None
    if _improved(base, v):
        return None, f"{key}: {base.kind} bound {base.value!r} improved to {v!r}"
    return None, None


def _short(value: Any, limit: int = 20) -> str:
    if isinstance(value, list) and len(value) > limit:
        return canonical(value[:limit])[:-1] + f",... (+{len(value) - limit})]"
    text = canonical(value)
    return text if len(text) <= 400 else text[:400] + "..."


# ---------------------------------------------------------------------------------------------
# Store


def git_rev(cwd: Path = BALANCE) -> str:
    try:
        out = subprocess.run(["git", "rev-parse", "HEAD"], cwd=cwd, capture_output=True, text=True, check=True)
        return out.stdout.strip()
    except (OSError, subprocess.CalledProcessError):
        return "unknown"


def today() -> str:
    return datetime.datetime.now(datetime.timezone.utc).date().isoformat()


def _write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(path.name + f".tmp{os.getpid()}")
    tmp.write_text(json.dumps(data, indent=1, sort_keys=True, ensure_ascii=False, allow_nan=True) + "\n",
                   encoding="utf-8")
    tmp.replace(path)


def _append_jsonl(path: Path, record: Mapping[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as f:
        f.write(json.dumps(record, sort_keys=True, ensure_ascii=False, allow_nan=True) + "\n")


def _read_jsonl(path: Path) -> list[dict]:
    if not path.exists():
        return []
    return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]


def _check_suite_name(suite: str) -> str:
    if not suite or not suite.replace("_", "").replace("-", "").isalnum():
        raise GoldenError(f"bad suite name {suite!r}: use letters, digits, '_' or '-'")
    return suite


@dataclass
class AcceptResult:
    suite: str
    changed: list[str] = field(default_factory=list)
    seeded_original: list[str] = field(default_factory=list)
    loosened: list[str] = field(default_factory=list)
    dropped: list[str] = field(default_factory=list)


@dataclass(frozen=True)
class GuardViolation:
    suite: str
    key: str
    original: Expectation
    current: Expectation | None

    def __str__(self) -> str:
        cur = "missing" if self.current is None else canonical(self.current.to_json())
        return (f"{self.suite}/{self.key}: current {_short(cur)} is looser than original "
                f"{_short(canonical(self.original.to_json()))} and no loosening is registered")


class GoldenStore:
    def __init__(self, root: Path | str = GOLDEN_DIR, latest: Path | str = LATEST_DIR):
        self.root = Path(root)
        self.latest_dir = Path(latest)

    # -- files ---------------------------------------------------------------------------------

    def path(self, layer: str, suite: str) -> Path:
        if layer not in LAYERS:
            raise GoldenError(f"unknown layer {layer!r}")
        return self.root / layer / f"{_check_suite_name(suite)}.json"

    def suites(self) -> list[str]:
        names = set()
        for layer in LAYERS:
            names.update(p.stem for p in (self.root / layer).glob("*.json"))
        return sorted(names)

    def load(self, layer: str, suite: str) -> dict[str, Expectation]:
        path = self.path(layer, suite)
        if not path.exists():
            return {}
        data = json.loads(path.read_text(encoding="utf-8"))
        if data.get("schema") != SCHEMA or data.get("suite") != suite:
            raise GoldenError(f"{path}: expected schema {SCHEMA!r} and suite {suite!r}")
        try:
            return {k: Expectation.from_json(v) for k, v in data["expectations"].items()}
        except GoldenError as e:
            raise GoldenError(f"{path}: {e}") from None

    def save(self, layer: str, suite: str, expectations: Mapping[str, Expectation]) -> None:
        _write_json(self.path(layer, suite), {"schema": SCHEMA, "suite": suite,
                                              "expectations": {k: v.to_json() for k, v in expectations.items()}})

    def loosening(self, suite: str) -> list[dict]:
        return _read_jsonl(self.root / "loosening" / f"{_check_suite_name(suite)}.jsonl")

    def accepted(self, suite: str) -> list[dict]:
        return _read_jsonl(self.root / "accepted" / f"{_check_suite_name(suite)}.jsonl")

    # -- latest measured values ----------------------------------------------------------------

    def latest_path(self, suite: str) -> Path:
        return self.latest_dir / f"{_check_suite_name(suite)}.json"

    def latest(self, suite: str) -> dict[str, Expectation]:
        path = self.latest_path(suite)
        if not path.exists():
            return {}
        data = json.loads(path.read_text(encoding="utf-8"))
        return {k: Expectation.from_json({f: v[f] for f in v if f != "recorded"}) for k, v in data["results"].items()}

    def record(self, suite: str, results: Mapping[str, Expectation]) -> None:
        path = self.latest_path(suite)
        data = json.loads(path.read_text(encoding="utf-8")) if path.exists() else {"suite": suite, "results": {}}
        stamp = {"git_rev": _cached_rev(), "date": today()}
        for key, exp in results.items():
            data["results"][key] = {**exp.to_json(), "recorded": stamp}
        _write_json(path, data)

    # -- test-side check -----------------------------------------------------------------------

    def check(self, suite: str, results: Mapping[str, Any]) -> None:
        """Record measured values, then compare them strictly with CURRENT.

        Raises :class:`GoldenMismatch` listing every failing key; warns
        :class:`GoldenTightenWarning` for ratchets that improved.
        """
        measured = {str(k): _as_expectation(v) for k, v in results.items()}
        self.record(suite, measured)
        current = self.load("current", suite)
        failures, hints = [], []
        for key in sorted(measured):
            base = current.get(key)
            if base is None:
                failures.append(f"{key}: no current baseline (measured {_short(measured[key].value)})")
                continue
            failure, hint = compare(key, base, measured[key])
            if failure:
                failures.append(failure)
            if hint:
                hints.append(hint)
        if hints:
            warnings.warn(GoldenTightenWarning(
                f"golden suite {suite!r}: ratchets improved, tighten with `python -m tufu_balance.golden accept "
                f"{suite} --keys ... --reason ...`:\n  " + "\n  ".join(hints)), stacklevel=3)
        if failures:
            raise GoldenMismatch(
                f"golden suite {suite!r}: {len(failures)} of {len(measured)} key(s) differ from current "
                f"(measured values recorded in {self.latest_path(suite)}; accept deliberate changes with "
                f"`python -m tufu_balance.golden accept {suite} --keys ... --reason ...`):\n  " + "\n  ".join(failures))

    # -- file suites (byte comparison, e.g. sim_run CSVs) ----------------------------------------

    def file_path(self, layer: str, suite: str, name: str) -> Path:
        if "/" in name or "\\" in name or name.startswith("."):
            raise GoldenError(f"bad file name {name!r}")
        base = self.latest_dir / "files" if layer == "latest" else self.root / layer
        return base / _check_suite_name(suite) / name

    def check_file(self, suite: str, name: str, data: bytes) -> None:
        latest = self.file_path("latest", suite, name)
        latest.parent.mkdir(parents=True, exist_ok=True)
        latest.write_bytes(data)
        current = self.file_path("current", suite, name)
        if not current.exists():
            raise GoldenMismatch(f"golden file {suite}/{name}: no current baseline (measured copy in {latest})")
        if current.read_bytes() != data:
            raise GoldenMismatch(f"golden file {suite}/{name}: bytes differ from current "
                                 f"(sha256 {_sha(current.read_bytes())} -> {_sha(data)}; measured copy in {latest})")

    def accept_files(self, suite: str, reason: str, names: Iterable[str] | None = None,
                     rev: str | None = None) -> list[str]:
        if not reason.strip():
            raise GoldenError("accept needs a non-empty --reason")
        src_dir = self.latest_dir / "files" / _check_suite_name(suite)
        available = sorted(p.name for p in src_dir.glob("*")) if src_dir.exists() else []
        names = sorted(names) if names else available
        missing = [n for n in names if n not in available]
        if missing:
            raise GoldenError(f"no recorded file(s) {missing} for suite {suite!r}")
        changes = []
        for name in names:
            data = (src_dir / name).read_bytes()
            dst = self.file_path("current", suite, name)
            old = dst.read_bytes() if dst.exists() else None
            if old == data:
                continue
            dst.parent.mkdir(parents=True, exist_ok=True)
            dst.write_bytes(data)
            changes.append({"file": name, "from": _sha(old) if old is not None else None, "to": _sha(data)})
        if changes:
            _append_jsonl(self.root / "accepted" / f"{suite}.jsonl",
                          {"action": "accept-files", "changes": changes, "reason": reason, "date": today(),
                           "git_rev": rev or git_rev()})
        return [c["file"] for c in changes]

    # -- guard ---------------------------------------------------------------------------------

    def covered(self, suite: str, key: str, current: Expectation | None,
                records: list[dict] | None = None) -> bool:
        """True when a registered loosening of ``key`` allows ``current``."""
        for r in self.loosening(suite) if records is None else records:
            if r.get("key") != key or not str(r.get("reason", "")).strip():
                continue
            if r.get("to") is None or r.get("kind") is None:
                if current is None:
                    return True
                continue
            try:
                limit = Expectation(r["kind"], r["to"], better=r.get("better"))
            except GoldenError:
                continue
            if current is not None and not is_looser(limit, current):
                return True
        return False

    def guard(self, suites: Iterable[str] | None = None) -> list[GuardViolation]:
        out = []
        for suite in suites or self.suites():
            original, current = self.load("original", suite), self.load("current", suite)
            records = self.loosening(suite)
            for key in sorted(original):
                cur = current.get(key)
                if is_looser(original[key], cur) and not self.covered(suite, key, cur, records):
                    out.append(GuardViolation(suite, key, original[key], cur))
        return out

    # -- accept / rebaseline -------------------------------------------------------------------

    def accept(self, suite: str, reason: str, keys: Iterable[str] | None = None, loosen: bool = False,
               drop: Iterable[str] = (), rev: str | None = None) -> AcceptResult:
        """Copy recorded values of ``keys`` (default: all recorded keys) into CURRENT.

        Keys new to the suite are also seeded into ORIGINAL (their first measurement is today's
        number). A change that is looser than ORIGINAL and not already covered by a registered
        loosening is refused unless ``loosen``; with ``loosen`` it is registered. ``drop`` removes
        keys from CURRENT, which always counts as loosening.
        """
        if not reason.strip():
            raise GoldenError("accept needs a non-empty --reason")
        latest = self.latest(suite)
        drop = sorted(set(drop))
        if keys is None:
            keys = [] if drop else sorted(latest)
            if not keys and not drop:
                raise GoldenError(f"no recorded results for suite {suite!r}: run the tests first "
                                  f"(looked in {self.latest_path(suite)})")
        keys = sorted(set(keys))
        missing = [k for k in keys if k not in latest]
        if missing:
            raise GoldenError(f"no recorded result for key(s) {missing} in suite {suite!r}")
        original, current = self.load("original", suite), self.load("current", suite)
        unknown_drop = [k for k in drop if k not in current]
        if unknown_drop:
            raise GoldenError(f"cannot drop key(s) {unknown_drop}: not in current/{suite}.json")
        if set(keys) & set(drop):
            raise GoldenError("a key cannot be both accepted and dropped")
        records = self.loosening(suite)
        changed = [k for k in keys if k not in current or not current[k].same_as(latest[k])]
        looser = [k for k in changed if k in original and is_looser(original[k], latest[k])
                  and not self.covered(suite, k, latest[k], records)]
        looser += [k for k in drop if k in original and not self.covered(suite, k, None, records)]
        if looser and not loosen:
            raise AcceptRefused(f"refusing to accept {suite}: key(s) {looser} would be looser than the original "
                                "baseline; pass --loosen to register the loosening", looser)
        result = AcceptResult(suite, changed=changed, dropped=drop, loosened=sorted(looser))
        if not changed and not drop:
            return result
        rev, date = rev or git_rev(), today()
        old = dict(current)
        for k in changed:
            current[k] = latest[k]
            if k not in original:
                original[k] = latest[k]
                result.seeded_original.append(k)
        for k in drop:
            del current[k]
        self.save("current", suite, dict(sorted(current.items())))
        if result.seeded_original:
            self.save("original", suite, dict(sorted(original.items())))
        for k in result.loosened:
            to = None if k in drop else latest[k]
            _append_jsonl(self.root / "loosening" / f"{suite}.jsonl", {
                "key": k, "from": original[k].value, "to": None if to is None else to.value,
                "kind": None if to is None else to.kind, "better": None if to is None else to.better,
                "reason": reason, "date": date, "git_rev": rev})
        _append_jsonl(self.root / "accepted" / f"{suite}.jsonl", {
            "action": "accept", "reason": reason, "date": date, "git_rev": rev,
            "changes": [{"key": k, "from": old[k].to_json() if k in old else None,
                         "to": None if k in drop else current[k].to_json()} for k in sorted(changed + drop)],
            "seeded_original": result.seeded_original, "loosened": result.loosened})
        return result

    def rebaseline_original(self, suite: str, reason: str, keys: Iterable[str] | None = None,
                            rev: str | None = None) -> list[str]:
        """Replace ORIGINAL expectations with recorded values (only when the shipped data changed)."""
        if not reason.strip():
            raise GoldenError("rebaseline-original needs a non-empty --reason")
        latest = self.latest(suite)
        keys = sorted(set(keys)) if keys else sorted(latest)
        missing = [k for k in keys if k not in latest]
        if missing or not keys:
            raise GoldenError(f"no recorded result for key(s) {missing or '(any)'} in suite {suite!r}")
        original = self.load("original", suite)
        changed = [k for k in keys if k not in original or not original[k].same_as(latest[k])]
        if not changed:
            return []
        old = dict(original)
        for k in changed:
            original[k] = latest[k]
        self.save("original", suite, dict(sorted(original.items())))
        _append_jsonl(self.root / "accepted" / f"{suite}.jsonl", {
            "action": "rebaseline-original", "reason": reason, "date": today(), "git_rev": rev or git_rev(),
            "changes": [{"key": k, "from": old[k].to_json() if k in old else None, "to": original[k].to_json()}
                        for k in changed]})
        return changed

    # -- status --------------------------------------------------------------------------------

    def status(self, suite: str) -> dict:
        original, current, latest = self.load("original", suite), self.load("current", suite), self.latest(suite)
        rows = []
        for key in sorted(set(original) | set(current) | set(latest)):
            cur, lat = current.get(key), latest.get(key)
            if cur is None:
                state = "new" if lat is not None else "dropped"
            elif lat is None:
                state = "not-measured"
            else:
                failure, hint = compare(key, cur, lat)
                state = "FAIL" if failure else "tighten" if hint else "ok"
            guard = "-"
            if key in original:
                loose = is_looser(original[key], cur)
                guard = ("loosened" if self.covered(suite, key, cur) else "VIOLATION") if loose else "ok"
            rows.append({"key": key, "kind": (cur or lat or original.get(key)).kind, "state": state, "guard": guard,
                         "current": None if cur is None else cur.value, "latest": None if lat is None else lat.value,
                         "original": original[key].value if key in original else None})
        return {"suite": suite, "rows": rows, "loosening": len(self.loosening(suite)),
                "accepted": len(self.accepted(suite))}


def _sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()[:16]


_REV: str | None = None


def _cached_rev() -> str:
    global _REV
    if _REV is None:
        _REV = git_rev()
    return _REV


DEFAULT_STORE = GoldenStore()


def check(suite: str, results: Mapping[str, Any], store: GoldenStore | None = None) -> None:
    """Compare measured results of ``suite`` with the repository's CURRENT baseline."""
    (store or DEFAULT_STORE).check(suite, results)


def check_file(suite: str, name: str, data: bytes, store: GoldenStore | None = None) -> None:
    (store or DEFAULT_STORE).check_file(suite, name, data)
