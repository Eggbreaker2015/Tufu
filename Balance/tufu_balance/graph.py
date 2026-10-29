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
from collections import Counter, defaultdict, deque
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
        self.states = {v["node"]: v for v in values.get("states", [])}
        self.nodes = {}
        self.out_edges = defaultdict(list)  # node -> [(target, label)]
        self.in_edges = defaultdict(list)
        self.problems = []
        self.output_deps = {}  # formula node -> {output node: set(input nodes)}, only where recorded
        for fid, f in self.formulas.items():
            fnode = f"formula:{fid}"
            self.nodes[fnode] = {"type": "formula", "label": f["name"], "subsystem": f["subsystem"],
                                 "status": f["status"], "kind": f["kind"],
                                 "writes_config": any(o["node"].startswith("config:") for o in f.get("outputs", []))}
            for item in f.get("inputs", []):
                node, role = item["node"], item.get("role")
                self._node(node, role, dictionary, fid)
                self._edge(node, fnode, item.get("symbol", ""))
            input_nodes = {i["node"] for i in f.get("inputs", [])}
            for item in f.get("outputs", []):
                node = item["node"]
                self._node(node, item.get("role"), dictionary, fid)
                self._edge(fnode, node, "")
                if "depends_on" in item:
                    unknown = set(item["depends_on"]) - input_nodes
                    if unknown:
                        self.problems.append(f"{fid}: output {node} depends on non-inputs {sorted(unknown)}")
                    self.output_deps.setdefault(fnode, {})[node] = set(item["depends_on"]) & input_nodes
        for node in list(self.nodes):
            # value:x.q[Q] specialises value:x.q: whatever moves the generic quantity moves each qualified one
            if node.startswith("value:") and node.endswith("]") and "[" in node:
                generic = node[:node.index("[")]
                if generic in self.nodes:
                    self._edge(generic, node, "specialises")
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
        if prefix == "state" and node not in self.states:
            self.problems.append(f"{fid}: state node {node} not in vocabulary")
        meta = self.values.get(node) or self.states.get(node, {})
        layer = meta.get("layer") or ("param" if prefix in ("config", "constant") else None)
        self.nodes.setdefault(node, {"type": prefix, "label": meta.get("meaning", ""), "layer": layer})

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

    def _passes(self, formula, via, nxt, downstream):
        """Whether a path entering `formula` from `via` may leave towards `nxt`, honouring per-output deps."""
        deps = self.output_deps.get(formula)
        if not deps or via is None:
            return True
        output, source = (nxt, via) if downstream else (via, nxt)
        return output not in deps or source in deps[output]

    def walk(self, starts, edges, runtime_only=False, stop_layers=()):
        """Breadth-first reach with depth and one parent per node (for printing a path).
        Formulas with per-output dependencies only connect an input to the outputs that use it.
        runtime_only skips formulas that write config (bake/editor generators) and the offline simulation
        layer, i.e. what changes in play without re-baking data."""
        downstream = edges is self.out_edges
        seen = {s: (0, None) for s in starts}
        self.last_trace = {(s, None): None for s in starts}  # state -> previous state, for path()
        self.last_first_state = {s: (s, None) for s in starts}
        queue = deque((s, None) for s in starts)
        while queue:
            current = queue.popleft()
            node, via = current
            for nxt, _ in edges[node]:
                if self._stop(nxt, runtime_only, stop_layers) and nxt not in seen:
                    seen[nxt] = (seen[node][0] + 1, node)  # reported, not expanded
                    self.last_first_state[nxt] = (nxt, "stop")
                    self.last_trace[(nxt, "stop")] = current
                    continue
                if self._stop(nxt, runtime_only, stop_layers):
                    continue
                if node.startswith("formula:") and not self._passes(node, via, nxt, downstream):
                    continue
                state = (nxt, node if nxt.startswith("formula:") else None)
                if state in self.last_trace:
                    continue
                self.last_trace[state] = current
                if nxt not in seen:
                    seen[nxt] = (seen[node][0] + 1, node)
                    self.last_first_state[nxt] = state
                queue.append(state)
        return seen

    def _stop(self, node, runtime_only, stop_layers):
        info = self.nodes.get(node, {})
        if runtime_only and (info.get("kind") == "editor_generator" or info.get("writes_config")
                             or info.get("layer") == "simulation"):
            return True  # design-time: regenerates config or belongs to the offline simulation
        return bool(stop_layers) and info.get("layer") in stop_layers

    def path(self, target):
        """Route of the first walk that reached target (call after walk)."""
        state, route = self.last_first_state.get(target), []
        while state is not None:
            route.append(state[0])
            state = self.last_trace.get(state)
        return list(reversed(route))

    def to_json(self):
        return {"nodes": self.nodes,
                "edges": [{"from": s, "to": t, "symbol": l} for s, targets in self.out_edges.items() for t, l in targets],
                "output_dependencies": {f: {o: sorted(i) for o, i in deps.items()} for f, deps in self.output_deps.items()},
                "problems": self.problems}


