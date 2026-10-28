"""Golden baselines: the repository guard, and self-tests of the store on scratch directories."""

from __future__ import annotations

import json
import math

import pytest

from tufu_balance.golden import (AcceptRefused, GoldenError, GoldenMismatch, GoldenStore, GoldenTightenWarning,
                                 at_least, at_most, exact, is_looser, same_set)
from tufu_balance.golden.__main__ import main as golden_main


def test_repository_current_not_looser_than_original():
    """Every CURRENT expectation is as tight as ORIGINAL, or its loosening is registered."""
    violations = GoldenStore().guard()
    assert not violations, "\n".join(map(str, violations))


# -- self-tests --------------------------------------------------------------------------------


@pytest.fixture
def store(tmp_path):
    return GoldenStore(tmp_path / "golden", tmp_path / "latest")


def seed(store, suite="demo", **results):
    """Record results and accept them as the first baseline (seeds original and current)."""
    store.record(suite, results)
    store.accept(suite, "initial baseline", rev="r0")


def write_current(store, suite, **changes):
    current = store.load("current", suite)
    current.update(changes)
    store.save("current", suite, current)


def test_guard_fails_on_unregistered_loosening(store):
    seed(store, count=at_least(701), err=at_most(0.2283))
    write_current(store, "demo", count=at_least(700), err=at_most(0.25))
    assert sorted(v.key for v in store.guard()) == ["count", "err"]


def test_guard_passes_registered_loosening_with_reason(store):
    seed(store, count=at_least(701))
    store.record("demo", {"count": at_least(690)})
    store.accept("demo", "port of island_timeline fixes a stage-4 rounding bug", loosen=True, rev="r1")
    assert store.guard() == []
    [rec] = store.loosening("demo")
    assert (rec["key"], rec["from"], rec["to"], rec["git_rev"]) == ("count", 701, 690, "r1")
    assert rec["reason"] and rec["date"]
    # tightening again after a registered loosening stays covered
    write_current(store, "demo", count=at_least(695))
    assert store.guard() == []
    # a record without a reason does not count
    path = store.root / "loosening" / "demo.jsonl"
    path.write_text(json.dumps({**rec, "reason": " "}) + "\n")
    assert [v.key for v in store.guard()] == ["count"]


def test_guard_allows_tightening(store):
    seed(store, count=at_least(701), err=at_most(0.2283), hits=exact(79, better="higher"),
         misses=same_set(["a", "b", "c"], better="subset"))
    write_current(store, "demo", count=at_least(705), err=at_most(0.2), hits=exact(80, better="higher"),
                  misses=same_set(["a", "b"], better="subset"))
    assert store.guard() == []


def test_guard_fails_when_exact_value_changes(store):
    seed(store, itd=exact(84), exceptions=same_set([1, 2, 3]))
    write_current(store, "demo", itd=exact(85))
    assert [v.key for v in store.guard()] == ["itd"]
    write_current(store, "demo", itd=exact(84), exceptions=same_set([1, 2, 4]))
    assert [v.key for v in store.guard()] == ["exceptions"]
    # changing the kind (exact -> ratchet) and dropping a key are loosenings too
    write_current(store, "demo", exceptions=same_set([1, 2, 3]), itd=at_least(84))
    assert [v.key for v in store.guard()] == ["itd"]
    current = store.load("current", "demo")
    del current["itd"]
    store.save("current", "demo", current)
    assert [(v.key, v.current) for v in store.guard()] == [("itd", None)]


def test_accept_refuses_looser_value_without_loosen(store):
    seed(store, count=at_least(701), itd=exact(84))
    before = store.path("current", "demo").read_bytes()
    store.record("demo", {"count": at_least(690), "itd": exact(84)})
    with pytest.raises(AcceptRefused) as e:
        store.accept("demo", "worse", rev="r1")
    assert e.value.keys == ["count"]
    assert store.path("current", "demo").read_bytes() == before
    assert store.loosening("demo") == []
    assert golden_main(["accept", "demo", "--reason", "worse"], store=store) == 2
    # a value that is tighter than original needs no --loosen
    store.record("demo", {"count": at_least(702)})
    res = store.accept("demo", "tighter", keys=["count"], rev="r2")
    assert res.changed == ["count"] and res.loosened == []
    assert store.load("original", "demo")["count"].value == 701


