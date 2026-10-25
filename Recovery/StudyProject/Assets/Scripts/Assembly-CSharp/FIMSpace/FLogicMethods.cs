using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace
{
	public static class FLogicMethods
	{
		public static float Lerp(this float from, float to, float value)
		{
			return 0f;
		}

		public static float InverseLerp(float from, float to, float value)
		{
			return 0f;
		}

		public static float InverseLerpUnclamped(float xx, float yy, float value)
		{
			return 0f;
		}

		public static float FLerp(float a, float b, float t, float factor = 0.01f)
		{
			return 0f;
		}

		public static int IntLerp(int a, int b, float t)
		{
			return 0;
		}

		public static void IntLerp(ref int source, int a, int b, float t)
		{
		}

		public static void IntLerp(ref int source, int b, float t)
		{
		}

		public static float FAbs(this float value)
		{
			return 0f;
		}

		public static float HyperCurve(this float value)
		{
			return 0f;
		}

		public static float TopDownDistanceManhattan(this Vector3 a, Vector3 b)
		{
			return 0f;
		}

		public static float TopDownDistance(this Vector3 a, Vector3 b)
		{
			return 0f;
		}

		public static float DistanceManhattan(this Vector3 a, Vector3 b)
		{
			return 0f;
		}

		public static float WrapAngle(float angle)
		{
			return 0f;
		}

		public static Vector3 WrapVector(Vector3 angles)
		{
			return default;
		}

		public static float UnwrapAngle(float angle)
		{
			return 0f;
		}

		public static Vector3 UnwrapVector(Vector3 angles)
		{
			return default;
		}

		public static bool IsAlmostEqual(float val, float to, int afterComma = 2, float addRange = 0f)
		{
			return false;
		}

		public static Quaternion TopDownAngle(Vector3 from, Vector3 to)
		{
			return default;
		}

		public static Quaternion TopDownAnglePosition2D(Vector2 from, Vector2 to, float offset = 0f)
		{
			return default;
		}

		public static bool ContainsIndex<T>(this List<T> list, int i, bool falseIfNull = true) where T : class
		{
			return false;
		}

		public static bool ContainsIndex<T>(this List<T> list, int i) where T : struct
		{
			return false;
		}

		public static bool ContainsIndex<T>(this T[] list, int i, bool falseIfNull) where T : class
		{
			return false;
		}
	}
}
