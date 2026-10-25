using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Services.Cheats;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerItemRewardCommandHandler : ICommandHandler<GrantSpawnerItemRewardCommand>
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private MetaStatsModel metaStatsModel;

		public void Execute(GrantSpawnerItemRewardCommand command)
		{
            var item = command.Unit != null && command.Unit.ItemReward != null ? command.Unit.ItemReward : command.Spawner.Config.ItemReward;
            if (item == null) return;
            int copies = command.Copies;
            if (copies < 1) copies = ItemDropAmountCalculator.CalculatePerKill(item, metaStatsModel);
            playerModel.CreateItem(item, copies);
            audioPlayer.PlaySoundOnce("upgrade_weapon");
        }
	}
}
