"""Trace outputs of a job (design 1.4.3, 2.6; unit C3c): ``daily.csv``, ``events.jsonl``, ``digests.csv`` and,
on request, ``digests.txt``. Written by the C# ``SimulationTrace`` for every non-empty job.

* ``daily.csv``: C1's columns plus ``ticks``, ``offline_steps``, ``complete``. One row per day: complete days at
  their day change, the open day (``complete = 0``) when the job stops inside it. ``time_chunk_index`` is the
  position of the day's forced snapshot in the full ``TimeChunkSnapshots``; ``playtime``/``total_playtime`` P
  and T at the day's end; ``active_minutes`` the day's P increase; ``offline_minutes``/``offline_steps`` the
  offline steps started in the day; ``kills``/``clears`` the write point's counts; ``fight_time_sum``/
  ``farmed_time_sum`` seconds reported by the fight modules (C5/C7); ``eligible_count``/``eligible_signature``
  the bot-filter set of the day's first planning (``-1``/empty when no planning ran, e.g. the minimal
  FastTick); ``location`` the island of the last fought spawner (else the engine's current location);
  ``unlocked_location`` the highest open island.
* ``events.jsonl``: C1's event types, in the order they happen. ``first_kill`` once per spawner and run;
  ``slot_unlock.count`` the unlocked slots of that slot type after the unlock.
* ``digests.csv``: per day, right after the day's forced snapshot and override, the sha256 of the state text
  (see :func:`parse_digest_texts` and Runbook 17); ``digests.txt`` holds the texts (output ``digest_text``).
  The day change clears the charges, so day d's text carries day d-1's usage at its close as
  ``charges_closed.<spawner>`` (and ``charges_closed.day``); ``charges.<spawner>`` is the usage so far.

``manifest.trace`` lists every written file with sha256, bytes and rows; :func:`read` checks them.
"""

from __future__ import annotations

import hashlib
import io
import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable, Mapping

from . import request as req, schema

SCHEMA = "tufu.sim.trace/1"
DAILY, EVENTS, DIGESTS, DIGEST_TEXT = "daily.csv", "events.jsonl", "digests.csv", "digests.txt"
FILES = (DAILY, EVENTS, DIGESTS, DIGEST_TEXT)
OUTPUT_DEFAULTS = {"daily": True, "events": True, "digests": True, "digest_text": False}
DAILY_EXTRA = (("ticks", "int"), ("offline_steps", "int"), ("complete", "bool01"))
TRACE_KEYS = {"schema": "str", "files": "object", "days": "int", "complete_days": "int", "digests": "int",
              "last_digest": "sha256?", "events": "object", "random": "object"}
RANDOM_KEYS = {"mode": "int", "seed": "int", "streams": "int", "state_sha256": "sha256"}
FILE_KEYS = {"sha256": "sha256", "bytes": "int", "rows": "int"}
# Line prefixes of the digest text (design 2.6 plus the engine state the next day depends on).
DIGEST_SECTIONS = ("clock", "modifiers", "stat", "health", "item", "slot", "location", "gate", "rpm", "soul_rpm",
                   "charges", "charges_closed", "cooldown", "pending_item", "fast", "souls", "currency", "policy", "random")

schema.register_csv_columns(DAILY, DAILY_EXTRA)
schema.register_output("digest_text", "C3c")
# The trace export also writes the parameter loader's report (manifest "params", tufu.sim.params/1; see
# paramsio) and the loader reads job.params_source.
schema.register_manifest_keys({"trace": "object", "params": "object"})
req.register_fields("job", {"params_source": (str, None)})


class TraceError(ValueError):
    pass


@dataclass
class Trace:
    manifest: dict
    daily: list[dict] = field(default_factory=list)
    events: list[dict] = field(default_factory=list)
    digests: list[dict] = field(default_factory=list)
    texts: dict[int, tuple[str, str]] = field(default_factory=dict)  # day -> (sha256, text)


