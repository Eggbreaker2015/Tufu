#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.Amulet;
using Codebase.Balance;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Economy;
using Codebase.Enemy;
using Codebase.Gates;
using Codebase.Location;
using Codebase.Mines;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Quests;
using Codebase.Quests.Commands;
using Codebase.Services.Cheats;
using Codebase.Services.Time;
using Codebase.UI;
using Codebase.UI.Inventory;
using Codebase.UI.Quests;
using Codebase.Unit;
using RecoveryCompatibility;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RecoveryTools
{
    // Learning-project conveniences. These use the restored models, not the disabled release debug UI.
    public static partial class StudyQuickTestActions
    {
        public static bool TryGetReady(out string reason)
        {
            reason = "请先运行游戏，等待 Main 场景和玩家加载完成。";
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                !GameContext.Has<PlayerModel>() || !GameContext.Has<CheatsModel>()) return false;
            var player = GameContext.Get<PlayerModel>();
            if (player.Lifetime.IsDisposed || player.Data == null || player.Unit == null) return false;
            reason = null;
            return true;
        }

        public static void RequireTestProfile(bool changeWorld = false)
        {
            if (!TryGetReady(out string reason)) throw new InvalidOperationException(reason);
            if (!StudyTestProfile.IsEnabled) throw new InvalidOperationException("请先建立或切换到独立测试存档。");
            if (changeWorld && (!GameContext.Get<PlayerModel>().CanMove || GameContext.Get<UINavigator>().HasActivePopups ||
                GameContext.Get<InventoryView>().gameObject.activeInHierarchy || IsPreviewRunning))
                throw new InvalidOperationException("请先结束当前对话、关闭背包或领取界面，再使用地图和剧情工具。");
            if (changeWorld && GameContext.Has<Codebase.BossTower.BossTowerModel>() && GameContext.Get<Codebase.BossTower.BossTowerModel>().IsFighting)
                throw new InvalidOperationException("请先从原界面结束或退出 Boss 战，再使用地图和剧情工具。");
        }

        public static string SetCombatOptions(bool godMode, bool quickKill, bool oneDamage, bool stopBossTimer, int dropMultiplier, float speed)
        {
            RequireTestProfile();
            var cheats = GameContext.Get<CheatsModel>();
            cheats.GodMode = godMode;
            cheats.OneHitMode = quickKill;
            cheats.OneDamageMode = oneDamage;
            cheats.StopBossTowerTimer = stopBossTimer;
            cheats.DropMultiplier = Mathf.Clamp(dropMultiplier, 1, 100);
            UnityEngine.Time.timeScale = Mathf.Clamp(speed, .25f, 5f);
            return "已更新本次运行的战斗开关；停止 Play 后恢复默认。";
        }

        public static string ApplyCombatPreset()
        {
            SetCombatOptions(true, true, false, false, 10, 2f);
            Heal();
            return "已开启无敌、快速击杀、10 倍掉落及 2 倍速。";
        }

        public static string ResetCombatOptions() => SetCombatOptions(false, false, false, false, 1, 1f);

        public static string Heal()
        {
            RequireTestProfile();
            var unit = GameContext.Get<PlayerModel>().Unit;
            if (unit.IsDead) throw new InvalidOperationException("角色已阵亡，请先使用游戏中的复活按钮。");
            unit.CurrentHealth = unit.GetStatValue(StatType.Health);
            return "生命已恢复。";
        }

        public static string AddGems(int amount)
        {
            RequireTestProfile();
            if (amount <= 0 || amount > 100000000) throw new ArgumentOutOfRangeException(nameof(amount));
            GameContext.Get<PlayerModel>().Gems += amount;
            return Save("已增加 " + amount.ToString("N0") + " 钻石。");
        }

        public static string GiveSouls(int amount)
        {
            RequireTestProfile();
            if (amount <= 0 || amount > 1000000) throw new ArgumentOutOfRangeException(nameof(amount));
            var amulet = GameContext.Get<AmuletModel>();
            foreach (var soul in amulet.SoulDefs) amulet.AddSouls(soul.Key, amount);
            return Save("每种灵魂已增加 " + amount.ToString("N0") + "。");
        }

        public static string UnlockAmulet()
        {
            RequireTestProfile();
            var amulet = GameContext.Get<AmuletModel>();
            amulet.Unlock();
            if (amulet.EmptyAmuletCount > 0) GameContext.Get<CurrencyModel>().TrySpend(CurrencyKeys.EmptyAmulet, amulet.EmptyAmuletCount);
            return Save("护符已解锁；可从背包打开灵魂之树。");
        }

        public static string AddAmuletLevels(int levels)
        {
            RequireTestProfile();
            GameContext.Get<AmuletModel>().AddLevels(Mathf.Clamp(levels, 1, 1000));
            return Save("已提升护符等级。");
        }

        public static string UnlockEquipmentSlots()
        {
            RequireTestProfile();
            foreach (var slot in GameContext.Get<PlayerModel>().ItemSlots) slot.IsUnlocked = true;
            return Save("装备槽已全部解锁。");
        }

        public static string GiveAllEquipment()
        {
            RequireTestProfile();
            var player = GameContext.Get<PlayerModel>();
            int added = 0;
            // Divine configs are visual/evolved variants. The inventory reaches them through Evolved
            // on the base item; giving those templates directly leaves no evolution cost row.
            foreach (var config in GameContext.Get<ItemsProvider>().Items.Where(c => c != null && !string.IsNullOrEmpty(c.Key) && c.Rarity != Rarity.Divine))
            {
                if (player.Items.Any(i => i.Key == config.Key)) continue;
                player.AddItem(new ItemModel(player.Lifetime, config), applySilently: true);
                added++;
            }
            return Save("已补齐 " + added + " 件装备，已有装备保留。");
        }

        private static readonly string[] RarityNames = { "普通", "优秀", "稀有", "史诗", "传说", "神化", "神话" };

        // Pet species in rarity order, as (item key, label) for the quick-test popup.
        public static (string Key, string Label)[] GetPetSpecies()
        {
            var petConfig = Codebase.Pet.PetConfig.Instance;
            if (petConfig == null || !GameContext.Has<ItemsProvider>()) return Array.Empty<(string, string)>();
            var items = GameContext.Get<ItemsProvider>().Items;
            return petConfig.Species
                .Select(d => (Def: d, Config: items.FirstOrDefault(c => c != null && c.Key == d.ItemKey)))
                .Where(p => p.Config != null)
                .OrderBy(p => (int)p.Config.Rarity)
                .Select(p => (p.Def.ItemKey, "[" + RarityNames[Mathf.Clamp((int)p.Config.Rarity, 0, RarityNames.Length - 1)] + "] " + p.Def.DisplayName + " (" + p.Def.ItemKey + ")"))
                .ToArray();
        }

        // Owned pets get the copies added and the level raised; new ones are created silently like GiveAllEquipment.
        public static string GivePet(string key, int level, int copies, bool equip)
        {
            RequireTestProfile();
            var player = GameContext.Get<PlayerModel>();
            var config = GameContext.Get<ItemsProvider>().Items.FirstOrDefault(c => c != null && c.Key == key);
            if (config == null) throw new InvalidOperationException("找不到宠物物品 " + key + "。");
            var item = GrantPet(player, config, level, copies, out bool created);
            string message = (created ? "已发放 " : "已追加 ") + config.Key + "：等级 " + item.Level + "，副本 " + item.Copies + "。";
            if (equip && !player.EquippedItems.Contains(item))
                message += player.EquipItem(item) ? " 已出战。" : " 没有空的已解锁槽，未出战。";
            if (!Codebase.Pet.PetConfig.Enabled) message += " 注意：宠物模式当前关闭。";
            return Save(message);
        }

        public static string GiveAllPets(int level, int copies)
        {
            RequireTestProfile();
            var player = GameContext.Get<PlayerModel>();
            var items = GameContext.Get<ItemsProvider>().Items;
            int added = 0;
            foreach (var (key, _) in GetPetSpecies())
            {
                var config = items.FirstOrDefault(c => c != null && c.Key == key);
                if (config == null || player.Items.Any(i => i.Key == key)) continue;
                GrantPet(player, config, level, copies, out _);
                added++;
            }
            return Save("已补齐 " + added + " 种宠物，已有宠物保留。");
        }

        public static string AddPetShards(int amount)
        {
            RequireTestProfile();
            Codebase.Pet.PetEconomy.AddShards(amount);
            return Save("万能碎片 +" + amount.ToString("N0") + "，当前 " + Codebase.Pet.PetEconomy.Shards.ToString("N0") + "。");
        }

        private static ItemModel GrantPet(PlayerModel player, ItemConfig config, int level, int copies, out bool created)
        {
            int maxLevel = GameContext.Get<CombatBalanceConfig>().MaxEvolutionLevel;
            copies = Mathf.Clamp(copies, 1, 100000);
            var item = player.Items.FirstOrDefault(i => i.Key == config.Key);
            created = item == null;
            if (created)
            {
                item = new ItemModel(player.Lifetime, config) { Copies = copies };
                player.AddItem(item, applySilently: true);
            }
            else item.Copies = Mathf.Clamp(item.Copies + copies, 1, 100000);
            item.Level = Mathf.Clamp(Mathf.Max(item.Level, level), 1, maxLevel);
            return item;
        }

        public static string UpgradeEquipment(int levels, int copies, bool evolve)
        {
            RequireTestProfile();
            foreach (var item in GameContext.Get<PlayerModel>().Items)
            {
                item.Level = Mathf.Clamp(levels, 1, GameContext.Get<CombatBalanceConfig>().MaxEvolutionLevel);
                item.Copies = Mathf.Clamp(copies, 1, 100000);
                if (evolve) item.Evolved = true;
            }
            return Save("装备已设为指定等级和副本数量。");
        }

        public static string EquipStrongest()
        {
            RequireTestProfile();
            var player = GameContext.Get<PlayerModel>();
            foreach (var item in player.EquippedItems.ToArray()) player.UnequipItem(item);
            var used = new HashSet<ItemModel>();
            foreach (var slot in player.ItemSlots.Where(s => s.IsUnlocked))
            {
                var item = player.Items.Where(i => !used.Contains(i) && i.Config.SlotType == slot.SlotType)
                    .OrderByDescending(i => i.IsArmor ? i.Armor : i.DPS).FirstOrDefault();
                if (item != null && player.TryEquipItemToSlot(item, slot)) used.Add(item);
            }
            return Save("已按伤害/护甲为每个已解锁槽装备可用装备。");
        }

        public static string UnlockAllGates()
        {
            RequireTestProfile(true);
            int opened = 0;
            foreach (var gate in GameContext.Get<GatesModel>().GateCores)
                if (gate != null && !gate.IsUnlocked) { gate.Open(); opened++; }
            int maximum = GameContext.Get<LocationsConfig>().Locations.Max(l => l.Number);
            GameContext.Get<PlayerModel>().UnlockLocation(maximum);
            GameContext.Get<PlayerModel>().AdvanceBossProgressLocation(maximum);
            return Save("已打开 " + opened + " 扇门并开放全部岛屿，任务进度保留。");
        }

        public static bool TryGetIslandDestination(int number, out Vector3 position)
        {
            position = default;
            var candidates = new List<Vector3>();
            if (number == 1 && GameContext.Has<Codebase.Buildings.FirecampSystem>()) candidates.Add(GameContext.Get<Codebase.Buildings.FirecampSystem>().transform.position);
            candidates.AddRange(GameContext.Get<GatesModel>().GateCores.Where(g => g != null && g.NextLocationNumber == number && g.Exit != null).Select(g => g.Exit.position));
            candidates.AddRange(GameContext.Get<EnemiesModel>().Spawners.Where(s => s != null && s.Config.LocationNumber == number && !s.Config.IsEvent && !s.Config.IsVip).Select(s => s.transform.position));
            foreach (var candidate in candidates)
                if (NavMesh.SamplePosition(candidate, out var hit, 5f, NavMesh.AllAreas)) { position = hit.position; return true; }
            return false;
        }

        public static string TeleportToIsland(int number)
        {
            RequireTestProfile(true);
            if (!GameContext.Get<LocationsConfig>().Locations.Any(l => l.Number == number)) throw new ArgumentOutOfRangeException(nameof(number));
            if (!TryGetIslandDestination(number, out Vector3 target)) throw new InvalidOperationException("目标岛未找到可行走落点。");
            foreach (var gate in GameContext.Get<GatesModel>().GateCores.Where(g => g != null && g.NextLocationNumber <= number && !g.IsUnlocked)) gate.Open();
            Warp(target);
            GameContext.Get<PlayerModel>().SetCurrentLocation(number);
            return Save("已传送到第 " + number + " 岛。");
        }

        public static string TeleportToTask(string chainKey)
        {
            RequireTestProfile(true);
            var config = GameContext.Get<QuestConfig>();
            var chain = config.GetChain(chainKey) ?? throw new InvalidOperationException("未找到任务链。");
            string step = GameContext.Get<QuestModel>().Chain.GetActiveStepKey(chain);
            if (step != null && GameContext.Get<QuestMobSpawnerService>().TryGetMobTransform(step, out var mob))
            {
                Warp(mob.position);
                GameContext.Get<PlayerModel>().SetCurrentLocation(config.GetStep(step).LocationNumber);
                return Save("已传送到当前任务目标附近。");
            }
            Transform npc = QuestNpcLocator.FindForChain(chain);
            if (npc == null) throw new InvalidOperationException("未找到任务目标或 NPC；可先接取该任务。");
            Warp(npc.position);
            var destinationStep = step != null ? config.GetStep(step) : chain.Steps.LastOrDefault();
            if (destinationStep != null && destinationStep.LocationNumber > 0) GameContext.Get<PlayerModel>().SetCurrentLocation(destinationStep.LocationNumber);
            return Save("已传送到任务 NPC 附近。");
        }

        public static string CompleteTaskStep(string chainKey)
        {
            RequireTestProfile(true);
            var chain = GameContext.Get<QuestConfig>().GetChain(chainKey);
            if (chain == null) throw new InvalidOperationException("未找到任务链。");
            var model = GameContext.Get<QuestModel>();
            string step = model.Chain.GetActiveStepKey(chain);
            if (step == null) return "任务步骤已全部完成，可到 NPC 领取奖励。";
            model.MarkGiven(step);
            App.ExecuteCommand(new CompleteQuestStepCommand { StepKey = step });
            return Save("已完成当前步骤，奖励仍从原任务界面领取。");
        }

        public static string JumpToFinale(string chainKey)
        {
            RequireTestProfile(true);
            var config = GameContext.Get<QuestConfig>();
            var target = config.GetChain(chainKey);
            if (target == null || target.Role == QuestChainRole.Hub || target.Steps.Count == 0)
                throw new InvalidOperationException("请选择一条支线任务。");
            var model = GameContext.Get<QuestModel>();
            foreach (var chain in config.Chains)
            {
                if (chain == target) break;
                foreach (var step in chain.Steps) { model.MarkGiven(step.StepKey); model.MarkComplete(step.StepKey); model.MarkStepRewardClaimed(step.StepKey); QuestStepWorldApplier.Apply(step.StepKey); }
                if (chain.HubTokenSlots != null) foreach (var slot in chain.HubTokenSlots) model.MarkHubTokenDelivered(slot.TokenKey);
                model.MarkChainRewardClaimed(chain.ChainKey);
            }
            for (int i = 0; i < target.Steps.Count - 1; i++)
            {
                var step = target.Steps[i];
                model.MarkGiven(step.StepKey); model.MarkComplete(step.StepKey); model.MarkStepRewardClaimed(step.StepKey); QuestStepWorldApplier.Apply(step.StepKey);
            }
            var last = target.Steps[target.Steps.Count - 1];
            var saved = model.Data.Steps.GetOrCreate(last.StepKey, out _);
            // The original chain-complete button plays the finale only after every hunt is complete.
            // Keep the chain reward unclaimed so that button still uses the normal reward/story flow.
            saved.IsGiven.Value = true; saved.IsComplete.Value = true; saved.RewardClaimed.Value = true;
            model.Data.Chains.GetOrCreate(target.ChainKey, out _).RewardClaimed.Value = false;
            model.ClearChainRewardClaimTimer(target.ChainKey);
            if (QuestStepWorldApplier.TryGetWorld(last.StepKey, out var world)) QuestWorldStateResetter.ApplyInitialState(world);
            string result = Save("已准备终章；跳过的步骤奖励未补发。");
            GameContext.Get<UINavigator>().Open<QuestsWindowView>("Assets/Prefabs/UI/Mines/Quests/QuestsWindow.prefab", true).Setup(target);
            return result + " 在原任务窗口点击完成，继续领奖和剧情。";
        }

        public static string UnlockMines()
        {
            RequireTestProfile();
            GameContext.Get<MinesModel>().Unlock();
            return Save("矿区功能已解锁。");
        }

        public static string SkipTime(float seconds)
        {
            RequireTestProfile();
            if (seconds <= 0 || seconds > 604800) throw new ArgumentOutOfRangeException(nameof(seconds));
            var clock = GameContext.Get<ITimeService>();
            clock.UpdateCheatOffset((float)clock.CheatOffset.TotalSeconds + seconds);
            return Save("测试档游戏时钟已前进 " + TimeSpan.FromSeconds(seconds) + "。");
        }

        public static string ResetTimeOffset()
        {
            RequireTestProfile();
            GameContext.Get<ITimeService>().UpdateCheatOffset(0f);
            return "已恢复测试档的实际 UTC 时间。";
        }

        public static string OpenInventory()
        {
            RequireTestProfile(true);
            GameContext.Get<InventoryView>().gameObject.SetActive(true);
            return "已打开背包。";
        }

        private static void Warp(Vector3 position)
        {
            var unit = GameContext.Get<PlayerModel>().Unit;
            if (unit.IsDead || !unit.Agent.enabled) throw new InvalidOperationException("角色无法移动，请先复活。");
            if (!NavMesh.SamplePosition(position, out var hit, 6f, NavMesh.AllAreas)) throw new InvalidOperationException("目标附近没有可行走区域。");
            if (!unit.Agent.Warp(hit.position)) throw new InvalidOperationException("NavMesh 传送失败。");
            unit.Agent.ResetPath();
            GameContext.Get<CameraFollowSystem>().Unfollow();
        }

        private static string Save(string message)
        {
            RequireTestProfile();
            StudyTestProfileController.SaveCurrent();
            return message + " 已保存到测试档。";
        }
    }
}
#endif
