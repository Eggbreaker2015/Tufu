using System;
using TMPro;
using UnityEngine;

namespace Codebase.UI
{
	public class SoulRewardView : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text text;

		[SerializeField]
		private float fadeDuration;

		[SerializeField]
		private float pauseDuration;

		public Action OnAnimationEnd;

		private float elapsed;

		private float totalDuration;

		private float activePauseDuration;

		public TMP_Text Text => this.text;

		public void SetPauseDuration(float duration)
		{
		}

		private void Awake()
		{
		}

		private void OnEnable()
		{
		}

		private void LateUpdate()
		{
		}

		private void SetAlpha(float alpha)
		{
		}
	}
}
