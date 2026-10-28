"""Partial split of SimulationSystem (design 0, 1.5, 4.1, 7.3 C3a, 7.4): a mechanical, behaviour-neutral move.

* The semantic signature lock (cscheck, P0) is identical to 6af011f; the only new members are the Rebuild*
  partial hooks of 7.4 (private/internal).
* Every empty method of 6af011f moved verbatim (declaration and body) into its module's partial file; the
  recovered bodies stay in SimulationSystem.cs, which is the 6af011f text minus the moved methods plus
  ``partial``. SimulationConfig only gains ``partial`` and annotations.
* Every empty method carries ``[SimulationRebuild(Provenance.Null | Provenance.DebugOnly)]``.
* The hooks are declared in .Rebuild.cs, implemented (empty) only in .Replay.cs and called nowhere yet.

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
PARTIAL_FILES = sorted([*MODULES, "Rebuild", "Replay"])
CONFIG_STUBS = {"ResetCombatConfigValues", "Tick", "ResetAmuletLevelBalance", "SetAmuletLevelBalance",
                "GetTrimmedTimeChunkSnapshots"}
# Design 4.1 "debug" rows; every other empty method starts as Provenance.Null.
DEBUG_ONLY = {"GetNextLocationInterpolationTiming", "GetBestSpawnerForInfo", "TryGetSpawnerPriority", "DrawSquare",
              "CalcDifficultSpawner", "LogSpawnerClear", "GetUnlockTimingLogData"}
# Design 7.4: hook -> file of its (single) implementation, None while no unit has implemented it.
HOOKS = {
    "RebuildApplyDayOverride(int)": "Replay",
    "RebuildApplyReplayCaches()": "Replay",
    "RebuildOnTickCompleted()": None,
    "RebuildResetForJob(ref bool)": None,
}
NEW_MEMBERS = {
    "method:RebuildApplyDayOverride(int)": "private",
    "method:RebuildApplyReplayCaches()": "private",
    "method:RebuildOnTickCompleted()": "private",
    "method:RebuildResetForJob(ref bool)": "private",
    "method:RebuildTryResetForJob()": "internal",
}
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
    assert {k for (_, k) in added} == set(NEW_MEMBERS)
    for (type_name, key), m in added.items():
        assert type_name == SIM
        assert m["name"].startswith("Rebuild")
        assert m["accessibility"] == NEW_MEMBERS[key]


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


def test_config_gains_only_partial_and_annotations(original):
    now = SC.read_text(encoding="utf-8")
    stripped = re.sub(r"(?m)^\t\t\[SimulationRebuild\(Provenance\.\w+\)\]\n", "", now)
    stripped = stripped.replace("using Codebase.Simulation.Rebuild;\n", "", 1)
    stripped = stripped.replace("\tpublic partial class SimulationConfig : ScriptableObject\n",
                                "\tpublic class SimulationConfig : ScriptableObject\n", 1)
    assert stripped == original["sources"][SC.name]
    assert len(now.splitlines()) - len(stripped.splitlines()) == len(CONFIG_STUBS) + 1


def test_every_method_kept_its_declaration_and_body(original, current):
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
            assert n["header"] == m["header"] and n["body"] == m["body"], m["key"]
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
    doc = members_of([SYSTEMS / f"SimulationSystem.{m}.cs" for m in PARTIAL_FILES], "SimulationSystem")
    assert {m["kind"] for m in doc["members"]} == {"MethodDeclaration"}
    assert len(doc["types"]) == len(PARTIAL_FILES)
    for t in doc["types"]:
        assert t["modifiers"] == ["public", "partial"] and t["base_list"] is None
        assert t["namespace"] == "Codebase.Simulation.Systems" and t["containing_type"] is None


# ---------------------------------------------------------------- annotations


def test_every_empty_method_is_annotated(current):
    seen = set()
    for type_name in ("SimulationSystem", "SimulationConfig"):
        for m in methods(current, type_name):
            attrs = rebuild_attributes(m)
            if not m["has_body"]:
                assert attrs == [], m["key"]
                continue
            if not is_stub(m):
                assert attrs == [], f"recovered or new body {m['key']} must not be annotated"
                continue
            assert len(attrs) == 1, m["key"]
            expected = "Provenance.DebugOnly" if m["name"] in DEBUG_ONLY else "Provenance.Null"
            assert attrs[0]["args"] == [expected], m["key"]
            seen.add((type_name, m["name"]))
    sim = {n for names in MODULES.values() for n in names}
    replay = {"RebuildApplyDayOverride", "RebuildApplyReplayCaches"}
    assert seen == {("SimulationSystem", n) for n in sim | replay} | {("SimulationConfig", n) for n in CONFIG_STUBS}


def test_registry_scanner_sees_the_same_annotations(current):
    """P1's static scanner (used for coverage without a run) agrees with the Roslyn view."""
    roslyn = sorted((m["file"], m["name"], rebuild_attributes(m)[0]["args"][0].split(".")[1])
                    for t in ("SimulationSystem", "SimulationConfig") for m in methods(current, t) if rebuild_attributes(m))
    scanned = sorted((Path(a.file).name, a.member, a.provenance) for a in registry.scan_csharp()
                     if a.type_name in ("SimulationSystem", "SimulationConfig"))
    assert scanned == roslyn and len(scanned) == 62


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
        assert m["statements"] == [], f"{m['key']} is an empty placeholder until C4"


def test_no_call_sites_yet(current):
    """C3b places the calls; the only call is the internal bridge for SimulationHarness to the S1 hook."""
    new_names = {k.split(":")[1].split("(")[0] for k in NEW_MEMBERS}  # RebuildPerFrameCaches is original
    calls = [(m["name"], c) for t in ("SimulationSystem", "SimulationConfig") for m in methods(current, t)
             for c in m["invocations"] if c in new_names]
    assert calls == [("RebuildTryResetForJob", "RebuildResetForJob")]
    stubs = [m for t in ("SimulationSystem", "SimulationConfig") for m in methods(current, t) if is_stub(m)]
    assert all(m["invocations"] == [] for m in stubs)


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
    new = [SIM_DIR / "Rebuild", ATTRIBUTE, *(SYSTEMS / f"SimulationSystem.{m}.cs" for m in PARTIAL_FILES)]
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
