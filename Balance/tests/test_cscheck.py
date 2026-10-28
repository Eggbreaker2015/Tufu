"""cscheck (design 1.6, unit P0): offline compile, signature lock, banned APIs.

Every mutation happens in a temporary copy of this worktree's C# sources; real worktrees are never touched.
Needs the .NET 8 SDK and the run worktree's Library (.rsp files); skipped otherwise.
"""

import json
import shutil
import time
from pathlib import Path

import pytest

from tufu_balance import cscheck

PROJECT = "Recovery/StudyProject"
CB = "Assets/Scripts/Assembly-CSharp/CodeBase"
SS = f"{CB}/Simulation/Systems/SimulationSystem.cs"
SD = f"{CB}/Simulation/Systems/SpawnerData.cs"
SC = f"{CB}/Simulation/SimulationConfig.cs"
SIM = "Codebase.Simulation.Systems.SimulationSystem"
SPAWNER = "Codebase.Simulation.Systems.SpawnerData"
CONFIG = "Codebase.Simulation.SimulationConfig"


def _tool_available():
    try:
        cscheck.dotnet()
    except cscheck.CsCheckError:
        return False
    return any((cscheck.run_worktree() / PROJECT / "Library" / "Bee" / "artifacts").glob("*/Assembly-CSharp.rsp"))


pytestmark = pytest.mark.skipif(not _tool_available(), reason="needs dotnet and the run worktree's Library/Bee .rsp")


def clone_sources(dest):
    """Copy the C# sources (and Packages manifests) of this worktree into dest, keeping the layout."""
    src = cscheck.WORKTREE / PROJECT
    for path in (src / "Assets").rglob("*"):
        if path.is_file() and path.suffix in (".cs", ".asmdef", ".asmref"):
            target = dest / PROJECT / path.relative_to(src)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, target)
    for name in ("manifest.json", "packages-lock.json"):
        (dest / PROJECT / "Packages").mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src / "Packages" / name, dest / PROJECT / "Packages" / name)
    return dest


def edit(root, rel, old, new):
    path = root / PROJECT / rel
    text = path.read_text(encoding="utf-8")
    assert text.count(old) == 1, f"{old!r} must occur once in {rel}"
    path.write_text(text.replace(old, new), encoding="utf-8")


def add(root, rel, text):
    path = root / PROJECT / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


@pytest.fixture(scope="module")
def baseline():
    return json.loads(cscheck.BASELINE.read_text(encoding="utf-8"))


@pytest.fixture(scope="module")
def real_result():
    start = time.monotonic()
    result = cscheck.check()
    result["wall_s"] = time.monotonic() - start
    return result


# ---------------------------------------------------------------- the real worktree


def test_real_worktree_passes(real_result):
    assert real_result["ok"], real_result["failures"]
    assert real_result["exit_code"] == 0


def test_compile_matches_unity(real_result, baseline):
    for asm in ("Assembly-CSharp", "Assembly-CSharp-Editor"):
        entry = real_result["compile"][asm]
        assert entry["errors"] == 0
        assert entry["warnings_by_id"] == baseline["warnings"][asm]
    # Unity's first open of 5806ec7 (W0) reported 3713 + 8 distinct CS warnings.
    assert real_result["rsp"]["langversion"] == "9.0" and real_result["rsp"]["unsafe"] is True


def test_no_editor_compile_within_baseline(real_result, baseline):
    ne = real_result["no_editor"]
    assert ne["new_errors"] == []
    assert ne["dropped_defines"] == ["UNITY_EDITOR", "UNITY_EDITOR_64", "UNITY_EDITOR_OSX"]
    assert "UnityEditor.dll" in ne["dropped_references"]
    assert ne["errors"] == len(baseline["no_editor_errors"])


def test_signature_and_bans_clean(real_result):
    assert real_result["signature"]["violations"] == []
    assert real_result["bans"]["violations"] == []
    assert f"{CB}/Simulation/Systems/SimulationSystem.cs" in real_result["bans"]["scope"]["files"]


def test_full_check_within_budget(real_result):
    assert real_result["timings"]["total_ms"] <= 30_000
    assert real_result["wall_s"] <= 30


# ---------------------------------------------------------------- signature lock file


