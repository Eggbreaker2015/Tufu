using UnityEngine;

namespace FIMSpace.Basics
{
	public class FAnimator
	{
		public readonly Animator Animator;

		public string CurrentAnimation { get; private set; }

		public string PreviousAnimation { get; private set; }

		public int Layer { get; private set; }

		public FAnimator(Animator animator, int layer = 0)
		{
		}

		public bool ContainsClip(string clipName, bool exactClipName = false)
		{
			return false;
		}

		public void CrossFadeInFixedTime(string clip, float transitionTime = 0.25f, float timeOffset = 0f, bool startOver = false)
		{
		}

		public void CrossFade(string clip, float transitionTime = 0.25f, float timeOffset = 0f, bool startOver = false)
		{
		}

		private void RefreshClipMemory(string name)
		{
		}

		public void SetFloat(string parameter, float value = 0f, float deltaSpeed = 60f)
		{
		}

		public void SetFloatUnscaledDelta(string parameter, float value = 0f, float deltaSpeed = 60f)
		{
		}

		internal bool IsPlaying(string clip)
		{
			return false;
		}
	}
}
