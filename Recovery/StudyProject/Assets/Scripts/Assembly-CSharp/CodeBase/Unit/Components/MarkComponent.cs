using Scellecs.Morpeh;

namespace Codebase.Unit.Components
{
	public struct MarkComponent : IComponent
	{
		public int Stacks;

		public int MaxStacks;

		public float DecayTimer;

		public bool AllowToStayAboveMax;
	}
}