def test_signature_generation_is_deterministic(tmp_path):
    first = cscheck.generate_signature(tmp_path / "a.json")
    second = cscheck.generate_signature(tmp_path / "b.json")
    assert first.read_bytes() == second.read_bytes()
    assert first.read_bytes() == cscheck.SIGNATURE.read_bytes()


def test_signature_covers_members():
    sig = json.loads(cscheck.SIGNATURE.read_text(encoding="utf-8"))
    assert sig["commit"].startswith("6af011f")
    types = {t["name"]: {m["key"]: m for m in t["members"]} for t in sig["types"]}
    assert set(types) == {SIM, f"{SIM}+FastTickChunkStatReward", SPAWNER, CONFIG}
    sim, spawner, config = types[SIM], types[SPAWNER], types[CONFIG]
    assert sim["method:MakeSnapshot(bool)"]["parameters"][0]["default"] == "false"
    assert sim["method:GetSimulationInterpolationCoefficient(float, Codebase.Balance.BalanceInterpolationType, Codebase.Player.Inventory.Rarity)"][
        "parameters"][2]["default"] == "Codebase.Player.Inventory.Rarity.Rare"
    assert sim["method:CalcDifficultSpawner(Codebase.Enemy.EnemySpawner, float)"]["parameters"][1]["default"] == "0f"
    assert spawner["method:GetPriority(UnityEngine.Vector3, Codebase.Unit.StatType?)"]["parameters"][1]["default"] == "null"
    assert sim["method:GetRPMConfig()"]["return_type"] == "(double, double, double, double)"
    assert "(Codebase.Enemy.EnemySpawner A, Codebase.Enemy.EnemySpawner B)" in sim["field:PathSpawners"]["type"]
    assert sim["type:FastTickChunkStatReward"]["accessibility"] == "private"
    assert sim["field:playerModel"]["attributes"] == ["Codebase.Core.InjectAttribute()"]
    assert config["field:MaxSimultaneousAttackers"]["constant"] == "4"
    assert config["field:TimeChunkSnapshots"]["attributes"] == [
        'UnityEngine.Serialization.FormerlySerializedAsAttribute("SpawnerChargesChunkSnapshots")']
    assert "method:.ctor()" in config  # the implicit constructor is part of the lock


# ---------------------------------------------------------------- injected violations

REBUILD_ATTRIBUTE = """namespace Codebase.Simulation.Rebuild
{
	[System.AttributeUsage(System.AttributeTargets.Method)]
	public sealed class SimulationRebuildAttribute : System.Attribute
	{
	}
}
"""

REBUILD_BANNED = """using System;
using System.Diagnostics;
using R = UnityEngine.Random;

namespace Codebase.Simulation.Rebuild
{
	internal static class RebuildBanned
	{
		internal static float Roll() { return UnityEngine.Random.Range(0f, 1f); }
		internal static float Alias() { return R.value; }
		internal static long Clock() { return DateTime.UtcNow.Ticks; }
		internal static long Offset() { return DateTimeOffset.Now.Ticks; }
		internal static long Watch() { return Stopwatch.StartNew().ElapsedTicks; }
		internal static int Ticks() { return Environment.TickCount; }
		internal static float Frame() { return UnityEngine.Time.deltaTime; }
		internal static DateTime Epoch() { return new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc); }
	}
}
"""

PARTIAL_FIGHT = """namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		private void ProcessDeath()
		{
		}

		private void RebuildSeed()
		{
			UnityEngine.Random.InitState(1);
		}
	}
}
"""

HARNESS = """using System.Diagnostics;

namespace Codebase.Simulation.Harness
{
	public static class SimulationHarnessProbe
	{
		public static long Elapsed() { return Stopwatch.StartNew().ElapsedMilliseconds; }
	}
}
"""

UI_REBUILD = """namespace Codebase.UI
{
	public class RebuildNeutralView
	{
		private float RebuildLayout() { return UnityEngine.Time.time; }
	}
}
"""

