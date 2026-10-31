# H4 design: equal death, revive and team wipe

This replaces the H4 paragraph in HeroRefactorBrief.md with a concrete design.

- Do it as **one change**.
- The game must compile and stay playable afterwards.
- H1–H3 are staged in git.
- CB = Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase.
- Line numbers are approximate; verify each one.

## Concepts
- **Down**: a hero with 0 HP while at least one other hero is alive. Every hero can be down, the protagonist included.
- **Wipe**: the moment the last living hero goes down. The original Pudge death sequence (`ProcessPlayerDeath` → `PlayerReviveSystem` → `PlayerDeathSequenceCommand` → `PlayerUnit.DeathSequenceCoroutine` → `RevivePlayerCommand`) then runs once and revives everyone at the fountain.
- **InWipe**: true from the wipe until `PlayerUnit.HandleWalkEnabled` finishes. Read it as `playerModel.Unit.IsDying || playerModel.Unit.IsReviving`; those two flags are now set **only** by the wipe path. Expose it as `HeroRosterModel.InWipe` (a property that reads PlayerModel) so every system asks the same question.

## 1. One death entry point: `HeroDeathService` (new, CB/Rpg/HeroDeathService.cs, registered in GameContext)
- `void OnHealthDepleted(HeroModel hero, string killer)` is called wherever a hero's HP reaches 0. Today those places are:
  - `UnitDamageSystem.ApplyDamageToHero`, around lines 130–133;
  - `UnstackSystem` (~37);
  - `BossRaidSystem` (~273), which can stay as-is if the boss tower is unreachable under RPG. Say which you chose.
  
  It does the following:
  1. Ignore the call if the hero is already down or InWipe.
  2. Set `hero.IsDown = true` and `hero.ReviveAt = now + PetConfig.ReviveCooldown` (15 s). Use a fallback of 15 if the config value is 0.
  3. Companions: call the existing `petsModel.OnPetDied(hero.Pet)`, which handles the agent, markers and death visual. Keep `PetModel.ReviveAt` in sync, or make `HeroModel.ReviveAt` the only source and have PetsModel read it.
  4. Protagonist: call the new `PlayerUnit.EnterDown()`:
     - set HP to 0;
     - set `CurrentTarget = null`;
     - stop and disable the agent if it is enabled and on the NavMesh;
     - remove `NavMeshAgentActiveMarker`;
     - set walking false and fire `DieTrigger`;
     - play `deathEffect`;
     - hide the health bar.
     It must **not** touch `IsDying`, `IsReviving`, `CanMove`, `UnitDeadMarker` or `PlayerDeathEvent`.
  5. Cancel the hero's skill cast (`skillService.CancelCast`).
  6. If no hero is alive any more → `Wipe(killer)`. Otherwise, if the hero was controlled → `roster.ForceControl(first living hero by SlotOrder)`.
- `void Wipe(string killer)`:
  1. `roster.ForceControl(protagonist)`, so the camera follows Pudge through the sequence.
  2. `unitDamageSystem.ProcessPlayerDeath(killer, pudge)`.
  3. Clear the timer revives: set `IsDown` false later, in the revive step, not here.
  
  `ProcessPlayerDeath` has to handle a Pudge who is already down:
  - **Pudge not down** (he was the last to fall): run `StartDeathAnimation` as today.
  - **Pudge already down**: skip `StartDeathAnimation`. `PlayDeathAnimation` calls `Agent.isStopped` on an agent that is now disabled, and that call throws. Make sure `IsDeathAnimationActive` is false so `PlayerReviveSystem` starts the sequence on the next frame. `DeathSequenceCoroutine` already guards against a disabled agent. Its `WaitForAnimatorStateEnd("Dying")` times out after 3 s if he is no longer in that state, which is acceptable.
- `void Revive(HeroModel hero, Vector3 position, float yaw, double health)`:
  - **Companions**: the existing `petsModel.Revive` path.
  - **Protagonist**: the new `PlayerUnit.ReviveFromDown(position, yaw, health)`:
    - enable the agent, `Warp`, set `NavMeshAgentActiveMarker`;
    - reset the animator to its default state (`animator.Rebind(); animator.Update(0f);`);
    - show the health bar and `aliveParticles`;
    - set HP.
  - Finally set `hero.IsDown = false`.

