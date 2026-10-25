using System.Collections.Generic;
using Codebase.Rewards;

namespace Codebase.Shop
{
	public struct ChestQueueEntry
	{
		public ChestQueueEntryType Type;

		public ChestDef ChestDef;

		public int Count;

		public List<RewardBase> Rewards;

		public string ChestKey;
	}
}
