using Codebase.Analytics;
using Codebase.Core;
using Scellecs.Morpeh;

namespace Codebase.Shop.Systems
{
	public class ShopOfferTimerSystem : SystemBase
	{
		[Inject]
		private UserModel userModel;

		private const float CheckInterval = 1f;

		private float timer;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
