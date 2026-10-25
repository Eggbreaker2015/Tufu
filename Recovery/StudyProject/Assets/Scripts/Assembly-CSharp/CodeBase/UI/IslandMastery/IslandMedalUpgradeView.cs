using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Location;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.IslandMastery
{
	public class IslandMedalUpgradeView : PopupView
	{
		private const string StatCapColor = "#A9E7FF";
		private const int StatCapTextSizePercent = 80;
		[SerializeField]
		private IslandMedalTierView previousTier;
		[SerializeField]
		private IslandMedalTierView currentTier;
		[SerializeField]
		private IslandMedalTierView nextTier;
		[SerializeField]
		private GameObject nextTierRoot;
		[SerializeField]
		private TMP_Text descriptionText;
		[SerializeField]
		private TMP_Text outgoingPercentText;
		[SerializeField]
		private TMP_Text incomingPercentText;
		[SerializeField]
		private Button closeButton;
		[SerializeField]
		private Graphic[] backgroundLayers;
		[Inject]
		private IAudioPlayer audioPlayer;
		private Sequence introSequence;
		private Vector3[] backgroundScales;
		private float[] backgroundAlphas;
		private Vector3 previousTierScale;
		private Vector3 currentTierScale;
		private Vector3 nextTierScale;
		private Vector3 closeButtonScale;
		private const float BackgroundDuration = 0.65f;
		private const float MedalDuration = 0.5f;
		private const float ElementDelay = 0.4f;
		private const float TextDuration = 0.4f;
		private const float IntroDelay = 0.3f;
		protected override void Awake()
		{
			base.Awake();
			closeButton.onClick.AddListener(Close);
			previousTierScale = previousTier.transform.localScale;
			currentTierScale = currentTier.transform.localScale;
			nextTierScale = nextTier.transform.localScale;
			closeButtonScale = closeButton.transform.localScale;
			backgroundScales = new Vector3[backgroundLayers.Length];
			backgroundAlphas = new float[backgroundLayers.Length];
			for (int i = 0; i < backgroundLayers.Length; i++)
			{
				backgroundScales[i] = backgroundLayers[i].transform.localScale;
				backgroundAlphas[i] = backgroundLayers[i].color.a;
			}
		}

		public void Setup(IslandMasteryPresentationResult presentation)
		{
			Injector.InjectInto(previousTier);
			Injector.InjectInto(currentTier);
			Injector.InjectInto(nextTier);
			previousTier.Setup(presentation.PreviousLevel, presentation.LocationNumber);
			currentTier.Setup(presentation.CurrentLevel, presentation.LocationNumber);
			int nextLevel = presentation.CurrentLevel + 1;
			bool hasNextTier = nextLevel <= IslandMasteryModel.MaxMedalLevel;
			nextTierRoot.SetActive(hasNextTier);
			if (hasNextTier)
				nextTier.Setup(nextLevel, presentation.LocationNumber);
			string islandName = global::Codebase.Utilities.LocalizationStrings.Get(string.Format("ISLAND_NAME_{0}", presentation.LocationNumber));
			descriptionText.text = global::Codebase.Utilities.LocalizationStrings.Get("ISLAND_MEDAL_DESCRIPTION").Replace("{island_name}", islandName);
			outgoingPercentText.text = FormatStatIncrease("atlas_57", presentation.CurrentLevel * 10, 50);
			incomingPercentText.text = FormatStatIncrease("atlas_58", presentation.CurrentLevel * 10, 50);
			PlayIntroAnimation(hasNextTier);
			audioPlayer.PlaySoundOnce("features_pop_up");
		}

		public override void Cleanup()
		{
			introSequence?.Kill(false);
			introSequence = null;
			base.Cleanup();
		}

		private void PlayIntroAnimation(bool hasNextTier)
		{
			introSequence?.Kill(false);
			for (int i = 0; i < backgroundLayers.Length; i++)
			{
				Color color = backgroundLayers[i].color;
				color.a = 0f;
				backgroundLayers[i].color = color;
				backgroundLayers[i].transform.localScale = backgroundScales[i] * .85f;
			}

			previousTier.transform.localScale = Vector3.zero;
			currentTier.transform.localScale = Vector3.zero;
			nextTier.transform.localScale = Vector3.zero;
			descriptionText.alpha = 0f;
			outgoingPercentText.alpha = 0f;
			incomingPercentText.alpha = 0f;
			closeButton.transform.localScale = Vector3.zero;
			introSequence = DOTween.Sequence().SetTarget(this).SetDelay(IntroDelay);
			for (int i = 0; i < backgroundLayers.Length; i++)
			{
				float at = (float)i * ElementDelay;
				introSequence.Insert(at, backgroundLayers[i].DOFade(backgroundAlphas[i], BackgroundDuration).SetEase(Ease.OutQuad));
				introSequence.Insert(at, backgroundLayers[i].transform.DOScale(backgroundScales[i], BackgroundDuration).SetEase(Ease.OutCubic));
			}

			float time = (float)backgroundLayers.Length * ElementDelay;
			introSequence.Insert(time, previousTier.transform.DOScale(previousTierScale, MedalDuration).SetEase(Ease.OutBack));
			introSequence.Insert(time + ElementDelay, currentTier.transform.DOScale(currentTierScale, MedalDuration).SetEase(Ease.OutBack));
			time += .8f;
			if (hasNextTier)
			{
				introSequence.Insert(time, nextTier.transform.DOScale(nextTierScale, MedalDuration).SetEase(Ease.OutBack));
				time += ElementDelay;
			}

			introSequence.Insert(time, descriptionText.DOFade(1f, TextDuration).SetEase(Ease.OutQuad));
			introSequence.Insert(time + ElementDelay, outgoingPercentText.DOFade(1f, TextDuration).SetEase(Ease.OutQuad));
			introSequence.Insert(time + .8f, incomingPercentText.DOFade(1f, TextDuration).SetEase(Ease.OutQuad));
			introSequence.Insert(time + .8f, closeButton.transform.DOScale(closeButtonScale, MedalDuration).SetEase(Ease.OutBack));
		}

		private static string FormatStatIncrease(string spriteName, int percent, int maxPercent)
		{
			return string.Format("<sprite name={0}> +{1}% <color={2}><size={3}%>/ {4}%</size></color>", spriteName, percent, StatCapColor, StatCapTextSizePercent, maxPercent);
		}
	}
}
