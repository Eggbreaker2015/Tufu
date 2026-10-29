"""Fast phase: request field, outputs, readers (design 1.4.3, 4.4, 4.5; unit C5).

* ``job.fast_tick_probe`` (C# ``SimulationRunRequest.FastTick.cs``): ``reference_flush`` replaces
  ``FlushFastTickChunkRewards`` by the original-shaped reference flush (native-replacement acceptance; editor only).
* manifest ``engine.fast_tick`` (``SimulationSystem.RebuildFastTickManifest``): the kill gate's provenance, formula
  id and effective L, ``policy_extrapolation`` (L from the AB table, or an AB group other than A), diagnostics,
  files, counters.
* ``fasttick_days.jsonl`` (output ``fasttick_days``, on by default): one line per FastTick (:func:`read_days`).
* ``spawner_days.csv.gz`` (output ``spawner_days``, off by default): one row per spawner of every planning round's
  bot data (:func:`read_spawner_days`); kill days and charge counts for P10's T05/T12.
* ``Balance/sim/params/defaults/fasttick.json``: the fast-phase fields of ``SimulationRebuildParams``.
"""

from __future__ import annotations

import csv
import gzip
import io
import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Mapping

from . import request as req, schema
from .probes import doubles, floats

BALANCE = Path(__file__).resolve().parents[2]
FASTTICK_DEFAULTS = BALANCE / "sim" / "params" / "defaults" / "fasttick.json"
PARAMS_SOURCE = (BALANCE.parent / "Recovery" / "StudyProject" / "Assets" / "Scripts" / "Assembly-CSharp" / "CodeBase"
                 / "Simulation" / "Rebuild" / "SimulationRebuildParams.FastTick.cs")
DAYS_FILE = "fasttick_days.jsonl"
SPAWNER_DAYS_FILE = "spawner_days.csv.gz"
SCHEMA = "tufu.sim.fast_tick/1"
KILL_GATE_FORMULA = "pending:simulation.fast_kill_gate"

# SimulationRebuildParams.FastTick.cs enums; JSON carries the integers.
KILL_STATE = {"StartOfDay": 0, "AfterPass": 1}
ACTIVE_MINUTES_MODEL = {"FarmedTime": 0, "NnTour": 1}
DIAGNOSTICS_OFF = {"DiagnosticDisableKillPredicate": False, "DiagnosticPlayerDpsScale": 1.0, "DiagnosticDefenseScale": 1.0}

PROBE_FIELDS = {"reference_flush": (bool,)}

req.register_fields("job", {"fast_tick_probe": (dict, None)})
req.register_fields("fast_tick_probe", PROBE_FIELDS)
schema.register_output("fasttick_days", "C5")


def check_fast_tick(job: Mapping[str, Any], where: str = "job") -> None:
    """Mirror of the C# field types (Newtonsoft rejects unknown members)."""
    probe = job.get("fast_tick_probe")
    if probe is not None:
        req._check_object(probe, "fast_tick_probe", where + ".fast_tick_probe")


req.register_job_check(check_fast_tick)


def fasttick_defaults() -> dict[str, Any]:
    return json.loads(FASTTICK_DEFAULTS.read_text(encoding="utf-8"))


def job_params(**overrides) -> dict[str, Any]:
    """The complete parameter object (all default layers) with ``overrides`` (enum members by name allowed)."""
    from ..calibrate import params as P
    return P.resolve(["defaults"], overrides or None).params


def fast_job(job_id: str, mode: str = "replay", *, override: Mapping | None = None, params: Mapping | None = None,
             reference_flush: bool = False, spawner_days: bool = False, **kw) -> dict:
    """A job with ``override`` (C4), a complete ``params`` object and the C5 probe/outputs."""
    from . import override as ov
    outputs = dict(kw.pop("outputs", None) or {})
    if spawner_days:
        outputs["spawner_days"] = True
    job = ov.override_job(job_id, mode, override, params=job_params(**(params or {})), outputs=outputs, **kw)
    if reference_flush:
        probe = {"reference_flush": True}
        req._check_object(probe, "fast_tick_probe", f"{job_id}.fast_tick_probe")
        job["fast_tick_probe"] = probe
    return job


# ---------------------------------------------------------------- outputs


@dataclass(frozen=True)
class Round:
    location: int  # unlocked island while the round planned
    bot: int  # spawners in the bot data (CalculateSpawnerData(false))
    eligible: int  # cleared (bot data, kill gate, charges left)
    signature: str  # SimulationTrace.SetSignature of the cleared spawners' ids
    clears: int
    kills: int
    fight_seconds: float
    farmed_seconds: float
    unlocked: int  # unlocked island after the round's gate flush


