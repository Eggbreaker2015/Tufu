using Codebase.Core;
using Codebase.Core.UI;
using Scellecs.Morpeh;

namespace Codebase.Location.Systems
{
	public class IslandMasteryPopupSystem : SystemBase
	{
		[Inject]
		private UINavigator navigator;
		[Inject]
		private IslandMasteryModel islandMasteryModel;
		private PopupView activePopup;
		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (activePopup != null && activePopup.IsInitialized)
				return;
			activePopup = null;
			if (islandMasteryModel.TryConsumePresentation(out var presentation))
				ShowUpgradePopup(presentation);
		}

		private void ShowUpgradePopup(IslandMasteryPresentationResult presentation)
		{
			var popup = navigator.Open<global::Codebase.UI.IslandMastery.IslandMedalUpgradeView>(global::Codebase.UI.UIConstants.IslandMedalUpgradeView, true);
			if (popup == null)
				return;
			activePopup = popup;
			popup.Setup(presentation);
			popup.SetUserCloseCallback(() =>
			{
				if (activePopup == popup)
					activePopup = null;
			});
		}
	}
}
