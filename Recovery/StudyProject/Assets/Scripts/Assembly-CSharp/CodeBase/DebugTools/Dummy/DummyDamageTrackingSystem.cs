using Codebase.Core;
using Scellecs.Morpeh;

namespace Codebase.DebugTools.Dummy
{
	public class DummyDamageTrackingSystem : SystemBase
	{
		[Inject]
		private Stash<DummyTargetTag> dummyStash;

		private Filter dummyFilter;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
