"""Partial split of SimulationSystem (design 0, 1.5, 4.1, 7.3 C3a, 7.4) and the engine bodies (7.3 C3b).

* The semantic signature lock (cscheck, P0) is identical to 6af011f; new members are Rebuild* and
  private/internal: the partial hooks of 7.4 (C3a) and the engine members of C3b.
* Every empty method of 6af011f moved (declaration verbatim) into its module's partial file; the recovered
  bodies stay in SimulationSystem.cs, which is the 6af011f text minus the moved methods plus ``partial``.
  Only the methods C3b, C5 (.FastTick.cs) and C7 filled (``FILLED``) have new bodies. SimulationConfig gains ``partial``, annotations
  and the bodies of Tick and GetTrimmedTimeChunkSnapshots.
* Every empty method of 6af011f carries one ``[SimulationRebuild]``: ``FILLED`` gives the provenance of the
  methods C3b, C5 and C7 filled; the rest stay ``Provenance.Null | Provenance.DebugOnly`` until their unit fills them.
* The hooks are declared in .Rebuild.cs; the cross-module call sites of 7.4 are in place (C3b).

Source structure is read with Roslyn (tools/csmembers, syntax only); semantics with cscheck.
"""

from __future__ import annotations

import fcntl
import json
import re
import subprocess
from pathlib import Path

import pytest

from tufu_balance import cscheck
from tufu_balance.model import registry

WORKTREE = cscheck.WORKTREE
PROJECT = WORKTREE / "Recovery" / "StudyProject"
SIM_DIR = PROJECT / "Assets" / "Scripts" / "Assembly-CSharp" / "CodeBase" / "Simulation"
SYSTEMS = SIM_DIR / "Systems"
SS = SYSTEMS / "SimulationSystem.cs"
SC = SIM_DIR / "SimulationConfig.cs"
HARNESS = SIM_DIR / "Harness" / "SimulationHarness.cs"
ATTRIBUTE = SIM_DIR / "Rebuild" / "SimulationRebuildAttribute.cs"
REV = cscheck.SIGNATURE_REV

TOOL_DIR = cscheck.BALANCE / "tools" / "csmembers"
TOOL_DLL = TOOL_DIR / "bin" / "Release" / "net8.0" / "csmembers.dll"
TOOL_SOURCES = ("csmembers.csproj", "Program.cs", "packages.lock.json")

SIM = "Codebase.Simulation.Systems.SimulationSystem"

# Design 1.2 / 4.1: module file of every empty SimulationSystem method of 6af011f.
MODULES = {
    "Run": ["PrepareSimulationRun", "ExecuteTick", "Tick", "CheckSimulationStopConditions", "UpdateSpawnerChargesChunk",
            "ResetCooldowns", "RecalculateChunkRpmValues", "GetSpawnerChargesPerClear",
            "UpdateInterpolationStatModifier", "GetSimulationInterpolationCoefficient",
            "GetSimulationSnapshotStatValue", "UsesInterpolationStats", "GetForecastUnlockedLocationNumber",
            "ConvertIslandTimingToDays", "GetNextLocationInterpolationTiming"],
    "Lifecycle": ["CheckLifeCycle", "NextLifeCycle", "ProcessAFK", "ApplyRegen", "ProcessSimulateAds",
                  "CalculateBestRewardStat"],
    "Snapshot": ["MakeSnapshot"],
    "Amulet": ["ResetAmuletAutoBalance", "ProcessAmuletAutoBalanceTargets", "ProcessCrystalSimulationChunk",
               "UpgradeCrystalsGreedy"],
    "FastTick": ["FastTick", "SimulateFightWithSpawnerFast", "AccumulateFastTickSpawnerRewards",
                 "FlushFastTickChunkRewards"],
    "Inventory": ["ProcessInventory", "GetSpawnerItemRewardCopies", "GetItemEmissionCoefficient",
                  "IsPreferredItemEmissionType", "RollFractionalCount", "GetChainedItemBaseCount",
                  "UnlockSimulationWeaponSlot", "TryApplyArmorBaseAutoBalance", "ApplyAutoBalanceForEvolution"],
    "Targeting": ["FindBestSpawner", "CalculateStatBenefit", "ModifyFromItem", "CalculateFarmPotencial",
                  "GetFightEfficiency", "GetTotalDPS", "GetTotalArmor", "CalculateSpawnerPaths",
                  "GetBestSpawnerForInfo", "TryGetSpawnerPriority", "DrawSquare"],
    "Fight": ["SimulateFightWithSpawner", "ProcessDeath", "CalcDifficultSpawner", "LogSpawnerClear",
              "GetUnlockTimingLogData"],
}
PARTIAL_FILES = sorted([*MODULES, "Rebuild", "Replay", "Trace"])  # Trace: C3c's daily records, events, digests
# Crosscheck: P11's DP8 export of spawnerData after an override (called from .Replay.cs)
PARTIAL_FILES = sorted([*PARTIAL_FILES, "Crosscheck"])
CONFIG_STUBS = {"ResetCombatConfigValues", "Tick", "ResetAmuletLevelBalance", "SetAmuletLevelBalance",
                "GetTrimmedTimeChunkSnapshots"}
