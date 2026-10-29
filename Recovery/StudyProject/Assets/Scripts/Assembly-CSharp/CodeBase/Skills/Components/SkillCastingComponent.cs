using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Skills.Components
{
	// Only while a skill is being cast (cast point + backswing).
	public struct SkillCastingComponent : IComponent
	{
		public UnitCore Caster;

		public SkillSlot Slot;

		public UnitCore Target;

		public Vector3 Point;

		public float Elapsed;

		public bool Fired;

		public SkillTelegraphView Telegraph;
	}
}
