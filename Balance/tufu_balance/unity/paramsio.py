"""Run parameters on the Unity side (design 4.8; unit C3c): request fields, the parameter asset, ``params
build|import``.

* Request: ``job.params`` is the complete object from :func:`tufu_balance.calibrate.params.resolve` and
  ``job.params_hash`` its ``params_hash``; C# checks the hash against its own canonical form of ``job.params``
  (``SimulationCanonicalJson``) and reports the hash of the effective values (manifest ``params.hash``), which
  equals ``params_hash`` when every float is a float32 value (:func:`effective_hash`). ``job.params_source =
  "asset"`` makes a batch job read the parameter asset through the AssetDatabase, as interactive runs do.
* Manifest ``params`` (``tufu.sim.params/1``): ``source`` (request, code_defaults, asset), ``interactive``,
  ``complete``/``missing`` (fields a partial request left at the code defaults), ``request_hash``,
  ``request_hash_given``, ``hash``, ``matches_request``, ``roll_mode``, ``seed``, ``asset_path``,
  ``asset_guid``, ``asset_found``, ``error``. A failed job carries it too when the loader ran.
* Asset: ``Recovery/StudyProject/Assets/Simulation/Simulation Rebuild Params.asset`` (:func:`read_asset`,
  :func:`asset_text`). Only :func:`import_params` changes it: a fresh batch editor runs
  ``SimulationParamsImporter.Import`` in the run worktree, the launcher accepts exactly that one file changing,
  checks the result, copies it to the caller's worktree (or ``dest``) and restores the run worktree.
"""

from __future__ import annotations

import dataclasses
import json
import re
import shutil
import subprocess
import time
from pathlib import Path
from typing import Any, Mapping, Sequence

import numpy as np
import yaml

from ..calibrate import params as cparams
from . import launcher, request as req, runner
from . import trace as _trace  # noqa: F401  registers job.params_source and manifest "params"

ASSET_REL = "Assets/Simulation/Simulation Rebuild Params.asset"
IMPORTER = "Codebase.Simulation.EditorTools.SimulationParamsImporter.Import"
INPUT_SCHEMA = "tufu.sim.params/1"
IMPORT_REPORT_SCHEMA = "tufu.sim.params-import/1"
SOURCES = ("request", "asset")
ASSET_NAME = "Simulation Rebuild Params"


class ParamsIoError(ValueError):
    pass


# ---------------------------------------------------------------- values as C# holds them

def _csharp(cs: cparams.CsParams | None) -> cparams.CsParams:
    cs = cs or cparams.read_csharp_params()
    if cs is None:
        raise ParamsIoError("SimulationRebuildParams is not declared in this worktree")
    return cs


def float32_text(value: float) -> str:
    """Shortest decimal of float32(value) (numpy's Dragon4), e.g. ``0.1``, ``120.0``, ``1e-05``."""
    return str(np.float32(value))


def effective(params: Mapping[str, Any], cs: cparams.CsParams | None = None) -> dict[str, Any]:
    """The object as C# holds it: every float field becomes the double of its float32's shortest text
    (``SimulationRebuildParams.EffectiveCanonicalJson``)."""
    cs = _csharp(cs)

    def conv(name, value):
        base = cs.fields.get(name, "").rstrip("[]")
        if isinstance(value, list):
            return [conv(name, v) for v in value]
        if base == "float" and isinstance(value, (int, float)) and not isinstance(value, bool):
            return float(float32_text(value))
        return value

    return {k: conv(k, params[k]) for k in sorted(params)}


def effective_hash(params: Mapping[str, Any], cs: cparams.CsParams | None = None) -> str:
    return cparams.params_hash(effective(params, cs))


def not_float32(params: Mapping[str, Any], cs: cparams.CsParams | None = None) -> list[str]:
    """Fields holding a float that C# would change on load (not the shortest text of a float32 value)."""
    held = effective(params, cs)
    return [k for k in sorted(params) if json.dumps(held[k]) != json.dumps(params[k])]


def round_float32(resolved: cparams.Resolved, cs: cparams.CsParams | None = None) -> cparams.Resolved:
    """``resolved`` with every float field rounded to its float32 value, and its ``params_hash`` recomputed
    (``params build|import --float32``; for fitted doubles, design 4.8)."""
    params = effective(resolved.params, cs)
    return dataclasses.replace(resolved, params=params, params_hash=cparams.params_hash(params))


