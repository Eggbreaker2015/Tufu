"""Dependency graph: config parameter -> formula -> derived value -> formula -> ...

Built from Balance/catalog/formulas.json (each formula's `inputs`/`outputs`), Balance/catalog/values.json
(the derived-value vocabulary) and the extracted data dictionary (Balance/out/balance.sqlite).

Usage (from Balance/):
    .venv/bin/python -m tufu_balance.graph build            # validate, write out/graph.json and the docs page
    .venv/bin/python -m tufu_balance.graph impact <node>    # what changes downstream if <node> changes
    .venv/bin/python -m tufu_balance.graph depends <node>   # what <node> is computed from
<node> may be an exact id (config:CombatBalanceConfig.IslandTargetDays, value:enemy.health, formula:combat.flat_defense_curve)
or a case-insensitive substring; every matching node is used as a start point.
"""

import argparse
import json
import sqlite3
import sys
from collections import defaultdict, deque
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
CATALOG = REPO / "Balance" / "catalog" / "formulas.json"
VALUES = REPO / "Balance" / "catalog" / "values.json"
DATABASE = REPO / "Balance" / "out" / "balance.sqlite"
GRAPH_OUT = REPO / "Balance" / "out" / "graph.json"
DOC = REPO / "Docs" / "Balance" / "DependencyGraph.md"

ROLES = ("config", "value", "state", "constant")


def dictionary_nodes():
    if not DATABASE.exists():
        sys.exit("Balance/out/balance.sqlite missing: run `python -m tufu_balance.extract` first")
    with sqlite3.connect(DATABASE) as db:
        return {row[0] for row in db.execute("SELECT node FROM data_dictionary")}


def config_known(node, dictionary):
    """A config node is known if it is a dictionary field, or a list/struct whose fields are in the dictionary."""
    name = node.removeprefix("config:")
    if name in dictionary:
        return True
    stripped = name.rstrip("]").rstrip("[") if name.endswith("[]") else name
    return any(d.startswith(stripped + ".") or d.startswith(stripped + "[") for d in dictionary)


class Graph:
    def __init__(self, catalog, values, dictionary):
        self.formulas = {f["id"]: f for f in catalog["formulas"]}
        self.values = {v["node"]: v for v in values["values"]}
        self.nodes = {}
        self.out_edges = defaultdict(list)  # node -> [(target, label)]
        self.in_edges = defaultdict(list)
        self.problems = []
        for fid, f in self.formulas.items():
            fnode = f"formula:{fid}"
            self.nodes[fnode] = {"type": "formula", "label": f["name"], "subsystem": f["subsystem"],
                                 "status": f["status"], "kind": f["kind"]}
            for item in f.get("inputs", []):
                node, role = item["node"], item.get("role")
                self._node(node, role, dictionary, fid)
                self._edge(node, fnode, item.get("symbol", ""))
            for item in f.get("outputs", []):
                node = item["node"]
                self._node(node, item.get("role"), dictionary, fid)
                self._edge(fnode, node, "")
        for node in self.values:
            if node not in self.nodes:
                self.nodes[node] = {"type": "value", "label": self.values[node].get("meaning", "")}
                self.problems.append(f"value {node} is in the vocabulary but no formula reads or writes it")

    def _node(self, node, role, dictionary, fid):
        prefix = node.split(":", 1)[0]
        if prefix not in ROLES:
            self.problems.append(f"{fid}: node {node} has unknown prefix")
        if role and role != prefix:
            self.problems.append(f"{fid}: node {node} declared role {role}")
        if prefix == "config" and not config_known(node, dictionary):
            self.problems.append(f"{fid}: config node {node} not in data dictionary")
        if prefix == "value" and node not in self.values:
            self.problems.append(f"{fid}: value node {node} not in vocabulary")
        meta = self.values.get(node, {})
        self.nodes.setdefault(node, {"type": prefix, "label": meta.get("meaning", "")})

    def _edge(self, source, target, label):
        self.out_edges[source].append((target, label))
        self.in_edges[target].append((source, label))

    def match(self, query):
        if query in self.nodes:
            return [query]
        for prefix in ROLES + ("formula",):
            if f"{prefix}:{query}" in self.nodes:
                return [f"{prefix}:{query}"]
        lowered = query.lower()
        return sorted(n for n in self.nodes if lowered in n.lower())

    def walk(self, starts, edges, runtime_only=False):
        """Breadth-first reach with depth and one parent per node (for printing a path).
        runtime_only skips editor/bake generators, i.e. what changes in play without re-baking data."""
        seen = {s: (0, None) for s in starts}
        queue = deque(starts)
        while queue:
            node = queue.popleft()
            for nxt, _ in edges[node]:
                if runtime_only and self.nodes.get(nxt, {}).get("kind") == "editor_generator":
                    continue
                if nxt not in seen:
                    seen[nxt] = (seen[node][0] + 1, node)
                    queue.append(nxt)
        return seen

    def to_json(self):
        return {"nodes": self.nodes,
                "edges": [{"from": s, "to": t, "symbol": l} for s, targets in self.out_edges.items() for t, l in targets],
                "problems": self.problems}


