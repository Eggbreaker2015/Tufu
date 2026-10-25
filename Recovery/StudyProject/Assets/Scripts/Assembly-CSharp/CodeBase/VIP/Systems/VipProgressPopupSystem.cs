using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Scellecs.Morpeh;

namespace Codebase.VIP.Systems
{
	public class VipProgressPopupSystem : SystemBase
	{
		[Inject]
		private UINavigator navigator;

		[Inject]
		private UserModel userModel;

		[Inject]
		private VipModel vipModel;

		private const float ShowDelay = 1.25f;

		private float lastKnownExperience;

		private float delayTimer;

		private bool isPendingShow;

		private int pendingPreviousExperience;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void ShowVipProgressPopup(int previousExperience)
		{
		}
	}
}
