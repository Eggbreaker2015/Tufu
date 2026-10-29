"""X1 (design 3.5): every snapshot's stat columns recomposed from Value and K2/K3, bit for bit.

The engine writes ``StatsFromFeaturesFlat = K2``, ``PercentMultiplier = K3``, ``TotalFlatValue = Value * (1 + K2)``
and ``TotalValue = TotalFlatValue * K3`` (direct stats) with the K2/K3 its interpolation modifiers carry: K(d) of
the snapshot's day d on both lists (the forced snapshot follows the modifier change; early snapshots use the
modifiers of their day). The oracle is ``model.snapshots.compose_stat`` with ``composition_coefficients`` under the
oracle's rounding (DP3: the editor is ``double_all``); both sides go through C2's ``probes.encode_snapshot``.
"""

from __future__ import annotations

from pathlib import Path

from ..model.snapshots import SimulationSnapshot, compose_stat
from ..unity import probes
from . import Check, CheckResult, Env, RunData, num, stat_name, ulps

FIELDS = ("StatsFromFeaturesFlat", "PercentMultiplier", "TotalFlatValue", "TotalValue")
COLUMNS = ("list", "index", "time_chunk_index", "stat", "stat_name", "field", "csharp", "oracle", "ulps")


def composed(snapshot: SimulationSnapshot, env: Env) -> SimulationSnapshot:
    """The snapshot with its stat columns recomposed at K(TimeChunkIndex)."""
    k2, k3 = env.k(snapshot.time_chunk_index)
    stats = [compose_stat(st.key, st.value, k2, k3, st.current_gpm) for st in snapshot.stats]
    return SimulationSnapshot(snapshot.playtime, snapshot.total_playtime, snapshot.location, snapshot.time_chunk_index,
                              stats, snapshot.items, snapshot.dps_data, snapshot.defense_data, snapshot.source_index)


def check_x1(run: RunData, env: Env, out_dir: str | Path | None = None) -> CheckResult:
    c = Check("X1", "stat columns from Value and K2/K3 (bit for bit)", COLUMNS)
    snaps = run.early + run.full
    for s in snaps:
        cs, os_ = probes.encode_snapshot(s.snapshot)["stats"], probes.encode_snapshot(composed(s.snapshot, env))["stats"]
        for a, b in zip(cs, os_):
            for i, name in enumerate(FIELDS, start=2):
                c.item(probes._same(a[i], b[i]), list=s.list, index=s.index, time_chunk_index=s.tc, stat=a[0],
                       stat_name=stat_name(a[0]), field=name, csharp=num(a[i]), oracle=num(b[i]), ulps=ulps(a[i], b[i]))
    c.counts.update(snapshots=len(snaps), early=len(run.early), day=len(run.full))
    if not snaps:
        c.vacuous_part("snapshots", "the run exported no snapshots")
    return c.result(out_dir)
