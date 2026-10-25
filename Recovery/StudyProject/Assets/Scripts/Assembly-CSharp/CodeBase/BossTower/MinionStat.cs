using System;
using Codebase.Unit;

namespace Codebase.BossTower
{
	[Serializable]
	public struct MinionStat
	{
		public StatType Type;
		public float Multiplier;
		public static readonly MinionStat Default = new MinionStat
		{
			Type = StatType.Health,
			Multiplier = 1f
		};
	}
}