@dataclass(frozen=True)
class FastDay:
    raw: dict
    day: int
    playtime: float  # P when the day planned (float32)
    location_start: int
    location_end: int
    kill_loss: float
    rounds: list[Round]
    round_unlocks: list[int]  # islands RebuildFlushFastTickGates opened between rounds
    flush_unlocks: list[int]  # islands FlushFastTickChunkRewards opened
    clears: int
    kills: int
    kill_gate_rejections: int
    fight_seconds: float
    farmed_seconds: float
    tour: float
    speed: float
    active_minutes: float  # float32
    stats: dict[int, float]  # accumulated stat rewards the flush applied (StatType -> double)
    items: dict[str, float]
    souls: dict[str, float]
    kill_count: int
    flush_stat_modifiers: int
    gate_spawners: int  # gate spawners left for the flush


def read_days(text: str) -> list[FastDay]:
    out = []
    for line in text.splitlines():
        if not line.strip():
            continue
        r = json.loads(line)
        rounds = [Round(int(x["location"]), int(x["bot"]), int(x["eligible"]), x["signature"], int(x["clears"]),
                        int(x["kills"]), doubles(x["fight_seconds"])[0], doubles(x["farmed_seconds"])[0], int(x["unlocked"]))
                  for x in r["rounds"]]
        out.append(FastDay(
            raw=r, day=int(r["day"]), playtime=floats(r["playtime"])[0], location_start=int(r["location_start"]),
            location_end=int(r["location_end"]), kill_loss=doubles(r["kill_loss"])[0], rounds=rounds,
            round_unlocks=[int(n) for n in r["round_unlocks"]], flush_unlocks=[int(n) for n in r["flush_unlocks"]],
            clears=int(r["clears"]), kills=int(r["kills"]), kill_gate_rejections=int(r["kill_gate_rejections"]),
            fight_seconds=doubles(r["fight_seconds"])[0], farmed_seconds=doubles(r["farmed_seconds"])[0],
            tour=doubles(r["tour"])[0], speed=doubles(r["speed"])[0], active_minutes=floats(r["active_minutes"])[0],
            stats={int(k): doubles(v)[0] for k, v in r["stats"].items()},
            items={k: doubles(v)[0] for k, v in r["items"].items()},
            souls={k: doubles(v)[0] for k, v in r["souls"].items()}, kill_count=int(r["kill_count"]),
            flush_stat_modifiers=int(r["flush_stat_modifiers"]), gate_spawners=int(r["gate_spawners"])))
    return out


@dataclass(frozen=True)
class SpawnerDay:
    day: int
    round: int
    spawner: str  # runtime id "<hierarchy path>|<Key>" (spawner_map)
    location: int
    dafc: bool
    killable: bool  # the kill gate (with the job's L and diagnostics)
    fights: int  # clears done in this round (0: not cleared)
    fight_time: float
    farmed_time: float
    loss: float
    max_health: float

    @property
    def loss_over_h(self) -> float:
        return self.loss / self.max_health


SPAWNER_DAYS_COLUMNS = ("day", "round", "spawner", "location", "dafc", "killable", "fights", "fight_time", "farmed_time",
                        "loss", "max_health")


def read_spawner_days(data: bytes) -> list[SpawnerDay]:
    """``spawner_days.csv.gz`` (one gzip member per day, one header)."""
    text = gzip.decompress(data).decode("utf-8")
    rows = list(csv.reader(io.StringIO(text)))
    if not rows or tuple(rows[0]) != SPAWNER_DAYS_COLUMNS:
        raise schema.SchemaError(f"{SPAWNER_DAYS_FILE}: header {rows[0] if rows else None} != {SPAWNER_DAYS_COLUMNS}")
    out = []
    for i, r in enumerate(rows[1:], 2):
        if len(r) != len(SPAWNER_DAYS_COLUMNS) or r[4] not in ("0", "1") or r[5] not in ("0", "1"):
            raise schema.SchemaError(f"{SPAWNER_DAYS_FILE}:{i}: malformed row {r}")
        out.append(SpawnerDay(int(r[0]), int(r[1]), r[2], int(r[3]), r[4] == "1", r[5] == "1", int(r[6]), float(r[7]),
                              float(r[8]), float(r[9]), float(r[10])))
    return out


def kill_days(rows: list[SpawnerDay]) -> dict[str, int]:
    """First day each spawner was cleared in the fast phase (``spawner_days``)."""
    out: dict[str, int] = {}
    for r in rows:
        if r.fights > 0 and r.spawner not in out:
            out[r.spawner] = r.day
    return out


def charge_counts(rows: list[SpawnerDay]) -> dict[tuple[int, str], int]:
    """Clears per (day, spawner) (P10: T12's per-charge plateau needs ``clears x Count``)."""
    out: dict[tuple[int, str], int] = {}
    for r in rows:
        if r.fights > 0:
            out[(r.day, r.spawner)] = out.get((r.day, r.spawner), 0) + r.fights
    return out