# Design 4.1 "debug" rows; every other empty method starts as Provenance.Null.
DEBUG_ONLY = {"GetNextLocationInterpolationTiming", "GetBestSpawnerForInfo", "TryGetSpawnerPriority", "DrawSquare",
              "CalcDifficultSpawner", "LogSpawnerClear", "GetUnlockTimingLogData"}
# Design 7.4: hook -> file of its (single) implementation, None while no unit has implemented it.
HOOKS = {
    "RebuildApplyDayOverride(int)": "Replay",
    "RebuildApplyReplayCaches()": "Replay",
    "RebuildOnTickCompleted()": "Rebuild",
    "RebuildResetForJob(ref bool)": None,
}
NEW_MEMBERS = {
    "method:RebuildApplyDayOverride(int)": "private",
    "method:RebuildApplyReplayCaches()": "private",
    "method:RebuildOnTickCompleted()": "private",
    "method:RebuildResetForJob(ref bool)": "private",
    "method:RebuildTryResetForJob()": "internal",
    # C3b: the write point, unlock, gate flush, boss lookup, fast-day advance (design 4.5, 7.4)
    "method:RebuildApplyClear(Codebase.Enemy.EnemySpawner, int, bool)": "private",
    "method:RebuildUnlockLocation(int)": "private",
    "method:RebuildFlushFastTickGates()": "private",
    "method:RebuildFindBoss(int)": "private",
    "method:RebuildAdvanceFastDay(float)": "private",
    "method:RebuildManifest()": "internal",
    "property:RebuildFailure": "internal",
    "property:RebuildFailureExitCode": "internal",
    "property:RebuildLastTickWasFast": "internal",
    # C3c: the harness flushes the open day of the trace at the job's end
    "method:RebuildTraceFlush()": "internal",
    # C7: what SpawnerData's priorities read from the system
    "method:RebuildPriorityFamily()": "internal",
    "method:RebuildStaticRewardValue(Codebase.Enemy.EnemySpawner)": "internal",
    "method:RebuildIsCurrentBoss(Codebase.Enemy.EnemySpawner)": "internal",
}
# Design 4.1: provenance of the empty methods C3b filled (SimulationSystem and SimulationConfig).
FILLED = {
    "PrepareSimulationRun": "Inferred", "ExecuteTick": "Inferred", "Tick": "Inferred",
    "CheckSimulationStopConditions": "Inferred", "UpdateSpawnerChargesChunk": "Inferred", "ResetCooldowns": "Inferred",
    "RecalculateChunkRpmValues": "Inferred", "GetSpawnerChargesPerClear": "Inferred",
    "UpdateInterpolationStatModifier": "Inferred", "GetSimulationInterpolationCoefficient": "Inferred",
    "GetSimulationSnapshotStatValue": "Inferred", "UsesInterpolationStats": "Inferred",
    "GetForecastUnlockedLocationNumber": "Inferred", "ConvertIslandTimingToDays": "Inferred",
    "GetNextLocationInterpolationTiming": "DebugOnly",
    "CheckLifeCycle": "Inferred", "NextLifeCycle": "Inferred", "ProcessAFK": "Inferred", "ApplyRegen": "Inferred",
    "ProcessSimulateAds": "Inferred", "CalculateBestRewardStat": "Inferred", "MakeSnapshot": "Inferred",
    # null modules: fail the job when switched on (design 4.5)
    "ResetAmuletAutoBalance": "Null", "ProcessAmuletAutoBalanceTargets": "Null", "ProcessCrystalSimulationChunk": "Null",
    "UpgradeCrystalsGreedy": "Null",
    # C5 (.FastTick.cs, design 4.1): planning rounds and active minutes calibrated, the kill gate, accumulation and
    # flush inferred
    "FastTick": "Calibrated", "FlushFastTickChunkRewards": "Inferred", "SimulateFightWithSpawnerFast": "Inferred",
    "AccumulateFastTickSpawnerRewards": "Inferred",
    # C7: the slow policy (.Targeting.cs, .Fight.cs); GetFightEfficiency, GetTotalDPS, GetTotalArmor,
    # CalculateFarmPotencial, ModifyFromItem and DrawSquare stay empty (unused by the rebuilt policy)
    "FindBestSpawner": "Calibrated", "CalculateStatBenefit": "Calibrated", "CalculateSpawnerPaths": "Calibrated",
    "GetBestSpawnerForInfo": "DebugOnly", "TryGetSpawnerPriority": "DebugOnly",
    "SimulateFightWithSpawner": "Calibrated", "ProcessDeath": "Calibrated", "CalcDifficultSpawner": "DebugOnly",
    "LogSpawnerClear": "DebugOnly", "GetUnlockTimingLogData": "DebugOnly",
}
FILLED_CONFIG = {"Tick": "Inferred", "GetTrimmedTimeChunkSnapshots": "Inferred"}
# Design 7.4: every cross-module call site (caller -> callee), placed by C3b.
CALL_SITES = [
    ("PrepareSimulationRun", "RebuildApplyDayOverride"), ("UpdateSpawnerChargesChunk", "RebuildApplyDayOverride"),
    ("Tick", "RebuildApplyReplayCaches"), ("Tick", "CalculateStatBenefit"), ("Tick", "FindBestSpawner"),
    ("Tick", "SimulateFightWithSpawner"), ("SimulateFightWithSpawner", "RebuildApplyClear"),
    ("SimulateFightWithSpawnerFast", "RebuildApplyClear"), ("RebuildApplyClear", "GetSpawnerItemRewardCopies"),
    ("RebuildApplyClear", "ProcessInventory"), ("RebuildApplyClear", "AccumulateFastTickSpawnerRewards"),
    ("RebuildUnlockLocation", "UnlockSimulationWeaponSlot"), ("RebuildAdvanceFastDay", "FlushFastTickChunkRewards"),
    ("FlushFastTickChunkRewards", "RollFractionalCount"), ("FlushFastTickChunkRewards", "ProcessInventory"),
    ("ExecuteTick", "RebuildOnTickCompleted"), ("RebuildOnTickCompleted", "OnTickCompleted"),
    ("FastTick", "RebuildAdvanceFastDay"), ("ExecuteTick", "FastTick"), ("ExecuteTick", "Tick"),
    ("UpdateSpawnerChargesChunk", "RecalculateChunkRpmValues"), ("UpdateSpawnerChargesChunk", "UpdateInterpolationStatModifier"),
    ("UpdateSpawnerChargesChunk", "MakeSnapshot"), ("PrepareSimulationRun", "MakeSnapshot"),
]
REBUILD_ATTR = re.compile(r"^(?:Codebase\.Simulation\.Rebuild\.)?SimulationRebuild(?:Attribute)?$")