def test_accept_requires_reason_and_records(store):
    store.record("demo", {"a": exact(1)})
    with pytest.raises(GoldenError):
        store.accept("demo", "  ")
    res = store.accept("demo", "first", rev="r0")
    assert res.seeded_original == ["a"]
    store.record("demo", {"a": exact(1), "b": exact([1.5, math.nan])})
    res = store.accept("demo", "add b", rev="r1")
    assert (res.changed, res.seeded_original) == (["b"], ["b"])
    log = store.accepted("demo")
    assert [r["reason"] for r in log] == ["first", "add b"]
    [change] = log[1]["changes"]
    assert change["key"] == "b" and change["from"] is None and change["to"]["value"][0] == 1.5
    assert math.isnan(change["to"]["value"][1])
    # dropping is a loosening
    with pytest.raises(AcceptRefused):
        store.accept("demo", "drop b", drop=["b"])
    store.accept("demo", "b moved to another suite", drop=["b"], loosen=True, rev="r2")
    assert "b" not in store.load("current", "demo") and store.guard() == []


def test_check_compares_strictly_with_current(store):
    seed(store, n=exact(84), better_n=exact(79, better="higher"), lo=at_least(10), hi=at_most(0.5),
         s=same_set(["x", "y"]))
    store.check("demo", {"n": 84, "better_n": exact(79, better="higher"), "lo": at_least(10),
                         "hi": at_most(0.5), "s": same_set(["y", "x"])})
    with pytest.raises(GoldenMismatch, match="better_n: expected exactly 79, measured 80"):
        store.check("demo", {"better_n": exact(80, better="higher")})  # better still fails
    with pytest.raises(GoldenMismatch, match=r"added \[\"z\"\]; removed \[\"x\"\]"):
        store.check("demo", {"s": same_set(["y", "z"])})
    with pytest.raises(GoldenMismatch, match="ratchet >= 10"):
        store.check("demo", {"lo": at_least(9)})
    with pytest.raises(GoldenMismatch, match="no current baseline"):
        store.check("demo", {"new_key": 1})
    with pytest.warns(GoldenTightenWarning, match="lo"):
        store.check("demo", {"lo": at_least(12), "hi": at_most(0.5)})
    # the measured values were recorded for accept
    assert store.latest("demo")["lo"].value == 12 and store.latest("demo")["new_key"].value == 1


def test_is_looser_directions():
    assert not is_looser(exact(1), exact(1.0))
    assert is_looser(exact(1, better="higher"), exact(0, better="higher"))
    assert not is_looser(same_set([1, 2], better="superset"), same_set([1, 2, 3], better="superset"))
    assert is_looser(same_set([1, 2], better="superset"), same_set([2, 3], better="superset"))
    assert is_looser(at_least(1.0), at_least(math.nan))
    assert is_looser(exact(1), None)


def test_file_suite_byte_comparison(store):
    with pytest.raises(GoldenMismatch, match="no current baseline"):
        store.check_file("sim_run", "daily.csv", b"day,kills\n0,3\n")
    assert store.accept_files("sim_run", "first K3 run", rev="r0") == ["daily.csv"]
    store.check_file("sim_run", "daily.csv", b"day,kills\n0,3\n")
    with pytest.raises(GoldenMismatch, match="bytes differ"):
        store.check_file("sim_run", "daily.csv", b"day,kills\n0,4\n")


def test_rebaseline_original_and_cli(store, capsys):
    seed(store, count=at_least(701))
    store.record("demo", {"count": at_least(720)})
    assert store.rebaseline_original("demo", "re-extracted after keyData fix", rev="r1") == ["count"]
    assert [v.key for v in store.guard()] == ["count"]  # current (701) is now looser than original (720)
    assert golden_main(["guard"], store=store) == 1
    assert golden_main(["status", "--all"], store=store) == 1
    out = capsys.readouterr().out
    assert "VIOLATION" in out and "demo: 1 key(s)" in out
    assert golden_main(["accept", "demo", "--reason", "catch up"], store=store) == 0
    assert golden_main(["guard"], store=store) == 0