## 2. Revive rules
- **`HeroReviveSystem`** (new; replaces `PetReviveSystem`, which stops being registered). Each frame, unless InWipe or a dungeon run is active, revive every down hero whose `now >= ReviveAt`. Revive it at the controlled hero's follow position for its `SlotOrder` (use `PetsModel.GetFollowPosition` + `TrySamplePosition` as pets do today), with `ReviveHealthFraction × max HP`; use 0.2 if the fraction is 0.
- **Wipe revive.** `RevivePlayerCommandHandler` calls `petsModel.ReviveAll` (companions) and sets Pudge's HP. Afterwards, set `IsDown = false` on every hero. `PlayerUnit.HandleWalkEnabled` already restores Pudge's agent and `CanMove`.
- **Fountain** (`FirecampSystem`):
  - remove the early return that checks Pudge dying, unless InWipe;
  - while the controlled hero is in the fountain radius, revive every down hero (Pudge included) through `HeroDeathService.Revive`;
  - the heal loop covers every living hero, Pudge included.
- **Dungeon** (`RpgDungeonSystem`):
  - the run fails only when every hero is down or InWipe; replace the `pudge.IsDying || pudge.IsDead` check (~88);
  - no timer revives inside a run (see `HeroReviveSystem`);
  - entry, exit and the party warp use the controlled hero and move every living hero;
  - down heroes are revived at the dungeon exit or wipe as today.

## 3. Replace every Pudge-global "dead" check
Every system must ask about the specific hero, or about InWipe, and never about Pudge as a stand-in for the whole party. Sites found so far are listed below; also search for `IsDying`, `IsDead`, `IsReviving` and `IsDeathAnimationActive` on `playerModel.Unit` / `PlayerUnit` and fix any others.
- `HeroAttackSystem`: `paused = !playerModel.CanMove || player.IsSleeping`. Drop the Pudge `IsDying`/`IsDead` check; per-hero `IsAlive` already skips down heroes.
- `HeroMovementSystem`:
  - `paused` as above;
  - the protagonist-follower branch stops only while InWipe;
  - down heroes are skipped.
- `EnemyAttackSystem`:
  - `playerUnavailable` = InWipe || player.IsSleeping || !playerModel.CanMove || no hero alive;
  - the default target in `SelectAggroTarget` = the nearest **living** hero (never a down Pudge);
  - the 30 m reset measures the nearest living hero;
  - `IsLivingHero` must return false for a down Pudge.
- `SkillAutoCastSystem` (~148, ~305): companion eligibility must not depend on Pudge. Pudge's own cast is cancelled while he is down.
- `HeroControlSystem` / `HeroRosterModel`:
  - force control to Pudge only while InWipe;
  - when the controlled hero is down or withdrawn, move control to the first living hero by `SlotOrder`;
  - `TrySwitch` refuses a down target and refuses while InWipe;
  - `RpgControl.ControlledUnit` must never return a down hero outside a wipe.
- `PetAttackSystem` / `PetMovementSystem` are no longer registered, so leave them for H5.
- `HeroModel.IsAlive` = `Unit != null && !IsDown && !Unit.IsDead && !(IsProtagonist && Unit.IsDying)`. Use it everywhere for both kinds of hero.
- `SkillTargetQuery.GetCandidates` already uses `IsAlive`.

## 4. One regen system and one health-bar system
- **`HeroRegenSystem`** (new): every living hero gains `HealthRegen × dt`, capped at max. It replaces `PlayerRegenSystem` and `PetRegenSystem`; unregister both. Drop `AbilityHealthRegenMultiplier`.
- **`HeroHealthBarSystem`** (new): ensure each living hero has a bar via `UnitCore.SetupHealthBar`, using the same prefab the pets use today, and hide it while the hero is down. It replaces `PetHealthBarSystem`. Make sure Pudge's existing bar from `PlayerModel.InitializeUnit` is not duplicated: either reuse it or stop creating it there.

## 5. UI
`RpgLineupCell`:
- the protagonist cell shows the same revive countdown and dimming as companion cells while Pudge is down;
- companion cells read `HeroModel.ReviveAt`;
- tapping a down hero does nothing.

## Report
- Files changed.
- Every Pudge-global death check you changed (file:line, old → new).
- Anything you left for H5.
