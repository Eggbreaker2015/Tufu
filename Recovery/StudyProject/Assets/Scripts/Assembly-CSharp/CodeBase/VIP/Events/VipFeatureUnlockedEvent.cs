using Scellecs.Morpeh;

namespace Codebase.VIP.Events
{
	public struct VipFeatureUnlockedEvent : IEventData
	{
		public IVipFeature Feature;

		public int Level;
	}
}
