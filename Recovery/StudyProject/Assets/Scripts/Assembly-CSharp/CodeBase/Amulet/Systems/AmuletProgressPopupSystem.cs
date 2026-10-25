using Codebase.Core;
using Codebase.Core.UI;
using Codebase.UI.Amulet;
using Scellecs.Morpeh;

namespace Codebase.Amulet.Systems
{
	public class AmuletProgressPopupSystem : SystemBase
	{
		[Inject]
		private UINavigator navigator;

		[Inject]
		private AmuletModel amuletModel;

		private const float ShowDelay = 0.625f;

		private double lastKnownExperience;

		private double pendingPreviousExperience;

		private float delayTimer;

		private bool isPendingShow;

		private AmuletProgressPopupView activePopup;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void ShowProgressPopup(double previousExperience)
		{
		}

		private void TryShowGraphLevelUnlock()
		{
		}
	}
}
