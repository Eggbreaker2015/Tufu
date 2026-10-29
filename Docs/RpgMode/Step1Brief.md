# Step 1 brief: RPG mode unified heroes with control switching

Repo worktree: /Users/joyfort/MyProjects/Tufu-rpg (branch feature/rpg-mode). CB = Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase. Read Docs/RpgMode/Design.md and Docs/PetSystem/Step1Design.md first.

Deliver this as three sequential tasks: 1A, 1B, 1C. Each must compile on its own and gets its own commit. Stop after each task for review.

## Goal
With the RPG toggle on, the player gets a squad of three heroes on island 1:
- Pudge, the protagonist (PlayerUnit).
- golem and cultist (PetUnit bodies built from the Pet Config species with those keys).

Every hero uses the same stat base, targeting and follow AI. The player switches which hero they control (Tab, keys 1-3, HUD portrait buttons). The camera, joystick and WASD drive the controlled hero, and every other hero runs AI.

With the toggle off, the original game and pet mode must behave exactly as today.

## Hard constraints
1. **Toggle definition.** `RpgModeConfig.Enabled => Instance != null && Instance.enabled && PetConfig.Enabled`. RPG deliberately runs on top of pet mode, so companions reuse the PetUnit, PetModel and PetsModel pipeline.
2. **Allowed hunks in pre-existing files.** Every hunk must be one of these:
   - (a) A pure insertion guarded by `RpgModeConfig.Enabled`.
   - (b) The HeroUnit base-class change plus the CurrentTarget hoist.
   - (c) A substitution by an `RpgControl` helper that returns exactly the old expression when RPG is off.
   - (d) The behavior-neutral `PetUnitFactory` extraction and the `lastOwner` type widening.

   Do not reformat or rewrite any other original line. Many methods carry "Native ELF VA" comments; keep their bodies byte-identical apart from gated insertions.
3. **Never re-point `PlayerModel.Unit`. Never move or add `PlayerTag`.** No new ECS tags in step 1.
4. **HeroUnit must not declare Awake or Start, and must have no serialized fields.** UnitCore's private Awake and Start do the registration (Unit/UnitCore.cs:63,70).
5. **Do not add `RpgUd` or any new `GameUserData` child in step 1.**
6. **Never call `CameraFollowSystem.Unfollow()` on a switch.**
7. **Companion despawn order.** If you ever despawn companions, call `World.RemoveEntity` before `Destroy`, as PetSpawnSystem.Despawn (:127-153) does.

## 1A: Foundation (no companions yet)
- **`CB/Rpg/RpgModeConfig.cs`** (new, plus a `.meta` with a fixed GUID) and **`Assets/Resources/Rpg Mode Config.asset`** (YAML referencing that GUID, `enabled: 1`).
  - Copy the PetConfig pattern: lazy `Resources.Load`, then `[StaticReset]` (Pet/PetConfig.cs:120-141).
  - Fields:
    - `bool enabled`
    - `List<HeroDefinition> Heroes`
    - `List<string> StartingHeroKeys` = protagonist, golem, cultist
    - `float SwitchCooldown` = 0.3
    - `float SwitchTeleportGrace` = 1
    - `float AiAcceleration` = 20
    - `float AiAngularSpeed` = 720
  - Also add an editor menu item `Tools/RPG/Create Config` as a fallback way to create the asset.
- **`CB/Rpg/HeroDefinition.cs`** (new, `[Serializable]`). Fields:
  - `Key`, `IsProtagonist`
  - `PetItemKey`: the visual. Resolve it via `PetConfig.Instance.Find(itemsProvider.Get(key))`.
  - `AttackItemKey`: an ItemConfig key. Use `golem` and `cultist`; these exist as MonoBehaviour/pet_golem.asset and pet_cultist.asset.
  - `HealthRatio`, `AttackRatio`, `DefenceFlat`, `AttackRangeOverride` (0 = none), `DisplayName`, `Sprite Icon`.
  - Asset values: protagonist 1/1/0; golem HealthRatio 1.6, AttackRatio 0.7, DefenceFlat 2; cultist HealthRatio 0.8, AttackRatio 0.9.
