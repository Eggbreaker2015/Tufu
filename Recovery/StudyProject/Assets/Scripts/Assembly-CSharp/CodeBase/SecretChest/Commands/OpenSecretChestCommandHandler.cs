using System;
using CodeBase.Audio;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Codebase.Core.UserData;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.Rewards.Commands;
using Codebase.SecretChest.Components;
using Codebase.UI;
using Codebase.UI.BossTower;
using Codebase.Unit;
using Codebase.Utilities;
using Cysharp.Threading.Tasks;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.SecretChest.Commands
{
    public class OpenSecretChestCommandHandler : ICommandHandler<OpenSecretChestCommand>
    {
        private const float CameraFocusDelay = 1f;
        private const float RewardPopupDelay = 0.1f;
        private const float CameraDistance = 3.4f;
        private const float CameraHeight = -15f;

        [Inject] private UINavigator uiNavigator;
        [Inject] private SecretChestModel secretChestModel;
        [Inject] private PlayerModel playerModel;
        [Inject] private UiModel uiModel;
        [Inject] private CameraFollowSystem cameraFollowSystem;
        [Inject] private IAudioPlayer audioPlayer;
        [Inject] private GameDataService gameDataService;
        private Filter filter;
        private bool opening;

        public void Execute(OpenSecretChestCommand command)
        {
            // APK ARM64 0x1412de4: reserve the entity before starting the async sequence.
            if (opening || string.IsNullOrEmpty(command.ChestKey) || secretChestModel.IsOpened(command.ChestKey)) return;
            if (filter == null) filter = World.Default.Filter.With<SecretChestComponent>().Build();
            var chests = World.Default.GetStash<SecretChestComponent>();
            var opened = World.Default.GetStash<SecretChestOpenedMarker>();
            foreach (var entity in filter)
            {
                var chest = chests.Get(entity);
                if (chest.ChestKey != command.ChestKey) continue;
                if (opened.Has(entity)) return;
                opened.Set(entity);
                opening = true;
                OpenAsync(entity, chest).Forget();
                return;
            }
        }

        private async UniTaskVoid OpenAsync(Entity entity, SecretChestComponent chest)
        {
            // APK 0x1413620; collection callback 0x141334c.
            bool collected = false;
            bool contentWasActive = chest.ContentCube != null && chest.ContentCube.activeSelf;
            BossRaidRewardView popup = null;
            var token = playerModel.Lifetime.ToCancellationToken();
            playerModel.CanMove = false;
            uiModel.IsHudVisible = false;
            uiModel.IsWorldUiVisible = false;
            try
            {
                cameraFollowSystem.FollowAlongTargetForward(chest.CameraTarget, CameraDistance, CameraHeight, CameraFocusDelay);
                PlayOpenSound(0f).Forget();
                await UniTask.Delay(TimeSpan.FromSeconds(CameraFocusDelay), false, (PlayerLoopTiming)8, token);
                if (chest.Animator != null) chest.Animator.SetBool(SecretChestAnimatorParams.Open, true);
                if (chest.GlowVfx != null) chest.GlowVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                await UniTask.Delay(TimeSpan.FromSeconds(RewardPopupDelay), false, (PlayerLoopTiming)8, token);

                var rewards = chest.Rewards;
                popup = uiNavigator.Open<BossRaidRewardView>("Assets/Prefabs/UI/BossTower/BossRaidRewardView.prefab", false);
                if (popup == null) throw new InvalidOperationException("Secret chest reward view could not be loaded.");
                popup.Setup(rewards, () =>
                {
                    if (collected || token.IsCancellationRequested || secretChestModel.IsOpened(chest.ChestKey)) return;
                    collected = true;
                    if (chest.ContentCube != null) chest.ContentCube.SetActive(false);
                    secretChestModel.MarkOpened(chest.ChestKey);
                    foreach (RewardBase reward in rewards)
                        App.ExecuteCommand(new GrantRewardCommand
                        {
                            Reward = reward,
                            SourcePath = "secret_chest",
                            SourceOverride = ModifierSource.SecretChest
                        });
                    // Learning adaptation: persist the claim and its rewards in the same save.
                    gameDataService.ForceSave();
                    RestorePlayerState();
                });
                await UniTask.WaitUntil(() => collected || popup == null || !popup.gameObject.activeInHierarchy ||
                    chest.Root == null, cancellationToken: token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                opening = false;
                if (!playerModel.Lifetime.IsDisposed)
                {
                    // An interrupted/closed unclaimed popup is retryable; no reward is granted here.
                    if (!collected)
                    {
                        if (popup != null && popup.gameObject.activeInHierarchy) popup.Close();
                        var world = World.Default;
                        if (!world.IsNullOrDisposed() && !world.IsDisposed(entity))
                        {
                            world.GetStash<SecretChestOpenedMarker>().Remove(entity);
                            world.GetStash<SecretChestNearMarker>().Remove(entity);
                        }
                        if (chest.Animator != null) chest.Animator.SetBool(SecretChestAnimatorParams.Open, false);
                        if (chest.GlowVfx != null) chest.GlowVfx.Play();
                        if (chest.ContentCube != null) chest.ContentCube.SetActive(contentWasActive);
                    }
                    RestorePlayerState();
                }
            }
        }

        private async UniTaskVoid PlayOpenSound(float delay)
        {
            // APK 0x1413f80: the normal call uses zero delay and "map_chest_open".
            var token = playerModel.Lifetime.ToCancellationToken();
            if (delay > 0f)
            {
                bool canceled = await UniTask.Delay(TimeSpan.FromSeconds(delay), false, (PlayerLoopTiming)8, token)
                    .SuppressCancellationThrow();
                if (canceled) return;
            }
            if (!token.IsCancellationRequested) audioPlayer.PlaySoundOnce("map_chest_open");
        }

        private void RestorePlayerState()
        {
            // APK 0x14132e4.
            cameraFollowSystem.Unfollow();
            playerModel.CanMove = true;
            uiModel.IsHudVisible = true;
            uiModel.IsWorldUiVisible = true;
        }
    }
}
