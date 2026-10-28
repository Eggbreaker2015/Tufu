"""Shared pytest setup for Balance/tests.

* ``--unity``: run tests marked ``unity`` (they read Unity run outputs; the session fixtures that
  find or produce those outputs live in ``tests/unity/conftest.py``). Without it they are skipped.
* Inputs (design 1.5): the extracted database, ``tests/golden/fixtures/*.json`` and Unity run
  outputs. The database is ``$TUFU_BALANCE_DB`` if set, else ``Balance/out/balance.sqlite``;
  tests that need it are skipped when it is missing. It is always opened read-only.
"""

from __future__ import annotations

import json
import os
import sqlite3
from pathlib import Path

import pytest

BALANCE = Path(__file__).resolve().parents[1]
TESTS = BALANCE / "tests"
FIXTURES = TESTS / "golden" / "fixtures"


def pytest_addoption(parser):
    parser.addoption("--unity", action="store_true", default=False,
                     help="run tests marked 'unity' (may start Unity in the run worktree)")


def pytest_collection_modifyitems(config, items):
    if config.getoption("--unity"):
        return
    skip = pytest.mark.skip(reason="needs Unity run outputs: pass --unity")
    for item in items:
        if "unity" in item.keywords:
            item.add_marker(skip)


def balance_db_path() -> Path:
    return Path(os.environ.get("TUFU_BALANCE_DB") or BALANCE / "out" / "balance.sqlite")


@pytest.fixture(scope="session")
def db_path() -> Path:
    path = balance_db_path()
    if not path.exists():
        pytest.skip(f"no extracted database at {path}: run `python -m tufu_balance.extract` or set TUFU_BALANCE_DB")
    return path


@pytest.fixture(scope="session")
def db(db_path):
    con = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
    con.row_factory = sqlite3.Row
    yield con
    con.close()


@pytest.fixture(scope="session")
def fixtures_dir() -> Path:
    return FIXTURES


@pytest.fixture(scope="session")
def load_fixture():
    """``load_fixture("stage4_slot_schedule")`` -> parsed ``tests/golden/fixtures/<name>.json``."""
    cache = {}

    def load(name: str):
        if name not in cache:
            path = FIXTURES / (name if name.endswith(".json") else f"{name}.json")
            if not path.exists():
                pytest.fail(f"fixture {path} missing (fixtures are frozen by W0)")
            cache[name] = json.loads(path.read_text(encoding="utf-8"))
        return cache[name]

    return load
