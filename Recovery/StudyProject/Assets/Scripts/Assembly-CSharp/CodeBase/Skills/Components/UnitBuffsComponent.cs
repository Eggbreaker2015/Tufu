using System.Collections.Generic;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Skills.Components
{
	public struct AppliedStatMod
	{
		public Stat Stat;

		public StatModifier Modifier;
	}

	public class BuffInstance
	{
		public BuffDef Def;

		public float Remaining;

		public int Stacks;

		public float TickTimer;

		public float Power;

		public UnitCore Source;

		// Hostile buff on the hero or a pet: slow, stun, defence/attack interval factors and ticks only (no silence, no other stats).
		public bool Restricted;

		public readonly List<AppliedStatMod> Applied = new List<AppliedStatMod>(2);
	}

	public struct UnitBuffsComponent : IComponent
	{
		public UnitCore Owner;

		public List<BuffInstance> Buffs;
	}
}
