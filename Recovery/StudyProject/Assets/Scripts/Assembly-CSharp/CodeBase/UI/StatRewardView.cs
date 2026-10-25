using System;
using TMPro;
using UnityEngine;

namespace Codebase.UI
{
	public class StatRewardView : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text text;

		public Vector2 TargetPosition;

		public bool MoveOnAppear = true;

		[SerializeField]
		private float fadeDuration = 0.5f;

		[SerializeField]
		private float pauseDuration = 0.3f;

		[SerializeField]
		private float totalDuration = 1.3f;

		[SerializeField]
		private float moveDistance = 40f;

		[SerializeField]
		private float moveSmoothSpeed = 20f;

		private float elapsed;

		public Action OnAnimationEnd;

		private RectTransform rect;

		public TMP_Text Text => this.text;

		private void Awake()
		{
			// Native ELF VA 0x15fd8f0.
			rect = text.rectTransform;
			totalDuration = fadeDuration * 2f + pauseDuration;
		}

		private void OnEnable()
		{
			// Native ELF VA 0x15fd934; final tail is SetVisualState(0).
			elapsed = 0f;
			rect.anchoredPosition = TargetPosition;
			((RectTransform)transform).anchoredPosition = Vector2.zero;
			text.rectTransform.anchoredPosition = Vector2.zero;
			SetVisualState(0f);
		}

		private void LateUpdate()
		{
			// Native ELF VA 0x15fdbf0; callback gate occurs before adding the current frame dt.
			if (elapsed >= totalDuration)
			{
			    OnAnimationEnd?.Invoke();
			    return;
			}
			elapsed += Time.deltaTime;
			SetVisualState(elapsed);
			rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, TargetPosition, Time.deltaTime * moveSmoothSpeed);
		}

		private void SetVisualState(float t)
		{
			// Native ELF VA 0x15fda58. Decompiler loses float math; reconstructed from bounded ARM64.
			float alpha;
			float movement;
			if (t < fadeDuration)
			{
			    alpha = t / fadeDuration;
			    movement = MoveOnAppear ? Mathf.Lerp(-moveDistance, 0f, alpha) : 0f;
			}
			else if (t < fadeDuration + pauseDuration)
			{
			    alpha = 1f;
			    movement = 0f;
			}
			else if (t < totalDuration)
			{
			    float progress = (t - fadeDuration - pauseDuration) / fadeDuration;
			    alpha = 1f - progress;
			    movement = Mathf.Lerp(0f, moveDistance, progress);
			}
			else
			{
			    alpha = 0f;
			    movement = 0f;
			}
			Color color = text.color;
			color.a = Mathf.Clamp01(alpha);
			text.color = color;
			text.rectTransform.anchoredPosition += Vector2.up * movement;
		}
	}
}
