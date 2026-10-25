using Codebase.Analytics;
using Codebase.Core;
using Codebase.Vibration.Events;
using Scellecs.Morpeh;

namespace Codebase.Vibration.Systems
{
	public class HapticSystem : SystemBase
	{
		private Request<HapticRequest> request;

		[Inject]
		private UserModel userModel;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
