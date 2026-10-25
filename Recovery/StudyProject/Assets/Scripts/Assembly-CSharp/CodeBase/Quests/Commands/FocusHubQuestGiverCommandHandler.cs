using Codebase.Core;
using Codebase.Core.Commands;
using Cysharp.Threading.Tasks;
using Codebase.Quests.Flows;

namespace Codebase.Quests.Commands
{
	public class FocusHubQuestGiverCommandHandler : ICommandHandler<FocusHubQuestGiverCommand>
	{
		[Inject]
		private QuestModel questModel;
		[Inject]
		private QuestConfig config;
		public void Execute(FocusHubQuestGiverCommand command)
		{ // Original ELF 0x143c03c; candidate only.
			var chain = config.GetHubChainForToken(command.TokenKey);
			if (chain == null || !questModel.Chain.IsChainUnlocked(chain) || !questModel.Hub.CanDeliverHubToken(chain, command.TokenKey))
				return;
			var target = QuestNpcLocator.FindForChain(chain);
			if (target == null)
				return;
			App.InitiateFlow(new QuestMobSpawnedFlow { TargetTransform = target }).Forget();
		}
	}
}
