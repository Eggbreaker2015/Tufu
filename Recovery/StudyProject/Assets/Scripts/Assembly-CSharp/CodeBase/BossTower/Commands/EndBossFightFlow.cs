using Codebase.Core.Flows;

namespace Codebase.BossTower.Commands
{
	public struct EndBossFightFlow : IFlow
	{
		public string BossKey;

		public bool Victory;
	}
}
