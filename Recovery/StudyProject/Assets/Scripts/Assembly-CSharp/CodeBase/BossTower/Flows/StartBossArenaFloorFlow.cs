using Codebase.Core.Flows;

namespace Codebase.BossTower.Flows
{
	public struct StartBossArenaFloorFlow : IFlow
	{
		public string BossKey;

		public bool ReviveFirst;

		public bool IsCalledFromArena;

		public bool UseCheats;
	}
}
