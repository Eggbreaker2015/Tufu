using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class CrystalRespawnedView : PopupView
	{
		private const float ProgressAnimationDelay = 1f;
		private const float ProgressAnimationDuration = 1.5f;
		private const float MaxShakeMagnitude = 15f;
		private const float ShakeFrequency = 11f;
		[SerializeField]
		private Button continueButton;
		[SerializeField]
		private TMP_Text progressText;
		[SerializeField]
		private Slider progressSlider;
		[SerializeField]
		private RectTransform shakeTarget;
		[Inject]
		private CrystalModel crystalModel;
		[Inject]
		private IAudioPlayer audioPlayer;
		private Tween progressTween;
		private Vector2 shakeBaseAnchoredPosition;
		protected override void Awake()
		{
			base.Awake();
			continueButton.onClick.AddListener(HandleContinue);
		}

		public override void Initialize()
		{
			base.Initialize();
			AnimateProgress();
		}

		public void PlayFeaturesPopUpSound()
		{
			audioPlayer.PlaySoundOnce("features_pop_up");
		}

		public override void Cleanup()
		{
			progressTween?.Kill();
			progressTween = null;
			shakeTarget.anchoredPosition = shakeBaseAnchoredPosition;
			base.Cleanup();
		}

		private void AnimateProgress()
		{
			int required = crystalModel.ObjectiveGeodesRequiredForRespawn;
			shakeBaseAnchoredPosition = shakeTarget.anchoredPosition;
			progressText.text = string.Format("0/{0}", required);
			progressSlider.value = 0f;
			progressTween?.Kill();
			progressTween = DOTween.To(() => 0f, value =>
			{
				progressText.text = string.Format("{0}/{1}", (int)System.Math.Round((double)value), required);
				progressSlider.value = required < 1 ? 0f : value / (float)required;
				ApplyShake(required < 1 ? 0f : value / (float)required);
			}, (float)required, 1.5f).SetDelay(1f).OnStart(() => audioPlayer.PlaySoundOnce("crystal_respawn_charge")).OnComplete(() => shakeTarget.anchoredPosition = shakeBaseAnchoredPosition).SetTarget(this);
		}

		private void ApplyShake(float progressFraction)
		{
			float phase = Time.time * 11f;
			float x = Mathf.PerlinNoise(phase, 0f), y = Mathf.PerlinNoise(0f, phase);
			shakeTarget.anchoredPosition = new Vector2((x - 0.5f + x - 0.5f) * progressFraction * 15f + shakeBaseAnchoredPosition.x, (y - 0.5f + y - 0.5f) * progressFraction * 15f + shakeBaseAnchoredPosition.y);
		}

		private void HandleContinue()
		{
			Close();
			App.InitiateFlow(default(Codebase.Crystals.Flows.CrystalObjectiveRespawnedFlow));
		}
	}
}
