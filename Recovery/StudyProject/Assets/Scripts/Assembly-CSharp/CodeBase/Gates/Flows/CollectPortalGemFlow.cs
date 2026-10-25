using Codebase.Core.Flows;

namespace Codebase.Gates.Flows
{
	public struct CollectPortalGemFlow : IFlow
	{
		public int GemIndex;

		public bool WaitForItemReward;
	}
}
