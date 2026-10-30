"""Spawner identity between the extracted data and the running game (design 1.5 item 5, 3.4; unit C1).

Python names a spawner by ``SpawnerId = (scene, file_id)``; the C# side has no file id and names it by
``"<scene name>:<hierarchy path>|<Key>"`` (``SimulationHarness.HierarchyPath`` plus ``Config.Key``). Key
alone is not unique (717 rows, 716 keys). The map is built from the ``spawners`` table: the runtime path
is the scene file's stem, ``:``, and ``go_path``. A spawner registers in ``EnemiesModel.Spawners`` in
``EnemySpawner.Start`` (``CodeBase/Enemy/EnemySpawner.cs:513-518``), which runs only for an enabled
behaviour on a GameObject active in the hierarchy; the predicted registered set is checked against
every run's ``ready_spawners.csv`` (tests/unity/test_export_roundtrip.py).

The launcher uses :func:`expected_ready_csv` to turn P6's ``expected_ready_spawners(variant)`` into the
readiness table the harness compares (exit code 8 on a mismatch).
"""

from __future__ import annotations

import csv
import hashlib
import io
import sqlite3
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Mapping

SpawnerId = tuple[str, int]


def runtime_path(scene: str, go_path: str) -> str:
    """``SimulationHarness.HierarchyPath``: ``<scene name>:<root>/<...>/<object>``."""
    return f"{Path(scene).stem}:{go_path}"


def runtime_id(path: str, key: str) -> str:
    return f"{path}|{key}"


@dataclass(frozen=True)
class SpawnerIdentity:
    scene: str
    file_id: int
    go_path: str
    key: str
    active: bool    # GameObject active in hierarchy
    enabled: bool   # EnemySpawner behaviour enabled

    @property
    def spawner_id(self) -> SpawnerId:
        return (self.scene, self.file_id)

    @property
    def path(self) -> str:
        return runtime_path(self.scene, self.go_path)

    @property
    def runtime_id(self) -> str:
        return runtime_id(self.path, self.key)

    @property
    def registers(self) -> bool:
        return self.active and self.enabled


class IdentityMap:
    """Both directions; ``runtime_id`` is unique (checked on construction)."""

    def __init__(self, entries: Iterable[SpawnerIdentity]):
        self.entries = sorted(entries, key=lambda e: e.spawner_id)
        self.by_spawner_id = {e.spawner_id: e for e in self.entries}
        self.by_runtime_id: dict[str, SpawnerIdentity] = {}
        for e in self.entries:
            if e.runtime_id in self.by_runtime_id:
                raise ValueError(f"runtime id {e.runtime_id!r} is not unique: {self.by_runtime_id[e.runtime_id].spawner_id}, "
                                 f"{e.spawner_id}")
            self.by_runtime_id[e.runtime_id] = e

    def __len__(self) -> int:
        return len(self.entries)

    def registered(self) -> list[SpawnerIdentity]:
        return [e for e in self.entries if e.registers]

    def unregistered(self) -> list[SpawnerIdentity]:
        """The difference set: rows of the scene that never register at runtime."""
        return [e for e in self.entries if not e.registers]

    def resolve(self, path: str, key: str) -> SpawnerId:
        return self.by_runtime_id[runtime_id(path, key)].spawner_id

    def compare_runtime(self, runtime: Iterable[tuple[str, str]]) -> dict:
        """Compare the runtime-registered ``(path, key)`` pairs with the predicted registered set."""
        counts = Counter(runtime_id(p, k) for p, k in runtime)
        seen_set = set(counts)
        predicted = {e.runtime_id for e in self.registered()}
        return {
            "runtime": sum(counts.values()),
            "runtime_duplicates": sorted(s for s, n in counts.items() if n > 1),
            "unknown": sorted(s for s in seen_set if s not in self.by_runtime_id),
            "registered_but_predicted_not": sorted(s for s in seen_set if s in self.by_runtime_id and s not in predicted),
            "predicted_but_not_registered": sorted(predicted - seen_set),
        }

    def to_csv(self) -> str:
        buf = io.StringIO()
        w = csv.writer(buf, lineterminator="\n")
        w.writerow(["scene", "file_id", "path", "key", "active", "enabled", "registers"])
        for e in self.entries:
            w.writerow([e.scene, e.file_id, e.path, e.key, int(e.active), int(e.enabled), int(e.registers)])
        return buf.getvalue()


def load_identity_map(con: sqlite3.Connection) -> IdentityMap:
    rows = con.execute("SELECT scene, file_id, go_path, Key, active, enabled FROM spawners ORDER BY scene, file_id")
    return IdentityMap(SpawnerIdentity(scene=str(r[0]), file_id=int(r[1]), go_path=str(r[2]), key=str(r[3]),
                                       active=bool(r[4]), enabled=bool(r[5])) for r in rows)


# ---------------------------------------------------------------- readiness table (P6 -> harness)

EXPECTED_COLUMNS = ("path", "key", "charges_total", "base_cooldown", "scene", "file_id")


def expected_ready_csv(con: sqlite3.Connection, variant: str = "A", ready: Mapping | None = None,
                       identity: IdentityMap | None = None) -> str:
    """P6's ``expected_ready_spawners(variant)`` keyed by the C# identity, for ``sandbox.expected_ready_spawners``.

    ``SimulationHarness.CompareExpectedSpawners`` reads ``path``, ``key``, ``charges_total``,
    ``base_cooldown`` (float32 text; ``repr`` of the widened double parses back to the same float).
    """
    from ..model.spawners import expected_ready_spawners

    identity = identity or load_identity_map(con)
    ready = ready if ready is not None else expected_ready_spawners(variant, con=con)
    buf = io.StringIO()
    w = csv.writer(buf, lineterminator="\n")
    w.writerow(EXPECTED_COLUMNS)
    for sid in sorted(ready):
        r = ready[sid]
        e = identity.by_spawner_id[sid]
        if not e.registers:
            raise ValueError(f"P6 expects {sid} at readiness but it does not register at runtime")
        w.writerow([e.path, e.key, r.charges_total, repr(r.base_cooldown), e.scene, e.file_id])
    return buf.getvalue()


def expected_ready_file_name(text: str) -> str:
    """Content-addressed name, so the run_key changes with the table."""
    return f"ready_spawners.expected.{hashlib.sha256(text.encode()).hexdigest()[:16]}.csv"
