"""Config patches in Unity: request fields, the spawner identity side file, applied_patch.json (design 6.2; unit C9).

* ``job.patch``: the overrides C# applies in memory before ``IsRunning`` (``SimulationConfigPatcher``), normally
  :attr:`tufu_balance.model.patch.PatchReport.expanded` (overrides as written plus the generated explicit sets).
  C# re-reads them from the request text with Python's number parsing, so a value is the double written here.
* ``job.patch_spawner_map``: ``{"path", "sha256"}`` of C1's identity map as JSON (:func:`spawner_map`); spawner
  rows are ``{"scene", "file_id"}`` on both sides.
* ``job.patch_on_error``: ``"fail"`` (default) ends the job with exit 5; ``"record"`` (``empty`` jobs only) writes
  the error into ``applied_patch.json`` and lets the job finish (the shared error cases run in one launch).
* ``<job>/applied_patch.json`` (``tufu.sim.applied_patch/1``): ``ops`` (rows and leaves per override), ``leaves``
  (net, ``model.patch`` leaf form), ``error``, ``ab_roundtrip`` (AB groups after the asset's JSON round trip),
  ``restore`` (written again when Play ends: digest of every patched object before the patch and after the
  restore). manifest key ``patch``: a summary.

:func:`compare` is the leaf-by-leaf check of ``applied_patch.json`` against a :class:`PatchReport` (P12 uses it).
"""

from __future__ import annotations

import hashlib
import json
import re
import sqlite3
import struct
from pathlib import Path
from typing import Any, Iterable, Mapping

from . import request as req, schema
from .spawner_map import load_identity_map

APPLIED_SCHEMA = "tufu.sim.applied_patch/1"
APPLIED_FILE = "applied_patch.json"
MAP_SCHEMA = "tufu.sim.spawner_map/1"
ON_ERROR = ("fail", "record")
INPUTS = Path(__file__).resolve().parents[2] / "out" / "sim" / "inputs"

req.register_fields("job", {"patch": (list, None), "patch_spawner_map": (dict, None), "patch_on_error": (str, None)})
req.register_fields("patch_spawner_map", {"path": (str,), "sha256": (str,)})
schema.register_manifest_keys({"patch": "object"})


def _check_job(job: Mapping[str, Any], where: str) -> None:
    """Mirror of SimulationRunRequest.ValidatePatchFields (the overrides themselves are checked in Play)."""
    patch, smap, on_error = job.get("patch"), job.get("patch_spawner_map"), job.get("patch_on_error")
    if patch is not None and not all(isinstance(o, Mapping) for o in patch):
        raise req.RequestError(f"{where}.patch must be a list of objects")
    if on_error is not None and on_error not in ON_ERROR:
        raise req.RequestError(f"{where}.patch_on_error must be one of {ON_ERROR}")
    if on_error == "record" and job.get("mode", "free") != "empty":
        raise req.RequestError(f"{where}.patch_on_error = record needs mode empty")
    if patch is None and (smap is not None or on_error is not None):
        raise req.RequestError(f"{where}: patch_spawner_map and patch_on_error need a patch")
    if smap is not None:
        req._check_object(smap, "patch_spawner_map", where + ".patch_spawner_map")
        if not re.fullmatch(r"[0-9a-f]{64}", smap.get("sha256") or ""):
            raise req.RequestError(f"{where}.patch_spawner_map needs a path and a lower-case hex sha256")


req.register_job_check(_check_job)


# ---------------------------------------------------------------- spawner identity side file

def spawner_map_text(con: sqlite3.Connection) -> str:
    entries = [{"scene": e.scene, "file_id": e.file_id, "path": e.path, "key": e.key}
               for e in load_identity_map(con).entries]
    return json.dumps({"schema": MAP_SCHEMA, "spawners": entries}, sort_keys=True, separators=(",", ":"),
                      ensure_ascii=False) + "\n"


