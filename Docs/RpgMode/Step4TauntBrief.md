# Step 4 brief: locked enemy targets and the tank's group taunt

Repo worktree: /Users/joyfort/MyProjects/Tufu-rpg (branch feature/rpg-mode; steps 1-3 are committed). CB = Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase. Read Docs/RpgMode/Design.md section 6 (the design this brief implements) and Docs/RpgMode/Step2Brief.md "Hard constraints" first. Those constraints all still apply. In particular, every hunk in a pre-existing file must be either a pure insertion gated by `global::Codebase.Rpg.RpgModeConfig.Enabled` or an exactly equivalent substitution. With RPG off, the original game and pet mode must behave exactly as today.

Line numbers below can be off by a few lines; verify each one.

## Decisions (confirmed by the user, 2026-10-30)
- No WoW-style threat table. Use WC3-style targeting: an enemy locks onto a target and keeps it.
- Only 重甲矛兵 (species `pick`, role Tank) gets a taunt: a group taunt that is auto-cast.
- Taunt works on elites and bosses, with half the duration.

## 1. Locked targets (CB/Enemy/EnemyAttackSystem.cs)
`SelectAggroTarget` (~line 593) currently returns the nearest of Pudge and the living pets every frame. Add an RPG branch at its top, used by both the ground (~129) and flying (~353) callers. It must still set `enemy.AggroTarget` and return the distance in the caller's convention (squared or not).

The branch picks the first that applies:
1. **Taunted.** The enemy has an active buff whose `BuffDef.Taunt` is true, and that buff's `Source` is a living hero (Pudge, or a pet in `petsModel.Pets` with `IsAlive`). Target = that Source.
2. **Locked.** `enemy.AggroTarget` is a living hero, and its distance is at most `max(enemy.AggroRange, attack range) + RpgModeConfig.TargetLockExtraRange` (default 4). Target = keep it.
3. **Retaliate.** A hero damaged this enemy within `RpgModeConfig.RetaliateWindow` seconds (default 3) and is alive. Target = that hero. Record the last hero attacker and its time on `EnemyUnit` (new fields, reset in the same place `AggroTimer` is reset on spawn, ~EnemyUnit.cs:186). Set them where damage to an enemy is applied (find it in UnitDamageSystem or SkillService.DealDamage), gated by RPG, and only when the attacker is a hero.
4. **Nearest.** Fall back to the existing loop.

While taunted:
- the `retreat` condition in both callers must be false, so the enemy does not evade or return home;
- `playerUnavailable` (Pudge dead or asleep) still sends the enemy home.

The 30 m full-heal check stays Pudge-based, as now. `ClearAggroTarget` already clears the lock when the enemy goes home. Make sure it also runs under RPG (it is gated on `PetConfig.Enabled`; check that RPG implies it).

## 2. Taunt buff
- **CB/Skills/BuffDef.cs:** add `public bool Taunt;` after `Silence`, with a one-line comment in the file's style. It only means something on enemies.
- **`SkillService.ApplyBuff`** (~SkillService.cs:444): under RPG, when `def.Taunt` is set and the target is an elite or boss enemy, scale the new instance's duration by `RpgModeConfig.TauntEliteDurationScale` (default 0.5). Use the existing `RpgSpawnerRules` kind for the enemy's spawner.
  - A refresh or re-apply must keep the newest `Source`, so a second taunt re-points the enemy.
  - If the existing stack modes do not allow that, document what happens.
- **Add the buff `rpg_taunt` to the skill library:** Duration 3, IsDebuff true, Taunt true, no stat mods, Stack mode = refresh. Put it wherever the existing buffs `stun_08` / `slow_50` live (Resources/Skills/Species Skills.asset), or in a new RPG skills asset if SkillLibrary supports more than one. Pick whichever is less invasive and say which.

## 3. Group taunt skill
- **Skill `rpg_group_taunt`:**
  - TargetType self or none, Range 0, CastPoint 0.3, Backswing 0.2, Cooldown 12, InitialCooldown 2.
  - Effects: `ApplyBuffEffect` with an Area of enemies within 6 m of the caster and BuffKey `rpg_taunt`, plus a `SpawnVfxEffect` using an existing VFX key that reads as a shout or ground ring (pick one from SkillLibrary's VFX list and name it).
  - No damage. Skill level must not change the duration.
- **New cast condition `EnemiesAttackingAlliesCondition`** (CB/Skills/Conditions, same pattern as `EnemiesInRadiusCondition`):
  - fields `Radius` (6) and `MinCount` (1);
  - true when at least MinCount living enemies within Radius of the caster have an `AggroTarget` that is a unit other than the caster;
  - use it as the skill's only `UseWhen`.
- **Hero extra skills:** add `List<string> ExtraSkillKeys` to `HeroDefinition`, set `["rpg_group_taunt"]` on the `pick` definition in `Assets/Resources/Rpg Mode Config.asset`, and handle it in `RpgHeroSpawnSystem.SpawnCompanion` (~line 93).
  - The species loadout is installed as now. Then the extra skills are installed on the same unit, at the same level, without editing the shared species loadout asset. If `SkillService.Install` replaces rather than appends, build a temporary `UnitSkillLoadout` that copies the species loadout's skill list and appends the extra skill slots.
  - Make sure star / skill-level updates (~line 64) still apply to all skills.
  - If the protagonist path ever resolves to `pick`, ignore it; only companions get extras.
- **New config fields in `RpgModeConfig`** (with defaults; also written into the asset): `TargetLockExtraRange` 4, `RetaliateWindow` 3, `TauntEliteDurationScale` 0.5.

## 4. Feedback
When a taunt buff is applied to an enemy under RPG, show a short floating "嘲讽" text above it once per application. Reuse the floating-text path used for the XP/gold/shard popups in step 3. Chinese text goes through `PetStatsText.SetText`.

## 5. Dev check (do it, report the numbers)
Use the Unity CLI against the open editor (port 7801), as in earlier steps: `~/.unity/bin/unity recompile --project-path <P> --json`, `unity command ... editor.playmode start/stop`, `script eval_file` / `script run`. Do not close the editor and do not save scenes.
- Compile with 0 errors and 0 new warnings.
- In Play mode with RPG on, use `Tools/RPG/Recruit Island 1-2 Heroes`. Put pick (重甲矛兵) in the lineup, then pull a group of island-1 enemies onto a non-tank hero, such as the knife hero. Log each enemy's `AggroTarget` per second for ~10 s and report:
  - (a) the taunt cast and which enemies switched to 重甲矛兵;
  - (b) that they stayed on 重甲矛兵 for ~3 s and then kept the lock, rather than jumping to whoever is nearest;
  - (c) that an elite got ~1.5 s if one is reachable.
- Report any console errors.
- With RPG off (toggle the asset back after the test), enemies still pick the nearest unit. A code-read argument is enough here.

## Deliverable
Leave the changes uncommitted. Do not touch Main.unity, Assets/ThirdParty or Assets/Editor/RpgMode. Write a short report:
- files changed;
- where the buff and skill were added;
- how extra skills are installed;
- the dev-check results;
- anything you could not do.
