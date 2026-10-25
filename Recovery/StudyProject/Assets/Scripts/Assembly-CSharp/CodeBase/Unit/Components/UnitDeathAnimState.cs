using Scellecs.Morpeh;

namespace Codebase.Unit.Components
{
	public struct UnitDeathAnimState : ISystemStateComponent, IComponent
	{
		public float Timer;

		public bool InPostDeathPhase;

		public bool InSinkPhase;
	}
}
