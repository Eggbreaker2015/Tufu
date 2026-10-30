# Recruit brief: heroes from item-dropping enemies, slots as the lineup

Repo worktree: /Users/joyfort/MyProjects/Tufu-rpg (branch feature/rpg-mode; steps 1 and 2 are committed). CB = Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase. Read Docs/RpgMode/Step2Brief.md ("Hard constraints") first; they all still apply. A code map with verified file:line facts and a lead plan is at /private/tmp/claude-501/-Users-joyfort-MyProjects-Tufu/285beea2-7448-476c-9eba-cbcb39875c21/scratchpad/recruit-map.md (read-only). Its "# PLAN" section is the plan this brief adopts; where this brief and the map differ, this brief wins. Line numbers can be off by a few lines; verify each one.

Deliver as three sequential tasks, R1, R2, R3. Each must compile on its own. Stop after each for review.

## Decisions (confirmed by the user, 2026-10-29)
1. RPG mode starts with only the protagonist. No starting weapon under RPG.
2. Enemies that dropped equipment (spawners with an `ItemReward` whose item is a weapon with a pet species) are recruitable. The FIRST kill of that species anywhere always recruits the hero (dedup by item key, not spawner key). Later kills give that hero shards. XP and gold are always given.
3. A recruited hero is an owned weapon `ItemModel` in `PlayerModel.Items` (so the roster persists with no new Ud fields). Shards are that item's `Copies`. Add them directly (`owned.Copies += n`), not through `AddItem` (avoid `DropMultiplier` and WeaponRewardView).
4. Shards per kill: normal 1, elite 3, boss 10 (by `RpgSpawnerRules.Kind`). Recruit = ★1 with Copies 1. Stars are automatic: Copies ≥ 10/30/70/150 → ★2/★3/★4/★5, cap ★5. Each star above 1 multiplies Health, Damage and UniversalDefenceFlat by 1.12 (modifier name `rpg.star`, PercentMultiplicative (1.12)^(star−1), Source RpgHero, remove-then-add). Skill level = star. All numbers in RpgModeConfig.
5. Roles and ratios (Health/Attack/Defence), new `HeroRole` field on HeroDefinition, written into Rpg Mode Config.asset:
   - pick 重甲矛兵: Tank 1.6/0.6/1.5
   - mailbox 棍棒兵: Bruiser (off-tank) 1.3/0.85/1.2
   - fork 矛卫: Guard 1.2/0.8/1.2
   - knife 斧头兵: MeleeDps 1.0/1.1/0.9
   - minisaw 剑士: CritDps 0.9/1.25/0.8
   - pike 长矛兵: ReachDps 1.0/1.15/0.9
   Any other species resolves to a default definition (ratios 1/1/1, role MeleeDps) so later islands work without config. golem and cultist stay in config but have no source in RPG.
6. The four weapon slots weapon_1..weapon_4 are all open from the start under RPG; weapon_5_vip, helmet and shield slots are unused. A new recruit auto-deploys into the first free slot (as pet mode does); if all four are full it only joins the roster.
7. The right-side HeroSwitchHud is removed. The bottom items hotbar becomes the lineup bar: a protagonist cell on the far left, then the four weapon slots. With the inventory closed, tapping a cell switches control to that hero; with the inventory open, tapping keeps the original select/equip behaviour (lineup management). Keyboard: 1 = protagonist, 2-5 = lineup positions 1-4, Tab cycles.
8. Universal shards and collection unlocks are off under RPG (no `UnlockPetSpeciesCommand`, no shard row, no collection book). Random item rewards from chests/quests/mail and item-slot rewards are off under RPG (they would recruit random heroes).
9. Old RPG test saves are deleted by the tester (`<saveDir>/Rpg`); no migration code.

