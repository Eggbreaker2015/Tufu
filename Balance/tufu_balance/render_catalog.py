"""Render Balance/catalog/formulas.json into the Markdown formula catalog under Docs/Balance/.

Usage (from Balance/):
    .venv/bin/python -m tufu_balance.render_catalog
"""

import json
import re
from collections import Counter
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
CATALOG = REPO / "Balance" / "catalog" / "formulas.json"
DOCS = REPO / "Docs" / "Balance"
CODE_FROM_DETAIL = "../../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/"

STATUS_LABELS = {
    "recovered_native": "原生证据恢复",
    "reconstructed": "重建（无原生标注）",
    "stub": "空壳",
    "editor_only_missing": "编辑器工具缺失",
    "data_only": "纯数据",
}
STATUS_MEANING = {
    "recovered_native": "已实现，且带原生地址注释或明确依据原生代码移植。可信。",
    "reconstructed": "已实现，但没有原生标注，行为是推断的。可用，但需实测或原生证据确认。",
    "stub": "原版运行时一定有逻辑，这里返回默认值或为空。建模时不能用当前代码，需补证据。",
    "editor_only_missing": "原团队的编辑器/烘焙/生成工具，APK 中没有，方法体为空，只剩产出数据。规则需从数据反推。",
    "data_only": "没有公式，是配置里手填或烘焙的数值，本身就是设计杠杆。",
}
SUBSYSTEM_TITLES = {
    "stats": "属性系统", "combat": "战斗结算", "items": "装备与进化", "meta": "永久成长与奖励",
    "spawners": "刷怪点、岛屿与门", "simulation": "原版模拟与烘焙管线", "powerups": "护符、怒气与能力",
    "cross": "跨系统",
}
CODE_FILE = re.compile(r"((?:[A-Z][\w]*/)+[\w]+\.cs)(?::(\d+)(?:[-–](\d+))?)?")


def cell(text):
    return str(text if text is not None else "").replace("|", "\\|").replace("\n", "<br>")


def link_code(text, prefix):
    def replace(match):
        path, line = match.group(1), match.group(2)
        anchor = f"#L{line}" if line else ""
        return f"[{match.group(0)}]({prefix}{path}{anchor})"
    return CODE_FILE.sub(replace, str(text))


def formula_section(f):
    lines = [f"### `{f['id']}`", "", f"**{f['name']}**", ""]
    lines += [f"- 状态：**{STATUS_LABELS.get(f['status'], f['status'])}**（`{f['status']}`）"
              + (f"，核验前为 `{f['status_original']}`" if f.get("status_original") else ""),
              f"- 类型：`{f['kind']}`" + (f"（核验前为 `{f['kind_original']}`）" if f.get("kind_original") else ""),
              f"- 来源：{ {'catalog': '子系统梳理', 'verifier': '核验时补充', 'critic': '完整性审查补充'}[f['origin']] }"]
    if f.get("native_va"):
        lines.append(f"- 原生地址：`{f['native_va']}`")
    lines += ["", "```text", f["expression"], "```", ""]
    if f.get("variables"):
        lines += ["| 符号 | 含义 | 来源 |", "|---|---|---|"]
        lines += [f"| `{cell(v['symbol'])}` | {cell(v['meaning'])} | {cell(v['source'])} |" for v in f["variables"]]
        lines.append("")
    lines.append(f"**输出**：{f['output']}")
    lines.append("")
    if f.get("code_refs"):
        lines.append("**代码**：" + "；".join(link_code(r, CODE_FROM_DETAIL) for r in f["code_refs"]))
        lines.append("")
    lines.append(f"**状态依据**：{f['status_evidence']}")
    lines.append("")
    if f.get("config_params"):
        lines.append("**配置参数**：" + "；".join(f"`{p}`" for p in f["config_params"]))
        lines.append("")
    if f.get("used_by"):
        lines.append("**使用方**：" + "；".join(link_code(u, CODE_FROM_DETAIL) for u in f["used_by"]))
        lines.append("")
    if f.get("notes"):
        lines += [f"**备注**：{f['notes']}", ""]
    if f["corrections"]:
        lines += ["**核验意见**（对抗性核验 agent 对照代码提出；`已应用` 的已改入上方字段，其余以此处为准）：", ""]
        for c in f["corrections"]:
            mark = "已应用" if c.get("applied") else "待人工合入"
            lines.append(f"- [{mark}] `{c['field']}`：{c['should_be']}  \n  依据：{link_code(c['evidence'], CODE_FROM_DETAIL)}")
        lines.append("")
    return lines