def load():
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    values = json.loads(VALUES.read_text(encoding="utf-8")) if VALUES.exists() else {"values": []}
    return Graph(catalog, values, dictionary_nodes())


def print_reach(graph, starts, edges, title, runtime_only=False):
    reach = graph.walk(starts, edges, runtime_only)
    by_type = defaultdict(list)
    for node, (depth, parent) in reach.items():
        if node not in starts:
            by_type[node.split(":", 1)[0]].append((depth, node, parent))
    print(f"{title}: {', '.join(starts)}")
    for kind in ("formula", "value", "config", "state", "constant"):
        rows = sorted(by_type.get(kind, []))
        if not rows:
            continue
        print(f"\n{kind} ({len(rows)})")
        for depth, node, parent in rows:
            info = graph.nodes.get(node, {})
            extra = f"  [{info.get('status')}]" if kind == "formula" else ""
            print(f"  {depth:>2}  {node}{extra}    <- {parent}")


def render_doc(graph):
    formulas = graph.formulas
    lines = [
        "# 数值依赖图",
        "",
        "由 `Balance/tufu_balance/graph.py build` 从 [formulas.json](../../Balance/catalog/formulas.json) 的输入输出映射和"
        " [values.json](../../Balance/catalog/values.json) 的派生量词表生成。公式细节见[公式清单](FormulaCatalog.md)。",
        "",
        "## 查询改动影响",
        "",
        "```bash",
        "cd Balance && .venv/bin/python -m tufu_balance.graph impact CombatBalanceConfig.IslandTargetDays",
        "```",
        "",
        "`impact` 列出某个配置、常量或派生量改动后，下游会受影响的所有公式和派生量（按传播步数排序，并给出上一跳）；"
        "`depends` 反向列出一个派生量依赖的全部配置和公式。参数可以是完整节点名，也可以是子串。",
        "",
        "烘焙类公式（`editor_generator`）把模拟结果写回配置，例如按玩家战力曲线生成敌人血量。默认查询包含这条链路，"
        "表示“改动后重新烘焙”的影响；加 `--runtime-only` 只看不重新烘焙时游戏内的直接影响。",
        "",
        "## 节点类型",
        "",
        "| 前缀 | 含义 |",
        "|---|---|",
        "| `config:` | 配置字段，名字与[数据字典](../../Balance/README.md)一致，可以直接在导出表里查到数值 |",
        "| `constant:` | 写死在代码里的常数，同样是设计杠杆 |",
        "| `state:` | 运行时状态（当前血量、怒气、随机数、时间等），不是设计参数 |",
        "| `value:` | 公式算出的派生量，含义见下表 |",
        "| `formula:` | 公式清单中的一条 |",
        "",
    ]
    counts = defaultdict(int)
    for node in graph.nodes:
        counts[node.split(":", 1)[0]] += 1
    edges = sum(len(t) for t in graph.out_edges.values())
    lines += ["## 规模", "", "| 类型 | 数量 |", "|---|---|"]
    lines += [f"| `{k}` | {counts[k]} |" for k in ("config", "constant", "state", "value", "formula")]
    lines += [f"| 边 | {edges} |", ""]

    # subsystem-level flow through shared values
    flow = defaultdict(set)
    for vnode in graph.values:
        producers = {graph.nodes[s]["subsystem"] for s, _ in graph.in_edges[vnode] if s.startswith("formula:")}
        consumers = {graph.nodes[t]["subsystem"] for t, _ in graph.out_edges[vnode] if t.startswith("formula:")}
        for p in producers:
            for c in consumers:
                if p != c:
                    flow[(p, c)].add(vnode)
    lines += ["## 子系统之间的数值流", "", "箭头上的数字是两个子系统之间传递的派生量个数。", "", "```mermaid", "flowchart LR"]
    lines += [f"  {p} -->|{len(v)}| {c}" for (p, c), v in sorted(flow.items())]
    lines += ["```", ""]

    lines += ["## 派生量", "", "| 节点 | 含义 | 单位 | 由谁计算 | 被谁使用 |", "|---|---|---|---|---|"]
    for vnode in sorted(graph.values):
        v = graph.values[vnode]
        producers = ", ".join(f"`{s.removeprefix('formula:')}`" for s, _ in graph.in_edges[vnode])
        consumers = ", ".join(f"`{t.removeprefix('formula:')}`" for t, _ in graph.out_edges[vnode])
        lines.append(f"| `{vnode}` | {v.get('meaning', '')} | {v.get('unit', '')} | {producers} | {consumers} |")

    lines += ["", "## 配置参数被哪些公式使用", "", "| 配置 | 公式 |", "|---|---|"]
    for node in sorted(n for n in graph.nodes if n.startswith(("config:", "constant:"))):
        users = ", ".join(f"`{t.removeprefix('formula:')}`" for t, _ in graph.out_edges[node])
        lines.append(f"| `{node}` | {users} |")

    unmapped = [fid for fid, f in formulas.items() if not f.get("inputs") and not f.get("outputs")]
    lines += ["", "## 校验", ""]
    lines.append(f"- 未映射输入输出的公式：{len(unmapped)}" + (("（" + "、".join(f"`{u}`" for u in unmapped) + "）") if unmapped else ""))
    lines.append(f"- 问题：{len(graph.problems)}")
    lines += [f"  - {p}" for p in graph.problems]
    lines.append("")
    DOC.write_text("\n".join(lines), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description="Balance dependency graph")
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("build")
    for name in ("impact", "depends"):
        p = sub.add_parser(name)
        p.add_argument("node")
        p.add_argument("--runtime-only", action="store_true", help="ignore editor/bake generators")
    args = parser.parse_args()
    graph = load()
    if args.command == "build":
        GRAPH_OUT.parent.mkdir(parents=True, exist_ok=True)
        GRAPH_OUT.write_text(json.dumps(graph.to_json(), ensure_ascii=False, indent=1), encoding="utf-8")
        render_doc(graph)
        print(f"{len(graph.nodes)} nodes, {sum(len(t) for t in graph.out_edges.values())} edges, "
              f"{len(graph.problems)} problems -> {GRAPH_OUT}, {DOC}")
        for problem in graph.problems[:40]:
            print("  " + problem)
        return
    starts = graph.match(args.node)
    if not starts:
        sys.exit(f"no node matches {args.node!r}")
    if args.command == "impact":
        print_reach(graph, starts, graph.out_edges, "改动影响", args.runtime_only)
    else:
        print_reach(graph, starts, graph.in_edges, "依赖来源", args.runtime_only)


if __name__ == "__main__":
    main()
