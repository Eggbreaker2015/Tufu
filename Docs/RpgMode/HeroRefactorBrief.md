# Hero refactor brief: one hero type, one set of systems, RPG-only branch

Repo worktree: /Users/joyfort/MyProjects/Tufu-rpg (branch feature/rpg-mode). CB = Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase.

A verified code map of today's paths is at /private/tmp/claude-501/-Users-joyfort-MyProjects-Tufu/285beea2-7448-476c-9eba-cbcb39875c21/scratchpad/hero-map.md. It is read-only; read it first, because its file:line facts are the starting point. Line numbers drift, so verify each one before editing.

## Decisions (user, 2026-10-30)
1. **This branch is RPG-only.** The original gameplay and pet mode are being removed here.
   - `RpgModeConfig.Enabled` and `PetConfig.Enabled` are always true.
   - The old rule "only gated insertions in original files" is gone. Rewrite or delete original code freely when the task needs it.
   - Keep systems RPG still uses: islands, spawners, quests, chests, gates, bosses, dungeon, skill system, NPCs, saves, camera.
2. **The protagonist is a hero like any other.** Pudge differs only by body (prefab, animator) and hero definition. All heroes run the same systems:
   - stats, skills, basic attack;
   - movement and AI, with control as an input;
   - damage taken, regen, health bar;
   - death, revive and team wipe.
