# Step 5 brief: instanced dungeon with deferred rewards and a 24 h lockout

Repo worktree: /Users/joyfort/MyProjects/Tufu-rpg (branch feature/rpg-mode; steps 1-4 are committed). CB = Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase. Read Docs/RpgMode/Design.md section 20 (the design this brief implements; rules there are confirmed by the user) and Docs/RpgMode/Step2Brief.md "Hard constraints" first. Those constraints all still apply. In particular, every hunk in a pre-existing file must be either a pure insertion gated by `global::Codebase.Rpg.RpgModeConfig.Enabled` or an exactly equivalent substitution. With RPG off, the original game and pet mode must behave exactly as today, including the Boss Tower.

Line numbers are not given; find each place yourself and verify it.

## Template to copy: the Boss Tower
The Boss Tower already does almost everything a dungeon needs. Read these before designing anything:
- `CB/BossTower/Commands/EnterBossArenaCommandHandler.cs`: warps Pudge into an arena in Main.unity, stores `playerModel.SavePositionOverride` (so a save taken inside reloads outside), revives pets into the arena with `petsModel.ReviveAll`, sets `playerModel.InBossTowerArena`, and keeps a raid component with the return position.
- `CB/BossTower/BossArenaProvider.cs` / `BossArenaSystem`: arena key, player and boss spawn points, bounds.
- `CB/BossTower/Flows/LeaveBossArenaFlow*`, `ExitBossArenaCommand*`, `EndBossFightFlow`: the way back out.
- `CB/Gates/Portal.cs` and `BossTowerPortalComponent`: the portal in the world.
- `CB/UI/BossTower/BossRaidVictoryView.cs`, `BossRaidRewardView.cs`, `BossTowerLeaveView.cs`: result and leave-confirm windows.

Under RPG the Boss Tower is closed, so its arenas in Main.unity are idle. The first dungeon uses one of them as its terrain. Find out how the Boss Tower is closed under RPG and make sure the dungeon does not reopen the tower's own UI or portal.

## Goal
With RPG on, island 1 has a dungeon portal. Walking into it (when not on cooldown) moves the whole party into a Boss Tower arena where several packs of island-1 enemies and one boss are spawned. Kills inside do not respawn and grant nothing on the spot; XP and gold are accumulated. Killing the boss pays everything out at once, shows a result window, sends the party back outside, and starts a 24 h rolling cooldown. Leaving early or being wiped discards the run; the next entry spawns everything fresh.

