using Scellecs.Morpeh;

namespace Codebase.Unit.Components
{
	public struct ShockComponent : IComponent
	{
		public bool IsShocked;

		public float ShockedTimer;

		public int ConsumedShockCount;

		public float ConsumedDecayTimer;
	}
}
