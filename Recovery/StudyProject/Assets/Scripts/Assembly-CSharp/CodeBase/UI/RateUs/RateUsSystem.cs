using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using JetBrains.Annotations;
using Scellecs.Morpeh;

namespace Codebase.UI.RateUs
{
	public class RateUsSystem : SystemBase
	{
		[Inject]
		private UINavigator navigator;
		[Inject]
		private UserModel userModel;
		private float timer = 1800f;
		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			timer -= deltaTime;
			if (timer <= 0f && !userModel.CompletedRateUs.Value)
			{
				timer = float.MaxValue;
				navigator.Open<RateUsView>("Assets/Prefabs/UI/RateUs.prefab", true).SetActive(true);
			}
		}

		[PublicAPI]
		public void RequestRateUs(float delay)
		{
			this.timer = delay;
		}
	}
}