3. **Every hero, Pudge included, dies alone.**
   - A dead hero revives after 15 s (`PetConfig.ReviveCooldown` / `ReviveHealthFraction`, today's values) at the controlled hero.
   - Only when every hero is dead does the team wipe: the existing fountain sequence runs once and revives everyone.
   - Inside the dungeon the existing rule stays, applied to all heroes: no timer revives, and the run fails when every hero is dead (today it fails when Pudge alone dies).
4. **The protagonist's original-only mechanics are removed:**
   - weapon item ability states;
   - rage;
   - the weak-enemy and event-golem 2.5× attack speed-up (today it applies to pets too; remove it everywhere);
   - `PlayerModel` ability bonuses (AbilityDamageFlat/Multiplier, AbilityHealthRegenMultiplier, NextHit*, BlockSuperCrit*, lethal-ignore, thorns and the other player proc state at PlayerModel.cs ~509);
   - item armour on incoming damage;
   - live-ops event and island-mastery damage multipliers on hero hits.
5. **One attack-speed stat that every hero reads.**
   - Add `StatType.AttackSpeed = 36`, appended after `VisualScaleMultiplier`. Base 1 on every hero.
   - Attack interval = attack item `AttackCooldown` / (AttackSpeed × the existing skill interval factor).
   - Equipment and buffs will add to it later (Flat +0.08 = +8%).

## Target shape
- **`HeroModel` is the single identity of a hero.** Fields:
  - Definition, Unit (`HeroUnit`), Item (the roster `ItemModel` for companions, null for the protagonist), IsProtagonist, alive/dead state, revive time;
  - attack state: a runtime attack `ItemModel` built from `HeroDefinition.AttackItemKey`, cooldown, in-progress, current target. The attack item is never added to inventory and has no ability states.
  - The protagonist definition gets the AttackItemKey of today's `PlayerConfig.PlayerAttackItem`.
- **`HeroRosterModel` is the only hero list.** Every system that iterates heroes reads the roster, including:
  - enemy targeting;
  - `SkillTargetQuery.GetCandidates`;
  - firecamp;
  - `UnitDamageSystem`;
  - auto-cast;
  - revive, movement, lineup UI and hero card.
  `PetsModel` / `PetModel` disappear by the end.
- **`HeroUnit` is the body adapter.** Each body (PlayerUnit, PetUnit) implements:
  - walking;
  - attack animation with a speed parameter;
  - death and revive visuals;
  - health-bar anchor.
  Systems talk only to `HeroUnit`.
- **One stats builder** for every hero. `HeroStats` applies:
  - the base (level-1 Health 120, Damage 10, UniversalDefenceFlat 2; today Pudge gets these via Player Config plus `rpg.base`);
  - role ratios, `rpg.level`, `rpg.star` (the protagonist is star 1), and AttackSpeed.
  The protagonist no longer takes stats from `PlayerConfig` / `LastStats`.
- **Skills** are installed for every hero in one place at spawn, from its definition. The protagonist uses today's hero loadout. Remove the Pudge-only install in `SkillAutoCastSystem`.
- **Damage contract.**
  - Outgoing hero hits use: the Damage stat, the typed flat and percent stats for the attack's damage type, crit and super crit from stats, and the enemy's resist and armour.
  - Incoming hits on any hero use: defence, universal %, evasion, block and the GlobalDefence skill factor.
  - Lifesteal stays, for all heroes.
  - Enemy-side calculations are unchanged.
- **Party anchor rules.** The places that use Pudge as the anchor today become:
  - saved position = the controlled hero's position;
  - gate triggers fire for the controlled hero;
  - the enemy 30 m reset measures the nearest living hero;
  - "player unavailable" means no living hero;
  - dungeon entry, exit and result use the controlled hero, and the party warp moves every hero.
- **Control switching.**
  - When the controlled hero dies, control moves to the first living hero in lineup order.
  - Withdrawing the controlled hero does the same.
  - While every hero is dead, control stays on the protagonist for the wipe sequence and camera.

## Stages
Each stage must compile and be Play-testable on its own. Stop after each stage and write a report. Do not commit.

**H1: RPG always on, plus hero identity, stats and skills.**
- Make both toggles constant true. Remove the asset toggle's effect, but keep the serialized field if removing it breaks assets.
- Build the HeroModel/roster as the source of truth for identity, stats and skills, and add `StatType.AttackSpeed`.
- Migrate the hero-list readers listed above to the roster. The existing attack, movement and revive systems may still run for now; feed them from the roster if needed.

**H2: one basic-attack system.**
- `HeroAttackSystem` replaces `PlayerAttackSystem` and `PetAttackSystem`.
  - Controlled hero: today's controlled-pet target rule, within range.
  - AI heroes: `HeroAi`.
- Fixed cast point from the attack item config. Emit `WeaponHitEvent` with Weapon = the hero's attack item. Play the attack animation through `HeroUnit`.
- Apply the damage contract in `DamageCalculator` / `UnitDamageSystem`, with one hero branch for both directions, and remove decision 4's mechanics there.

**H3: one movement system.**
- `HeroMovementSystem` replaces `UnitMovement`'s Pudge path, `HeroControlSystem.Move`, `ProtagonistAiSystem` and `PetMovementSystem`.
  - Controlled hero: joystick, camera-relative, with the skill movement factor.
  - Every other living hero: follow the controlled hero, engage, leash 12 m, teleport 18 m (today's `PetConfig` values).
- Both bodies are driven by their NavMeshAgent the same way.

**H4: equal death, revive and wipe.**
- Apply decision 3 and the control-switching rules.
- Replace every Pudge-global death assumption at once. The map's "Solo-death audit" list covers:
  - EnemyAttackSystem availability and the default target;
  - SkillAutoCast eligibility;
  - PetAttack pause;
  - HeroControl and roster fallbacks;
  - revive blocking;
  - firecamp's early return;
  - dungeon wipe;
  - `PlayerUnit`'s death sequence, which must run only on a full wipe;
  - global `CanMove`.
- One regen system and one health-bar system for all heroes.
- The lineup bar shows the revive countdown for the protagonist cell too.

**H5: delete dead hero and pet code.**
Remove:
- `PetsModel`, `PetModel` and the replaced Player/Pet systems;
- pet spawn, stat sync and collection bonus;
- the pet collection book;
- universal shards;
- rage;
- the toggle branches made dead by H1 (simplify the `if`s);
- now-unused `PlayerModel` fields and the ability effects that only fed them.

Clean registrations in `Core/SystemsInstaller.cs` and `Core/Boot.cs`, and serialized references in scenes and prefabs.

Leave amulets, VIP, crystals, shop/IAP and simulation/forecast for a later cleanup. Hide them in RPG if they are not already hidden.

## Rules for every stage
- Work only in /Users/joyfort/MyProjects/Tufu-rpg. Never touch /Users/joyfort/MyProjects/Tufu.
- Do not commit. Do not edit `Assets/Scenes/Main.unity` unless a removed component forces it, and if it does, say so. Do not touch `Assets/ThirdParty` or `Assets/Editor/RpgMode`.
- Chinese UI text goes through `PetStatsText.SetText`.
- Match the surrounding code style: comment density, naming, one-line guards.
- Report:
  - files changed;
  - what was deleted;
  - every behaviour change you know of;
  - anything left half-migrated for a later stage.

## Reviewer's Play checks (run by Claude with the Unity CLI after each stage)
- H1: fresh RPG save; recruit via `Tools/RPG/Recruit Island 1-2 Heroes`; the stats of Pudge and two companions match HeroStats; skills are installed at star level; AttackSpeed = 1 on all; no console errors.
- H2: each hero attacks controlled and as AI; damage numbers are sane; a +0.5 AttackSpeed modifier visibly shortens the interval; the axe still hits enemies and trees.
- H3: switch through all heroes while moving; leash and teleport work; skill dashes and knockbacks still work.
- H4: Pudge dies alone while others fight and cast → revives after 15 s; the controlled companion dies → control moves; full wipe → one fountain sequence → all revived; the dungeon run fails only on full wipe.
- H5: compile clean; fresh and old saves load; 10 minutes on islands 1-2 without exceptions.
