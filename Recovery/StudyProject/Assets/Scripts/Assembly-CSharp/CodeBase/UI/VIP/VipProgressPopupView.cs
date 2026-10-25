using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.VIP;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.VIP
{
	public class VipProgressPopupView : PopupView
	{
		[CompilerGenerated]
		private sealed class _003CAnimateExperienceGain_003Ed__24 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public VipProgressPopupView _003C_003E4__this;

			private float _003Celapsed_003E5__2;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CAnimateExperienceGain_003Ed__24(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[Header("UI Elements")]
		[SerializeField]
		private TMP_Text currentLevelText;

		[SerializeField]
		private TMP_Text nextLevelText;

		[SerializeField]
		private TMP_Text progressText;

		[SerializeField]
		private Slider progressSlider;

		[SerializeField]
		private Transform vipIcon;

		[Header("Animation Settings")]
		[SerializeField]
		private float experienceAnimationDuration;

		[SerializeField]
		private float delayBeforeClose;

		[SerializeField]
		private Ease animationEase;

		[SerializeField]
		private float punchScale;

		[SerializeField]
		private int punchVibrato;

		[SerializeField]
		private float punchElasticity;

		[Inject]
		private VipModel vipModel;

		[Inject]
		private VipConfig vipConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private UiModel uiModel;

		private int startExperience;

		private int targetExperience;

		private int startLevel;

		private int lastDisplayedLevel;

		private float currentSliderValue;

		private Coroutine animationCoroutine;

		protected override void Awake()
		{
		}

		public override void Initialize()
		{
		}

		public void StartAnimation(int previousExperience)
		{
		}

		[IteratorStateMachine(typeof(_003CAnimateExperienceGain_003Ed__24))]
		private IEnumerator AnimateExperienceGain()
		{
			return null;
		}

		private void UpdateDisplay(int experience, int level, float sliderValue)
		{
		}

		private void PlayLevelUpAnimation()
		{
		}

		private int CalculateLevelForExperience(int experience)
		{
			return 0;
		}

		protected override void Render()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