def _dotnet_available():
    try:
        cscheck.dotnet()
    except cscheck.CsCheckError:
        return False
    return True


def _rsp_available():
    return any((cscheck.run_worktree() / "Recovery" / "StudyProject" / "Library" / "Bee" / "artifacts")
               .glob("*/Assembly-CSharp.rsp"))


pytestmark = pytest.mark.skipif(not _dotnet_available(), reason="needs the .NET 8 SDK")
needs_rsp = pytest.mark.skipif(not _rsp_available(), reason="needs the run worktree's Library/Bee .rsp")


def _tool():
    def stale():
        return not TOOL_DLL.exists() or any((TOOL_DIR / n).stat().st_mtime > TOOL_DLL.stat().st_mtime
                                            for n in TOOL_SOURCES)

    if stale():
        (TOOL_DIR / "obj").mkdir(exist_ok=True)
        with open(TOOL_DIR / "obj" / ".build.lock", "w") as lock:
            fcntl.flock(lock, fcntl.LOCK_EX)
            if stale():
                proc = subprocess.run([cscheck.dotnet(), "build", str(TOOL_DIR / "csmembers.csproj"), "-c", "Release",
                                       "--nologo", "-v", "q", "-p:RestoreLockedMode=true"],
                                      capture_output=True, text=True)
                assert proc.returncode == 0, proc.stdout[-4000:] + proc.stderr[-2000:]
                TOOL_DLL.touch()
    return TOOL_DLL