- **`CB/Unit/HeroUnit.cs`** (new): `public abstract class HeroUnit : UnitCore`.
  - Members: `public EnemyUnit CurrentTarget { get; set; }`, `public bool IsControlled { get; internal set; }`, `public abstract void SetWalking(bool value);`.
- **`CB/Player/PlayerUnit.cs`**
  - Change the base class to `HeroUnit` (:23) and delete the `CurrentTarget` property (:357).
  - Add `public override void SetWalking(bool v) => animator.SetBool(IsWalking, v);`.
- **`CB/Pet/PetUnit.cs`**
  - Change the base class to `HeroUnit` (:11) and delete `CurrentTarget` (:52).
  - Make `SetWalking` (:196) `public override`.
- **`CB/Rpg/HeroModel.cs`** (new). Fields:
  - `HeroDefinition Definition`, `HeroUnit Unit`
  - `PetModel Pet`: null for the protagonist
  - `int SlotOrder`
  - `float PathFailTimer`
  - `EnemyUnit AiTarget`
  - `bool IsProtagonist`
  - `bool IsAlive`: protagonist = `Unit != null && !Unit.IsDying && !Unit.IsDead`; companion = `Pet != null && Pet.IsAlive`.
- **`CB/Rpg/HeroRosterModel.cs`** (new, a `Model`).
  - Members: `List<HeroModel> Heroes`, `HeroModel Controlled`, `event Action<HeroModel, HeroModel> ControlledChanged`, `float LastSwitchTime`, `float GraceUntil`.
  - `bool TrySwitch(HeroModel next)`: checks the preconditions listed under 1C, then calls `Apply`.
  - `void ForceControl(HeroModel next)`.
  - `HeroModel Get(UnitCore unit)`.
  - Register it in `Core/Boot.cs` right after PetsModel (:40). Registering unconditionally is fine, because it is empty when RPG is off.
- **`CB/Rpg/RpgControl.cs`** (new, static)
  - `UnitCore ControlledUnit(PlayerModel pm)` returns `RpgModeConfig.Enabled && roster?.Controlled?.Unit != null ? roster.Controlled.Unit : pm.Unit`. Get the roster via `GameContext.Has/Get<HeroRosterModel>()`.
  - `bool IsControlled(UnitCore u)` returns `RpgModeConfig.Enabled ? (u is HeroUnit h && h.IsControlled) : u is PlayerUnit`.
  - `bool IsAiHero(UnitCore u)` returns `RpgModeConfig.Enabled && u is HeroUnit h && !h.IsControlled`.
- **Separate save: `CB/Core/UserData/GameDataService.cs`**
  - Constructor (:156-161): after the `#if UNITY_EDITOR` override, `if (RpgModeConfig.Enabled) saveDirectory = Path.Combine(saveDirectory, "Rpg");`.
  - `Initialize` (:169-174): `if (RpgModeConfig.Enabled) migrateLegacy = false;`.
- **Pet systems return early under RPG**
  - `CB/Pet/Systems/PetSpawnSystem.cs` OnUpdate: insert `if (RpgModeConfig.Enabled) return;` right after the `!PetConfig.Enabled` block, at about :52. Without it, `pet.Slot.IsEquipped` at :59 would throw for slot-less RPG companions.
  - `PetStatsSyncSystem.OnUpdate` (:49) and `PetCollectionBonusSystem.OnUpdate`: `if (RpgModeConfig.Enabled) return;` as the first line.
- **Modifier sources**
  - `CB/Unit/ModifierSource.cs`: add `RpgHero = 23, RpgItem = 24, RpgFirstKill = 25`.
  - `PlayerModel.IsRuntimeOnlySource` (:709-712): also return true for these three.
- **`CB/Rpg/Systems/RpgHeroSpawnSystem.cs`** (new SystemBase; register in `Core/SystemsInstaller.cs` right before PetSpawnSystem at :75).
  - Returns when RPG is off.
  - In 1A it only waits for `playerModel.Unit` and then registers the protagonist HeroModel with SlotOrder 0. It sets it as Controlled through `ForceControl`, which sets `Unit.IsControlled = true`.
