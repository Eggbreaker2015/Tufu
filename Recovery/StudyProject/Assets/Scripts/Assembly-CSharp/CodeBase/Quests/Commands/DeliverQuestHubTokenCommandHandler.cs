using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Economy;
using System;
using System.Collections.Generic;
using Codebase.Analytics;

namespace Codebase.Quests.Commands
{
	public class DeliverQuestHubTokenCommandHandler : ICommandHandler<DeliverQuestHubTokenCommand>
	{
		[Inject]
		private QuestModel questModel;
		[Inject]
		private QuestConfig config;
		[Inject]
		private CurrencyModel currencyModel;
		public void Execute(DeliverQuestHubTokenCommand command)
		{ // Original ELF 0x143c2d0; candidate only.
			var chain = config.GetChain(command.ChainKey);
			if (chain == null || chain.Role != QuestChainRole.Hub)
				return;
			if (!questModel.Hub.CanDeliverHubToken(chain, command.TokenKey))
				return;
			currencyModel.TrySpend(command.TokenKey, 1.0);
			questModel.MarkHubTokenDelivered(command.TokenKey);
		}
	}
}
