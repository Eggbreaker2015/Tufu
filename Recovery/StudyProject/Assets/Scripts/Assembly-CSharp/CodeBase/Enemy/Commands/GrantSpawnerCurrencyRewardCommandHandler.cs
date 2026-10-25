using System;
using Codebase.Amulet;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Codebase.Economy;
using Codebase.UI.Amulet;
using Cysharp.Threading.Tasks;
namespace Codebase.Enemy.Commands
{
    public struct GrantSpawnerCurrencyRewardCommandHandler : ICommandHandler<GrantSpawnerCurrencyRewardCommand>
    {
        [Inject] private AmuletModel amuletModel;
        [Inject] private CurrencyModel currencyModel;
        public void Execute(GrantSpawnerCurrencyRewardCommand command)
        {
            string key = command.Spawner.Config.CurrencyReward;
            if (string.IsNullOrWhiteSpace(key)) return;
            double amount = command.Spawner.Config.CurrencyRewardAmount;
            if (amount <= 0.0) return;
            if (key == "empty_amulet")
            {
                double previous = amuletModel.EmptyAmuletCount;
                currencyModel.Add(key, amount);
                if (previous <= 0.0) OpenUnlockPopupDelayed().Forget();
            }
            else currencyModel.Add(key, amount);
        }
        private static async UniTaskVoid OpenUnlockPopupDelayed()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1), false, PlayerLoopTiming.Update);
            GameContext.Get<UINavigator>().Open<AmuletUnlockView>("Assets/Prefabs/UI/Amulet/AmuletUnlockView.prefab", true);
        }
    }
}
