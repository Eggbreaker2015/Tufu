using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
	[Serializable]
	public sealed class EmissionAcceleratingBlinkStep : GameObjectAnimationStep
	{
		[SerializeField]
		private string propertyName = "_EmissionColor";

		[SerializeField]
		private Color targetColor = Color.white;

		[SerializeField]
		private Color offColor = new Color(0f, 0f, 0f, 1f);

		[SerializeField]
		private float minFrequency = 0.5f;

		[SerializeField]
		private float maxFrequency = 4.5f;

		private Renderer previousRenderer;

		private Color previousColor;

		public override string DisplayName => "Emission Accelerating Blink";

		public override void AddTweenToSequence(Sequence animationSequence)
		{
			// Original ELF VA 0x149b784; the original intentionally uses sharedMaterial.
			Renderer renderer = target.GetComponent<Renderer>();
			if (renderer == null)
			{
				Debug.LogError(string.Format("{0} does not have a Renderer component", target));
				return;
			}
			previousRenderer = renderer;
			previousColor = renderer.sharedMaterial.GetColor(propertyName);
			float elapsed = 0f;
			Tween tween = DOTween.To(() => elapsed, value =>
			{
				// Original setter ELF VA 0x149bc68.
				elapsed = value;
				float progress = Mathf.Clamp01(elapsed / duration);
				float phase = Mathf.Lerp(minFrequency, maxFrequency, progress * progress * progress) * elapsed * Mathf.PI;
				float amount = (Mathf.Sin(phase + phase - 1.5707963705062866f) + 1f) * 0.5f;
				renderer.sharedMaterial.SetColor(propertyName, Color.Lerp(offColor, targetColor, amount));
			}, duration, duration).SetEase(Ease.Linear).SetDelay(Delay);
			if (FlowType == FlowType.Join) animationSequence.Join(tween);
			else animationSequence.Append(tween);
		}

		public override void ResetToInitialState()
		{
			// Original ELF VA 0x149ba4c.
			if (previousRenderer == null) return;
			previousRenderer.sharedMaterial.SetColor(propertyName, previousColor);
		}

		public override string GetDisplayNameForEditor(int index)
		{
			// Original ELF VA 0x149bae8; the supplied index is preserved.
			return string.Format("{0}. {1}: {2}", index, target != null ? target.name : "NULL", DisplayName);
		}
	}
}
