"""Chinese designer report and deterministic CSV artifacts."""

from __future__ import annotations

from collections import Counter
import json
from pathlib import Path

from . import impact, tables


def json_file(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, sort_keys=True, indent=2, default=_plain) + "\n",
                    encoding="utf-8")


def _plain(value):
    if hasattr(value, "item"):
        return value.item()
    raise TypeError(type(value).__name__)


def write(comp, out_dir, *, timings=None):
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    for path, rows in comp.comparisons.items():
        tables.write(out / path, rows, tables.CHANGE_COLUMNS)
    tables.write(out / "touched_formulas.csv", comp.formulas, impact.COLUMNS)
    leaves = [{"node": x.node, "selector": x.row, "index": x.index, "base": x.old, "patched": x.new,
               "csharp": "PASS" if comp.consistency and comp.consistency["identical"] else "未运行"}
              for x in comp.static.report.leaves]
    leaves += [{"node": x.node, "selector": x.where, "index": x.index, "base": None, "patched": x.arg,
                "csharp": "仅 C#；Python 未建模"} for x in comp.static.report.passthrough]
    tables.write(out / "overrides.csv", leaves, ("node", "selector", "index", "base", "patched", "csharp"))
    ops = [{"node": o.node, "selector": o.override.where if o.override else None,
            "index": o.override.index if o.override else None,
            "source": o.source, "hits": o.rows, "leaves": len(o.leaves)} for o in comp.static.report.ops]
    tables.write(out / "operations.csv", ops, ("node", "selector", "index", "source", "hits", "leaves"))
    floor = [{"spawner": r["spawner"], "is_days": r["is_days"], "field": stat,
              "stored": r.get("stored_" + stat), "model": r.get("new_" + stat),
              "delta": r.get("new_" + stat, 0) - r["stored_" + stat] if "new_" + stat in r else None}
             for r in comp.static.base.tables["bake"] for stat in ("health", "damage")]
    tables.write(out / "model_floor.csv", floor, ("spawner", "is_days", "field", "stored", "model", "delta"))
    json_file(out / "manifest.json", comp.manifest)
    json_file(out / "patch_report.json", comp.static.report.to_json())
    json_file(out / "validation.json", {"consistency": comp.consistency, "warnings": comp.warnings,
                                      "crosschecks": comp.simulation.get("crosschecks", [])})
    if timings is not None:
        json_file(out / "timings.json", {**timings, "jobs": comp.simulation.get("jobs", [])})
    for side in ("baseline", "patched"):
        for name, rows in comp.simulation.get(side, {}).items():
            columns = sorted({k for r in rows for k in r}) or ["key"]
            tables.write(out / "sim" / side / f"{name}.csv", rows, columns)
    lines = [f"# 数值改动对比：{comp.patch.name}", "", comp.patch.description, "",
             "## 1. 补丁摘要与校验", "", "有变化。" if comp.changed else "无变化。所有对比表都没有变化行。",
             f"覆盖 {len(comp.patch.overrides)} 条；展开后 {len(comp.static.report.leaves)} 个叶子。",
             "两侧一致性：" + ("PASS（逐叶子一致）" if comp.consistency and comp.consistency["identical"] else "未运行 C#（静态预言机）"),
             "", "比较基线和补丁使用同一套模型；场景天制数值来自未知状态 B，原始残差只在 model_floor.csv 作背景。",
             "", "## 2. 受影响公式与实现覆盖", ""]
    unique = {r["id"]: r for r in comp.formulas}
    counts = Counter(r["status"] for r in unique.values())
    missing = sum(r["implementers"] == ["未建模"] for r in unique.values())
    lines += [f"触及 {len(unique)} 条公式；状态 {dict(sorted(counts.items()))}；未建模 {missing} 条。", ""]
    for scope, _, _ in impact.SCOPES:
        lines += [f"### {scope}", ""]
        lines += [f"- `{r['id']}`：{r['name']}（{', '.join(r['provenance']) or '未建模'}）"
                  for r in comp.formulas if r["scope"] == scope] or ["无。"]
        lines.append("")
    lines += ["## 3. 静态表", "", "表 | 变化单元格 | 变化行 | 最大绝对差值", "--- | ---: | ---: | ---:"]
    for path, rows in comp.comparisons.items():
        if not path.startswith("static/"):
            continue
        maximum = max((abs(r["delta"]) for r in rows if r["delta"] is not None), default=0)
        lines.append(f"{path} | {len(rows)} | {len({tables.text(r['key']) for r in rows})} | {maximum!r}")
    top = (comp.patch.report or {}).get("top_n", 10)
    for path, rows in comp.comparisons.items():
        if path.startswith("static/") and rows:
            lines += ["", f"### {path}（前 {top} 项）", "", *_preview(rows, top)]
    lines += ["", "## 4. 烘焙", "", "快照来源：" + ("随包快照（--skip-sim，Python）" if comp.manifest["skip_sim"]
             else "各自模拟轨迹（C#，X4 核对）"), ""]
    bake = comp.comparisons["bake/spawners.csv"]
    by_id = {r["spawner"]: r for r in comp.static.base.tables["optimum_time"]}
    islands = Counter(by_id.get(r["key"][0], {}).get("location", "?") for r in bake)
    lines += [f"变化单元格 {len(bake)}；按岛统计 {dict(sorted(islands.items(), key=lambda p: str(p[0])))}。",
              "", *_preview(bake, top)]
    lines += ["", "## 5. 模拟", ""]
    if comp.manifest["skip_sim"]:
        lines += ["已跳过；空 CSV 表示未运行，不代表模拟没有影响。"]
    else:
        lines += [f"状态：**{comp.manifest['simulation_label']}**。", "",
                  "同一代码与参数、相同烘焙轮数；基线由 P10 守卫缓存复用。",
                  "关键天的 DPS/防御见 sim/combat.csv；逐日属性、Gpm、物品与进化、到岛日、首杀、活跃分钟见 sim/。", ""]
        for path, rows in comp.comparisons.items():
            if path.startswith("sim/"):
                lines += [f"### {path}：{len(rows)} 个变化单元格", "", *_preview(rows, top), ""]
    lines += ["", "## 6. 警告", ""]
    for w in sorted(comp.warnings, key=tables.text):
        lines.append(f"- `{w.get('code')}`：{w.get('message', '')} "
                     + tables.text({k: v for k, v in w.items() if k not in ("code", "message")}))
    if not comp.warnings:
        lines.append("无。")
    (out / "report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return out / "report.md"


def _preview(rows, n):
    return [f"- {tables.text(r['key'])} / {r['field']}：{tables.text(r['base'])} → {tables.text(r['patched'])}"
            for r in sorted(rows, key=lambda r: (tables.text(r["key"]), str(r["field"])))[:n]] or ["无变化。"]
