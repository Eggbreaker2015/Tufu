using Codebase.Core.Flows;

namespace Codebase.Events.Flows
{
	public struct RosterMiniBossKilledFlow : IFlow
	{
		public int ProgressIndex;

		public bool DidAdvanceLeague;
	}
}
