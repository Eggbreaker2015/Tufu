using Codebase.UI.VIP;
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
            // Native 0x15c06d8.
            lastKnownExperience = userModel.InAppsBoughtValueInRewarded.Value;
            delayTimer = 0f;
            isPendingShow = false;
		}

		public override void OnUpdate(float deltaTime)
		{
            // Native 0x15c074c: batch successive gains, keep the first previous total and delay.
            float experience = userModel.InAppsBoughtValueInRewarded.Value;
            if (experience > lastKnownExperience)
            {
                if (!isPendingShow)
                {
                    isPendingShow = true;
                    pendingPreviousExperience = (int)lastKnownExperience;
                    delayTimer = ShowDelay;
                }
                lastKnownExperience = experience;
            }
            if (!isPendingShow) return;
            delayTimer -= deltaTime;
            if (delayTimer <= 0f)
            {
                isPendingShow = false;
                ShowVipProgressPopup(pendingPreviousExperience);
            }
		}

		private void ShowVipProgressPopup(int previousExperience)
		{
            // Native 0x15c0838; no background and no extra spending/unlock mutation.
            VipProgressPopupView popup = navigator.Open<VipProgressPopupView>("Assets/Prefabs/UI/VIP/VipProgressPopup.prefab");
            if (popup != null) popup.StartAnimation(previousExperience);
		}
	}
}
