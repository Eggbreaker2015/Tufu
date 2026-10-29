using Codebase.Player.Inventory;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Skills
{
	public struct SkillCastContext
	{
		// The unit the effects are resolved from ("Self", circle center); the buff holder for buff ticks.
		public UnitCore Caster;

		public Entity CasterEntity;

		// Whose stats and side deal the damage; the caster for casts, the applier for buff ticks.
		public UnitCore Instigator;

		public UnitCore PrimaryTarget;

		public Vector3 Point;

		// Flat unit vector locked at cast start; zero = aim at PrimaryTarget, else the caster's forward.
		public Vector3 Direction;

		public SkillDef Skill;

		public BuffDef Buff;

		public int Level;

		// Level scale x external power (item ability Value); effects multiply their own Power by it.
		public float Power;

		// Item ability adapters only; null for enemy casts.
		public ItemModel SourceItem;

		public UnitCore Source => Instigator != null ? Instigator : Caster;
	}
}
