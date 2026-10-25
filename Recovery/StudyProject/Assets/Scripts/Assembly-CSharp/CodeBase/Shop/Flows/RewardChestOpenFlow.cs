using System.Collections.Generic;
using Codebase.Core.Flows;
using Codebase.Rewards;
using Codebase.Utilities;

namespace Codebase.Shop.Flows
{
	public struct RewardChestOpenFlow : IFlow
	{
		public List<RewardBase> Rewards;

		[KeysDropdown(typeof(ChestKeys))]
		public string ChestKey;
	}
}
