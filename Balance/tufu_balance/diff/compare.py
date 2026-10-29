"""Static/Unity comparison orchestration and read-only asset-version expansion."""

from __future__ import annotations

from dataclasses import dataclass, field
import hashlib
import json
import math
import os
from pathlib import Path
import re
import subprocess
import tempfile

from ..model import config as C, patch as P, pipeline
from . import impact, patchfile, tables

BALANCE = Path(__file__).resolve().parents[2]
REPO = BALANCE.parent
ASSET_PREFIX = "Recovery/StudyProject/Assets/"


@dataclass
class Comparison:
    patch: P.Patch
    static: pipeline.PipelineResult
    formulas: list[dict]
    warnings: list[dict]
    comparisons: dict[str, list[dict]]
    manifest: dict = field(default_factory=dict)
    consistency: dict | None = None
    simulation: dict = field(default_factory=dict)
    bake_base: list[dict] = field(default_factory=list)
    bake_head: list[dict] = field(default_factory=list)

    @property
    def changed(self):
        return any(self.comparisons.values()) or bool(self.static.report.leaves or self.static.report.passthrough)


def sha(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def git(*args, worktree=REPO):
    return subprocess.check_output(["git", "--no-pager", "-C", str(worktree), *args], text=True)


def run(cfg, patch, *, skip_sim=False, driver=None, rev=None, out_root=None):
    patchfile.validate(patch)
    if not skip_sim and (patch.simulate or {}).get("enabled", True):
        source, env_db = C.source_path(cfg), os.environ.get("TUFU_BALANCE_DB")
        if source and env_db and Path(source).resolve() != Path(env_db).resolve():
            raise ValueError("--db/config source and TUFU_BALANCE_DB differ; use the same database")
    result = pipeline.run(cfg, patch)
    nodes = [o.node for o in patch.overrides] + [leaf.node for leaf in result.report.leaves]
    formulas, extra = impact.affected(cfg, nodes)
    comparisons = {f"static/{name}.csv": [tables.change(c["key"], c["field"], c["base"], c["patched"])
                                            for c in result.diff.get(name, [])]
                   for name in result.base.keys}
    comparisons["bake/spawners.csv"] = tables.compare_rows(result.base.tables["bake"],
                                                           result.patched.tables["bake"], ("spawner",))
    for name in ("timechunk_stats", "island_arrival", "items", "daily", "rpm", "first_kill", "combat", "evolution", "item_first"):
        comparisons[f"sim/{name}.csv"] = []
    canonical = {"overrides": [o.to_json() for o in patch.overrides], "regenerate": dict(patch.regenerate),
                 "simulate": patch.simulate, "report": patch.report}
    comp = Comparison(patch, result, formulas, result.warnings + extra, comparisons,
                      {"schema": "tufu.diff/1", "patch_sha256": hashlib.sha256(
                          json.dumps(canonical, sort_keys=True, ensure_ascii=False).encode()).hexdigest(),
                       "code_rev": git("rev-parse", rev or "HEAD").strip(),
                       "db_sha256": sha(C.source_path(cfg)) if C.source_path(cfg) else None,
                       "skip_sim": skip_sim or not (patch.simulate or {}).get("enabled", True),
                       "run_keys": [], "params_hash": None,
                       "timings": "timings.json (wall-clock measurements excluded from deterministic report)"},
                      bake_base=result.base.tables["bake"], bake_head=result.patched.tables["bake"])
    if patch.regen("optimum_time_from_relative") and any(o.node == pipeline.ITD for o in patch.overrides):
        exceptions = [r["spawner"] for r in result.base.tables["optimum_time"] if r["exception"]]
        comp.warnings.append({"code": "ot_exceptions_preserved", "message": "OT 例外保留原值；仅窗口发生变化的普通点重算",
                              "spawners": exceptions})
    calibrated_rules = sorted({r["id"] for r in formulas if "Calibrated" in r["provenance"] or "fit" in r["provenance"]})
    if calibrated_rules:
        comp.warnings.append({"code": "calibrated_inputs", "message": "改动触及拟合或校准规则的输入", "formulas": calibrated_rules})
    if any(o.node == "SimulationConfig.TimeChunkDurationMinutes" for o in patch.overrides):
        comp.warnings.append({"code": "no_simulation_effect", "message": "TimeChunkDurationMinutes 不影响模拟（Q15）；依赖图仍列出其它读取方"})
    cached_nodes = sorted({o.node for o in patch.overrides if o.node in (
        "EnemySpawnerConfig.ItemReward", "EnemySpawnerConfig.Cooldown", "EnemySpawnerConfig.ChargesBeforeBigRespawn")
        or o.node.startswith("ABTestDataConfig.spawnerCooldowns")})
    if cached_nodes:
        comp.warnings.append({"code": "initialization_cache", "message": "C9 在场景初始化后应用补丁；ItemReward/HasItemReward、ChargesTotal/BaseCooldown 的初始化缓存可能仍是旧值，不能假定模拟已响应", "nodes": cached_nodes})
    if (patch.simulate or {}).get("stop_day", 405) > 405:
        comp.warnings.append({"code": "extrapolation", "message": "超过第 405 天，属于外推"})
    if not comp.manifest["skip_sim"]:
        _simulate(comp, cfg, driver=driver, rev=rev, out_root=out_root)
    return comp


def _simulate(comp, cfg, *, driver, rev, out_root):
    from ..calibrate import driver as D, params, runcache
    from ..unity import bake_report, patcher, probes, request as req
    from ..crosscheck import run_checks

    out_root = Path(out_root or BALANCE / "out" / "sim")
    options = comp.patch.simulate or {}
    layer = options.get("params", "defaults")
    calibrated = layer == "calibrated" and (params.PARAMS_ROOT / "calibrated.json").exists()
    if layer == "calibrated" and not calibrated:
        layer = "defaults"
        comp.warnings.append({"code": "uncalibrated", "message": "calibrated 参数层尚不存在，使用 defaults；模拟为 uncalibrated"})
    resolved = params.resolve(("defaults",) if layer == "defaults" else ("defaults", layer))
    resolved = params.resolve(("defaults",), overrides={**resolved.params,
                              "AutoBalanceMode": 1 if comp.patch.regen("sim_autobalance") == "regenerate" else 0})
    comp.manifest.update(params_hash=resolved.params_hash, simulation_label="calibrated" if calibrated else "uncalibrated",
                         bake_iterations=options.get("bake_iterations", 1),
                         kill_loss_multiplier=resolved.params["KillLossMultiplier"])
    if resolved.params.get("KillLossFromAb"):
        comp.warnings.append({"code": "policy_extrapolation", "message": "KillLossFromAb 已开启：策略外推"})
    # Resolve readiness from this config's DB, including when --db is used without an environment variable.
    from ..unity import launcher
    template = getattr(driver, "template", None) or req.make_request([])
    sandbox = dict(template["sandbox"])
    ready_name, _ = launcher.expected_ready_table(sandbox.get("ab_variant", "A"), db=C.source_path(cfg))
    sandbox["expected_ready_spawners"] = ready_name
    template = {**template, "sandbox": sandbox}
    if driver is None:
        driver = D.Driver(D.BatchBackend(rev=rev, out_root=out_root, readiness_db=C.source_path(cfg)), cache=runcache.RunCache(out_root), template=template)
    else:
        driver.template = template
    smap = patcher.spawner_map(cfg.source, out_root / "inputs")
    configs = [cfg, comp.static.config]
    overrides = [[], comp.static.report.expanded]
    job_results = []
    for iteration in range(options.get("bake_iterations", 1)):
        jobs = []
        for i, label in enumerate(("baseline", "patched")):
            job = req.make_job(f"diff-{label}", day=options.get("stop_day", 405), params=resolved.params,
                               outputs={"daily": True, "events": True, "digests": True, "spawner_days": True})
            jobs.append(bake_report.bake_outputs(patcher.with_patch(job, overrides[i], spawner_map=smap)))
        results = driver.run(jobs)
        job_results += [{"iteration": iteration + 1, "side": side, "key": r.job_key, "status": r.status,
                         "attempts": r.attempts} for side, r in zip(("baseline", "patched"), results)]
        if any(not r.ok for r in results):
            raise ValueError("Unity diff failed: " + "; ".join(str(r.error or r.exit_code) for r in results if not r.ok))
        for i, res in enumerate(results):
            expected = comp.static.report if i == 1 and iteration == 0 else P.apply(
                cfg, P.parse_patch({"overrides": overrides[i]}))[1]
            applied = patcher.compare(expected, patcher.load_applied(res.job_dir))
            if not applied["identical"]:
                raise ValueError("C# applied_patch differs from Python: " + patcher.describe_mismatch(applied))
            if i == 1 and iteration == 0:
                comp.consistency = applied
        baked = []
        for i, res in enumerate(results):
            con = configs[i].connection()
            checks = run_checks(res.job_dir, con, checks=("X1", "X2", "X3", "X4", "X5", "X6", "X7"),
                                out_dir=out_root / "diff_checks" / res.job_key)
            bad = [k for k, c in checks.items() if c.status == "fail"]
            if bad:
                raise ValueError(f"crosscheck failed ({res.job_key}): {bad}")
            comp.simulation.setdefault("crosschecks", []).append({"side": i, "iteration": iteration + 1,
                "checks": {k: {"status": c.status, "items": c.items, "mismatches": c.mismatches,
                               "vacuous": c.vacuous} for k, c in checks.items()}})
            report = bake_report.read_report(res.job_dir)
            baked.append(_bake_rows(report, con))
        comp.bake_base, comp.bake_head = baked
        comp.comparisons["bake/spawners.csv"] = tables.compare_rows(*baked, ("spawner",))
        if iteration + 1 < options.get("bake_iterations", 1):
            for i in range(2):
                sets = _rebake_overrides(configs[i], baked[i])
                configs[i], expanded = P.apply(configs[i], P.parse_patch({"overrides": sets}))
                overrides[i] = overrides[i] + expanded.expanded
    comp.manifest["run_keys"] = [r.job_key for r in results]
    # Cache status and wall time are operational evidence, not deterministic model output.
    comp.simulation["jobs"] = job_results
    _sim_tables(comp, *[r.job_dir for r in results])
    if not calibrated:
        comp.warnings.append({"code": "uncalibrated", "message": "模拟为 uncalibrated：当前重建策略尚未完成 K1–K3 校准"})


def _events(directory):
    return [json.loads(line) for line in (directory / "events.jsonl").read_text().splitlines() if line.strip()]


def _bake_rows(report, con):
    from ..unity import probes, spawner_map
    identity = spawner_map.load_identity_map(con)
    out = []
    for r in report["spawners"]:
        entry = identity.by_runtime_id.get(r["id"])
        if entry is None:
            raise ValueError(f"unknown bake spawner: {r['id']}")
        v = r.get("report", {})
        row = {"spawner": f"{entry.scene}:{entry.file_id}", "error": r.get("error")}
        for src, dst in (("NewHealth", "new_health"), ("NewDamage", "new_damage"),
                         ("ReCalcOptimumTime", "recalc_optimum_time")):
            if src in v:
                # The report carries IEEE 64 for doubles and IEEE 32 for float fields.
                row[dst] = probes.doubles(v[src])[0] if len(v[src]) == 16 else probes.floats(v[src])[0]
        out.append(row)
    return out


def _rebake_overrides(cfg, rows):
    by_id = {f"{r['scene']}:{r['file_id']}": r for r in cfg.tables[C.SPAWNERS].rows}
    out = []
    for baked in rows:
        r = by_id[baked["spawner"]]
        if baked.get("error") or str(r.get("Key", "")).startswith("BossTower") or any(
                r.get(k) for k in ("HandBalance", "IsEvent", "IsVip", "IsDummy")):
            continue
        for stat, value in (("Health", "new_health"), ("Damage", "new_damage")):
            if value in baked:
                out.append({"node": f"EnemySpawnerConfig.Stats[{stat}]",
                            "where": {"scene": r["scene"], "file_id": r["file_id"]}, "set": baked[value]})
    return out


def _sim_tables(comp, base, head):
    from ..crosscheck import load_run
    from ..unity.fasttick import read_spawner_days
    datasets = []
    threshold = comp.manifest.get("kill_loss_multiplier", 1.0)
    key_days = (comp.patch.report or {}).get("key_days", [5, 10, 20, 46, 104, 206, 405])
    for directory in (base, head):
        run = load_run(directory)
        stats, combat, items, arrival = [], [], [], {}
        for snap in run.early + run.full:
            arrival.setdefault(snap.snapshot.location, snap.tc)
        first_items = {}
        for snap in run.early:
            for item in snap.snapshot.items:
                first_items.setdefault(item.key, snap.tc)
        for snap in run.full:
            s = snap.snapshot
            arrival.setdefault(s.location, snap.tc)
            for stat in s.stats:
                stats.append({"day": snap.tc, "stat": int(stat.key), "value": stat.value,
                              "total_value": stat.total_value, "gpm": stat.current_gpm})
            if snap.tc in key_days:
                for kind, data in (("dps", s.dps_data), ("defense", s.defense_data)):
                    for typ, value in data:
                        combat.append({"day": snap.tc, "kind": kind, "type": int(typ), "value": value})
            for item in s.items:
                first_items.setdefault(item.key, snap.tc)
                items.append({"day": snap.tc, "item": item.key, "level": item.level, "copies": item.copies})
        daily = tables.read_csv(directory / "daily.csv")
        events = _events(directory)
        kills = [{"spawner": e.get("spawner"), "day": e.get("day"), "playtime": e.get("playtime")}
                 for e in events if e.get("type") == "first_kill"]
        evolves = [{"sequence": i, **e} for i, e in enumerate(e for e in events if e.get("type") == "evolve")]
        for e in events:
            if e.get("type") == "unlock":
                arrival[e["location"]] = min(e["day"], arrival.get(e["location"], e["day"]))
        rpm = [{"day": snap.tc, "stat": int(st.key), "gpm": st.current_gpm}
               for snap in run.full for st in snap.snapshot.stats]
        spawner_days = read_spawner_days((directory / "spawner_days.csv.gz").read_bytes())
        boundary = [{"day": r.day, "round": r.round, "spawner": r.spawner, "loss_over_h": r.loss_over_h,
                     "killable": r.killable} for r in spawner_days if r.max_health > 0 and abs(r.loss_over_h - threshold) <= 0.03 * abs(threshold)]
        datasets.append({"timechunk_stats": (stats, ("day", "stat")), "combat": (combat, ("day", "kind", "type")),
                         "items": (items, ("day", "item")), "daily": (daily, ("day",)),
                         "island_arrival": ([{"island": k, "day": v} for k, v in arrival.items()], ("island",)),
                         "first_kill": (kills, ("spawner",)), "rpm": (rpm, ("day", "stat")),
                         "boundary": (boundary, ("day", "round", "spawner")),
                         "item_first": ([{"item": k, "day": v} for k, v in first_items.items()], ("item",)),
                         "evolution": (evolves, ("sequence",))})
    for name, (rows, keys) in datasets[0].items():
        comp.comparisons[f"sim/{name}.csv"] = tables.compare_rows(rows, datasets[1][name][0], keys)
    _mark_vacuous(comp, datasets)
    for side, ds in zip(("baseline", "patched"), datasets):
        comp.simulation[side] = {name: rows for name, (rows, _) in ds.items()}
    itd = comp.static.config.value(pipeline.ITD)
    late = _late_arrivals(datasets[1]["island_arrival"][0], itd)
    if late:
        comp.warnings.append({"code": "arrival_extrapolation", "message": "到岛日超过目标 2 天或超过第 17 岛", "rows": late})
    if comp.comparisons["sim/boundary.csv"]:
        comp.warnings.append({"code": "kill_boundary_changed", "message": f"loss/H 在 L={threshold!r} 的 ±3% 范围内的集合或数值变化；详见 sim/boundary.csv"})


def _late_arrivals(rows, itd):
    from ..model.timeline import island_start_day
    return [r for r in rows if r["island"] > 17 or
            0 < r["island"] <= len(itd) + 1 and r["day"] > island_start_day(itd, r["island"]) + 2]


def _mark_vacuous(comp, datasets):
    vacuous = {}
    for name in datasets[0]:
        if not any(ds[name][0] for ds in datasets):
            vacuous[f"sim/{name}.csv"] = f"两侧均无数据：当前重建未模拟或本次轨迹未发生 {name}。"
    no_evolves = not any(ds["evolution"][0] for ds in datasets)
    # The starting knife alone is initialization, not evidence of simulated item rewards.
    no_drops = all({r["item"] for r in ds["item_first"][0]} <= {"knife"} and all(
        r["item"] == "knife" and r["level"] == 1 and r["copies"] == 1 for r in ds["items"][0]) for ds in datasets)
    if no_evolves:
        vacuous["sim/evolution.csv"] = "两侧均无数据：当前重建未模拟进化或本次轨迹未发生进化（等待 C6）。"
    if no_drops:
        for name in ("items", "item_first"):
            vacuous[f"sim/{name}.csv"] = "两侧均无数据：当前重建未模拟物品掉落或本次轨迹未发生掉落（仅有初始物品，等待 C6）。"
    comp.simulation["vacuous_tables"] = vacuous
    nodes = sorted({o.node for o in comp.patch.overrides} | {x.node for x in comp.static.report.leaves})
    touched = [n for n in nodes if any(s in n.lower() for s in ("item", "evolution", "emission", "shard"))]
    if touched and (no_evolves or no_drops):
        comp.warnings.append({"code": "sim_not_modelled", "message": "物品/进化输入改变，但两侧轨迹未发生掉落或进化；空比较不能说明改动没有效果（等待 C6）",
                              "nodes": touched, "tables": sorted(vacuous)})


def asset_patch(cfg, *, base="HEAD", worktree=REPO, scratch=None):
    """Re-extract modified ScriptableObjects into explicit sets; never check out or write an asset.

    The baseline DB supplies scenes and unchanged assets. Changed assets are decoded on both sides
    using the extractor, including float types and references, before comparing typed leaves.
    """
    from .. import extract as E
    from ..unity_yaml import GuidIndex, iter_documents, payload, resolve_refs
    from ..field_types import FieldTypes
    from ..csharp_enums import EnumDecoder, parse_enums

    worktree = Path(worktree)
    ref = git("rev-parse", base, worktree=worktree).strip()
    changed = set(git("diff", "--name-only", ref, "--", ASSET_PREFIX, worktree=worktree).splitlines())
    changed.update(git("ls-files", "--others", "--exclude-standard", "--", ASSET_PREFIX,
                       worktree=worktree).splitlines())
    files = sorted(p for p in changed if p.endswith((".asset", ".unity", ".prefab", ".meta")))
    ignored_meta = [p for p in files if p.endswith(".meta") and _unchanged_nonconfig_guid(p, ref, worktree)]
    files = [p for p in files if p not in ignored_meta]
    unsupported = [p for p in files if not p.endswith(".asset")]
    if unsupported:
        raise ValueError("asset mode cannot safely translate scene/prefab/identity edits into config leaves: "
                         + ", ".join(unsupported) + "; extract versioned DBs first or use a numeric patch")
    assets = worktree / ASSET_PREFIX
    index = GuidIndex(assets)
    decoder = EnumDecoder(parse_enums(assets / "Scripts", exclude=E.REBUILD_SCRIPT_DIRS))
    types = FieldTypes(assets / "Scripts", decoder)
    scratch = Path(scratch or BALANCE / "out" / "diff" / "scratch")
    scratch.mkdir(parents=True, exist_ok=True)
    base_sets, head_sets, warnings, raw_diffs = [], [], [], []
    with tempfile.TemporaryDirectory(prefix="assets-", dir=scratch) as temp:
        for number, path in enumerate(files):
            rel = path.removeprefix(ASSET_PREFIX)
            try:
                before = git("show", f"{ref}:{path}", worktree=worktree)
                after = (worktree / path).read_text(encoding="utf-8")
            except (subprocess.CalledProcessError, OSError):
                raise ValueError(f"asset added or deleted: {path}; structural changes require re-extraction") from None
            extracted = []
            for side, content in enumerate((before, after)):
                if content.startswith("version https://git-lfs.github.com/spec/v1"):
                    raise ValueError(f"{path}: asset is an LFS pointer; materialize it in the unit worktree first")
                temp_file = Path(temp) / f"{number}-{side}.asset"
                temp_file.write_text(content, encoding="utf-8")
                store = E.Tables()
                for doc in iter_documents(temp_file):
                    if doc.class_id != 114:
                        continue
                    cls = index.script_class(doc.script_guid())
                    data = E.load_document(doc)
                    owner = rel if doc.file_id == 11400000 else f"{rel}#{doc.file_id}"
                    raw = resolve_refs(payload(data), index)
                    decoded = types.decode(cls, raw)
                    E.add_values(store, owner, cls, raw)
                    E.add_record_tables(store, "c", cls, {"asset": owner, "asset_name": data.get("m_Name")},
                                        {k: v for k, v in decoded.items() if k != "references"}, "assets")
                extracted.append(store)
            raw = tables.compare_rows(extracted[0].rows["config_values"], extracted[1].rows["config_values"],
                                      ("owner", "class", "field_path"))
            raw_diffs += raw
            handled = set()
            for spec in C.TABLES:
                aa, bb = extracted[0].rows.get(spec.sql, []), extracted[1].rows.get(spec.sql, [])
                if not aa and not bb:
                    continue
                a = {spec.row_key(r): {k: E.cell(v) for k, v in r.items()} for r in aa}
                b = {spec.row_key(r): {k: E.cell(v) for k, v in r.items()} for r in bb}
                if a.keys() != b.keys():
                    raise ValueError(f"{path}: rows added/removed in {spec.sql}; structural changes require re-extraction")
                for key in a:
                    original = cfg.tables[spec.sql].row(key)
                    for f in spec.fields:
                        av, bv, stored = f.decode(a[key]), f.decode(b[key]), f.decode(original)
                        where = spec.row_id(a[key])
                        def op(value):
                            return {"node": f.node, "set": list(value) if isinstance(value, tuple) else value,
                                    **({"where": where} if where else {})}
                        if not tables.same(av, stored):
                            base_sets.append(op(av))
                        if not tables.same(av, bv):
                            head_sets.append(op(bv))
                        # Track raw path patterns this typed field covers.
                        pattern = re.escape(f.node.split(".", 1)[1]).replace(r"\[\]", r"\[\d+\]")
                        if f.is_array:
                            pattern += r"\[\d+\]"
                        handled.add((f.node.split(".", 1)[0], pattern))
            for diff in raw:
                _, cls, field_path = diff["key"]
                if any(cls == c and re.fullmatch(p, field_path) for c, p in handled):
                    continue
                # Scalar dictionary fields outside GameConfig can still go through C9.
                node = f"{cls}.{field_path}"
                if node in cfg.dictionary and "[" not in field_path and diff["field"] in ("value_num", "value_text"):
                    head_sets.append({"node": node, "set": diff["patched"]})
                else:
                    warnings.append({"code": "asset_unmodelled", "node": node,
                                     "message": "资产叶子未建模；仅列出原始差异，模型不会响应",
                                     "formulas": P.formulas_reading(cfg, re.sub(r"\[\d+\]", "[]", node))})
    base_cfg, _ = P.apply(cfg, P.parse_patch({"name": "asset-base", "overrides": base_sets}))
    patch = P.parse_patch({"name": "asset-worktree", "description": f"资产版本 {ref[:12]} → worktree",
                           "overrides": head_sets})
    return base_cfg, patch, {"asset_base_ref": ref, "asset_files": files, "raw_diff": raw_diffs,
                             "ignored_meta": ignored_meta,
                             "baseline_overlay_leaves": len(base_sets),
                             "warnings": warnings}


def _unchanged_nonconfig_guid(path, ref, worktree):
    asset = worktree / path.removesuffix(".meta")
    if not asset.is_file() or asset.suffix.lower() in (".asset", ".unity", ".prefab", ".cs", ".dll"):
        return False
    try:
        before = git("show", f"{ref}:{path}", worktree=worktree)
        after = (worktree / path).read_text(encoding="utf-8")
    except (subprocess.CalledProcessError, OSError):
        return False
    guid = lambda s: re.findall(r"(?m)^guid: ([0-9a-f]{32})$", s)
    return len(guid(before)) == 1 and guid(before) == guid(after)
