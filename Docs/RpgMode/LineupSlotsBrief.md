# Lineup slots open with island progress

Repo worktree: /Users/joyfort/MyProjects/Tufu-rpg (branch feature/rpg-mode, RPG-only). CB = Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase.

## Rule (user, 2026-10-30)
- The lineup is the protagonist plus 4 companion slots. Today all 4 companion slots are forced open on the first frame (`Rpg/Systems/RpgHeroSpawnSystem.cs`, the `unlocked` block).
- New rule: at the start the protagonist's cell and one companion slot are usable. Each island's gate boss opens one more companion slot the first time it is killed. The 3rd island's boss fills the lineup to 5. (Changed 2026-10-31; the first version opened no companion slot at the start.)
  - Each island has exactly one gate-opening spawner, and it is a Boss. Killing it calls `Gate.Open`, which calls `PlayerModel.AdvanceBossProgressLocation(NextLocationNumber)`. That stores the max in `PlayerUd.BossProgressLocationNumber`, which starts at 1.
  - So companion slot i (0-based) is open when `playerModel.Data.BossProgressLocationNumber.Value >= CompanionSlotUnlockLocations[i]`.
  - The default is `{ 1, 2, 3, 4 }`: 1 companion at the start, island 1 boss → 2, island 2 boss → 3, island 3 boss → 4.
- Recruited heroes that find no open empty slot go to the bench (名单), as `PlayerModel.EquipItem` already does when no unlocked slot is free. `RpgProgressHudView.ShowRecruit` already says "已进入名单" in that case.
- The player can swap bench heroes in through the inventory, as today.
- When a slot opens during play, fill it automatically with the best bench hero:
  - highest star (`RpgStars.FromCopies(item.Copies)`);
  - ties go to the earliest in `playerModel.Items`.
- The slot state is **derived** from progress every time; never trust the saved `IsUnlocked`. Old saves have all 4 open. On load, a slot that should be locked but holds a hero must unequip that hero to the bench.

## Steps
You must edit files. Do these in order.

1. **`Rpg/RpgModeConfig.cs`**
   - Add `public List<int> CompanionSlotUnlockLocations = new List<int> { 1, 2, 3, 4 };` next to the other progression fields.
   - Add a helper `public int OpenCompanionSlots(int bossProgressLocation)` that counts the entries ≤ the given location.
   - Do not edit the `.asset`. A missing field keeps its initializer.
2. **New `Rpg/RpgLineup.cs`**
   - A static class with `public static event Action<int> SlotOpened;`, where the argument is the new lineup size including the protagonist.
   - Add an `Invoke` helper.
   - Add a `[StaticReset]` reset, following the pattern in `RpgRecruitService`. Create the `.meta` the same way other new scripts in this folder have one, with a fresh GUID.
3. **`Rpg/Systems/RpgHeroSpawnSystem.cs`**: replace the `unlocked` field and block with a slot sync that runs each frame before the despawn/spawn loops.
   - Companion slots are the `playerModel.ItemSlots` entries with `SlotType == ItemSlotType.Weapon && !IsVip`, in list order. That is the same order the spawn loop uses for `SlotOrder`.
   - `open = RpgModeConfig.Instance.OpenCompanionSlots(playerModel.Data.BossProgressLocationNumber.Value)`.
     - Keep a `lastOpen` field, starting at -1.
     - Only when `open != lastOpen`:
       1. set `IsUnlocked = index < open` on each companion slot;
       2. for every now-locked slot that `IsEquipped`, call `playerModel.UnequipItem(slot.ItemModel)`. The existing removal loop then despawns that companion, and `roster.Remove` moves control if needed;
       3. if `lastOpen >= 0 && open > lastOpen` (a real unlock during play, not the first evaluation after load), fill each newly opened empty slot with the best bench hero via `playerModel.TryEquipItemToSlot`;
       4. call `RpgLineup.Invoke(1 + open)` once;
       5. set `lastOpen = open`.
   - A bench hero is an item in `playerModel.Items` that:
     - has `Config.SlotType == ItemSlotType.Weapon`;
     - has `PetEconomy.FindSpecies(item.Config) != null` (the same test `RpgRecruitService.OnKill` uses);
     - is not in any slot (`playerModel.ItemSlots.All(slot => slot.ItemModel != item)`).
4. **`Rpg/UI/RpgProgressHudView.cs`**
   - Subscribe to `RpgLineup.SlotOpened` next to `RpgRecruitService.Recruited`, and unsubscribe in `OnDestroy`.
   - Show `ShowNotice("队伍格子解锁（" + size + "/5）")`.
   - Play the existing `"unlock"` sound through `IAudioPlayer` if that is easy to reach. Gate opening already plays it, so skip it if both would play in the same frame.
5. **`Rpg/UI/RpgLineupCell.cs`**: for slot cells (not the protagonist), add a centred `TMP_Text` hint that is visible only while `!slot.IsUnlocked`.
   - Text: `"第" + (unlockLocation - 1) + "岛\nBoss"`, where unlockLocation = `CompanionSlotUnlockLocations[index]` and index is this slot's position among the companion slots. Compute it once in `AddSlot`, using the same filter as step 3.
   - Font size about 24. Go through `PetStatsText.SetText`, as the other labels do. It must not block raycasts.
   - Keep the original locker icon visible. Put the text in the lower half if it would overlap the lock.

## Do not
- Do not commit.
- Do not touch `Assets/Scenes/Main.unity`, `Assets/ThirdParty` or `Assets/Editor/RpgMode`.
- Do not change the quest `ItemSlotReward` or spawner `SlotReward` handlers. They are already no-ops and stay that way.
- Match the surrounding style: one-line guards and sparse comments.

## Report
- The files changed.
- Anything you could not do or were unsure about.
