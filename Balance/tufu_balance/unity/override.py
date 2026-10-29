"""Replay and checkpoint state override: request fields, defaults, output (design 1.4.2, 4.6; unit C4).

* ``job.override`` (C# ``SimulationRunRequest.Override.cs``), read by ``replay`` and ``checkpoint`` jobs:
  ``from_day`` (null = 0), ``econ_caches`` (``stored`` | ``recomputed``), ``replay_items`` (``snapshot`` |
  ``recompute``), ``unlock`` (``derived`` | ``snapshot``), ``items_evolved`` (``{"<TC>": {"<Key>": bool|null}}``,
  P4's :func:`tufu_balance.model.items.items_evolved_override`) and ``slots`` (``{"<TC>": {"weapon": n,
  "armor": m}}``, by default the stage-4 fixture; :func:`slots_p12` gives the closed-form P12 rule). Keys are
  shipped day-snapshot TimeChunkIndexes; a day takes the entry of the snapshot that provides its items.
* manifest ``override`` (``SimulationStateOverride``): options, calls/applied/first/last day, counters.
* ``override_days.jsonl`` (output ``override_days``, on by default): one line per applied day, the state the
  engine holds after the override; doubles/floats as hex bits (:func:`read_days` decodes them).
* :func:`expected_day` is the independent oracle of what a day's override must set (``model.snapshots``).
"""

from __future__ import annotations

import json
import sqlite3
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Mapping, Sequence

from . import request as req, schema
from .probes import doubles, floats

BALANCE = Path(__file__).resolve().parents[2]
SLOT_FIXTURE = BALANCE / "tests" / "golden" / "fixtures" / "stage4_slot_schedule.json"
DAYS_FILE = "override_days.jsonl"
SCHEMA = "tufu.sim.override/1"

OVERRIDE_MODES = ("replay", "checkpoint")
ECON_CACHES = ("stored", "recomputed")
REPLAY_ITEMS = ("snapshot", "recompute")
UNLOCK = ("derived", "snapshot")

OVERRIDE_FIELDS = {"from_day": (int, None), "econ_caches": (str,), "replay_items": (str,), "unlock": (str,),
                   "items_evolved": (dict, None), "slots": (dict, None)}

req.register_fields("job", {"override": (dict, None)})
req.register_fields("override", OVERRIDE_FIELDS)
req.register_fields("slot_counts", {"weapon": (int,), "armor": (int,)})
schema.register_manifest_keys({"override": "object"})
schema.register_output("override_days", "C4")


def _day_key(key: Any) -> bool:
    return isinstance(key, str) and key.isdigit() and key.isascii() and len(key) <= 6 and str(int(key)) == key


def check_override(job: Mapping[str, Any], where: str = "job") -> None:
    """Mirror of ``SimulationRunRequest.ValidateOverrideFields`` (plus the unknown-field check Newtonsoft does)."""
    o = job.get("override")
    if o is None:
        return
    w = where + ".override"
    req._check_object(o, "override", w)
    if o.get("from_day") is not None and o["from_day"] < 0:
        raise req.RequestError(f"{w}.from_day must be >= 0")
    for name, allowed in (("econ_caches", ECON_CACHES), ("replay_items", REPLAY_ITEMS), ("unlock", UNLOCK)):
        if name in o and o[name] not in allowed:
            raise req.RequestError(f"{w}.{name} must be one of {allowed}")
    for key, per in (o.get("items_evolved") or {}).items():
        if not _day_key(key):
            raise req.RequestError(f"{w}.items_evolved key {key!r} is not a TimeChunkIndex")
        if not isinstance(per, Mapping):
            raise req.RequestError(f"{w}.items_evolved.{key} must be an object")
        for item, flag in per.items():
            if flag is not None and not isinstance(flag, bool):
                raise req.RequestError(f"{w}.items_evolved.{key}.{item} must be true, false or null")
    for key, counts in (o.get("slots") or {}).items():
        if not _day_key(key):
            raise req.RequestError(f"{w}.slots key {key!r} is not a TimeChunkIndex")
        req._check_object(counts, "slot_counts", f"{w}.slots.{key}")
        for name in ("weapon", "armor"):
            if name not in counts:
                raise req.RequestError(f"{w}.slots.{key}.{name} is required")
            if counts[name] < 0:
                raise req.RequestError(f"{w}.slots.{key}.{name} must be >= 0")


req.register_job_check(check_override)


# ---------------------------------------------------------------- defaults

def items_evolved_default(con: sqlite3.Connection, undetermined: str = "unevolved") -> dict[str, dict[str, bool | None]]:
    """P4's evolved flags for every shipped day snapshot (design 1.4.2 ``items_evolved``)."""
    from ..model import data as md, items as mi
    inferred = mi.infer_evolved_snapshots(md.load_simulation_data(con).time_chunk_snapshots, mi.load_item_defs(con),
                                          mi.load_item_tables(con))
    return mi.items_evolved_override(inferred, undetermined=undetermined)


def slots_fixture(path: Path = SLOT_FIXTURE) -> dict[str, dict[str, int]]:
    """Weapon/armor slots per shipped day snapshot from ``stage4_slot_schedule.json`` (fitted values)."""
    chunks = json.loads(Path(path).read_text(encoding="utf-8"))["chunks"]
    return {str(c["TimeChunkIndex"]): {"weapon": int(c["weapon_slots"]), "armor": int(c["armor_slots"])} for c in chunks}


