using BrunoMikoski.AnimationSequencer;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using Codebase.VIP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.VIP
{
	public class VipFeatureDescriptionView : View
	{
		[SerializeField]
		private TMP_Text descriptionText;

		[SerializeField]
		private GameObject newState;

		[SerializeField]
		private GameObject upState;

		[SerializeField]
		private GameObject defaultState;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private AnimationSequencerController revealAnimation;

		[SerializeField]
		private Button previewButton;

		[SerializeField]
		private GameObject previewButtonObject;

		private const string COLOR_TAG = "#FFD500";

		private float animationTimer;

		private ItemConfig previewItemConfig;

		protected override void Awake()
		{
		}

		public void Setup(string localizationKey, VipFeatureChangeType changeType, int level, ItemConfig itemConfig = null)
		{
		}

		private void OnPreviewButtonClicked()
		{
		}

		private void LateUpdate()
		{
		}

		public void PlayRevealAnimation(int index)
		{
		}

		private string ProcessColorTags(string text)
		{
			return null;
		}

		private string WrapInColor(string text)
		{
			return null;
		}
	}
}