def render_subsystem(key, subsystem, formulas):
    title = SUBSYSTEM_TITLES.get(key, key)
    lines = [f"# {title}（`{key}`）", "", "[返回公式清单总览](../FormulaCatalog.md)", ""]
    if subsystem:
        lines += ["## 概述", "", subsystem["summary"], "", "**核验结论**：" + subsystem["verdict_summary"], ""]
    lines += ["## 公式索引", "", "| ID | 名称 | 状态 | 类型 |", "|---|---|---|---|"]
    for f in formulas:
        anchor = re.sub(r"[^\w-]", "", f["id"].lower().replace(".", ""))
        lines.append(f"| [`{f['id']}`](#{anchor}) | {cell(f['name'])} | {STATUS_LABELS.get(f['status'])} | `{f['kind']}` |")
    lines += ["", "## 公式", ""]
    for f in formulas:
        lines += formula_section(f)
    if subsystem:
        lines += ["## 配置参数", "", "| 资产 | 字段 | 类型 | 含义 | 用于 |", "|---|---|---|---|---|"]
        lines += [f"| {cell(p['asset'])} | `{cell(p['field'])}` | {cell(p['type'])} | {cell(p['meaning'])} | "
                  f"{cell(', '.join(p['used_in']))} |" for p in subsystem["params"]]
        lines += ["", "## 枚举", "", "| 枚举 | 文件 | 取值 |", "|---|---|---|"]
        lines += [f"| `{e['name']}` | {link_code(e['file'], CODE_FROM_DETAIL)} | {cell(e['values'])} |" for e in subsystem["enums"]]
        lines += ["", "## 未解决问题", ""]
        lines += [f"{i}. {q}" for i, q in enumerate(subsystem["open_questions"], 1)]
        lines.append("")
    return "\n".join(lines)


def render_overview(catalog, keys):
    formulas = catalog["formulas"]
    counts = Counter((f["subsystem"], f["status"]) for f in formulas)
    statuses = catalog["status_values"]
    lines = [
        "# 数值公式清单",
        "",
        "核心战斗成长相关的全部公式、配置参数及其恢复状态。数据导出见 [Balance/README.md](../../Balance/README.md)。",
        "",
        f"共 {len(formulas)} 条公式，由 7 个子系统梳理 agent 逐一读代码整理，每个子系统再由一个对抗性核验 agent "
        "对照代码检查表达式、状态和行号，最后由完整性审查 agent 补漏。"
        f"{sum(1 for f in formulas if f['corrections'])} 条带有核验意见，见各子系统页。",
        "",
        "结构化原始数据是 [formulas.json](../../Balance/catalog/formulas.json)，本页及子页由 "
        "`Balance/tufu_balance/render_catalog.py` 从它生成；修改请改 JSON 后重新生成。公式细节保留英文原文，"
        "以免翻译改动表达式和标识符。",
        "",
        "本次范围不含 BossTower、水晶活动、VIP、商店、通行证、矿区和任务奖励。",
        "",
        "## 状态说明",
        "",
        "| 状态 | 含义 |",
        "|---|---|",
    ]
    lines += [f"| {STATUS_LABELS[s]}（`{s}`） | {STATUS_MEANING[s]} |" for s in statuses]
    lines += ["", "## 各子系统", "", "| 子系统 | " + " | ".join(STATUS_LABELS[s] for s in statuses) + " | 合计 |",
              "|---|" + "---|" * (len(statuses) + 1)]
    for key in keys:
        row = [counts[(key, s)] for s in statuses]
        lines.append(f"| [{SUBSYSTEM_TITLES.get(key, key)}](formulas/{key}.md) | " + " | ".join(map(str, row)) + f" | {sum(row)} |")
    total = [sum(counts[(k, s)] for k in keys) for s in statuses]
    lines.append("| **合计** | " + " | ".join(f"**{n}**" for n in total) + f" | **{sum(total)}** |")
    for status, heading in (("stub", "空壳：建模前必须补证据"), ("editor_only_missing", "编辑器工具缺失：只能从数据反推")):
        lines += ["", f"## {heading}", "", "| ID | 名称 | 代码 |", "|---|---|---|"]
        for f in formulas:
            if f["status"] == status:
                refs = "；".join(link_code(r, "../../Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase/")
                                for r in f["code_refs"][:2])
                lines.append(f"| [`{f['id']}`](formulas/{f['subsystem']}.md) | {cell(f['name'])} | {cell(refs)} |")
    lines += ["", "## 重叠条目", "",
              "各子系统并行梳理，同一段代码可能在多个子系统下各有一条。建依赖图前需要合并成一个节点。"
              "下表按首个代码引用的文件分组，只列跨子系统的文件。", "",
              "| 文件 | 条目 |", "|---|---|"]
    by_file = {}
    for f in formulas:
        match = CODE_FILE.search(f["code_refs"][0]) if f["code_refs"] else None
        if match:
            by_file.setdefault(match.group(1), []).append(f["id"])
    for path, ids in by_file.items():
        if len({i.split(".")[0] for i in ids}) > 1:
            lines.append(f"| `{path}` | " + "、".join(f"`{i}`" for i in ids) + " |")
    lines += ["", "## 完整性审查结论", "", catalog["critic_summary"], ""]
    return "\n".join(lines)


def main():
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    subsystems = {s["key"]: s for s in catalog["subsystems"]}
    keys = list(subsystems) + sorted({f["subsystem"] for f in catalog["formulas"]} - set(subsystems))
    (DOCS / "formulas").mkdir(parents=True, exist_ok=True)
    for key in keys:
        formulas = [f for f in catalog["formulas"] if f["subsystem"] == key]
        (DOCS / "formulas" / f"{key}.md").write_text(render_subsystem(key, subsystems.get(key), formulas), encoding="utf-8")
    (DOCS / "FormulaCatalog.md").write_text(render_overview(catalog, keys), encoding="utf-8")
    print(f"rendered {len(catalog['formulas'])} formulas into {DOCS}")


if __name__ == "__main__":
    main()
