using Codebase.Utilities;
using Codebase.BossTower.Systems;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossHealthBarView : MonoBehaviour
	{
		[SerializeField]
		private Slider healthSlider;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private TMP_Text healthText;

		[SerializeField]
		private Image fillImage;

		[SerializeField]
		private Image ghostFillImage;

		private const float MATERIALIZE_DURATION = 1.3f;

		[SerializeField]
		private float blinkMinFrequency = .5f;

		[SerializeField]
		private float blinkMaxFrequency = 3f;

		private bool isMaterializing;

		private Tween blinkTween;

		public void StartGhostAnimation()
		{
			// Native ELF 0x13bc260, callbacks 0x13bc65c/664/714; rodata duration 1.3f.
			isMaterializing = true; healthText.text = "";
			float elapsed = 0f;
			blinkTween = DOTween.To(() => elapsed, value => {
			    elapsed = value;
			    float progress = Mathf.Clamp01(elapsed / MATERIALIZE_DURATION);
			    float frequency = Mathf.Lerp(blinkMinFrequency, blinkMaxFrequency, progress * progress * progress);
			    float phase = frequency * elapsed * Mathf.PI;
			    phase = phase + phase - Mathf.PI / 2f;
			    canvasGroup.alpha = (Mathf.Sin(phase) + 1f) * .5f;
			}, MATERIALIZE_DURATION, MATERIALIZE_DURATION).SetEase(Ease.Linear).OnComplete(() => {
			    isMaterializing = false; canvasGroup.alpha = 1f;
			    transform.DOPunchScale(Vector3.one * .2f, .4f, 5, .5f);
			});
		}

		public void SetData(BossHealthBarData data)
		{
			// Native ELF 0x13bc494.
			if (isMaterializing) { healthSlider.value = 0f; return; }
			fillImage.enabled = data.OriginalUnitGuid == null;
			ghostFillImage.enabled = data.OriginalUnitGuid != null;
			healthSlider.value = data.HealthFraction; canvasGroup.alpha = data.Alpha;
			healthText.text = data.CurrentHealth.Format() + "/<color=#FFBDBD>" + data.MaxHealth.Format();
		}

		private void OnDisable()
		{
			// Native ELF 0x13bc5f0; pool reuse resets the blink state.
			blinkTween?.Kill(); blinkTween = null; isMaterializing = false; canvasGroup.alpha = 1f;
		}
	}
}
