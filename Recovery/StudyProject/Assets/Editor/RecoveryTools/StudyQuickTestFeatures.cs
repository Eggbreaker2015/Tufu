#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeBase.Audio;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Economy;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.Player.Models;
using Codebase.Quests;
using Codebase.Quests.Components;
using Codebase.UI;
using Codebase.Utilities;
using Cysharp.Threading.Tasks;
using Scellecs.Morpeh;
using UniMob;
using UnityEngine;

namespace RecoveryTools
{
    public static partial class StudyQuickTestActions
    {
        private static CancellationTokenSource previewCancellation;
        public static bool IsPreviewRunning => previewCancellation != null;

        public static string UnlockCrystals()
        {
            RequireTestProfile();
            var model = GameContext.Get<CrystalModel>();
            model.IsUnlocked = true;
            foreach (var slot in model.Slots) slot.IsUnlocked = true;
            return Save("水晶系统和水晶槽已解锁。");
        }

        public static string AddCrystalMaterials(int amount)
        {
            RequireTestProfile();
            if (amount <= 0 || amount > 100000) throw new ArgumentOutOfRangeException(nameof(amount));
            var model = GameContext.Get<CrystalModel>();
            model.AddGeodes(amount);
            model.AddEnchantmentStones(amount);
            GameContext.Get<CurrencyModel>().Add(CurrencyKeys.CrystalMagicDust, amount * 100.0);
            return Save("已补充水晶原石、附魔石和魔尘；超出容量的原石进入原待领取队列。");
        }

        public static string UnlockBossTower()
        {
            RequireTestProfile();
            var player = GameContext.Get<PlayerModel>();
            player.PortalGemsCollected = PlayerModel.PORTAL_GEMS_COUNT;
            player.IsUnlockAnimationShown = true;
            var model = GameContext.Get<BossTowerModel>();
            model.Data.IsUnlocked.Value = true;
            var upgrades = GameContext.Get<AmuletUpgradesModel>().Upgrades;
            foreach (var boss in model.AllBosses)
                if (!string.IsNullOrEmpty(boss.UnlockUpgradeKey) && upgrades.GetDef(boss.UnlockUpgradeKey) != null && upgrades.GetLevel(boss.UnlockUpgradeKey) == 0)
                    upgrades.TryUpgrade(boss.UnlockUpgradeKey, true);
            model.AddTickets(100);
            return Save("Boss 传送门、Boss 节点已开放，并增加 100 张入场券。");
        }

        public static string SetBossFloor(string key, int floor)
        {
            RequireTestProfile();
            var model = GameContext.Get<BossTowerModel>();
            var boss = model.GetBoss(key) ?? throw new InvalidOperationException("未找到 Boss。");
            boss.Floor = Mathf.Clamp(floor, 0, 1000);
            model.RebakeRewards(boss);
            return Save("已设置 Boss 层数并重算对应奖励。");
        }