def members_of(files, *types):
    """csmembers dump of `types` in `files`; each member's "file" is the file name and "text" its source."""
    args = [cscheck.dotnet(), str(_tool())]
    for t in types:
        args += ["--type", t]
    proc = subprocess.run([*args, "--define", "UNITY_EDITOR", *map(str, files)], capture_output=True, text=True)
    assert proc.returncode == 0, proc.stderr
    doc = json.loads(proc.stdout)
    texts = {str(f): Path(f).read_text(encoding="utf-8") for f in files}
    for m in doc["members"]:
        m["text"] = texts[m["file"]][m["full_span"][0]:m["full_span"][1]]
        m["file"] = Path(m["file"]).name
    for t in doc["types"]:
        t["file"] = Path(t["file"]).name
    return doc


def methods(doc, type_name):
    return [m for m in doc["members"] if m["type"] == type_name and m["kind"] == "MethodDeclaration"]


def is_stub(m):
    """Empty method of the decompiled source: placeholder statements only and no recovered-body marker."""
    return m["trivial_body"] and not any("ELF" in c for c in m["comments"])


def rebuild_attributes(m):
    return [a for a in m["attributes"] if REBUILD_ATTR.match(a["name"])]


@pytest.fixture(scope="module")
def original(tmp_path_factory):
    tmp = tmp_path_factory.mktemp("orig")
    files = []
    for path in (SS, SC):
        rel = path.relative_to(WORKTREE).as_posix()
        out = tmp / path.name
        out.write_bytes(subprocess.run(["git", "-C", str(WORKTREE), "show", f"{REV}:{rel}"],
                                       capture_output=True, check=True).stdout)
        files.append(out)
    doc = members_of(files, "SimulationSystem", "SimulationConfig")
    doc["sources"] = {p.name: p.read_text(encoding="utf-8") for p in files}
    return doc


@pytest.fixture(scope="module")
def current():
    files = [SS, *(SYSTEMS / f"SimulationSystem.{m}.cs" for m in PARTIAL_FILES), SC]
    return members_of(files, "SimulationSystem", "SimulationConfig")


# ---------------------------------------------------------------- semantic lock (cscheck)


@needs_rsp
def test_cscheck_signature_lock_has_no_violations():
    result = cscheck.check(steps=("signature",))
    assert result["signature"]["violations"] == []
    assert result["ok"], result["failures"]


