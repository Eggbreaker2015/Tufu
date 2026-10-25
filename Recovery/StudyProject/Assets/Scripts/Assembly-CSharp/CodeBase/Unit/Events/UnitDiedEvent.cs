using Scellecs.Morpeh;

namespace Codebase.Unit.Events
{
	public struct UnitDiedEvent : IEventData
	{
		public UnitCore Unit;

		public Entity Entity;
	}
}
