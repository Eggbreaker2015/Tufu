using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
	[Serializable]
	public sealed class AudioSourceAnimationStep : AnimationStepBase
	{
		[SerializeField]
		private AudioSource audioSource;

		[SerializeField]
		private float duration = 1f;

		[SerializeField]
		private bool loop;

		[SerializeField]
		private bool volumeCurveEnabled;

		[Range(0f, 1f)]
		[SerializeField]
		private float initialVolume = 1f;

		[Range(0f, 1f)]
		[SerializeField]
		private float finalVolume;

		[SerializeField]
		private AnimationCurve volumeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		private float previousVolume;

		public override string DisplayName => "Audio Source Play";

		public override void AddTweenToSequence(Sequence animationSequence)
		{
			// Original ELF VA 0x149b0c8 and closures 0x149b5f0..0x149b710.
			if (audioSource == null)
			{
				Debug.LogError("AudioSourceAnimationStep: AudioSource is null");
				return;
			}
			previousVolume = audioSource.volume;
			float elapsed = 0f;
			Tween tween = DOTween.To(() => elapsed, value =>
			{
				elapsed = value;
				if (!volumeCurveEnabled) return;
				float progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
				audioSource.volume = Mathf.Lerp(initialVolume, finalVolume, volumeCurve.Evaluate(progress));
			}, duration, duration).SetEase(Ease.Linear).SetDelay(Delay)
			.OnStart(() =>
			{
				audioSource.loop = loop;
				if (volumeCurveEnabled) audioSource.volume = initialVolume;
				audioSource.Play();
			})
			.OnComplete(() =>
			{
				if (loop) audioSource.Stop();
			});
			if (FlowType == FlowType.Join) animationSequence.Join(tween);
			else animationSequence.Append(tween);
		}

		public override void ResetToInitialState()
		{
			// Original ELF VA 0x149b408: keep the loop setting, stop and restore volume.
			if (audioSource == null) return;
			audioSource.Stop();
			audioSource.volume = previousVolume;
		}

		public override string GetDisplayNameForEditor(int index)
		{
			// Original ELF VA 0x149b4a0.
			return string.Format("{0}. {1}: {2}", index, audioSource != null ? audioSource.name : "NULL", DisplayName);
		}
	}
}