@needs_rsp
def test_locked_members_identical_and_only_rebuild_members_added(tmp_path):
    out = tmp_path / "signature_now.json"
    proc = subprocess.run([cscheck.dotnet(), str(cscheck.ensure_built()), "signature", "--worktree", str(WORKTREE),
                           "--run-worktree", str(cscheck.run_worktree()), "--out", str(out)],
                          capture_output=True, text=True)
    assert proc.returncode == 0, proc.stderr
    locked = json.loads(cscheck.SIGNATURE.read_text(encoding="utf-8"))
    now = json.loads(out.read_text(encoding="utf-8"))
    assert [t["name"] for t in now["types"]] == [t["name"] for t in locked["types"]]
    added = {}
    for lt, nt in zip(locked["types"], now["types"]):
        assert {k: v for k, v in nt.items() if k != "members"} == {k: v for k, v in lt.items() if k != "members"}
        old = {m["key"]: m for m in lt["members"]}
        new = {m["key"]: m for m in nt["members"]}
        assert old.keys() <= new.keys(), sorted(old.keys() - new.keys())
        for key, m in old.items():
            assert new[key] == m, key
        added.update({(nt["name"], k): new[k] for k in new.keys() - old.keys()})
    assert set(NEW_MEMBERS) <= {k for (_, k) in added}
    # The internal surface (what SimulationHarness may call) is exact; private Rebuild* helpers are free.
    assert {k for (_, k), m in added.items() if m["accessibility"] == "internal"} == \
        {k for k, a in NEW_MEMBERS.items() if a == "internal"}
    for (type_name, key), m in added.items():
        assert type_name == SIM
        assert m["name"].startswith("Rebuild")
        assert m["accessibility"] == NEW_MEMBERS.get(key, m["accessibility"]) and m["accessibility"] in ("private", "internal")


# ---------------------------------------------------------------- the move is verbatim


def test_classifier_finds_exactly_the_design_stubs(original):
    sim = {m["name"] for m in methods(original, "SimulationSystem") if is_stub(m)}
    cfg = {m["name"] for m in methods(original, "SimulationConfig") if is_stub(m)}
    assert sim == {n for names in MODULES.values() for n in names} and len(sim) == 55
    assert cfg == CONFIG_STUBS
    assert len(DEBUG_ONLY - sim) == 0


def test_main_file_is_original_minus_moved_methods(original):
    text = original["sources"][SS.name]
    stubs = sorted((m for m in methods(original, "SimulationSystem") if is_stub(m)), key=lambda m: m["full_span"][0])
    out, pos = [], 0
    for m in stubs:
        out.append(text[pos:m["full_span"][0]])
        pos = m["full_span"][1]
    out.append(text[pos:])
    expected = "".join(out).replace("\tpublic class SimulationSystem : SystemBase\n",
                                    "\tpublic partial class SimulationSystem : SystemBase\n", 1)
    assert SS.read_text(encoding="utf-8") == expected


def test_config_gains_only_partial_annotations_and_filled_bodies(original, current):
    now = SC.read_text(encoding="utf-8")
    # The two filled methods (with their comments and annotation) go back to the 6af011f text first.
    orig_text = original["sources"][SC.name]
    spans = {m["name"]: m["full_span"] for m in methods(original, "SimulationConfig") if m["name"] in FILLED_CONFIG}
    for m in sorted((m for m in methods(current, "SimulationConfig") if m["name"] in FILLED_CONFIG),
                    key=lambda m: -m["full_span"][0]):
        a, b = spans[m["name"]]
        now = now[:m["full_span"][0]] + orig_text[a:b] + now[m["full_span"][1]:]
    stripped = re.sub(r"(?m)^\t\t\[SimulationRebuild\(Provenance\.\w+\)\]\n", "", now)
    stripped = stripped.replace("using Codebase.Simulation.Rebuild;\n", "", 1)
    stripped = stripped.replace("\tpublic partial class SimulationConfig : ScriptableObject\n",
                                "\tpublic class SimulationConfig : ScriptableObject\n", 1)
    assert stripped == original["sources"][SC.name]
    assert len(now.splitlines()) - len(stripped.splitlines()) == len(CONFIG_STUBS) - len(FILLED_CONFIG) + 1


