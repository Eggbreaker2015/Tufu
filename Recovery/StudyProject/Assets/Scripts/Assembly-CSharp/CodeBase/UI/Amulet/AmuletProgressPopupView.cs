using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.Amulet;
using Codebase.Core;
using Codebase.Core.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class AmuletProgressPopupView : PopupView
	{
		[CompilerGenerated]
		private sealed class _003CAnimateExperienceGain_003Ed__26 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public AmuletProgressPopupView _003C_003E4__this;

			private float _003Celapsed_003E5__2;

			private double _003Cfrom_003E5__3;

			private double _003Cto_003E5__4;

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
			public _003CAnimateExperienceGain_003Ed__26(int _003C_003E1__state)
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
		private TMP_Text progressText;

		[SerializeField]
		private Slider progressSlider;

		[SerializeField]
		private Transform amuletIcon;

		[Header("Death Animation")]
		[SerializeField]
		private AnimationSequencerController deathSequencer;

		[Header("Animation Settings")]
		[SerializeField]
		private float experienceAnimationDuration;

		[SerializeField]
		private float delayBeforeDeath;

		[SerializeField]
		private Ease animationEase;

		[SerializeField]
		private float punchScale;

		[SerializeField]
		private int punchVibrato;

		[SerializeField]
		private float punchElasticity;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		private double displayedExperience;

		private double targetExperience;

		private int lastDisplayedLevel;

		private float currentSliderValue;

		private Coroutine animationCoroutine;

		private bool deathSequenceStarted;

		private bool deathListenerBound;

		public bool CanContinue => false;

		public override void Initialize()
		{
		}

		public void StartAnimation(double previousExperience)
		{
		}

		public void ContinueAnimation()
		{
		}

		private void RestartAnimation()
		{
		}

		[IteratorStateMachine(typeof(_003CAnimateExperienceGain_003Ed__26))]
		private IEnumerator AnimateExperienceGain()
		{
			return null;
		}

		private void UpdateDisplay(double experience, int level, float sliderValue)
		{
		}

		private float CalculateSliderValue(double experience, int level)
		{
			return 0f;
		}

		private string FormatLevel(int level)
		{
			return null;
		}

		private void PlayLevelUpAnimation()
		{
		}

		private void BindDeathListener()
		{
		}

		private void ResetDeathSequencer()
		{
		}

		private void HandleDeathSequenceFinished()
		{
		}

		protected override void Render()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
