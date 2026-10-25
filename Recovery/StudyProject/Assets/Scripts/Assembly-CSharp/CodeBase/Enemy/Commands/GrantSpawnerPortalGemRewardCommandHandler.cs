using Codebase.Gates.Flows;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Player.Models;
using Codebase.UI;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerPortalGemRewardCommandHandler : ICommandHandler<GrantSpawnerPortalGemRewardCommand>
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private UiModel uiModel;

		public void Execute(GrantSpawnerPortalGemRewardCommand command)
		{
            if (command.GemIndex <= playerModel.PortalGemsCollected) return;
            playerModel.PortalGemsCollected = command.GemIndex;
            uiModel.IsPortalGemCollectShowing = true;
            App.InitiateFlow(new CollectPortalGemFlow { GemIndex = playerModel.PortalGemsCollected, WaitForItemReward = command.HasItemReward });
        }
	}
}
