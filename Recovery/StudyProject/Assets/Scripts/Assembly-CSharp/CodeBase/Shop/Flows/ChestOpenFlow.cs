using Codebase.Core.Flows;
using Codebase.Rewards;

namespace Codebase.Shop.Flows
{
	public struct ChestOpenFlow : IFlow
	{
		public int ChestCount;

		public ChestDef ChestDef;
	}
}