def job(job_id: str, *, params: Mapping[str, Any] | None = None, source: str | None = None,
        cs: cparams.CsParams | None = None, **kw) -> dict:
    """``request.make_job`` plus ``params_source`` (``"asset"`` takes no params). Params are checked against the
    C# class as C# checks them (exact field names, JSON types; partial objects allowed), so a mis-cased or mistyped
    field fails here instead of in Unity with exit 5. ``request.validate`` stays schema-free for params."""
    if source is not None and source not in SOURCES:
        raise req.RequestError(f"params_source must be one of {SOURCES}")
    if source == "asset" and params is not None:
        raise req.RequestError('params_source "asset" takes no params')
    if params is not None:
        problems = cparams.check_against_csharp(params, _csharp(cs), complete=False)
        if problems:
            raise req.RequestError("params: " + "; ".join(problems))
    j = req.make_job(job_id, params=params, **kw)
    if source is not None:
        j["params_source"] = source
    return j


# ---------------------------------------------------------------- the asset

def asset_path(worktree: str | Path | None = None) -> Path:
    return runner.project_dir(Path(worktree or runner.WORKTREE)) / ASSET_REL


def asset_script_guid(script: str | Path | None = None) -> str:
    script = script or cparams.SIMULATION_SCRIPTS / "Rebuild" / "SimulationRebuildParamsAsset.cs"
    text = Path(str(script) + ".meta").read_text(encoding="utf-8")
    m = re.search(r"^guid: ([0-9a-f]{32})$", text, re.M)
    if not m:
        raise ParamsIoError(f"{script}.meta has no guid")
    return m.group(1)


def _yaml_scalar(value: Any, base: str, cs: cparams.CsParams) -> str:
    if value is None:
        return ""
    if base == "bool":
        return "1" if value else "0"
    if base == "float":
        text = float32_text(value)
        return text[:-2] if text.endswith(".0") else text
    if base == "string":
        return "'" + str(value).replace("'", "''") + "'" if value != "" else "''"
    return str(int(value)) if base in ("int", "long") or base in cs.enums else repr(float(value))


def asset_text(params: Mapping[str, Any], cs: cparams.CsParams | None = None, script_guid: str | None = None) -> str:
    """The parameter asset as Unity 6000.6 writes the ScriptableObject (fields in declaration order); the
    importer's output for the same values is this text byte for byte (tests/unity/test_params.py)."""
    cs = _csharp(cs)
    guid = script_guid or asset_script_guid()
    lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
             "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
             "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
             f"  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}", f"  m_Name: {ASSET_NAME}",
             "  m_EditorClassIdentifier: ", "  Params:"]
    unknown = sorted(set(params) - set(cs.fields))
    if unknown:
        raise ParamsIoError(f"no such fields in SimulationRebuildParams: {unknown}")
    for name, ctype in cs.fields.items():
        value, base = params[name], ctype.rstrip("[]")
        if ctype.endswith("[]"):
            if not value:
                lines.append(f"    {name}: []")
            else:
                lines.append(f"    {name}:")
                lines += [f"    - {_yaml_scalar(v, base, cs)}" for v in value]
        else:
            scalar = _yaml_scalar(value, base, cs)
            lines.append(f"    {name}: {scalar}" if scalar else f"    {name}:")
    return "\n".join(lines) + "\n"


def _typed(raw: Any, ctype: str, cs: cparams.CsParams, name: str) -> Any:
    base = ctype.rstrip("[]")
    if ctype.endswith("[]"):
        if raw in (None, ""):
            return []
        if not isinstance(raw, list):
            raise ParamsIoError(f"asset Params.{name}: expected a list")
        return [_typed(v, base, cs, name) for v in raw]
    if base == "string":
        return raw if raw is not None else ""
    if not isinstance(raw, str):
        raise ParamsIoError(f"asset Params.{name}: expected a scalar, got {raw!r}")
    if base == "bool":  # Unity writes 0 or 1
        if raw.strip() not in ("0", "1"):
            raise ParamsIoError(f"asset Params.{name}: expected 0 or 1, got {raw!r}")
        return raw.strip() == "1"
    if base in ("float", "double"):
        return float(raw)
    return int(raw)


