using Scellecs.Morpeh;

namespace Codebase.Skills.Components
{
	public struct SkillForcedMovementComponent : IComponent
	{
		// Reference identity lets nested OnHit effects safely replace a movement.
		public SkillForcedMovement.State Movement;
	}
}