        public static async UniTask<string> MaxAmuletTree()
        {
            RequireTestProfile();
            var amulet = GameContext.Get<AmuletModel>();
            UnlockAmulet();
            amulet.AddLevels(amulet.MaxLevel);
            var model = GameContext.Get<AmuletUpgradesModel>();
            int total = 0;
            var token = amulet.Lifetime.ToCancellationToken();
            for (int pass = 0; pass < 100; pass++)
            {
                int changed = 0;
                foreach (var def in model.Upgrades.Defs)
                {
                    int remaining = Math.Max(0, def.MaxLevel - model.GetLevel(def.Key));
                    for (int i = 0; i < remaining && model.TryUpgrade(def.Key, true); i++)
                    {
                        changed++; total++;
                        if (total % 20 == 0) await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }
                if (changed == 0) break;
            }
            int incomplete = model.Upgrades.Defs.Count(d => !model.IsMaxLevel(d.Key));
            return Save("灵魂之树已执行 " + total + " 次免费升级，剩余未满级节点 " + incomplete + " 个。");
        }

        public static async UniTask<string> StartCrystalEvent()
        {
            RequireTestProfile(true);
            const string key = "crystal_event_roster";
            var repository = GameContext.Get<EventsRepository>();
            if (repository.CurrentEvent != null && repository.CurrentEvent.Key != key)
                throw new InvalidOperationException("请先结束当前活动，再启动水晶活动。");
            var model = repository.Get(key) ?? throw new InvalidOperationException("未找到水晶活动配置。");
            GameContext.Get<PlayerModel>().UnlockLocation(Math.Max(4, model.RequiredLocationNumber));
            repository.ResetEvent(key);
            repository.ClearLastPlayedSlot();
            model.MakeReady();
            await repository.StartEventAsync(key).AttachExternalCancellation(model.Lifetime.ToCancellationToken());
            if (model.State != EventState.Active) throw new InvalidOperationException("水晶活动没有进入进行中状态。");
            return Save("水晶活动已开始，可从游戏中的骷髅入口体验。");
        }

        public static string AddEventCurrency(int amount)
        {
            RequireTestProfile();
            var model = GameContext.Get<EventsRepository>().CurrentEvent;
            if (model == null || !model.IsCrystalRosterEvent) throw new InvalidOperationException("请先开始水晶活动。");
            if (amount <= 0 || amount > 100000) throw new ArgumentOutOfRangeException(nameof(amount));
            model.AddCurrency("red_crystal", amount);
            model.AddCurrency("blue_crystal", amount);
            return Save("活动红水晶、蓝水晶各增加 " + amount + "。");
        }

        public static string CompleteEvent()
        {
            RequireTestProfile(true);
            var model = GameContext.Get<EventsRepository>().CurrentEvent;
            if (model == null || model.State != EventState.Active) throw new InvalidOperationException("当前没有正在进行的活动。");
            model.Complete();
            return Save("活动已结束，奖励仍使用原领取和防重流程。");
        }

        public static string OpenFeature(string address)
        {
            RequireTestProfile(true);
            if (address != UIConstants.AmuletUpgradeGraphView && address != UIConstants.AmuletCrystalsView && address != UIConstants.BossTowerHubView && address != UIConstants.VipView)
                throw new ArgumentException("不支持的功能入口。", nameof(address));
            if (GameContext.Get<UINavigator>().Open(address, true) == null) throw new InvalidOperationException("功能页面未能加载。");
            return "已打开功能页面。";
        }

        public static string[] GetPreviewStepKeys()
        {
            if (!TryGetReady(out _)) return Array.Empty<string>();
            var result = new List<string>();
            var stash = World.Default.GetStash<QuestStepWorldComponent>();
            foreach (var entity in World.Default.Filter.With<QuestStepWorldComponent>().Build())
                if (stash.Has(entity) && stash.Get(entity).Cutscene != null) result.Add(stash.Get(entity).StepKey);
            return result.ToArray();
        }

        public static void StopPreview() => previewCancellation?.Cancel();

        public static async UniTask<string> PreviewCutscene(string stepKey)
        {
            RequireTestProfile(true);
            if (!QuestStepWorldApplier.TryGetWorld(stepKey, out var world) || world.Cutscene == null)
                throw new InvalidOperationException("该任务没有可重播的 Timeline。");
            var director = world.Cutscene;
            var root = director.transform.parent;
            var states = root.GetComponentsInChildren<Transform>(true).Select(t => new
                { target = t, active = t.gameObject.activeSelf, position = t.localPosition, rotation = t.localRotation, scale = t.localScale }).ToArray();
            var hidden = world.ObjectsToHideDuringCutscene.Select(o => new { target = o, active = o.activeSelf }).ToArray();
            var renderers = root.GetComponentsInChildren<Renderer>(true).Select(r =>
            {
                var block = new MaterialPropertyBlock(); r.GetPropertyBlock(block); return new { target = r, block };
            }).ToArray();
            var player = GameContext.Get<PlayerModel>();
            var ui = GameContext.Get<UiModel>();
            bool canMove = player.CanMove, hud = ui.IsHudVisible, worldUi = ui.IsWorldUiVisible;
            float speed = UnityEngine.Time.timeScale;
            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(player.Lifetime.ToCancellationToken());
            previewCancellation = cancellation;
            try
            {
                UnityEngine.Time.timeScale = 1f;
                player.CanMove = false; ui.IsHudVisible = false; ui.IsWorldUiVisible = false;
                QuestStepWorldApplier.HideDuringCutscene(world);
                if (world.CamerasRoot != null) world.CamerasRoot.SetActive(true);
                director.gameObject.SetActive(true);
                foreach (var sound in world.CutsceneSounds) PlayPreviewSound(sound, cancellation.Token).Forget();
                await new QuestCutsceneService().PlayAndHoldLastFrame(director).AttachExternalCancellation(cancellation.Token);
                await UniTask.Delay(750, true, cancellationToken: cancellation.Token);
                return "过场重播结束，任务完成和领奖标记保留。";
            }
            catch (OperationCanceledException) { return "已停止过场预览并恢复游戏画面。"; }
            finally
            {
                cancellation.Cancel();
                if (director != null) QuestCutsceneService.Release(director);
                foreach (var state in states)
                    if (state.target != null)
                    {
                        state.target.localPosition = state.position; state.target.localRotation = state.rotation; state.target.localScale = state.scale;
                        state.target.gameObject.SetActive(state.active);
                    }
                foreach (var state in hidden) if (state.target != null) state.target.SetActive(state.active);
                foreach (var item in renderers) if (item.target != null) item.target.SetPropertyBlock(item.block);
                if (!player.Lifetime.IsDisposed) player.CanMove = canMove;
                if (!ui.Lifetime.IsDisposed) { ui.IsHudVisible = hud; ui.IsWorldUiVisible = worldUi; }
                UnityEngine.Time.timeScale = speed;
                previewCancellation = null;
                cancellation.Dispose();
            }
        }

        private static async UniTask PlayPreviewSound(QuestCutsceneSound sound, CancellationToken token)
        {
            if (sound.Delay > 0) await UniTask.Delay(TimeSpan.FromSeconds(sound.Delay), cancellationToken: token);
            token.ThrowIfCancellationRequested();
            if (sound.Clip != null) GameContext.Get<IAudioPlayer>().PlayClipOnce(sound.Clip, 1f);
        }
    }
}
#endif
