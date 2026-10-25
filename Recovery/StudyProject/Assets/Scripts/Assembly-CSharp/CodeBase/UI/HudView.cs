using Codebase.Core;
using Codebase.Core.UI;
using UnityEngine;

namespace Codebase.UI
{
	public class HudView : View
	{
		[SerializeField]
		private GameObject minimap;

		[SerializeField]
		private VisibilityAnimator hudVisible;

		[Inject]
		private UiModel uiModel;

		protected override void Awake()
		{
			// Native ELF VA 0x15d6efc.
			base.Awake();
			Initialize();
		}

		protected override void Render()
		{
			// Native ELF VA 0x15d6f20; bounded original body calls VisibilityAnimator.SetVisible.
			minimap.SetActive(uiModel.IsMinimapVisible);
			hudVisible.SetVisible(uiModel.IsHudVisible);
		}
	}
}