def slots_p12(con: sqlite3.Connection) -> dict[str, dict[str, int]]:
    """Slots per shipped day snapshot under the closed-form P12 rule at the snapshot's Location (R2b, closed loop)."""
    from ..model import data as md, simcalc as sc, spawners as sp
    unlocks = sc.slot_unlock_locations(sp.load_spawners(con))
    out = {}
    for s in md.load_simulation_data(con).time_chunk_snapshots:
        w, a = sc.slots_p12(s.location, unlocks)
        out[str(s.time_chunk_index)] = {"weapon": w, "armor": a}
    return out


def override_options(*, from_day: int | None = None, econ_caches: str = "stored", replay_items: str = "snapshot",
                     unlock: str = "derived", items_evolved: Mapping | None = None,
                     slots: Mapping | None = None) -> dict:
    o = {"from_day": from_day, "econ_caches": econ_caches, "replay_items": replay_items, "unlock": unlock,
         "items_evolved": None if items_evolved is None else {k: dict(v) for k, v in items_evolved.items()},
         "slots": None if slots is None else {k: dict(v) for k, v in slots.items()}}
    check_override({"override": o})
    return o


def default_override(con: sqlite3.Connection, **kw) -> dict:
    """Design defaults with P4's ``items_evolved`` and the fixture's ``slots`` filled in."""
    kw.setdefault("items_evolved", items_evolved_default(con))
    kw.setdefault("slots", slots_fixture())
    return override_options(**kw)


def override_job(job_id: str, mode: str, override: Mapping | None, *, probe: Mapping | None = None, **kw) -> dict:
    """``request.make_job`` plus ``override`` (and the C3b ``engine_probe``, e.g. ``null_policy``)."""
    from . import engine
    job = engine.engine_job(job_id, mode=mode, probe=probe, **kw)
    if override is not None:
        job["override"] = dict(override)
    check_override(job, job_id)
    return job


# ---------------------------------------------------------------- output

@dataclass(frozen=True)
class DayRecord:
    raw: dict
    day: int
    stats: dict[int, tuple[float, float, float]]  # key -> (snapshot Value, Stat.FastValue, GetForecastStatValue)
    items: list[tuple[str, int, int, bool, int]]  # (Key, Level, Copies, Evolved, EffectiveLevel)
    dps: list[tuple[int, float]]
    dps_cache: list[tuple[int, float]]
    defense: list[tuple[int, float]]
    defense_cache: list[tuple[int, float]]
    rpm: dict[int, float]
    k2: float
    k3: float
    current_health: float


def _pairs(rows) -> list[tuple[int, float]]:
    return [(int(w), doubles(h)[0]) for w, h in rows]


def read_days(text: str) -> list[DayRecord]:
    out = []
    for line in text.splitlines():
        if not line.strip():
            continue
        r = json.loads(line)
        out.append(DayRecord(
            raw=r, day=int(r["day"]),
            stats={int(k): (doubles(v)[0], doubles(f)[0], doubles(g)[0]) for k, v, f, g in r["stats"]},
            items=[(k, int(lv), int(c), bool(e), int(el)) for k, lv, c, e, el in r["items"]],
            dps=_pairs(r["dps"]), dps_cache=_pairs(r["dps_cache"]), defense=_pairs(r["defense"]),
            defense_cache=_pairs(r["defense_cache"]), rpm={int(k): doubles(h)[0] for k, h in r["rpm"]},
            k2=float(floats(r["k2"])[0]), k3=float(floats(r["k3"])[0]),
            current_health=doubles(r["current_health"])[0]))
    return out


# ---------------------------------------------------------------- oracle

@dataclass(frozen=True)
class ExpectedDay:
    day: int
    snapshot: Any  # model.snapshots.SimulationSnapshot (interpolated or shipped)
    items_time_chunk: int
    items: list[tuple[str, int, int, bool]]  # (Key, snapshot Level, Copies, Evolved)


def expected_day(shipped, mode: str, day: int, items_evolved: Mapping[str, Mapping[str, bool | None]] | None) -> ExpectedDay | None:
    """What the override of ``day`` sets, from ``model.snapshots`` (independent of the C# code): replay takes
    ``GetByTimeChunkIndex(day)`` and the items of the snapshot whose list it returns (the left side), checkpoint the
    shipped snapshot of that day (None when there is none)."""
    snaps: Sequence = shipped.time_chunk_snapshots
    if mode == "replay":
        s = shipped.get_by_time_chunk_index(day)
        ordered = sorted(snaps, key=lambda x: x.time_chunk_index)
        keys = [x.time_chunk_index for x in ordered]
        if day <= keys[0]:
            src = ordered[0]
        elif not day < keys[-1]:
            src = ordered[-1]
        else:
            src = ordered[next(i for i in range(1, len(keys)) if keys[i - 1] <= day <= keys[i]) - 1]
    else:
        s = next((x for x in snaps if x.time_chunk_index == day), None)
        if s is None:
            return None
        src = s
    per = (items_evolved or {}).get(str(src.time_chunk_index)) if items_evolved is not None else None
    items = []
    for it in src.items:
        flag = per.get(it.key) if per is not None else None
        items.append((it.key, it.level, it.copies, it.level != it.copies if flag is None else bool(flag)))
    return ExpectedDay(day, s, src.time_chunk_index, items)
