using UnityEngine;

namespace FIMSpace
{
	public static class FAnimatorMethods
	{
		public static void LerpFloatValue(this Animator animator, string name = "RunWalk", float value = 0f, float deltaSpeed = 8f)
		{
		}

		public static bool CheckAnimationEnd(this Animator animator, int layer = 0, bool reverse = false, bool checkAnimLoop = true)
		{
			return false;
		}

		public static void ResetLayersWeights(this Animator animator, float speed = 10f)
		{
		}

		public static void LerpLayerWeight(this Animator animator, int layer = 0, float newValue = 1f, float speed = 8f)
		{
		}

		public static bool StateExists(this Animator animator, string clipName, int layer = 0)
		{
			return false;
		}
	}
}