def read_asset(path: str | Path | None = None, cs: cparams.CsParams | None = None) -> dict[str, Any]:
    """The asset's ``Params`` as JSON values (enums as integers, float fields as Python floats), keys sorted."""
    cs = _csharp(cs)
    text = Path(path or asset_path()).read_text(encoding="utf-8")
    body = "\n".join("---" if line.startswith("--- ") else line for line in text.splitlines() if not line.startswith("%"))
    doc = yaml.load(body, Loader=yaml.BaseLoader)  # every scalar as text: the field type decides
    try:
        raw = doc["MonoBehaviour"]["Params"]
    except (KeyError, TypeError):
        raise ParamsIoError(f"{path}: no MonoBehaviour.Params") from None
    extra = sorted(set(raw) - set(cs.fields))
    missing = sorted(set(cs.fields) - set(raw))
    if extra or missing:
        raise ParamsIoError(f"{path}: Params fields differ from the C# class (extra {extra}, missing {missing})")
    return {name: _typed(raw[name], cs.fields[name], cs, name) for name in sorted(cs.fields)}


# ---------------------------------------------------------------- params build

def resolve_layers(layers: Sequence[str | Path | Mapping[str, Any]] = (), overrides: Mapping[str, Any] | None = None
                   ) -> cparams.Resolved:
    """``calibrate.params.resolve`` with the defaults put first when the caller left them out."""
    layers = list(layers)
    if not layers or layers[0] != "defaults":
        layers.insert(0, "defaults")
    return cparams.resolve(layers, overrides)


def input_document(resolved: cparams.Resolved) -> dict:
    return {"schema": INPUT_SCHEMA, "params": resolved.params, "params_hash": resolved.params_hash,
            "layers": list(resolved.layers)}


# ---------------------------------------------------------------- params import

@dataclasses.dataclass
class ImportResult:
    exit_code: int
    out_dir: Path
    report: dict
    dest: Path | None


def _status_entries(tree: Path) -> list[tuple[str, str]]:
    raw = runner.git(tree, "status", "--porcelain", "-z", "-uall")
    return [(e[:2], e[3:]) for e in raw.split("\0") if e]


