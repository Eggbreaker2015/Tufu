"""Dependency walks and implementation coverage, without parsing CLI output."""

from __future__ import annotations

import json

from .. import graph as G
from ..model import registry as R
from ..model.patch import _ancestors

COLUMNS = ("id", "name", "status", "layer", "scope", "depth", "previous", "implementers", "provenance", "coverage")
SCOPES = (("immediate", True, ("combat", "progress")), ("progression", True, ("combat",)),
          ("full", False, ()))


def graph(cfg):
    # graph.load() uses a fixed local DB path; construct the same graph with the caller's RO dictionary.
    return G.Graph(json.loads(G.CATALOG.read_text(encoding="utf-8")),
                   json.loads(G.VALUES.read_text(encoding="utf-8")), set(cfg.dictionary))


def affected(cfg, nodes):
    g = graph(cfg)
    R.import_model_modules()
    cs = R.scan_csharp()
    rows, warnings = [], []
    starts = set()
    for node in sorted(set(nodes)):
        found = [f"config:{n}" for n in _ancestors(node) if f"config:{n}" in g.nodes]
        if not found or not any(g.out_edges[n] for n in found):
            warnings.append({"code": "no_formula_reader", "node": node,
                             "message": "目录里没有公式读取这个字段"})
        for n in found:
            starts.update(g.match(n))
    for scope, runtime, stop in SCOPES:
        reach = g.walk(sorted(starts), g.out_edges, runtime_only=runtime, stop_layers=stop)
        for node, (depth, previous) in sorted(reach.items()):
            if not node.startswith("formula:"):
                continue
            fid = node.removeprefix("formula:")
            formula = g.formulas[fid]
            py = R.REGISTRY.implementers(fid)
            rebuilt = [a for a in cs if a.formula_id == fid]
            # Catalog recovery describes implementation availability, not per-run branch coverage.
            native = formula["status"] == "recovered_native" or fid == "stats.raw_value_aggregation"
            implementers = [f"{i.module}.{i.qualname}" for i in py] + [f"{a.type_name}.{a.member}" for a in rebuilt]
            if native:
                implementers += ["C# 原生：" + ref for ref in formula.get("code_refs", [])]
            rows.append({"id": fid, "name": formula["name"], "status": formula["status"],
                         "layer": formula["subsystem"], "scope": scope, "depth": depth, "previous": previous,
                         "implementers": sorted(implementers) or ["未建模"],
                         "coverage": (["Python"] if py else []) + (["C# 重建"] if rebuilt else []) + (["C# 原生"] if native else []),
                         "provenance": sorted({i.provenance for i in py} | {a.provenance for a in rebuilt if a.provenance}
                                              | ({"C# 原生"} if native else set()))})
    return rows, warnings