- **Check:** RPG on starts a fresh prologue save under `.../Rpg/`, Pudge plays exactly as in pet mode, and no slot pets spawn. RPG off: pet mode and the original game are unchanged.

## 1B: Companions
- **`CB/Pet/PetUnitFactory.cs`** (new static class)
  - `PetUnit Create(ItemModel item, PetDefinition def, Vector3 pos, Quaternion rot, float agentSpeed)`.
  - Move the construction block of `PetSpawnSystem.Spawn` (:97-115) into it verbatim, from `new GameObject` through `petTagStash.Set`. Keep the PetTag set and `Injector.InjectInto`.
  - `PetSpawnSystem.Spawn` then calls it; its behavior must stay identical.
- **`RpgHeroSpawnSystem`**: for each non-protagonist key in StartingHeroKeys:
  - Build `ItemModel item = new ItemModel(lifetime, itemsProvider.Get(def.AttackItemKey))`.
  - Set `AbilityDamageMultiplier`, `AbilityAttackSpeedMultiplier` and `AbilityCritMultiplier` to 1, as PlayerModel.cs:733-737 does.
  - Call `item.AbilityStates.Clear()`.
  - Resolve `PetDefinition petDef` from PetItemKey.
  - Create the unit at `PetsModel.GetFollowPosition(pudge, slotOrder)`, sampled with `PetsModel.TrySamplePosition`.
  - `unit.Stats = HeroStats.Build(def)`, then `BuildStatLookup()`, and set `CurrentHealth` to max.
  - Register `new PetModel(null, item, petDef, slotOrder)` (slot orders 1 and 2) in `petsModel.Pets`, with `pet.Unit = unit`.
  - Install the loadout `SkillLibrary.FindLoadout(SkillOwnerKind.Pet, item.Key)` exactly as PetSpawnSystem.cs:117-120 does.
  - Add a HeroModel with `Pet = pet`.
  - From here, PetMovementSystem, PetAttackSystem, PetReviveSystem, PetRegenSystem and PetHealthBarSystem, the pet damage branch, enemy aggro and the fountain all pick it up unchanged.
- **`CB/Rpg/HeroStats.cs`** (new): `Build(HeroDefinition def)`.
  - For every stat in `PlayerConfig.Stats`, create `new Stat(lifetime, base, type)`. Damage base = base + `PetConfig.Instance.PlayerBaseAttack`, mirroring PlayerModel.GetInitialBaseValue (:546-552).
  - Add `ModifierSource.RpgHero` modifiers:
    - Health: PercentMultiplicative, Value = HealthRatio.
    - Damage: PercentMultiplicative, Value = AttackRatio.
    - UniversalDefenceFlat: Flat, DefenceFlat.
    - AttackRange: Flat, the override minus base, if the override is set.
  - Follow the modifier shape used at PetStatsSyncSystem.cs:176. Skip ratios equal to 1. Do not touch Pudge's stats in step 1.
- **Damage gates (RPG only)**
  - `Services/Damage/DamageCalculator.cs:91`: skip the PetStats defence ratio when RPG is on.
  - `:138`: when RPG is on and the source is a HeroUnit, `weaponDamage = 0`.
  - `Skills/SkillService.cs:315-323`: when RPG is on, add no pet own-attack and no item.Damage.
- **Check:** golem and cultist follow Pudge, fight, take damage, show health bars, regen, die, revive after 15 s at 20 % HP, revive at the fountain, and revive with Pudge's firecamp sequence. No InvalidCastException or NullReferenceException.