def test_every_method_kept_its_declaration_and_body(original, current):
    """Headers never change; bodies change only for the methods C3b filled."""
    filled = {"SimulationSystem": FILLED, "SimulationConfig": FILLED_CONFIG}
    for type_name in ("SimulationSystem", "SimulationConfig"):
        now = {}
        for m in methods(current, type_name):
            assert m["key"] not in now, f"{m['key']} declared twice"
            if "partial" in m["modifiers"] and not m["has_body"]:
                continue
            now[m["key"]] = m
        for m in methods(original, type_name):
            n = now.get(m["key"])
            assert n is not None, f"{type_name}.{m['key']} missing"
            assert n["header"] == m["header"], m["key"]
            assert (n["body"] != m["body"]) == (m["name"] in filled[type_name]), m["key"]
            assert [a for a in n["attributes"] if not REBUILD_ATTR.match(a["name"])] == m["attributes"], m["key"]


def test_methods_are_in_their_module_files(original, current):
    where = {m["key"]: m["file"] for m in methods(current, "SimulationSystem") if m["has_body"]}
    module_of = {n: mod for mod, names in MODULES.items() for n in names}
    for m in methods(original, "SimulationSystem"):
        expected = f"SimulationSystem.{module_of[m['name']]}.cs" if is_stub(m) else SS.name
        assert where[m["key"]] == expected, m["key"]
    for m in methods(current, "SimulationConfig"):
        assert m["file"] == SC.name


def test_partial_files_hold_only_methods():
    """Methods only, except the engine state (Rebuild* fields and properties) in .Rebuild.cs."""
    doc = members_of([SYSTEMS / f"SimulationSystem.{m}.cs" for m in PARTIAL_FILES], "SimulationSystem")
    others = [m for m in doc["members"] if m["kind"] != "MethodDeclaration"]
    assert {m["kind"] for m in others} <= {"FieldDeclaration", "PropertyDeclaration"}
    for m in others:
        assert m["file"] == "SimulationSystem.Rebuild.cs", m["key"]
        assert all(name.strip().startswith("Rebuild") for name in m["name"].split(",")), m["key"]
    assert len(doc["types"]) == len(PARTIAL_FILES)
    for t in doc["types"]:
        assert t["modifiers"] == ["public", "partial"] and t["base_list"] is None
        assert t["namespace"] == "Codebase.Simulation.Systems" and t["containing_type"] is None


# ---------------------------------------------------------------- annotations


def test_every_empty_method_is_annotated(original, current):
    """Each empty method of 6af011f: one annotation, its provenance per FILLED (else Null/DebugOnly), a formula
    id once filled. Recovered bodies carry none; new Rebuild* members may."""
    filled = {"SimulationSystem": FILLED, "SimulationConfig": FILLED_CONFIG}
    for type_name in ("SimulationSystem", "SimulationConfig"):
        stubs = {m["key"]: m for m in methods(original, type_name) if is_stub(m)}
        for m in methods(current, type_name):
            attrs = rebuild_attributes(m)
            if not m["has_body"]:
                assert attrs == [], m["key"]
                continue
            if m["key"] not in stubs:
                assert attrs == [] or m["name"].startswith("Rebuild"), f"recovered body {m['key']} must not be annotated"
                continue
            assert len(attrs) == 1, m["key"]
            args = attrs[0]["args"]
            if m["name"] in filled[type_name]:
                assert args[0] == "Provenance." + filled[type_name][m["name"]], m["key"]
                assert len(args) == 2 and args[1].startswith('"'), f"{m['key']} needs a formula id"
            else:
                expected = "Provenance.DebugOnly" if m["name"] in DEBUG_ONLY else "Provenance.Null"
                assert args[0] == expected, m["key"]
        assert set(stubs) <= {m["key"] for m in methods(current, type_name) if rebuild_attributes(m)}


