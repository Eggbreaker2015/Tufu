using DG.Tweening;
using Codebase.BossTower.Systems;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class MinionHealthBarView : MonoBehaviour
	{
		[SerializeField]
		private Slider healthSlider;

		[SerializeField]
		private CanvasGroup canvasGroup;

		private float previousHealthFaction;

		private TweenerCore<float, float, FloatOptions> appearTween;

		private void OnEnable()
		{
			// Native ELF VA 0x13c8844; rodata duration 0.7f, Ease numeric value 7.
			appearTween = healthSlider.DOValue(1f, .7f).SetEase((Ease)7).From(0f);
			previousHealthFaction = 1f;
		}

		public void SetData(MinionHealthBarData data)
		{
			// Native ELF VA 0x13c88f8. Keep incoming updates deferred during the authored appear tween.
			if (!Mathf.Approximately(previousHealthFaction, data.HealthFraction) && (appearTween == null || !appearTween.IsPlaying()))
			{
			    previousHealthFaction = data.HealthFraction;
			    healthSlider.value = data.HealthFraction;
			}
			canvasGroup.alpha = data.Alpha;
		}
	}
}