EXPECTED_SIGNATURE = {
    ("member_changed", SIM, "method:MakeSnapshot(bool)"),                     # default value
    ("new_member_name", SIM, "field:helperCounter"),                          # new non-Rebuild member
    ("member_changed", SPAWNER, "method:GetMoveTime(UnityEngine.Vector3)"),    # return type
    ("new_member_accessibility", SPAWNER, "method:HelperPublic()"),          # new public member
    ("new_member_name", SPAWNER, "method:HelperPublic()"),
    ("new_member_accessibility", CONFIG, "field:RebuildPublicKnob"),         # public even with the prefix
    ("member_removed", CONFIG, "method:ResetAmuletLevelBalance(int)"),
}

EXPECTED_BANS = {
    ("RebuildBanned.cs", 3, "UnityEngine.Random"),
    ("RebuildBanned.cs", 9, "UnityEngine.Random.Range"),
    ("RebuildBanned.cs", 10, "UnityEngine.Random.value"),
    ("RebuildBanned.cs", 11, "System.DateTime.UtcNow"),
    ("RebuildBanned.cs", 12, "System.DateTimeOffset.Now"),
    ("RebuildBanned.cs", 13, "System.Diagnostics.Stopwatch.StartNew"),
    ("RebuildBanned.cs", 14, "System.Environment.TickCount"),
    ("RebuildBanned.cs", 15, "UnityEngine.Time.deltaTime"),
    ("SimulationSystem.Fight.cs", 11, "UnityEngine.Random.InitState"),
    ("SpawnerData.cs", "RebuildClock", "UnityEngine.Time.realtimeSinceStartup"),
    ("SpawnerData.cs", "GetDynamicPriority", "System.DateTime.Now"),
}


@pytest.fixture(scope="module")
def injected(tmp_path_factory):
    root = clone_sources(tmp_path_factory.mktemp("injected"))
    # Allowed: partial keyword, a method moved to a partial file, private/internal Rebuild* members.
    edit(root, SS, "public class SimulationSystem : SystemBase", "public partial class SimulationSystem : SystemBase")
    edit(root, SS, "\t\tprivate void ProcessDeath()\n\t\t{\n\t\t}\n\n", "")
    add(root, f"{CB}/Simulation/Systems/SimulationSystem.Fight.cs", PARTIAL_FIGHT)
    edit(root, SS, "\t\tprivate bool uiDisabled;\n",
         "\t\tprivate bool uiDisabled;\n\n\t\tprivate int helperCounter;\n\n\t\tprivate int RebuildCounter;\n\n"
         "\t\tinternal void RebuildHelper()\n\t\t{\n\t\t}\n")
    edit(root, SS, "private void MakeSnapshot(bool force = false)", "private void MakeSnapshot(bool force = true)")
    edit(root, SD, "public double GetMoveTime(Vector3 position)\n\t\t{\n\t\t\treturn 0.0;",
         "public float GetMoveTime(Vector3 position)\n\t\t{\n\t\t\treturn 0f;")
    edit(root, SD, "\t\tpublic double GetDynamicPriority(Vector3 position, StatType? statType = null)\n\t\t{\n\t\t\treturn 0.0;",
         "\t\tpublic void HelperPublic()\n\t\t{\n\t\t}\n\n"
         "\t\tprivate double RebuildClock()\n\t\t{\n\t\t\treturn UnityEngine.Time.realtimeSinceStartup;\n\t\t}\n\n"
         "\t\t[Codebase.Simulation.Rebuild.SimulationRebuild]\n"
         "\t\tpublic double GetDynamicPriority(Vector3 position, StatType? statType = null)\n\t\t{\n"
         "\t\t\treturn System.DateTime.Now.Ticks;")
    edit(root, SC, "\t\tpublic void ResetAmuletLevelBalance(int levelsCount)\n\t\t{\n\t\t}\n\n", "")
    edit(root, SC, "\t\tpublic bool IsRunning;\n", "\t\tpublic bool IsRunning;\n\t\tpublic int RebuildPublicKnob;\n")
    add(root, f"{CB}/Simulation/Rebuild/SimulationRebuildAttribute.cs", REBUILD_ATTRIBUTE)
    add(root, f"{CB}/Simulation/Rebuild/RebuildBanned.cs", REBUILD_BANNED)
    add(root, f"{CB}/Simulation/Harness/SimulationHarnessProbe.cs", HARNESS)
    add(root, f"{CB}/UI/RebuildNeutralView.cs", UI_REBUILD)
    return cscheck.check(root, steps=("signature", "bans"))