## R1: First-kill recruitment, lineup-driven companions, HeroSwitchHud removed
Follow the map's "任务1" changes, with these points:
- New `CB/Rpg/RpgRecruitService.cs`: `OnKill(ItemConfig item, EnemySpawnerConfig config, EnemyUnit enemy)`. Ignore items whose `SlotType` is not Weapon or that have no pet species. Not owned → `playerModel.CreateItem(item, 1)` and raise `Recruited(ItemModel)`. Owned → add shards to Copies and raise `ShardsGained(ItemModel, int, Vector3)`; if the star changed, raise `StarChanged(ItemModel, int)`. Called from EnemySpawner.HandleDeath under RPG, after the existing gated item block. Quest mobs do not recruit.
- PlayerModel: skip the starting weapon under RPG; under RPG, equipping/unequipping/applying equipped items must not add the item's `StatModifiers` to Pudge; `EquipItem` must not pick VIP slots under RPG. `ItemModel.IsAbilityUnlocked` returns false under RPG (hero items carry no item abilities). Gate `GrantRewardCommandRunner.HandleItemReward` / `HandleItemSlotReward`, `UnlockPetSpeciesCommandHandler`, and `ExtraWeaponSlotFeature.OnUnlocked` under RPG.
- Under RPG on the first frame, set weapon_1..weapon_4 `IsUnlocked = true` (RPG save only).
- Rewrite `RpgHeroSpawnSystem` to follow the slots each frame: keep the protagonist record logic; despawn a companion whose slot no longer holds its item (order: `roster.Remove` which first forces control back to Pudge if needed → `petsModel.RememberRevive` → remove from Pets → destroy via a new `PetUnitFactory.Destroy` extracted verbatim from `PetSpawnSystem.Despawn`, with PetSpawnSystem calling it so pet mode is unchanged); spawn a companion for each equipped non-VIP weapon slot that has none, with `new PetModel(slot, slot.ItemModel, petDef, order)` and `petsModel.RestoreDeath(pet)`; keep skill level = star and the `rpg.star` modifier in sync. Companion stats: `HeroStats` base + role (from `ResolveHero`) + level + star.
- `HeroRosterModel`: add `Remove(HeroModel)` and a pending request (`Request/TakeRequest`) used by HeroControlSystem. HeroControlSystem reads requests from the roster, keys 1-5, and no longer creates the HUD; it lazily creates `RpgProgressHudView` itself. Delete `HeroSwitchHudView.cs` and its .meta. The FirstLiving/NextLiving helpers must handle a roster that grows and shrinks.
- Remove the step-1 temporary attack item code in SpawnCompanion and the `StartingHeroKeys` usage (keep the field only if removing it breaks the asset; then mark it unused).
- Write the six HeroDefinitions, the role field and the new numbers into `Assets/Resources/Rpg Mode Config.asset`.
- Dev helper (Assets/Editor/RpgGrowthTools.cs): a menu item to recruit all six island 1-2 heroes and one to add 10 shards to every owned hero.

## R2: Bottom bar as the lineup bar
Follow the map's "任务2". Under RPG: show the hotbar again, only the protagonist cell plus weapon_1..4 cells, no helmet/shield/VIP cells, no separator; tapping a cell with the inventory closed sends `roster.Request(hero)`; highlight the controlled cell; add an HP fill (raycastTarget off) and keep the pet revive countdown; show ★star instead of the weapon-type icon; the hotbar must receive taps (blocksRaycasts) under RPG. Move the XP/gold HUD up so it does not overlap the hotbar. Pet mode and the original game unchanged.

## R3: Inventory as the hero roster, recruit and shard feedback
Follow the map's "任务3". Under RPG: show the inventory button again (remove it from the step-2 pruner), show only the weapon category titled 英雄, hide amulet, auto-equip, loadout, soul search, damage-type stat rows, damage/DPS/attack-speed/per-copy rows, weapon-type row, evolution UI and the universal shard row. The selected hero shows a hero card: name, role, team level, ★star, shards Copies/next threshold, live Health/Attack/Defence/Range (from the spawned unit, or computed from HeroStats on a temporary stat list when not deployed), skill level. Equip/unequip buttons read 上阵/下阵. Recruit popup "<名字>（<定位>）加入队伍", plus "已进入名单" when the lineup is full; "碎片 +N" floating text on shard gains; a short popup on star-up. Chinese text through `PetStatsText.SetText`. Make sure amulet/soul flows can never open the inventory in amulet state under RPG. The pet collection book stays hidden under RPG; ability details (`OpenAbilities`) may be opened from the hero card.

## Acceptance checks
1. RPG off, pet mode on: starting knife, drops, slot-driven pets, revive timers, inventory, amulet, auto-equip, collection book all as before.
2. RPG on, fresh save: only Pudge; inventory empty; no right-side portrait column; XP/gold HUD visible.
3. First kill of a knife spawner on island 1: knife enters Items with Copies 1, goes to weapon_1, 斧头兵 spawns at ring 1 and a recruit popup shows. Further kills add shards (+1 normal), never a second 斧头兵.
4. At 10 shards the hero becomes ★2: `rpg.star` modifier present, skill level 2, popup shown. Team level-ups still apply `rpg.level`.
5. A fifth recruit goes to the roster only. Pudge has no Source=Item modifiers; hero items have no ability states.
6. Bottom bar: protagonist cell + 4 cells; tapping switches control (respecting the switch cooldown and refusals), highlight follows control; HP fill and revive countdown work; keys 1-5 and Tab work.
7. Inventory: only 英雄; hero card shows role, star, shards, live stats; deploy/withdraw/swap updates the squad immediately; withdrawing the controlled hero returns control to Pudge without errors.
8. Chests, quest chain rewards and mail do not grant items or slots under RPG.
9. 10 minutes on islands 1-2 recruiting and swapping: no exceptions.