def spawner_map(db: str | Path | sqlite3.Connection, out_dir: str | Path | None = None) -> dict:
    """Write C1's identity map (every scene spawner row) as a content-addressed JSON file and return the
    ``patch_spawner_map`` field for it."""
    con = db if isinstance(db, sqlite3.Connection) else sqlite3.connect(f"file:{db}?mode=ro", uri=True)
    try:
        text = spawner_map_text(con)
    finally:
        if con is not db:
            con.close()
    sha = hashlib.sha256(text.encode("utf-8")).hexdigest()
    path = Path(out_dir or INPUTS) / f"spawner_map.{sha[:16]}.json"
    if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest() != sha:
        path.parent.mkdir(parents=True, exist_ok=True)
        tmp = path.with_suffix(".tmp")
        tmp.write_text(text, encoding="utf-8")
        tmp.replace(path)
    return {"path": str(path.resolve()), "sha256": sha}


# ---------------------------------------------------------------- jobs

def with_patch(job: dict, overrides: Iterable[Mapping[str, Any]], *, spawner_map: Mapping[str, str] | None = None,
               on_error: str | None = None) -> dict:
    """``job`` plus the patch fields (validated like the rest of the request)."""
    out = dict(job)
    out["patch"] = [dict(o) for o in overrides]
    if spawner_map is not None:
        out["patch_spawner_map"] = dict(spawner_map)
    if on_error is not None:
        out["patch_on_error"] = on_error
    _check_job(out, out.get("job_id", "job"))
    return out


def patch_job(job_id: str, overrides: Iterable[Mapping[str, Any]], *, spawner_map: Mapping[str, str] | None = None,
              on_error: str | None = None, frames: int = 5, **kw) -> dict:
    """An ``empty`` job (harness lifecycle only) that applies ``overrides`` and restores them when Play ends."""
    return with_patch(req.empty_job(job_id, frames=frames, **kw), overrides, spawner_map=spawner_map, on_error=on_error)


# ---------------------------------------------------------------- applied_patch.json

def load_applied(job_dir: str | Path) -> dict:
    data = json.loads((Path(job_dir) / APPLIED_FILE).read_text(encoding="utf-8"))
    if data.get("schema") != APPLIED_SCHEMA:
        raise schema.SchemaError(f"{APPLIED_FILE}: schema must be {APPLIED_SCHEMA}")
    return data


def leaf_address(node: str, row: Mapping[str, Any], index: int | None) -> tuple:
    return (node, json.dumps(dict(row), sort_keys=True), -1 if index is None else index)


def same_value(a: Any, b: Any) -> bool:
    """Identical leaf values: same JSON type, floats bit for bit (NaN equals NaN, -0.0 is not 0.0)."""
    if isinstance(a, float) or isinstance(b, float):
        return isinstance(a, float) and isinstance(b, float) and struct.pack("<d", a) == struct.pack("<d", b)
    return type(a) is type(b) and a == b


def compare(report, applied: Mapping[str, Any], passthrough: Iterable[str] = ()) -> dict:
    """Leaf-by-leaf comparison of a :class:`~tufu_balance.model.patch.PatchReport` (its C# leaves) with
    ``applied_patch.json``. Leaves of ``passthrough`` nodes (Python has none) are listed apart."""
    skip = set(passthrough) | {o.node for o in getattr(report, "passthrough", ())}
    want = {leaf_address(x.node, x.row, x.index): x for x in report.csharp_leaves()}
    got, apart = {}, []
    for x in applied.get("leaves", []):
        if x["node"] in skip:
            apart.append(x)
        else:
            got[leaf_address(x["node"], x["row"], x["index"])] = x
    missing = sorted(set(want) - set(got))
    extra = sorted(set(got) - set(want))
    different = []
    for key in sorted(set(want) & set(got)):
        w, g = want[key], got[key]
        if not (same_value(w.old, g["old"]) and same_value(w.new, g["new"])):
            different.append({"leaf": key, "python": [w.old, w.new], "csharp": [g["old"], g["new"]]})
    return {"python": len(want), "csharp": len(got), "missing": missing, "extra": extra, "different": different,
            "passthrough_leaves": apart, "identical": not (missing or extra or different)}


def describe_mismatch(result: Mapping[str, Any], limit: int = 10) -> str:
    def fmt(x):
        return json.dumps(x, default=repr)[:300]
    parts = [f"python {result['python']} leaves, csharp {result['csharp']}"]
    for name in ("missing", "extra", "different"):
        if result[name]:
            parts.append(f"{name} ({len(result[name])}): " + "; ".join(fmt(x) for x in result[name][:limit]))
    return "\n".join(parts)