def test_injected_run_fails(injected):
    assert not injected["ok"] and injected["exit_code"] == 1
    assert any(f.startswith("signature lock: 7 violation") for f in injected["failures"])
    assert any(f.startswith("banned APIs: 11 use") for f in injected["failures"])


@pytest.mark.parametrize("expected", sorted(EXPECTED_SIGNATURE), ids=lambda e: f"{e[0]}:{e[2]}")
def test_signature_violation_caught(injected, expected):
    found = {(v["rule"], v["type"], v["member"]) for v in injected["signature"]["violations"]}
    assert expected in found


def test_signature_has_no_other_violations(injected):
    """partial, the moved ProcessDeath, RebuildCounter, RebuildHelper, RebuildSeed and the
    [SimulationRebuild] attribute itself must not count as violations."""
    found = {(v["rule"], v["type"], v["member"]) for v in injected["signature"]["violations"]}
    assert found == EXPECTED_SIGNATURE


def test_signature_change_details(injected):
    by_member = {v["member"]: v for v in injected["signature"]["violations"] if v["rule"] == "member_changed"}
    assert by_member["method:GetMoveTime(UnityEngine.Vector3)"]["actual"]["return_type"] == "float"
    assert by_member["method:MakeSnapshot(bool)"]["actual"]["parameters"][0]["default"] == "true"


def _ban_keys(result):
    keys = set()
    for v in result["bans"]["violations"]:
        name = Path(v["file"]).name
        if name == "SpawnerData.cs":
            keys.add((name, v["scope"].removeprefix("member:"), v["api"]))
        else:
            keys.add((name, v["line"], v["api"]))
    return keys


@pytest.mark.parametrize("expected", sorted(EXPECTED_BANS, key=str), ids=lambda e: f"{e[0]}:{e[2]}")
def test_banned_api_caught(injected, expected):
    assert expected in _ban_keys(injected)


def test_bans_have_no_false_positives(injected):
    """Stopwatch in Harness/, Time in a Rebuild* member of a non-locked UI type and DateTime construction
    in a rebuild file are allowed."""
    assert _ban_keys(injected) == EXPECTED_BANS
    scope = injected["bans"]["scope"]
    assert f"{CB}/Simulation/Rebuild/RebuildBanned.cs" in scope["files"]
    assert f"{CB}/Simulation/Systems/SimulationSystem.Fight.cs" in scope["files"]
    assert not any("Harness" in f for f in scope["files"])
    assert f"{SD}:GetDynamicPriority" in scope["members"] and f"{SD}:RebuildClock" in scope["members"]


# ---------------------------------------------------------------- compile steps on a modified copy

PROBE = """namespace Codebase.Simulation.Rebuild
{
	public static class RebuildProbeMarker
	{
		public static int Value = 1;

		public static void Touch()
		{
			UnityEditor.AssetDatabase.Refresh();
		}
	}

	internal class RebuildProbeUnused
	{
		private int unusedField;
	}
}
"""

BROKEN = """namespace Codebase.Simulation.Rebuild
{
	internal static class RebuildBroken
	{
		internal static int Value() { return "text"; }
	}
}
"""

EDITOR_USER = """namespace Codebase.Simulation.EditorTools
{
	public static class ProbeUser
	{
		public static int Read() { return Codebase.Simulation.Rebuild.RebuildProbeMarker.Value; }
	}
}
"""


@pytest.fixture(scope="module")
def compiled(tmp_path_factory):
    root = clone_sources(tmp_path_factory.mktemp("compiled"))
    add(root, f"{CB}/Simulation/Rebuild/RebuildProbeMarker.cs", PROBE)
    add(root, f"{CB}/Simulation/Rebuild/RebuildBroken.cs", BROKEN)
    add(root, "Assets/Editor/Simulation/ProbeUser.cs", EDITOR_USER)
    return cscheck.check(root, steps=("compile", "no_editor"))