def validate_manifest_trace(trace: Mapping[str, Any], where: str = "manifest.trace") -> None:
    for key, value in trace.items():
        if key not in TRACE_KEYS:
            raise TraceError(f"{where}: unknown key {key!r}")
        schema._check_value(TRACE_KEYS[key], value, f"{where}.{key}")
    for key in ("schema", "files", "days", "digests"):
        if key not in trace:
            raise TraceError(f"{where}: missing {key!r}")
    if trace["schema"] != SCHEMA:
        raise TraceError(f"{where}.schema must be {SCHEMA}")
    for name, info in trace["files"].items():
        if name not in FILES:
            raise TraceError(f"{where}.files: unknown file {name!r}")
        for key in FILE_KEYS:
            if key not in info:
                raise TraceError(f"{where}.files.{name}: missing {key!r}")
        for key, value in info.items():
            if key not in FILE_KEYS:
                raise TraceError(f"{where}.files.{name}: unknown key {key!r}")
            schema._check_value(FILE_KEYS[key], value, f"{where}.files.{name}.{key}")
    if "random" in trace:
        for key, value in trace["random"].items():
            if key not in RANDOM_KEYS:
                raise TraceError(f"{where}.random: unknown key {key!r}")
            schema._check_value(RANDOM_KEYS[key], value, f"{where}.random.{key}")


def read(job_dir: str | Path) -> Trace:
    """The job's trace files, each checked against ``manifest.trace`` (sha256, bytes, rows) and its schema."""
    job_dir = Path(job_dir)
    manifest = json.loads((job_dir / "manifest.json").read_text(encoding="utf-8"))
    schema.validate_manifest(manifest)
    trace = manifest.get("trace")
    if trace is None:
        raise TraceError(f"{job_dir}: manifest has no trace")
    validate_manifest_trace(trace)
    out = Trace(manifest=trace)
    for name in FILES:
        path = job_dir / name
        info = trace["files"].get(name)
        if info is None:
            if path.exists():
                raise TraceError(f"{path}: not listed in manifest.trace")
            continue
        raw = path.read_bytes()
        if hashlib.sha256(raw).hexdigest() != info["sha256"] or len(raw) != info["bytes"]:
            raise TraceError(f"{path}: sha256/size differ from manifest.trace")
        text = raw.decode("utf-8")
        if name == EVENTS:
            out.events = schema.read_events(text)
            rows = len(out.events)
        elif name == DIGEST_TEXT:
            out.texts = parse_digest_texts(text)
            rows = len(out.texts)
        else:
            rows_ = schema.read_csv(name, text)
            rows = len(rows_)
            if name == DAILY:
                out.daily = rows_
            else:
                out.digests = rows_
        if rows != info["rows"]:
            raise TraceError(f"{path}: {rows} rows, manifest.trace says {info['rows']}")
    if out.texts:
        for row in out.digests:
            sha, _ = out.texts.get(row["day"], (None, None))
            if sha != row["sha256"]:
                raise TraceError(f"digests.txt day {row['day']}: {sha} differs from digests.csv {row['sha256']}")
    return out


def parse_digest_texts(text: str) -> dict[int, tuple[str, str]]:
    """``digests.txt`` -> {day: (sha256, text)}; each text must hash to its header's sha256."""
    out: dict[int, tuple[str, str]] = {}
    day, sha, lines = None, None, []

    def close():
        if day is None:
            return
        body = "".join(lines)
        if hashlib.sha256(body.encode("utf-8")).hexdigest() != sha:
            raise TraceError(f"digests.txt day {day}: the text does not hash to {sha}")
        out[day] = (sha, body)

    for line in io.StringIO(text):
        if line.startswith("# day "):
            close()
            _, _, d, sha = line.split()
            day, lines = int(d), []
        else:
            lines.append(line)
    close()
    return out


def digest_fields(text: str) -> dict[str, str]:
    """A digest text as {key: value} (keys are unique; values keep their IEEE-bit hex)."""
    out = {}
    for line in text.splitlines():
        key, _, value = line.partition("=")
        if key in out:
            raise TraceError(f"digest key {key} repeated")
        out[key] = value
    return out


def set_signature(ids: Iterable[str]) -> str:
    """``SimulationTrace.SetSignature``: sha256 of the sorted identities, one per line; "" for none."""
    ordered = sorted(ids, key=lambda s: s.encode("utf-16-be"))
    return hashlib.sha256(("\n".join(ordered) + "\n").encode("utf-8")).hexdigest() if ordered else ""
