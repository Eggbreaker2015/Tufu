using Codebase.ABTests;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class HudButtons : View
	{
		[SerializeField]
		private GameObject levelPassButton;

		[SerializeField]
		private VisibilityAnimator rightButtonsVisibility;

		[SerializeField]
		private VerticalLayoutGroup verticalLayoutGroup;

		[Inject]
		private ABTestService abTestService;

		[Inject]
		private BossTowerModel bossTowerModel;

		private bool isDisplayed;

		protected override void Awake()
		{
			// Native ELF VA 0x15d6dec.
			base.Awake();
			Initialize();
		}

		protected override void Render()
		{
			// Native ELF VA 0x15d6e10; layout toggles only when displayed state changes.
			levelPassButton.SetActive(abTestService.IsLevelPassEnabled());
			rightButtonsVisibility.SetVisible(!bossTowerModel.IsFighting);
			if (isDisplayed != bossTowerModel.IsFighting) return;
			isDisplayed = !bossTowerModel.IsFighting;
			verticalLayoutGroup.enabled = false;
			verticalLayoutGroup.enabled = true;
		}
	}
}
