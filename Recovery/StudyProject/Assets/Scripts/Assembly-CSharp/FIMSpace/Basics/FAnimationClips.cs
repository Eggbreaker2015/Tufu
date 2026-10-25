using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Basics
{
	public class FAnimationClips : Dictionary<string, int>
	{
		public readonly Animator Animator;

		public int Layer;

		public string CurrentAnimation { get; private set; }

		public string PreviousAnimation { get; private set; }

		public FAnimationClips(Animator animator)
		{
		}

		public void Add(string clipName, bool exactClipName = false)
		{
		}

		public void AddClip(string clipName, bool exactClipName = false)
		{
		}

		public void AddClip(Animator animator, string clipName, bool exactClipName = false)
		{
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
