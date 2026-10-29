"""X4 (design 3.5): the C# bake report on the run's snapshots against ``model.bake``, NewHealth/NewDamage bit for bit.

The oracle bakes every spawner of the ``spawners`` table on this run's snapshots: ``Snapshots`` from
``simulation_data.json`` and the untrimmed day list from ``timechunk_full.json`` as ``config.TimeChunkSnapshots``
(what ``GetSnapshotForBake`` reads while the run's lists are in the config). The C# side is C8's ``bake_report.json``
(``outputs.bake_report``, ``bake.snapshots = "run"``): ``spawners[]`` with the runtime ``id``, ``registered``, and
``report`` fields as IEEE hex (``NewHealth``, ``NewDamage``) or ``error``. Without that file (C8 not merged, or the
output off) the oracle side still runs and the comparison is reported as vacuous. C8's own ``compare_x4`` also checks
the intermediates against the report's exact inputs; this check is the run-level one against the exported files.
"""

from __future__ import annotations

import json
from pathlib import Path

from ..model import bake as B
from ..model import data as MD
from ..model.snapshots import SimulationData
from ..unity import probes
from . import Check, CheckResult, Env, RunData, num, same

COLUMNS = ("spawner", "scene", "file_id", "is_days", "registered", "csharp_new_health", "oracle_new_health",
           "csharp_new_damage", "oracle_new_damage", "csharp_error", "oracle_error")
REPORT = "bake_report.json"


def oracle_bake(run: RunData, env: Env) -> dict:
    sd = SimulationData(snapshots=[s.snapshot for s in run.early], time_chunk_snapshots=[s.snapshot for s in run.full])
    ctx = MD.load_bake_context(env.con, simulation_data=sd, rounding=env.rounding)
    ctx.config_time_chunk_snapshots = sd.time_chunk_snapshots
    armors = MD.load_armor_configs(env.con)
    out = {}
    for r in MD.load_spawner_rows(env.con):
        inp = MD.spawner_input_from_row(r, armors)
        try:
            out[inp.spawner_id] = (inp, B.bake_spawner(ctx, inp))
        except B.BakeError as e:
            out[inp.spawner_id] = (inp, e)
    return out


def check_x4(run: RunData, env: Env, out_dir: str | Path | None = None) -> CheckResult:
    c = Check("X4", "bake report on the run's snapshots against model.bake (NewHealth/NewDamage bit for bit)", COLUMNS)
    if not run.early or not run.full:
        c.vacuous_part("snapshots", "the run has no Snapshots or no day list: GetSnapshotForBake would read the shipped data")
        return c.result(out_dir)
    oracle = oracle_bake(run, env)
    c.counts["oracle_spawners"] = len(oracle)
    c.counts["oracle_errors"] = sum(isinstance(o, B.BakeError) for _, o in oracle.values())
    path = run.job_dir / REPORT
    if not path.exists():
        c.vacuous_part("report", f"no {REPORT} in the job (C8's bake export not merged or outputs.bake_report off): "
                                 f"the oracle baked {len(oracle)} spawners, nothing to compare with")
        return c.result(out_dir)
    report = json.loads(path.read_text(encoding="utf-8"))
    if report.get("snapshots", "run") != "run":
        c.vacuous_part("report", f"the report baked the {report.get('snapshots')} snapshots, not this run's")
        return c.result(out_dir)
    seen = set()
    for row in report.get("spawners", []):
        sid = env.spawner_id(row["id"])
        if sid is None or sid not in oracle:
            c.item(False, spawner=row["id"], scene="", file_id="", is_days="", registered=row.get("registered"),
                   csharp_new_health="", oracle_new_health="", csharp_new_damage="", oracle_new_damage="",
                   csharp_error="unknown spawner", oracle_error="")
            continue
        seen.add(sid)
        inp, o = oracle[sid]
        base = dict(spawner=row["id"], scene=sid[0], file_id=sid[1], is_days=int(inp.is_days),
                    registered=int(bool(row.get("registered"))))
        if "error" in row or isinstance(o, B.BakeError):
            c.item("error" in row and isinstance(o, B.BakeError), **base, csharp_new_health="", oracle_new_health="",
                   csharp_new_damage="", oracle_new_damage="", csharp_error=row.get("error", ""),
                   oracle_error=str(o) if isinstance(o, B.BakeError) else "")
            continue
        ch, cd = probes.doubles(row["report"]["NewHealth"])[0], probes.doubles(row["report"]["NewDamage"])[0]
        c.item(same(ch, o.new_health) and same(cd, o.new_damage), **base, csharp_new_health=num(ch),
               oracle_new_health=num(o.new_health), csharp_new_damage=num(cd), oracle_new_damage=num(o.new_damage),
               csharp_error="", oracle_error="")
    missing = [sid for sid in oracle if sid not in seen]
    c.counts["report_missing"] = len(missing)
    for sid in missing:
        c.item(False, spawner=env.runtime_id(sid), scene=sid[0], file_id=sid[1], is_days="", registered="",
               csharp_new_health="", oracle_new_health="", csharp_new_damage="", oracle_new_damage="",
               csharp_error="not in the report", oracle_error="")
    return c.result(out_dir)
