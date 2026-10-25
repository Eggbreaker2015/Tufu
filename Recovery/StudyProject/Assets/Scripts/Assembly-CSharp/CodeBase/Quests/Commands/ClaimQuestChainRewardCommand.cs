using System;
using Codebase.Core.Commands;

namespace Codebase.Quests.Commands
{
	public struct ClaimQuestChainRewardCommand : ICommand
	{
		public string ChainKey;

		public Action OnComplete;
	}
}
