using Codebase.Core;
using Codebase.Core.Commands;
using Cysharp.Threading.Tasks;
using Codebase.Quests.Flows;

namespace Codebase.Quests.Commands
{
	public class FocusNextQuestGiverCommandHandler : ICommandHandler<FocusNextQuestGiverCommand>
	{
		[Inject]
		private QuestModel questModel;
		[Inject]
		private QuestConfig config;
		public void Execute(FocusNextQuestGiverCommand command)
		{ // Original ELF 0x143c19c; candidate only.
			var completed = config.GetChain(command.CompletedChainKey);
			if (completed == null)
				return;
			var next = questModel.Chain.GetNextAvailableChain(completed);
			if (next == null)
				return;
			var target = QuestNpcLocator.FindForChain(next);
			if (target == null)
				return;
			App.InitiateFlow(new QuestMobSpawnedFlow { TargetTransform = target }).Forget();
		}
	}
}