## 1. Data: no Main.unity edits
Do not edit Main.unity (the user has uncommitted changes in it). Everything is defined in data and created at runtime:
- New ScriptableObject `RpgDungeonDef` (CB/Rpg/Dungeon/), listed in `RpgModeConfig` as `List<RpgDungeonDef> Dungeons` (or stored inline in `Rpg Mode Config.asset`, whichever is simpler). Fields:
  - `Key` (e.g. `dungeon.island1`), `DisplayName` ("幽暗地穴" or similar, Chinese), `Island` (1);
  - `PortalPosition` / `PortalRotationY` on island 1: pick a reachable spot on the island-1 NavMesh near the island's middle, away from gates and quest NPCs; verify with `NavMesh.SamplePosition`;
  - `ArenaKey`: which Boss Tower arena to use (pick the one with the most open floor; say which and why);
  - `Packs`: list of { enemy source, local offset from the arena's player spawn point or absolute position, count, level }. Enemy source = an existing island-1 `EnemySpawnerConfig` (or whatever the spawner uses to build an enemy) so prefabs, skills and visuals come for free. 4-5 packs of 2-4 normal island-1 enemies at level 5;
  - `Boss`: one entry, using island 1's elite or gate boss as the source, level 6, placed at the arena's boss spawn point;
  - `CooldownHours` 24, `ClearBonusFraction` 0.5.
- Portal visual: reuse the Boss Tower portal prefab/VFX if one is reachable as an asset; otherwise any existing portal-like VFX. Spawn it at runtime when RPG is on. It needs a trigger (or a proximity check in a system) that starts the enter flow, and a label above it: dungeon name, or "冷却中 HH:MM:SS" while on cooldown.
- An exit portal inside the arena near the player spawn point, using the same visual. Walking into it opens a confirm window: "离开副本？本次进度和奖励将清空。" with 离开 / 取消.

## 2. Spawning enemies without spawner respawn
Dungeon enemies must not respawn and must not touch the persistent spawner state (charges, `SpawnerUd`, first-kill records, recruit drops, island mastery, quests, bounty counters). Find the least invasive way to create an `EnemyUnit` from an existing spawner config at an arbitrary position and level. Options to evaluate: `BossSpawnService`'s path, or the spawner's own spawn method on a detached runtime instance. Whatever you pick:
- Apply RPG scaling (`RpgEnemyScaling`) with the dungeon's level instead of the spawner's computed level.
- Tag the enemy as a dungeon enemy (a field on `EnemyUnit` set at spawn and reset where step 4 resets its fields; no new ECS tags).
- On death, the enemy is cleaned up as usual (devour animation is fine), but is never queued for respawn.
- Elite/boss kind for XP/gold multipliers comes from the source config, as `RpgSpawnerRules` does today.

## 3. Deferred rewards
In `RpgRewardService.Kill` (and anywhere else kill rewards are granted under RPG, check first-kill, recruit drops and bounty): when the enemy is a dungeon enemy, compute XP and gold exactly as now (level-difference factor uses the team level at kill time), but add them to the active run instead of granting. Show a small grey floating "+经验 +金币 (结算)" or similar so the player sees it counted; reuse the step-3 floating-text path, Chinese via `PetStatsText.SetText`.

Run state, a plain class held by a new `RpgDungeonService` (or model registered in `GameContext` like `RpgProgressModel`): dungeon key, start time, kills, pending XP, pending gold, list of living dungeon enemies, return position.

## 4. Flows
- **Enter**: refuse if not RPG, on cooldown, already inside, or Pudge dead. Otherwise: store the return position (the portal position, offset so the player is not standing in the trigger), set `SavePositionOverride` like the Boss Tower, warp Pudge (and the controlled hero if different, see Design.md section 19) to the arena player spawn, revive and full-heal all heroes into the arena the way `EnterBossArenaCommandHandler` does under pet mode, spawn all packs and the boss, start the run. Fade or a short black screen is welcome if the Boss Tower flow already has one.
- **Clear** (boss dies): grant pending XP and gold plus `ClearBonusFraction` of each through `RpgProgressModel.AddXp` / `CurrencyModel.Add(CurrencyKeys.RpgGold, ...)` in one go; record level before and after; set the cooldown end (UTC, from the game's time service, not `DateTime.Now` directly if a time service exists) in `RpgUd`; show the result window (section 5). On 领取: close, warp the party back to the return position, clear `SavePositionOverride`, despawn any leftover dungeon enemies.
- **Leave** (exit portal confirm): discard the run, despawn dungeon enemies, warp back.
- **Wipe**: find the path that currently sends the party back to the fountain under RPG (Pudge death or full wipe, whichever step 3 implemented). When it fires while a run is active: discard the run and despawn dungeon enemies. Then let the party go back to the fountain as usual, or warp to the portal if that is just as simple; say which.
- **Save/load mid-run**: the run is not saved. On load the party must be outside (the `SavePositionOverride` handles position; check nothing else like `InBossTowerArena`-style flags leaks across). Make sure enemies from a previous run cannot persist.

## 5. Result window
New view under CB/Rpg/UI (or reuse `BossRaidVictoryView` if it can be fed arbitrary text without touching original behaviour; a new, simple code-built panel is fine and probably safer). Content, Chinese via `PetStatsText.SetText` / `ApplyCjkFont`:
- title "副本通关" and the dungeon name;
- 用时 mm:ss, 击杀 N;
- 经验 +X (怪物 A + 通关奖励 B), 金币 +Y (same split);
- 等级 a → b, only when the level changed;
- "下次可挑战：24 小时后";
- one button 领取.
Parent it under HudContainer as the step-2 constraints say; it should block input to the joystick while open (a full-screen dimmer with raycastTarget on is fine, this one is meant to block).

## 6. Save
Add to `RpgUd` a list of { dungeon key, cooldown end UTC ticks }. RpgUd is RPG-only, so adding a child there is allowed; follow how RpgUd's existing children are declared and keep old RPG saves loading (missing child = no cooldowns).

## 7. Dev tools
Under the existing `Tools/RPG` menu (find RpgGrowthTools.cs): "Teleport To Dungeon Portal", "Reset Dungeon Cooldowns", and "Kill All Dungeon Enemies Except Boss" for fast testing.

## 8. Dev check (do it, report the numbers)
Use the Unity CLI against the open editor on port 7801, as in earlier steps (`~/.unity/bin/unity recompile --project-path <P> --json`, `unity command ... editor.playmode start/stop`, `script eval_file`). Do not close the editor and do not save scenes.
- Compile with 0 errors and 0 new warnings.
- Play mode, RPG on, recruit island 1-2 heroes with the existing tool. Then:
  (a) teleport to the portal, enter; report how many enemies spawned, their levels and HP, and that all heroes are inside;
  (b) kill a few; confirm team XP and gold did not change and the run's pending numbers did; wait past the normal respawn time (45 s) and confirm nothing respawned;
  (c) leave through the exit portal; confirm the run was discarded; re-enter and confirm a full fresh spawn;
  (d) clear it (kill-all tool, then the boss); report pending vs granted XP/gold, the level change, capture a game_view screenshot of the result window (save under Assets/Temp, copy it to Docs/RpgMode/step5_result.png, delete the Assets copy and its .meta);
  (e) after 领取 the party is outside, the portal shows a ~24 h countdown and refuses entry; reset the cooldown with the tool and enter again;
  (f) die inside (set Pudge HP to 0 or use whatever kills the party) and confirm the run is discarded and the party is outside the dungeon.
- Report console errors.
- With RPG off: no portal, Boss Tower unchanged. A code-read argument is enough.

## Deliverable
Leave the changes uncommitted. Do not touch Main.unity, Assets/ThirdParty or Assets/Editor/RpgMode. Write a short report: files changed; which arena and enemy sources you used and why; how enemies are spawned without respawn; how each flow (enter, clear, leave, wipe, load) works; dev-check results; anything you could not do or decided differently from this brief.
