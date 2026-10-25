using BrunoMikoski.AnimationSequencer;
using UnityEngine;
using System.Reflection;

namespace Codebase.UI
{
	public class VisibilityAnimator : MonoBehaviour
	{
		[SerializeField]
		private AnimationSequencerController animController;

		private bool targetVisible;

		private bool skipAllCalls;

		private const int MaxSaneLoopCount = 1000000;

		private void Awake()
		{
			// Native ELF VA 0x15ffcd8.
			if (HasInfiniteLoops())
			{
				Debug.LogError("[VisibilityAnimator] '" + gameObject.name + "': AnimationSequencer or one of its steps is set to infinite loops (loops == -1). SetProgress/Play on such a sequence performs Goto(Infinity) and freezes the main thread on device. This VisibilityAnimator is disabled \u2014 fix the prefab to use finite loops.", this);
				skipAllCalls = true;
			}
		}

		private static bool IsInfiniteLoopCount(int loopCount)
		{
			// Native ELF VA 0x15fff7c uses an unsigned comparison (negative is infinite).
			return (uint)loopCount >= MaxSaneLoopCount;
		}

		private bool HasInfiniteLoops()
		{
			// Native ELF VA 0x15ffdb0 retains the original private field lookup.
			if (animController == null) return false;
			FieldInfo loops = typeof(AnimationSequencerController).GetField("loops", BindingFlags.Instance | BindingFlags.NonPublic);
			if (loops != null && IsInfiniteLoopCount((int)loops.GetValue(animController))) return true;
			foreach (AnimationStepBase step in animController.AnimationSteps)
				if (step is DOTweenAnimationStep tween && IsInfiniteLoopCount(tween.LoopCount)) return true;
			return false;
		}

		private void OnEnable()
		{
			// Native ELF VA 0x15fff90.
			if (skipAllCalls) return;
			animController.SetProgress(1f, false);
			targetVisible = true;
		}

		public void SetVisible(bool visible, bool forceToPlay = false)
		{
			// Native ELF VA 0x15fffd4; original animation directions/progress.
			if (skipAllCalls || (targetVisible == visible && !forceToPlay)) return;
			targetVisible = visible;
			animController.ClearPlayingSequence();
			if (visible)
			{
				animController.Kill(false);
				animController.ResetToInitialState();
				animController.SetProgress(0f, false);
				animController.PlayForward(true);
			}
			else
			{
				animController.ResetToInitialState();
				animController.SetProgress(1f, false);
				animController.PlayBackwards(false);
			}
		}

		public void SetVisibleImmediate(bool visible)
		{
			// Native ELF VA 0x16000ec.
			if (skipAllCalls || targetVisible == visible) return;
			targetVisible = visible;
			SetVisibleInternal(visible);
		}

		private void SetVisibleInternal(bool visible)
		{
			// Native ELF VA 0x1600114 clears then seeks. Reconstruction repair:
			// restore captured starts before GenerateSequence can recapture a completed end pose.
			if (skipAllCalls) return;
			animController.ClearPlayingSequence();
			animController.ResetToInitialState();
			animController.SetProgress(visible ? 1f : 0f, false);
		}
	}
}