def test_registry_scanner_sees_the_same_annotations(current):
    """P1's static scanner (used for coverage without a run) agrees with the Roslyn view."""
    roslyn = sorted((m["file"], m["name"], rebuild_attributes(m)[0]["args"][0].split(".")[1])
                    for t in ("SimulationSystem", "SimulationConfig") for m in methods(current, t) if rebuild_attributes(m))
    scanned = sorted((Path(a.file).name, a.member, a.provenance) for a in registry.scan_csharp()
                     if a.type_name in ("SimulationSystem", "SimulationConfig"))
    assert scanned == roslyn and len(scanned) >= 62


def test_attribute_declares_the_design_provenances():
    doc = members_of([ATTRIBUTE], "SimulationRebuildAttribute")
    ctor = [m for m in doc["members"] if m["kind"] == "ConstructorDeclaration"]
    assert [m["key"] for m in ctor] == ["SimulationRebuildAttribute(Provenance, string)"]
    text = ATTRIBUTE.read_text(encoding="utf-8")
    enum = re.search(r"public enum Provenance\s*\{(.*?)\}", text, re.S).group(1)
    names = re.findall(r"^\t\t(\w+) = \d+,?$", enum, re.M)
    assert {"Inferred", "Calibrated", "Null", "DebugOnly"} <= set(names)
    assert "namespace Codebase.Simulation.Rebuild" in text


# ---------------------------------------------------------------- hooks


def test_hooks_declared_in_rebuild_file(current):
    decls = {m["key"]: m for m in methods(current, "SimulationSystem")
             if "partial" in m["modifiers"] and not m["has_body"]}
    assert set(decls) == set(HOOKS)
    for m in decls.values():
        assert m["file"] == "SimulationSystem.Rebuild.cs"
        assert m["modifiers"] == ["partial"] and m["return_type"] == "void"  # implicitly private, optional body
    impls = {m["key"]: m for m in methods(current, "SimulationSystem") if "partial" in m["modifiers"] and m["has_body"]}
    assert {k: m["file"] for k, m in impls.items()} == {
        k: f"SimulationSystem.{f}.cs" for k, f in HOOKS.items() if f is not None}
    for m in impls.values():
        if m["file"] == "SimulationSystem.Replay.cs":
            assert m["statements"], f"{m['key']} is filled by C4"


def test_cross_module_call_sites_in_place(current):
    """Design 7.4: every call site exists; RebuildApplyClear is called only from the two fight bodies (and the
    test-only write-point probe), never from Tick/FastTick; stubs still unfilled call nothing."""
    calls = {(m["name"], c) for m in methods(current, "SimulationSystem") for c in m["invocations"]}
    missing = [edge for edge in CALL_SITES if edge not in calls]
    assert missing == []
    clear_callers = {caller for caller, callee in calls if callee == "RebuildApplyClear"}
    assert clear_callers == {"SimulateFightWithSpawner", "SimulateFightWithSpawnerFast", "RebuildRunWritePointProbe"}
    stubs = [m for t in ("SimulationSystem", "SimulationConfig") for m in methods(current, t) if is_stub(m)]
    assert all(m["invocations"] == [] for m in stubs)


def test_day_override_follows_the_forced_snapshot(current):
    """Design 4.6: the replay/checkpoint override of day d takes effect after day d's forced snapshot, on day 0
    as on every day change (the 4.4 pseudo-code has it before; the deviation is in Runbook 16.6)."""
    by_name = {m["name"]: m["invocations"] for m in methods(current, "SimulationSystem")}
    for caller in ("PrepareSimulationRun", "UpdateSpawnerChargesChunk"):
        calls = by_name[caller]
        assert calls.count("RebuildApplyDayOverride") == 1 and calls.count("MakeSnapshot") == 1, caller
        assert calls.index("MakeSnapshot") < calls.index("RebuildApplyDayOverride"), caller
    calls = by_name["UpdateSpawnerChargesChunk"]
    last_modifier = max(i for i, c in enumerate(calls) if c in ("RecalculateChunkRpmValues", "UpdateInterpolationStatModifier"))
    assert last_modifier < calls.index("MakeSnapshot")