## 1C: Control switching
- **`CB/Rpg/Systems/HeroControlSystem.cs`** (new; register after PetAttackSystem). Each frame:
  1. Read input: `Input.GetKeyDown(Tab)` picks the next living hero by SlotOrder, and `Alpha1..Alpha3` pick by index. Taps from the HUD queue a request.
  2. Check auto-switch. If Pudge `IsDying` or `IsReviving` and Controlled is not Pudge, call `ForceControl(Pudge)`. Else, if Controlled is not alive, switch to Pudge if he is alive, otherwise to the first living hero.
  3. Apply at most one switch. Refuse it if any of these holds:
     - `!playerModel.CanMove`
     - `CameraFollowSystem.IsOverriding`
     - Pudge is dying or reviving
     - the target is not alive or is already controlled
     - `Time.time - LastSwitchTime < SwitchCooldown`

     Apply steps:
     - Old hero: `IsControlled = false`. If the old hero is Pudge, set `agent.acceleration = AiAcceleration` and `agent.angularSpeed = AiAngularSpeed`.
     - New hero: `IsControlled = true`, then `agent.ResetPath()` if on the NavMesh, `agent.velocity = Vector3.zero`, and `SetWalking(false)`.
     - Set `GraceUntil = now + SwitchTeleportGrace` and raise `ControlledChanged`.
  4. If the controlled hero is a PetUnit, move it with a port of `UnitMovement.Update` and `UpdateVisualTarget` (Unit/UnitMovement.cs:42-95), with these differences:
     - Bail out on IsDead, stunned, displaced or channeling (use FriendlyDebuffs as in :46).
     - `speed = CanMove ? MovementSpeed specific value × skillService.GetMoveSpeedFactor(unit) : 0`.
     - The input angle comes from the camera's flattened forward vector.
     - Call `agent.Move`, then turn at 460°/s toward the visual target (own CurrentTarget, else the nearest enemy with 4 m / 6 m hysteresis) or toward the move direction.
     - `SetWalking(input.magnitude > 0)`.
- **`Unit/UnitMovement.cs`**: right after `if (!isPlayer) return;` (:42), insert `if (RpgModeConfig.Enabled && !player-unit.IsControlled) return;`. Use `GameContext.Get<PlayerModel>().Unit.IsControlled`. Pudge keeps the native mover while he is controlled.
- **`Player/PlayerUnit.cs` Update (:393-405)**: insert at the top `if (RpgModeConfig.Enabled && !IsControlled && !IsDying) { ... return; }`. Inside:
  - walking = `Agent.enabled && Agent.velocity.sqrMagnitude > 0.01f`
  - `animator.SetBool(IsWalking, walking)`, `animator.speed = 1`
  - footstep volume = `0.15 * clamp01(velocity / max(speed, 0.01))`
  - `rig.localScale = Vector3.one * 1.35f`
- **`Player/PlayerAttackSystem.cs:104`**: when RPG is on and Pudge is not controlled, `speedTarget = 0`.
- **`CB/Pet/HeroAi.cs`** (new static helper). Copy these from `PetMovementSystem.cs:155-190` and generalize them to `UnitCore` and `NavMeshAgent`: `MoveTo`, `Chase`, `FlatSqrDistance`. Also add `SelectFollowerTarget(HeroUnit self, HeroUnit leader, float engageSqr)`, `FindAggressor` and `FindNearest`, copied from `PetAttackSystem.cs:208-275`. Leave PetMovementSystem's and PetAttackSystem's own private copies untouched, so pet-mode code does not change.
- **`CB/Rpg/Systems/ProtagonistAiSystem.cs`** (new; register after PetMovementSystem). Runs when RPG is on and Pudge is alive, not dying and not controlled. It applies the PetMovementSystem rules to Pudge's agent, with owner = `RpgControl.ControlledUnit`:
  - Ring slot `PetsModel.GetFollowPosition(owner, 0)`.
  - Leash `PetConfig.LeashRadius`. Teleport past `TeleportDistance` or when off the NavMesh, skipped while `Time.time < GraceUntil` unless off the NavMesh. Teleport = sample, then `agent.Warp`, then `ResetPath`.
  - `agent.speed`: MovementSpeed × `FollowSpeedMultiplier` while following, own MovementSpeed while chasing.
  - Target via `SelectFollowerTarget`, stored in `HeroModel.AiTarget`. Clear it when paused (`!CanMove`).
  - When `AiTarget` is within Pudge's AttackRange, set `Pudge.CurrentTarget = AiTarget`, call `ResetPath` and rotate toward it at 720°/s. Otherwise do not write CurrentTarget. PlayerAttackSystem still performs the attacks.
