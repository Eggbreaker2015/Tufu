"""Golden baselines: the repository guard, and self-tests of the store on scratch directories."""

from __future__ import annotations

import json
import math

import pytest

from tufu_balance.golden import (AcceptRefused, GoldenError, GoldenMismatch, GoldenStore, GoldenTightenWarning,
                                 at_least, at_most, exact, is_looser, same_set)
from tufu_balance.golden import store as store_mod
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
    """Record results and accept them as the first baseline (freezes them as original too)."""
    store.record(suite, results)
    store.accept(suite, "initial baseline", rev="r0", seed_original=True)


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
    res = store.accept("demo", "first", rev="r0", seed_original=True)
    assert res.seeded_original == ["a"]
    store.record("demo", {"a": exact(1), "b": exact([1.5, math.nan], original=[1.5, math.nan])})
    res = store.accept("demo", "add b", rev="r1")
    assert (res.changed, res.seeded_original) == (["b"], ["b"])
    log = store.accepted("demo")
    assert [r["reason"] for r in log] == ["first", "add b"]
    assert [s["from"] for r in log for s in r["seeded_original"]] == ["measured", "declared"]
    [change] = log[1]["changes"]
    assert change["key"] == "b" and change["from"] is None and change["to"]["value"][0] == 1.5
    assert math.isnan(change["to"]["value"][1])
    # dropping is a loosening
    with pytest.raises(AcceptRefused):
        store.accept("demo", "drop b", drop=["b"])
    store.accept("demo", "b moved to another suite", drop=["b"], loosen=True, rev="r2")
    assert "b" not in store.load("current", "demo") and store.guard() == []


def test_original_is_seeded_only_from_declaration_or_seed_original(store, capsys):
    """A first measurement that misses the design's number cannot become ORIGINAL silently."""
    store.record("timeline", {"G01": exact(83, original=84), "G03": at_least(705, original=701),
                              "R2b": at_most(0.31)})
    # no declared original: refused unless --seed-original, nothing written
    with pytest.raises(AcceptRefused) as e:
        store.accept("timeline", "first run", keys=["G03", "R2b"])
    assert e.value.keys == ["R2b"] and not store.path("original", "timeline").exists()
    # declared 84, measured 83: looser than the design's original
    with pytest.raises(AcceptRefused) as e:
        store.accept("timeline", "first run", keys=["G01", "G03"])
    assert e.value.keys == ["G01"] and not store.path("current", "timeline").exists()
    assert golden_main(["accept", "timeline", "--reason", "first run"], store=store) == 2
    # tighter than declared: original keeps the design's number, current the measurement
    res = store.accept("timeline", "first run", keys=["G03"])
    assert res.seeded_original == ["G03"] and res.loosened == []
    assert store.load("original", "timeline")["G03"].value == 701
    assert store.load("current", "timeline")["G03"].value == 705
    assert golden_main(["accept", "timeline", "--keys", "R2b", "--seed-original", "--reason", "R2b: measured on "
                        "first run (design 3.3)"], store=store) == 0
    assert store.load("original", "timeline")["R2b"].value == 0.31
    # G01 only with a registered loosening from the declared 84
    res = store.accept("timeline", "stage-4 script miscounted the tail", keys=["G01"], loosen=True)
    [rec] = store.loosening("timeline")
    assert (rec["key"], rec["from"], rec["to"]) == ("G01", 84, 83)
    assert store.guard() == []
    # a test whose declared original disagrees with original/ fails, and accept refuses it
    with pytest.raises(GoldenMismatch, match="declares original 700"):
        store.check("timeline", {"G03": at_least(705, original=700)})
    with pytest.raises(GoldenError, match="declared original differs"):
        store.accept("timeline", "x", keys=["G03"])
    store.check("timeline", {"G03": at_least(705, original=701)})


def test_accept_refuses_results_from_another_commit(store, monkeypatch):
    monkeypatch.setattr(store_mod, "_REV", "0" * 40)
    store.record("demo", {"a": exact(1, original=1)})
    with pytest.raises(GoldenMismatch, match="no current baseline"):
        store.check_file("sim_run", "daily.csv", b"day\n0\n")
    monkeypatch.setattr(store_mod, "_REV", None)
    with pytest.raises(AcceptRefused, match="another commit") as e:
        store.accept("demo", "old run")
    assert e.value.keys == ["a"]
    with pytest.raises(AcceptRefused, match="another commit"):
        store.accept_files("sim_run", "old run")
    assert golden_main(["accept", "demo", "--reason", "old run"], store=store) == 2
    assert store.accept("demo", "checked by hand", allow_stale=True).changed == ["a"]


def test_new_session_drops_earlier_results(store, monkeypatch):
    monkeypatch.setenv("TUFU_GOLDEN_RUN", "session-1")
    store.record("demo", {"old_key": exact(1), "a": exact(1)})
    with pytest.raises(GoldenMismatch):
        store.check_file("sim_run", "old.csv", b"x")
    monkeypatch.setenv("TUFU_GOLDEN_RUN", "session-2")
    store.record("demo", {"a": exact(2)})
    store.record("demo", {"b": exact(3)})
    assert {k: v.value for k, v in store.latest("demo").items()} == {"a": 2, "b": 3}
    with pytest.raises(GoldenMismatch):
        store.check_file("sim_run", "daily.csv", b"y")
    assert sorted(p.name for p in (store.latest_dir / "files" / "sim_run").iterdir()) == ["daily.csv"]


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
    store.record("demo", {"count": at_least(725, original=720)})
    assert store.rebaseline_original("demo", "re-extracted after keyData fix", rev="r1") == ["count"]
    assert store.load("original", "demo")["count"].value == 720  # the declared number, not the measurement
    assert [v.key for v in store.guard()] == ["count"]  # current (701) is now looser than original (720)
    assert golden_main(["guard"], store=store) == 1
    assert golden_main(["status", "--all"], store=store) == 1
    out = capsys.readouterr().out
    assert "VIOLATION" in out and "demo: 1 key(s)" in out
    assert golden_main(["accept", "demo", "--reason", "catch up"], store=store) == 0
    assert golden_main(["guard"], store=store) == 0
