using System;
using CodeBase.Audio;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.UI;
using Codebase.UI.Amulet;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Codebase.Enemy.Commands
{
    public struct GrantSpawnerSoulRewardCommandHandler : ICommandHandler<GrantSpawnerSoulRewardCommand>
    {
        [Inject] private PlayerModel playerModel;
        [Inject] private AmuletModel amuletModel;
        [Inject] private AmuletUpgradesModel amuletUpgradesModel;
        [Inject] private AmuletConfig amuletConfig;
        [Inject] private UserModel userModel;
        [Inject] private CheatsModel cheatsModel;
        [Inject] private IAudioPlayer audioPlayer;
        [Inject] private SoulRewardContainer soulRewardContainer;
        [Inject] private MetaStatsModel metaStatsModel;
        public void Execute(GrantSpawnerSoulRewardCommand command)
        {
            string key = command.Spawner.Config.SoulReward;
            if (string.IsNullOrWhiteSpace(key) || !amuletUpgradesModel.IsSoulsDropUnlocked(command.Spawner)) return;
            double amount = SoulDropAmountCalculator.CalculatePerKill(key, command.Spawner.CurrentDevourRewardMultiplier, metaStatsModel) * cheatsModel.DropMultiplier;
            bool doubled = userModel.SoulPassPurchased.Value && UnityEngine.Random.value < amuletConfig.PassDoubleSoulChance;
            if (doubled) amount += amount;
            bool showFirstSoul = amount > 0.0 && key == "huge" && amuletModel.EmptyAmuletCount > 0.0 && !amuletModel.IsUnlocked && amuletModel.GetSouls("huge") <= 0.0;
            amuletModel.AddSouls(key, amount);
            soulRewardContainer.Create(key, amount, doubled);
            if (showFirstSoul) OpenSoulHarvestedPopupDelayed().Forget();
        }
        private static async UniTaskVoid OpenSoulHarvestedPopupDelayed()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1), false, PlayerLoopTiming.Update);
            GameContext.Get<UINavigator>().Open<AmuletSoulHarvestedView>("Assets/Prefabs/UI/Amulet/AmuletSoulHarvestedView.prefab", true);
        }
    }
}
