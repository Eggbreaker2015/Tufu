using Codebase.Core;
using Codebase.UI.Inventory;
using Codebase.Utilities;
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
            // Native 0x16046c4.
            base.Awake();
            if (previewButton != null) previewButton.onClick.AddListener(OnPreviewButtonClicked);
		}

		public void Setup(string localizationKey, VipFeatureChangeType changeType, int level, ItemConfig itemConfig = null)
		{
            // Native 0x16047a8.
            descriptionText.text = ProcessColorTags(LocalizationStrings.Get(localizationKey).Replace("{value}", level.ToString()));
            newState.SetActive(changeType == VipFeatureChangeType.New);
            upState.SetActive(changeType == VipFeatureChangeType.Up);
            defaultState.SetActive(changeType == VipFeatureChangeType.None);
            previewItemConfig = itemConfig;
            if (previewButtonObject != null) previewButtonObject.SetActive(itemConfig != null);
		}

		private void OnPreviewButtonClicked()
		{
            // Native 0x1604a44: isolated level-1 preview ItemModel, never added to inventory.
            if (previewItemConfig == null) return;
            ItemModel item = new ItemModel(Lifetime, previewItemConfig);
            item.Level = 1;
            GameContext.Get<UINavigator>().Open<ItemMiniView>("Assets/Prefabs/UI/ItemMiniView.prefab", true).Setup(item);
		}

		private void LateUpdate()
		{
            // Native 0x1604ba0.
            if (animationTimer > 0f)
            {
                animationTimer -= Time.deltaTime;
                if (animationTimer <= 0f) revealAnimation.Play();
            }
		}

		public void PlayRevealAnimation(int index)
		{
            // Native 0x1604bfc; original ELF float delay = 0.1f.
            canvasGroup.alpha = 0f;
            animationTimer = index / 4f + 0.1f;
		}

		private string ProcessColorTags(string text)
		{
            // Native 0x160493c: paired $...$ markup only.
            while (text.Contains("$"))
            {
                int first = text.IndexOf('$');
                int second = text.IndexOf('$', first + 1);
                if (second == -1) return text;
                string before = text.Substring(0, first);
                string inner = text.Substring(first + 1, second - first - 1);
                string after = text.Substring(second + 1);
                text = before + WrapInColor(inner) + after;
            }
            return text;
		}

		private string WrapInColor(string text)
		{
            // Native 0x1604c44 bounded tail is String.Concat.
            return "<color=#FFD500>" + text + "</color>";
		}
	}
}