- **`Pet/Systems/PetMovementSystem.cs`**
  - `lastOwner` becomes `UnitCore`.
  - `owner = RpgControl.ControlledUnit(playerModel)` replaces the `player` reads at :57-68. Pause and `heroSkillFactor` stay based on Pudge.
  - Skip a pet whose `unit.IsControlled` (RPG only).
  - Under RPG, skip the distance teleport while `Time.time < GraceUntil`.
  - Under RPG, speeds: follow = `max(own, owner) MovementSpeed × FollowSpeedMultiplier`; chase = own MovementSpeed; then × `GetMoveSpeedFactor(unit)` as today.
- **`Pet/Systems/PetAttackSystem.cs`**
  - `SelectTarget` leader: under RPG use `(HeroUnit)RpgControl.ControlledUnit(playerModel)` for both `CurrentTarget` and the `FindNearest` center (:214-222). Pause stays Pudge-based (:64).
  - If the pet is controlled, use `SelectControlledTarget`: keep the current target while it is in range, otherwise pick the nearest valid enemy within the pet's own AttackRange. No chasing.
- **`Skills/Systems/SkillAutoCastSystem.cs:243` and `:404`**: replace `!(unit is PlayerUnit)` / `!(cast.Caster is PlayerUnit)` with `!RpgControl.IsControlled(unit)`.
- **`Skills/SkillForcedMovement.cs:95` and `:116`**: extend the condition with `|| RpgControl.IsAiHero(unit)`. That is a no-op when RPG is off, and for a PetUnit it is already true.
- **`Unit/Systems/UnitDamageSystem.cs:333`**: when RPG is on, replace `data.Source is PlayerUnit` with `RpgControl.IsControlled(data.Source)`. Keep the original expression when RPG is off.
- **`Camera/CameraFollowSystem.cs`**
  - :272 and :313 read `RpgControl.ControlledUnit(playerModel)`; :313 needs a UnitCore-typed local.
  - Add `public bool IsOverriding => followingTransform != null || isOverridingTarget;`.
- **Proximity sites use `RpgControl.ControlledUnit(playerModel)`**:
  - `Quests/Systems/QuestNpcProximitySystem.cs:131`
  - `SecretChest/Systems/SecretChestProximitySystem.cs:36-37`
  - `UI/Map/MiniMapView.cs:160`
  - The `FirecampSystem.UpdatePets` revive trigger (:64-67). Healing is unchanged.
  - `Pet/Systems/PetReviveSystem.cs:31` (revive position). The `player.IsDead` skip stays.
- **`CB/Rpg/UI/HeroSwitchHudView.cs`** (new). Build it in code like `UI/Pet/PetUiBuilder.cs`, on the Joystick's canvas: a right-side column of buttons, one per hero. Each button shows the icon or name and an HP fill, is greyed out when dead and highlighted when controlled. A tap calls `TrySwitch`.
- **Check:** see the acceptance list below.

## Acceptance checks (whole step 1)
1. RPG off and pet mode off: the original game is unchanged, and the QuickTest original-save byte check passes.
2. RPG off and pet mode on: the pet play test from Docs/PetSystem/Step1Design.md passes. Pets spawn from slots, follow, attack, die, revive after 15 s and at the fountain.
3. RPG on: the save lives under `<dir>/Rpg/`, the game starts at the prologue, and the original game_data.bin is byte-identical after a session.
4. Golem and cultist spawn with enemy visuals. They follow, fight, die and revive, at 20 % HP after 15 s, and also at the fountain.
5. Tab, keys 1-3 and the HUD switch control. The camera pans smoothly. Input moves only the controlled hero, and its walk animation plays.
6. Uncontrolled Pudge follows the controlled hero, chases and claw-attacks, walk-animates from his velocity, and devour still works.
7. A switch is refused while CanMove is false, while the camera override is active, while Pudge is dying or reviving, and when the target is dead.
8. When the controlled companion dies, control goes back to Pudge. When Pudge dies, control is forced to Pudge, the original firecamp sequence plays, the party revives, and CanMove is true afterwards.
9. While a companion is controlled, NPC dialogue, the secret chest prompt and the fountain revive all trigger.
10. 10 minutes on island 1 with frequent switching: no NullReferenceException, InvalidCastException or MissingReferenceException in the Console. `git diff` of pre-existing files contains only the allowed hunk kinds.