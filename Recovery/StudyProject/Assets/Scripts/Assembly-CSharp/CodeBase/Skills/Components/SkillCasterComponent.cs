using System.Collections.Generic;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Skills.Components
{
	public class SkillSlot
	{
		public SkillDef Def;

		public int Level;

		public float Cooldown;

		// Passive aura pulse timer.
		public float AuraTimer;
	}

	public struct SkillCasterComponent : IComponent
	{
		public UnitCore Owner;

		public List<SkillSlot> Slots;

		public float NextThinkTime;

		// Enemy casters: in combat last frame; entering combat re-arms InitialCooldown.
		public bool Engaged;
	}
}
