using DG.Tweening;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Location;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.IslandMastery
{
	public class IslandMedalTierView : View
	{
		[SerializeField]
		private Image medalImage;

		[SerializeField]
		private TMP_Text levelText;

		[SerializeField]
		private GameObject clearedCheckmark;

		[Inject]
		private IslandMedalStyleConfig styleConfig;

		[Inject]
		private IslandMasteryModel islandMasteryModel;

		private CanvasGroup visibilityCanvasGroup;

		private int level;

		private int islandNumber;

		private bool isBoundToLocation;

		private bool isVisible = true;

		private const float FadeDuration = 0.15f;

		private const float FadeInDelay = 0.5f;

		protected override void Awake()
		{
			base.Awake();
			visibilityCanvasGroup = GetComponent<CanvasGroup>();
		}

		public void Setup(int medalLevel, int medalIslandNumber)
		{
			level = medalLevel;
			islandNumber = medalIslandNumber;
			isBoundToLocation = false;
			Initialize();
			Render();
		}

		public void SetupForLocation(int islandLocationNumber)
		{
			islandNumber = islandLocationNumber;
			isBoundToLocation = true;
			if (!IsInitialized) Initialize();
			Render();
			PlayAppearanceAnimation();
		}

		public void SetVisible(bool value)
		{
			if (isVisible == value) return;
			isVisible = value;
			if (IsInitialized) Render();
			visibilityCanvasGroup.DOKill();
			visibilityCanvasGroup.DOFade(value ? 1f : 0f, .15f).SetDelay(value ? .5f : 0f).SetEase(Ease.OutQuad);
		}

		protected override void Render()
		{
			_ = islandMasteryModel.StateVersion;
			int medalLevel = isBoundToLocation ? islandMasteryModel.GetMedalLevel(islandNumber) : level;
			if (medalLevel < 1)
			{
			    clearedCheckmark.SetActive(false);
			    medalImage.enabled = false;
			    levelText.enabled = false;
			    return;
			}
			clearedCheckmark.SetActive(isBoundToLocation && islandMasteryModel.IsFullyCleared(islandNumber));
			IslandMedalStyle style = styleConfig.GetStyle(medalLevel);
			medalImage.enabled = true;
			medalImage.sprite = style.Sprite;
			levelText.enabled = true;
			levelText.text = islandNumber.ToString();
			levelText.color = style.LevelColor;
		}

		private void PlayAppearanceAnimation()
		{
			visibilityCanvasGroup.DOKill();
			visibilityCanvasGroup.alpha = 0f;
			if (isVisible) visibilityCanvasGroup.DOFade(1f, .15f).SetEase(Ease.OutQuad);
		}
	}
}
