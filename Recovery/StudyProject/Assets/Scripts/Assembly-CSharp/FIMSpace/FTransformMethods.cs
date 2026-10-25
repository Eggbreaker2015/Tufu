using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace
{
	public static class FTransformMethods
	{
		public static Transform FindChildByNameInDepth(string name, Transform transform, bool findInDeactivated = true, string[] additionalContains = null)
		{
			return null;
		}

		public static List<T> FindComponentsInAllChildren<T>(Transform transformToSearchIn, bool includeInactive = false, bool tryGetMultipleOutOfSingleObject = false) where T : Component
		{
			return null;
		}

		public static T FindComponentInAllChildren<T>(Transform transformToSearchIn) where T : Component
		{
			return null;
		}

		public static T FindComponentInAllParents<T>(Transform transformToSearchIn) where T : Component
		{
			return null;
		}

		public static void ChangeActiveChildrenInside(Transform parentOfThem, bool active)
		{
		}

		public static void ChangeActiveThroughParentTo(Transform start, Transform end, bool active, bool changeParentsChildrenActivation = false)
		{
		}

		public static Transform GetObjectByPath(Transform root, string path)
		{
			return null;
		}

		public static string CalculateTransformPath(Transform child, Transform root)
		{
			return null;
		}
	}
}
