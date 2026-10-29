using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Skills.Components
{
	// Only while a skill is being cast (cast point + channel + backswing, or an unfinished dash).
	public struct SkillCastingComponent : IComponent
	{
		public UnitCore Caster;

		public SkillSlot Slot;

		public UnitCore Target;

		public Vector3 Point;

		public Vector3 Direction;

		public float Elapsed;

		public bool Fired;

		public bool Channeling;

		public float ChannelElapsed;

		public int ChannelTicks;

		public SkillTelegraphView Telegraph;
	}
}
