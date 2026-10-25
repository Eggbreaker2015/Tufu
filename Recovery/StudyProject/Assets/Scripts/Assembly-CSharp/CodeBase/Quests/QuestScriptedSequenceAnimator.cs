using UnityEngine;
using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Analytics;

namespace Codebase.Quests
{
	public static class QuestScriptedSequenceAnimator
	{
		public const float StoneTransitionStartProgress = 0.3f;
		public const float StoneTransitionEndProgress = 1f;
		public const float SpeedStartValue = 1f;
		public const float SpeedEndValue = 0f;
		private static readonly int StoneTransitionProgressId = Shader.PropertyToID("_Progress");
		public static void SetStoneTransitionProgress(Renderer renderer, float progress)
		{ // Original ELF 0x142c56c; candidate only.
			var properties = new MaterialPropertyBlock();
			renderer.GetPropertyBlock(properties);
			properties.SetFloat(StoneTransitionProgressId, progress);
			renderer.SetPropertyBlock(properties);
		}

		public static void ApplyStonedState(GameObject[] scriptedCharacters, Renderer[] stoneTransitionRenderers, Animator stoneTransitionAnimator)
		{ // Original ELF 0x142c634; candidate only.
			SetBool(scriptedCharacters, QuestScriptedSequenceAnimatorParams.IsStoned, true);
			foreach (var renderer in stoneTransitionRenderers)
				SetStoneTransitionProgress(renderer, 1f);
			SetFloat(stoneTransitionAnimator, QuestScriptedSequenceAnimatorParams.Speed, 0f);
		}

		public static Animator Get(GameObject root)
		{ // Original ELF 0x142c8d8; candidate only.
			if (root == null)
				return null;
			foreach (var animator in root.GetComponentsInChildren<Animator>(true))
				if (animator.runtimeAnimatorController != null)
					return animator;
			return root.GetComponentInChildren<Animator>(true);
		}

		public static void SetFloat(Animator animator, int parameter, float value)
		{ // Original ELF 0x142c830; candidate only.
			if (animator != null)
				animator.SetFloat(parameter, value);
		}

		public static void SetBool(GameObject[] roots, int parameter, bool value)
		{ // Original ELF 0x142c778; candidate only.
			foreach (var root in roots)
				SetBool(root, parameter, value);
		}

		public static void SetBool(GameObject root, int parameter, bool value)
		{ // Original ELF 0x142ca24; candidate only.
			var animator = Get(root);
			if (animator != null)
				animator.SetBool(parameter, value);
		}
	}
}