def test_new_files_are_compiled(compiled):
    runtime = compiled["sources"]["Assembly-CSharp"]
    assert f"{CB}/Simulation/Rebuild/RebuildProbeMarker.cs" in runtime["added"]
    assert "Assets/Editor/Simulation/ProbeUser.cs" in compiled["sources"]["Assembly-CSharp-Editor"]["added"]


def test_compile_error_caught(compiled):
    errors = compiled["compile"]["Assembly-CSharp"]["error_list"]
    assert [(Path(e["file"]).name, e["id"]) for e in errors] == [("RebuildBroken.cs", "CS0029")]
    assert not compiled["ok"]


def test_editor_assembly_sees_new_runtime_types(compiled):
    # The editor compile references the Assembly-CSharp being checked, not Unity's last build.
    assert compiled["compile"]["Assembly-CSharp-Editor"]["errors"] == 0


def test_new_warning_caught(compiled, baseline):
    increased = compiled["compile"]["Assembly-CSharp"]["new_warnings"]
    assert increased == {"CS0169": f"{baseline['warnings']['Assembly-CSharp']['CS0169']} -> "
                                   f"{baseline['warnings']['Assembly-CSharp']['CS0169'] + 1}"}


def test_editor_api_outside_define_breaks_player_compile(compiled):
    new = compiled["no_editor"]["new_errors"]
    assert any("RebuildProbeMarker.cs" in e and "UnityEditor" in e for e in new)
    assert any("RebuildBroken.cs|CS0029" in e for e in new)
    assert any(f.startswith("no-UNITY_EDITOR compile") for f in compiled["failures"])


# ---------------------------------------------------------------- source membership and environment


def test_membership_follows_unity_rules(tmp_path):
    root = clone_sources(tmp_path)
    removed = "Assets/Scripts/Assembly-CSharp/BackgroundColorAttribute.cs"
    (root / PROJECT / removed).unlink()
    add(root, "Assets/Scripts/.hidden/Hidden.cs", "class Hidden {}\n")
    add(root, "Assets/Scripts/Skipped~/Skipped.cs", "class Skipped {}\n")
    add(root, "Assets/Scripts/Tools/Editor/NestedEditor.cs", "class NestedEditor {}\n")
    add(root, "Assets/Plugins/FirstPass.cs", "class FirstPass {}\n")
    add(root, "Assets/Custom/Custom.asmdef", '{"name": "Custom"}\n')
    add(root, "Assets/Custom/Inside.cs", "class Inside {}\n")
    result = cscheck.sources(root)["assemblies"]
    assert result["Assembly-CSharp"]["removed"] == [removed]
    assert result["Assembly-CSharp"]["added"] == []
    assert result["Assembly-CSharp-Editor"]["added"] == ["Assets/Scripts/Tools/Editor/NestedEditor.cs"]
    assert result["Assembly-CSharp-firstpass"]["files"] == ["Assets/Plugins/FirstPass.cs"]
    assert result["asmdef:Assets/Custom"]["files"] == ["Assets/Custom/Inside.cs"]
    checked = cscheck.check(root, steps=("signature",))
    assert any("Assembly-CSharp-firstpass" in f for f in checked["failures"])


def test_membership_reproduces_rsp_on_run_worktree():
    run = cscheck.run_worktree()
    built = [p.stat().st_mtime for p in (run / PROJECT / "Library" / "ScriptAssemblies").glob("Assembly-CSharp*.dll")]
    assets = run / PROJECT / "Assets"
    newest = max(p.stat().st_mtime for p in [*assets.rglob("*.cs"), *(d for d in assets.rglob("*") if d.is_dir())])
    if not built or newest > max(built):
        pytest.skip("run worktree sources changed after Unity's last compile")
    result = cscheck.sources(run)["assemblies"]
    for asm in ("Assembly-CSharp", "Assembly-CSharp-Editor"):
        assert result[asm]["added"] == [] and result[asm]["removed"] == []


def test_package_mismatch_is_a_tool_error(tmp_path):
    root = clone_sources(tmp_path)
    lock = root / PROJECT / "Packages" / "packages-lock.json"
    lock.write_text(lock.read_text(encoding="utf-8") + "\n", encoding="utf-8")
    with pytest.raises(cscheck.CsCheckError, match="packages-lock.json differs"):
        cscheck.check(root, steps=("signature",))
