"""Designer patch files and validation of the diff-only options."""

from __future__ import annotations

from pathlib import Path
import re

from ..model.patch import Patch, PatchError, load_patch, parse_patch


def validate(patch: Patch) -> Patch:
    if not re.fullmatch(r"[\w.-]+", patch.name) or patch.name in (".", ".."):
        raise PatchError("bad_patch", "name must be a single safe directory name")
    sim, report = patch.simulate or {}, patch.report or {}
    if not isinstance(sim, dict) or not isinstance(report, dict):
        raise PatchError("bad_patch", "simulate and report must be mappings")
    if set(sim) - {"enabled", "params", "stop_day", "bake_iterations"}:
        raise PatchError("bad_patch", "unknown simulate option")
    if set(report) - {"key_days", "top_n"}:
        raise PatchError("bad_patch", "unknown report option")
    if not isinstance(sim.get("enabled", True), bool):
        raise PatchError("bad_patch", "simulate.enabled must be true/false")
    for key, default in (("stop_day", 405), ("bake_iterations", 1)):
        x = sim.get(key, default)
        if type(x) is not int or x < 1:
            raise PatchError("bad_patch", f"simulate.{key} must be a positive integer")
    if not isinstance(sim.get("params", "defaults"), (str, dict)):
        raise PatchError("bad_patch", "simulate.params must be a layer, JSON path or mapping")
    days = report.get("key_days", [5, 10, 20, 46, 104, 206, 405])
    if not isinstance(days, list) or any(type(d) is not int or d < 0 for d in days):
        raise PatchError("bad_patch", "report.key_days must be nonnegative integer days")
    if type(report.get("top_n", 10)) is not int or report.get("top_n", 10) < 1:
        raise PatchError("bad_patch", "report.top_n must be a positive integer")
    return patch


def load(path: str | Path) -> Patch:
    return validate(load_patch(path))


def empty() -> Patch:
    return validate(parse_patch({"name": "empty"}))
