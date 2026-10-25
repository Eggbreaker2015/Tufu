using Codebase.Unit;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Meta;
using Codebase.Player.Systems;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerStatRewardCommandHandler : ICommandHandler<GrantSpawnerStatRewardCommand>
	{
		[Inject]
		private PlayerRewardSystem playerRewardSystem;

		[Inject]
		private MetaStatsModel metaStatsModel;

		public void Execute(GrantSpawnerStatRewardCommand command)
		{
            var rewards = command.Unit != null && command.Unit.StatRewards != null ? command.Unit.StatRewards : command.Spawner.Config.StatRewards;
            if (rewards == null) return;
            EnemySpawnerConfig config = command.Spawner.Config;
            int count = command.Unit != null ? command.Unit.SpawnerStatRewardMultiplier : 1;
            foreach (StatModifier reward in rewards)
            {
                string name = reward.Name;
                if (reward.Source == ModifierSource.Enemy && (string.IsNullOrEmpty(name) || name == "Devoured")) name = ResolveEnemyName(config);
                var copy = new StatModifier { Name = name, Value = reward.Value, ModifierType = reward.ModifierType, StatType = reward.StatType, StringParam = reward.StringParam, Source = reward.Source };
                copy.Value *= (float)SpawnerStatDropAmountCalculator.GetMultiplier(config, metaStatsModel);
                if (count > 1) copy.Value *= count;
                playerRewardSystem.GrantStatReward(copy, -1.0, count > 1 ? count : 1);
            }
        }

		private static string ResolveEnemyName(EnemySpawnerConfig config)
		{
			return config.IsStatic ? "map.tree" : config.IsEliteBoss ? "map.elite" : "map.normal_mob";
		}
	}
}
