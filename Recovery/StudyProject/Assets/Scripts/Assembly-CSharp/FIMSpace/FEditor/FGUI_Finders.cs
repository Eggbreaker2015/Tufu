using UnityEngine;

namespace FIMSpace.FEditor
{
	public static class FGUI_Finders
	{
		public static Component FoundAnimator;

		private static bool checkForAnim;

		private static int clicks;

		public static void ResetFinders(bool resetClicks = true)
		{
		}

		public static bool CheckForAnimator(GameObject root, bool needAnimatorBox = true, bool drawInactiveWarning = true, int clicksTohide = 1)
		{
			return false;
		}

		public static Component SearchForParentWithAnimator(GameObject root)
		{
			return null;
		}

		public static SkinnedMeshRenderer GetBoneSearchArray(Transform root)
		{
			return null;
		}

		public static bool IsChildOf(Transform child, Transform rootParent)
		{
			return false;
		}

		public static Transform GetLastChild(Transform rootParent)
		{
			return null;
		}

		public static bool? IsRightOrLeft(string name, bool includeNotSure = false)
		{
			return null;
		}

		public static bool? IsRightOrLeft(Transform child, Transform itsRoot)
		{
			return null;
		}

		public static bool HaveKey(string text, string[] keys)
		{
			return false;
		}
	}
}