def load():
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    values = json.loads(VALUES.read_text(encoding="utf-8")) if VALUES.exists() else {"values": []}
    return Graph(catalog, values, dictionary_nodes())


LAYER_ORDER = ("param", "static", "progress", "combat", "simulation", None)


def print_reach(graph, starts, edges, title, runtime_only=False, stop_layers=()):
    reach = graph.walk(starts, edges, runtime_only, stop_layers)
    by_type = defaultdict(list)
    for node, (depth, parent) in reach.items():
        if node not in starts:
            by_type[node.split(":", 1)[0]].append((depth, node, parent))
    print(f"{title}: {', '.join(starts)}")
    if stop_layers:
        print(f"（不经过 {', '.join(stop_layers)} 层继续传播；这些层的节点仍会列出）")
    layers = Counter(graph.nodes[n].get("layer") for n in reach if n.startswith(("value:", "state:")) and n not in starts)
    print("派生量按层：" + "，".join(f"{k or '未分层'} {layers[k]}" for k in LAYER_ORDER if layers[k]))
    for kind in ("formula", "value", "config", "state", "constant"):
        rows = sorted(by_type.get(kind, []), key=lambda r: (r[0], LAYER_ORDER.index(graph.nodes[r[1]].get("layer"))
                                                              if graph.nodes[r[1]].get("layer") in LAYER_ORDER else 9, r[1]))
        if not rows:
            continue
        print(f"\n{kind} ({len(rows)})")
        for depth, node, parent in rows:
            info = graph.nodes.get(node, {})
            extra = f"  [{info.get('status')}]" if kind == "formula" else f"  ({info.get('layer') or '-'})"
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
        "cd Balance && .venv/bin/python -m tufu_balance.graph impact CombatBalanceConfig.IslandTargetDays --stop-at combat",
        "```",
        "",
        "| 命令 | 作用 |",
        "|---|---|",
        "| `impact <节点>` | 改动这个配置、常量或派生量后，下游受影响的全部公式和派生量，按传播步数排序，标出所在层和上一跳 |",
        "| `depends <节点>` | 反向：一个派生量由哪些配置、常量和公式决定 |",
        "| `path <起点> <终点>` | 一条实际传播路线，回答“为什么 A 会影响 B” |",
        "",
        "节点参数可以是完整名，也可以是子串（匹配到的全部作为起点）。含 `[` 的名字在 shell 里要加引号。",
        "",
        "### 分层与 `--stop-at`",
        "",
        "游戏是一个闭环：玩家战力 → 击杀速度 → 奖励速度 → 玩家战力。所以几乎任何改动最终都会传到大部分公式，"
        "这本身没错，但不利于判断。派生量因此分为四层：",
        "",
        "| 层 | 含义 |",
        "|---|---|",
        "| `static` | 只由配置和常数决定，例如某等级武器的伤害、基础冷却、插值系数 |",
        "| `progress` | 随游玩累积的成长状态及其直接推导，例如获得的属性加成、玩家属性、装备等级、货币、护符等级 |",
        "| `combat` | 战斗中的即时状态，例如当前血量、目标、触发事件、单次伤害、怒气 |",
        "| `simulation` | 原版离线模拟、烘焙和预测数据 |",
        "",
        "`--stop-at` 指定不再继续传播的层，被截断层的节点仍会列出，表示影响到这里为止。常用组合由窄到宽：",
        "",
        "| 参数 | 回答的问题 |",
        "|---|---|",
        "| `--runtime-only --stop-at combat,progress` | 改了这个数，哪些推导值立刻跟着变（不重新烘焙） |",
        "| `--runtime-only --stop-at combat` | 再加上长期成长：奖励速度、玩家属性、装备等级随之变化后的连锁影响 |",
        "| `--runtime-only` | 再加上战斗中的即时连锁，例如击杀触发能力、怒气 |",
        "| （无参数） | 再加上重新模拟、烘焙后写回配置的影响，例如敌人血量重新生成 |",
        "",
        "例如：武器成长参数在第一种范围只影响 13 条公式；刷怪冷却在第一种范围影响约 10 条，"
        "放开成长层后影响约 150 条，因为它改变奖励速度，进而改变整条成长曲线。",
        "",
        "每个输出都记录了它真正依赖哪些输入（`depends_on`），玩家与敌人共用的公式不会把敌人数据错误地传到玩家属性上。",
        "",
        "烘焙类公式把模拟结果写回配置，例如按玩家战力曲线生成敌人血量。默认查询包含这条链路，"
        "表示“改动后重新模拟和烘焙”的影响；加 `--runtime-only` 跳过所有写回配置的公式和离线模拟层，"
        "只看不重新烘焙时游戏内的影响。",
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

    lines += ["", "## 运行时状态", "", "| 节点 | 含义 | 被谁使用 |", "|---|---|---|"]
    for snode in sorted(graph.states):
        consumers = ", ".join(f"`{t.removeprefix('formula:')}`" for t, _ in graph.out_edges[snode])
        lines.append(f"| `{snode}` | {graph.states[snode].get('meaning', '')} | {consumers} |")

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
        p.add_argument("--runtime-only", action="store_true",
                       help="ignore formulas that write config (bake/editor) and the offline simulation layer")
        p.add_argument("--stop-at", default="", help="comma list of layers not to propagate through: "
                                                     "static,progress,combat,simulation")
    p = sub.add_parser("path", help="shortest dependency route from one node to another")
    p.add_argument("source")
    p.add_argument("target")
    p.add_argument("--runtime-only", action="store_true")
    p.add_argument("--stop-at", default="")
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
    stop_layers = tuple(x for x in args.stop_at.split(",") if x)
    if args.command == "path":
        sources, targets = graph.match(args.source), graph.match(args.target)
        if len(sources) != 1 or len(targets) != 1:
            sys.exit(f"source/target must match exactly one node: {sources[:8]} / {targets[:8]}")
        reach = graph.walk(sources, graph.out_edges, args.runtime_only, stop_layers)
        if targets[0] not in reach:
            sys.exit("no path")
        for i, node in enumerate(graph.path(targets[0])):
            info = graph.nodes.get(node, {})
            tag = info.get("status") if node.startswith("formula:") else info.get("layer")
            print(f"{'  ' * i}{node}  ({tag or '-'})")
        return
    starts = graph.match(args.node)
    if not starts:
        sys.exit(f"no node matches {args.node!r}")
    if args.command == "impact":
        print_reach(graph, starts, graph.out_edges, "改动影响", args.runtime_only, stop_layers)
    else:
        print_reach(graph, starts, graph.in_edges, "依赖来源", args.runtime_only, stop_layers)


if __name__ == "__main__":
    main()
