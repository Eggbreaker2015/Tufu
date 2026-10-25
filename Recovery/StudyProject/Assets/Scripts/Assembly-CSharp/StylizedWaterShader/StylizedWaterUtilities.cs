using UnityEngine;

namespace StylizedWaterShader
{
	public static class StylizedWaterUtilities
	{
		public static class CameraUtils
		{
			public static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign, float clipPlaneOffset)
			{
				return default;
			}

			public static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
			{
			}

			public static void CopyCameraSettings(Camera src, Camera dest)
			{
			}
		}

		public static bool DEBUG;

		public static string[] ComposeDropdown(Texture2D[] resource, string replaceFilter)
		{
			return null;
		}

		public static bool IsApproximatelyEqual(float a, float b)
		{
			return false;
		}

		public static bool HasVertexColors(Mesh mesh)
		{
			return false;
		}
	}
}