def test_harness_calls_the_reset_bridge():
    """Design 7.4: SimulationHarness -> RebuildResetForJob (S1), through the internal bridge, at job start."""
    harness = {m["name"]: m["invocations"] for m in methods(members_of([HARNESS], "SimulationHarness"), "SimulationHarness")}
    calls = harness["StartJob"]
    assert "RebuildTryResetForJob" in calls
    assert calls.index("RebuildTryResetForJob") < calls.index("StateDigestText")


def test_hooks_do_not_collide_with_harness_hooks(current):
    harness = members_of([HARNESS], "SimulationHarness")
    harness_hooks = {m["name"] for m in methods(harness, "SimulationHarness") if "partial" in m["modifiers"]}
    assert {"RebuildJobApplyPatch", "RebuildJobApplyOverride", "RebuildJobExportOutputs",
            "RebuildJobExportBake"} <= harness_hooks
    ours = {m["name"] for m in methods(current, "SimulationSystem") if m["name"].startswith("Rebuild")}
    assert ours and not ours & harness_hooks
    assert not any(n.startswith("RebuildJob") for n in ours)


# ---------------------------------------------------------------- Unity assets


def test_new_files_have_unique_meta():
    new = [SIM_DIR / "Rebuild", ATTRIBUTE, *(SYSTEMS / f"SimulationSystem.{m}.cs" for m in PARTIAL_FILES),
           SIM_DIR / "Rebuild" / "SimulationRebuildParams.cs", SIM_DIR / "Harness" / "SimulationRunRequest.Engine.cs",
           # C3c
           *(SIM_DIR / "Rebuild" / f for f in ("SimulationRandom.cs", "SimulationTrace.cs", "SimulationRebuildParamsAsset.cs")),
           SIM_DIR / "Harness" / "SimulationRunRequest.Params.cs",
           # C7
           *(SIM_DIR / "Rebuild" / f for f in ("SimulationRebuildParams.SlowPolicy.cs", "SimulationSlowPolicyState.cs")),
           SIM_DIR / "Harness" / "SimulationRunRequest.SlowPolicy.cs",
           SIM_DIR / "Harness" / "SimulationCrosscheck.cs",  # P11
           PROJECT / "Assets" / "Editor" / "Simulation" / "SimulationParamsImporter.cs",
           # C5
           SIM_DIR / "Rebuild" / "SimulationRebuildParams.FastTick.cs", SIM_DIR / "Harness" / "SimulationRunRequest.FastTick.cs",
           SIM_DIR / "Harness" / "SimulationFastTickRecorder.cs",
           PROJECT / "Assets" / "Simulation"]
    tracked = subprocess.run(["git", "-C", str(WORKTREE), "ls-files", "-z", "--", "*.meta"],
                             capture_output=True, text=True, check=True).stdout.split("\0")
    ours = {str((p.parent / (p.name + ".meta")).relative_to(WORKTREE)) for p in new}
    guids = {}
    for rel in tracked + sorted(ours - set(tracked)):
        if not rel:
            continue
        path = WORKTREE / rel
        if not path.exists():
            continue
        m = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(encoding="utf-8", errors="replace"), re.M)
        if m:
            guids.setdefault(m.group(1), []).append(rel)
    for p in new:
        meta = p.parent / (p.name + ".meta")
        text = meta.read_text(encoding="utf-8")
        guid = re.search(r"^guid: ([0-9a-f]{32})$", text, re.M).group(1)
        assert guids[guid] == [str(meta.relative_to(WORKTREE))], f"{meta.name} guid not unique"
        assert ("folderAsset: yes" in text) == p.is_dir()
        assert p.is_dir() or "MonoImporter:" in text