def import_params(layers: Sequence[str | Path | Mapping[str, Any]] = (), overrides: Mapping[str, Any] | None = None, *,
                  rev: str | None = None, dest: str | Path | None = None, opts: launcher.Options | None = None,
                  worktree: Path | None = None, run_tree: Path | None = None, paths: launcher.ProtectedPaths | None = None,
                  out_root: Path | None = None, timeout_s: float = 900.0, cs: cparams.CsParams | None = None,
                  float32: bool = False, log=print) -> ImportResult:
    """Write the resolved parameters into the parameter asset through ``SimulationParamsImporter`` in a fresh
    batch editor on the run worktree at ``rev`` (default: this worktree's HEAD), then copy the asset to
    ``dest`` (default: this worktree's asset) and restore the run worktree. Exit codes as the launcher's; the
    only project file allowed to change is the asset. The asset holds float32 values: a float field that is
    not one is an error before Unity starts (the importer would refuse it), unless ``float32`` rounds it."""
    opts = opts or launcher.Options()
    worktree = Path(worktree or runner.WORKTREE)
    run_tree = Path(run_tree or runner.run_worktree())
    cs = _csharp(cs)
    resolved = resolve_layers(layers, overrides)
    if float32:
        resolved = round_float32(resolved, cs)
    odd = not_float32(resolved.params, cs)
    if odd:
        raise ParamsIoError(f"float fields not exactly float32 values: {', '.join(odd)} (the asset stores float32; "
                            "round them in the layer or pass --float32)")
    project = runner.project_dir(run_tree)
    launcher._refuse_main(project, opts)
    if not (project / "Library").is_dir():
        raise launcher.LauncherError(f"{project} has no Library; params import runs only in the run worktree")
    dest = Path(dest) if dest else asset_path(worktree)
    if dest.resolve().is_relative_to(Path(run_tree).resolve()):
        raise launcher.LauncherError(f"dest {dest} is inside the run worktree, which is restored after the import")
    lock = launcher.UnityLock(run_tree)
    lock.acquire("params-import", opts.lock_wait_s)
    try:
        others = launcher.unity_processes(project)
        if others:
            raise launcher.LauncherError(f"Unity already has {project} open: {others}")
        runner.require_clean(run_tree)
        code_rev = runner.checkout(run_tree, rev or runner.head(worktree))
        dll_status = launcher.check_dlls(project)
        out_dir = Path(out_root or launcher.OUT_ROOT) / "params" / f"{resolved.params_hash[:16]}-{code_rev[:12]}"
        if out_dir.exists():
            shutil.rmtree(out_dir)
        out_dir.mkdir(parents=True)
        doc = input_document(resolved)
        (out_dir / "params.json").write_text(json.dumps(doc, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
        return _import(resolved, out_dir, project=project, run_tree=run_tree, code_rev=code_rev, worktree=worktree,
                       dest=dest, opts=opts, paths=paths, timeout_s=timeout_s,
                       cs=cs, material={"dll_status": dll_status}, log=log)
    finally:
        lock.release()


def _import(resolved, out_dir: Path, *, project: Path, run_tree: Path, code_rev: str, worktree: Path, dest: Path,
            opts: launcher.Options, paths, timeout_s: float, cs, material: dict, log) -> ImportResult:
    paths = paths or launcher.ProtectedPaths.for_project(project)
    asset = project / ASSET_REL
    asset_git = (runner.PROJECT_SUBDIR / ASSET_REL).as_posix()
    report: dict[str, Any] = {"schema": "tufu.sim.params-launcher/1", "code_rev": code_rev, "params_hash": resolved.params_hash,
                              "layers": list(resolved.layers), "started": launcher.now_iso(), **material,
                              "asset_sha256_before": runner.sha256_file(asset)}
    main_before = launcher.main_checkout_editors()
    before = launcher.snapshot(paths, project)
    backup = out_dir / "_real_backup"
    launcher.backup_real(paths, backup)
    log_file, import_report = out_dir / "unity.log", out_dir / "import_report.json"
    cmd = [str(opts.unity), "-batchmode", "-projectPath", str(project), *launcher.FIXED_ARGS, "-logFile", str(log_file),
           "-executeMethod", IMPORTER, "-tufuSimParams", str(out_dir / "params.json"),
           "-tufuSimParamsReport", str(import_report)]
    if opts.nographics:
        cmd.append("-nographics")
    report["command"] = cmd
    log(f"unity params import: {resolved.params_hash[:12]}… at {code_rev[:10]}")
    t0 = time.monotonic()
    proc = subprocess.Popen(cmd, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                            start_new_session=True)
    others_seen: list = []
    reason = None
    while proc.poll() is None:
        for pid, cmdline in launcher.unity_processes(project):
            if pid != proc.pid and "AssetImportWorker" not in cmdline and pid not in [p for p, _ in others_seen]:
                others_seen.append((pid, cmdline))
        if time.monotonic() - t0 > timeout_s:
            reason = f"timeout {timeout_s:g} s"
            launcher._kill(proc)
            break
        time.sleep(0.5)
    unity_exit = launcher.EXIT_TIMEOUT if reason else proc.returncode
    report.update(unity_exit=unity_exit, reason=reason, process_wall_s=time.monotonic() - t0,
                  workers_left=launcher.wait_for_project_free(project, opts.worker_exit_wait_s))

    after = launcher.snapshot(paths, project)
    problems = launcher.diff_snapshots(before, after)
    allowed = f"project file changed on disk: {ASSET_REL}"
    asset_changed = allowed in problems
    problems = [p for p in problems if p != allowed]
    analytics = [p for p in problems if launcher._is_editor_analytics(p)]
    problems = [p for p in problems if not launcher._is_editor_analytics(p)]
    report["editor_analytics_leftovers"] = analytics
    unexpected = [f"{code} {path}" for code, path in _status_entries(run_tree) if path != asset_git or code != " M"]
    if unexpected:
        problems.append("run worktree changed beyond the parameter asset: " + " | ".join(unexpected[:10]))
    try:
        imported = json.loads(import_report.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        imported = None
    report["importer"] = imported
    report["asset_sha256_after"] = runner.sha256_file(asset)
    exit_code = unity_exit if unity_exit in launcher.EXIT_NAMES else launcher.EXIT_EXCEPTION
    written = None
    if exit_code == launcher.EXIT_OK:
        checks = _verify_import(resolved, asset, imported, cs)
        report["checks"] = checks
        failed = [k for k, ok in checks.items() if not ok]
        if failed:
            problems.append("import checks failed: " + ", ".join(failed))
            exit_code = launcher.EXIT_EXCEPTION
        else:
            written = asset.read_bytes()
    elif asset_changed:
        problems.append("the parameter asset changed although the import failed")
    if any(path == asset_git for _, path in _status_entries(run_tree)):
        runner.git(run_tree, "checkout", "--quiet", "HEAD", "--", asset_git)  # index too, should it be staged
    report["restored_run_worktree_asset"] = runner.sha256_file(asset) == report["asset_sha256_before"]

    # Real save/PlayerPrefs: the importer never enters Play (its report says so), so a change made while the
    # user's editor on the main checkout ran for the whole import is that editor's (Runbook 13, 17).
    real_problems = [p for p in problems if p.startswith("real ")]
    main_editors = sorted(main_before & launcher.main_checkout_editors())
    report["main_checkout_editors"] = main_editors
    foreign = others_seen or launcher.unity_processes(runner.MAIN_CHECKOUT / runner.PROJECT_SUBDIR)
    restored = []
    if real_problems:
        if (main_editors and unity_exit == 0 and not others_seen and len(real_problems) == len(problems)
                and imported and imported.get("entered_play") is False):
            report["real_changed_by_main_editor"] = real_problems
            problems = []
        elif not foreign:
            restored = launcher.restore_real(paths, backup, save=any("save file" in p for p in real_problems),
                                             prefs=any("PlayerPrefs" in p for p in real_problems))
    if analytics and not foreign:
        restored += launcher.restore_real(paths, backup, save=True, prefs=False)
    report["restored"] = restored
    if any(not p.startswith("import checks") for p in problems):
        exit_code = launcher.EXIT_GUARD
    if not (real_problems or analytics) or restored:
        shutil.rmtree(backup, ignore_errors=True)
    copied = None
    if exit_code == launcher.EXIT_OK and written is not None:
        Path(dest).parent.mkdir(parents=True, exist_ok=True)
        Path(dest).write_bytes(written)
        copied = Path(dest)
    report.update(guard_problems=problems, others_seen=others_seen, exit_code=exit_code, dest=str(copied) if copied else None,
                  finished=launcher.now_iso())
    (out_dir / "import.json").write_text(json.dumps(report, indent=1, ensure_ascii=False, default=str) + "\n", encoding="utf-8")
    log(f"unity params import: exit {exit_code} ({launcher.EXIT_NAMES.get(exit_code, '?')})"
        + (f", {'; '.join(problems[:3])}" if problems else "") + (f" -> {copied}" if copied else ""))
    return ImportResult(exit_code, out_dir, report, copied)


def _verify_import(resolved, asset: Path, imported: Mapping | None, cs) -> dict[str, bool]:
    """The importer's report and an independent read of the written asset agree with the resolved object."""
    checks = {"importer_report": bool(imported) and imported.get("schema") == IMPORT_REPORT_SCHEMA
              and imported.get("ok") is True and imported.get("entered_play") is False}
    imported = imported or {}
    for key in ("params_hash", "request_hash", "effective_hash", "read_back_hash"):
        checks[f"importer_{key}"] = imported.get(key) == resolved.params_hash
    # The run loader (SimulationRebuildParamsLoader.Load) on the written asset: a params_source "asset" job and
    # the interactive branch.
    for key in ("read_back_job", "read_back_interactive"):
        loader = imported.get(key) or {}
        checks[f"importer_{key}"] = (loader.get("source") == "asset" and loader.get("asset_found") is True
                                     and loader.get("error") is None and loader.get("hash") == resolved.params_hash
                                     and loader.get("result_hash") == resolved.params_hash)
    try:
        values = read_asset(asset, cs)
        checks["asset_values"] = values == effective(resolved.params, cs)
        checks["asset_hash"] = effective_hash(values, cs) == resolved.params_hash
    except (OSError, ParamsIoError, yaml.YAMLError, ValueError):
        checks["asset_values"] = checks["asset_hash"] = False
    checks["asset_file_sha256"] = imported.get("file_sha256") == runner.sha256_file(asset)
    return checks
