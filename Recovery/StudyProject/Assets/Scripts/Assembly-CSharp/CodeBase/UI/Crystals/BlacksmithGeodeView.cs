using System;
using AssetKits.ParticleImage;
using DG.Tweening;
using UnityEngine;

namespace Codebase.UI.Crystals
{
	public class BlacksmithGeodeView : MonoBehaviour
	{
		[SerializeField]
		private RectTransform geodeTransform;

		[SerializeField]
		private CanvasGroup[] geodeStageLayers;

		[SerializeField]
		private ParticleImage impactParticles;

		[SerializeField]
		private float openStepDuration = .5f;

		[SerializeField]
		private float geodeShakeStrength = 15f;

		[SerializeField]
		private float idleShakeStrength = 3f;

		[SerializeField]
		private float idleShakeStepDuration = .1f;

		private Sequence stepSequence;

		private Tween idleShakeTween;

		private bool idleShakeActive;

		private Vector2 initialAnchoredPosition;

		private Action stepCompleteCallback;

		private void Awake()
		{
            // Native ELF0x13b3584.
            initialAnchoredPosition = geodeTransform.anchoredPosition;
        }

		public void ResetVisuals()
		{
            // Native ELF0x13b35ac.
            KillAnimations(); gameObject.SetActive(true); ApplyGeodeStage(0);
        }

		public void Hide()
		{
            // Native ELF0x13b3730.
            KillAnimations(); gameObject.SetActive(false);
        }

		public void PlayIdleShake()
		{
            // Native ELF0x13b3760.
            if (idleShakeActive) return;
            idleShakeActive = true; geodeTransform.anchoredPosition = initialAnchoredPosition; PlayIdleShakeStep();
        }

		private void PlayIdleShakeStep()
		{
            // Native ELF0x13b37a4.
            if (!idleShakeActive) return;
            float x = UnityEngine.Random.Range(-idleShakeStrength, idleShakeStrength);
            float y = UnityEngine.Random.Range(-idleShakeStrength, idleShakeStrength);
            float duration = idleShakeStepDuration * UnityEngine.Random.Range(.6f, 1.4f);
            idleShakeTween = geodeTransform.DOAnchorPos(new Vector2(x + initialAnchoredPosition.x, y + initialAnchoredPosition.y), duration)
                .SetEase((Ease)4).OnComplete(PlayIdleShakeStep).SetTarget(this);
        }

		public void StopIdleShake()
		{
            // Native ELF0x13b3938.
            idleShakeActive = false;
            if (idleShakeTween != null) idleShakeTween.Kill();
            idleShakeTween = null; geodeTransform.anchoredPosition = initialAnchoredPosition;
        }

		public void KillAnimations()
		{
            // Native ELF0x13b35e8.
            if (stepSequence != null) stepSequence.Kill();
            stepSequence = null; stepCompleteCallback = null;
            StopIdleShake(); geodeTransform.DOKill(); geodeTransform.anchoredPosition = initialAnchoredPosition;
            foreach (var layer in geodeStageLayers) layer.DOKill();
        }

		public void PlayGeodeStep(int targetStageIndex, Action onComplete)
		{
            // Native ELF0x13b3990.
            KillAnimations(); stepCompleteCallback = onComplete;
            stepSequence = DOTween.Sequence().SetTarget(this);
            impactParticles.Stop(); impactParticles.Play();
            stepSequence.Append(geodeTransform.DOShakeAnchorPos(openStepDuration, geodeShakeStrength, 20, 90f, false, true).SetEase((Ease)1));
            stepSequence.Join(BuildGeodeStageCrossfade(targetStageIndex));
            stepSequence.OnComplete(HandleStepCompleted);
        }

		public void CompleteGeodeStep(int targetStageIndex)
		{
            // Native ELF0x13b3cbc.
            KillAnimations(); ApplyGeodeStage(targetStageIndex);
        }

		public void RevealInstant()
		{
            // Native ELF0x13b3ce4.
            KillAnimations(); ApplyGeodeStage(geodeStageLayers.Length - 1);
        }

		private void HandleStepCompleted()
		{
            // Native ELF0x13b3d10.
            stepSequence = null; var callback = stepCompleteCallback; stepCompleteCallback = null; callback?.Invoke();
        }

		private Tween BuildGeodeStageCrossfade(int targetStageIndex)
		{
            // Native ELF0x13b3b54.
            var previous = geodeStageLayers[targetStageIndex - 1]; var current = geodeStageLayers[targetStageIndex];
            previous.alpha = 1f; current.alpha = 0f;
            var sequence = DOTween.Sequence().SetTarget(this);
            sequence.Join(previous.DOFade(0f, openStepDuration).SetEase((Ease)6));
            sequence.Join(current.DOFade(1f, openStepDuration).SetEase((Ease)6));
            return sequence;
        }

		private void ApplyGeodeStage(int stageIndex)
		{
            // Native ELF0x13b36b4.
            for (int i = 0; i < geodeStageLayers.Length; i++) geodeStageLayers[i].alpha = i == stageIndex ? 1f : 0f;
        }
	}
}
