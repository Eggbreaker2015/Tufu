using UnityEngine;

namespace FIMSpace
{
	public static class FEngineering
	{
		private static Plane axis2DProjection;

		private static PhysicsMaterial _slidingMat;

		private static PhysicsMaterial _frictMat;

		private static PhysicsMaterial2D _slidingMat2D;

		private static PhysicsMaterial2D _frictMat2D;

		public static PhysicsMaterial PMSliding => null;

		public static PhysicsMaterial PMFrict => null;

		public static PhysicsMaterial2D PMSliding2D => null;

		public static PhysicsMaterial2D PMFrict2D => null;

		public static bool VIsZero(this Vector3 vec)
		{
			return false;
		}

		public static bool VIsSame(this Vector3 vec1, Vector3 vec2)
		{
			return false;
		}

		public static Vector3 TransformVector(this Quaternion parentRot, Vector3 parentLossyScale, Vector3 childLocalPos)
		{
			return default;
		}

		public static Vector3 TransformInDirection(this Quaternion childRotation, Vector3 parentLossyScale, Vector3 childLocalPos)
		{
			return default;
		}

		public static Vector3 InverseTransformVector(this Quaternion tRotation, Vector3 tLossyScale, Vector3 worldPos)
		{
			return default;
		}

		public static Vector3 VAxis2DLimit(this Transform parent, Vector3 parentPos, Vector3 childPos, int axis = 3)
		{
			return default;
		}

		public static Quaternion QToLocal(this Quaternion parentRotation, Quaternion worldRotation)
		{
			return default;
		}

		public static Quaternion QToWorld(this Quaternion parentRotation, Quaternion localRotation)
		{
			return default;
		}

		public static Quaternion QRotateChild(this Quaternion offset, Quaternion parentRot, Quaternion childLocalRot)
		{
			return default;
		}

		public static Quaternion ClampRotation(this Vector3 current, Vector3 bounds)
		{
			return default;
		}

		public static Vector3 QToAngularVelocity(this Quaternion deltaRotation, bool fix = false)
		{
			return default;
		}

		public static Vector3 QToAngularVelocity(this Quaternion deltaRotation, float multiplyAngle)
		{
			return default;
		}

		public static Vector3 QToAngularVelocity(this Quaternion currentRotation, Quaternion targetRotation, bool fix = false)
		{
			return default;
		}

		public static Vector3 QToAngularVelocity(this Quaternion currentRotation, Quaternion targetRotation, float multiply)
		{
			return default;
		}

		public static bool QIsZero(this Quaternion rot)
		{
			return false;
		}

		public static bool QIsSame(this Quaternion rot1, Quaternion rot2)
		{
			return false;
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

		public static Quaternion SmoothDampRotation(this Quaternion current, Quaternion target, ref Quaternion velocityRef, float duration, float delta)
		{
			return default;
		}

		public static Quaternion SmoothDampRotation(this Quaternion current, Quaternion target, ref Quaternion velocityRef, float duration, float maxSpeed, float delta)
		{
			return default;
		}

		public static float PerlinNoise3D(float x, float y, float z)
		{
			return 0f;
		}

		public static float PerlinNoise3D(Vector3 pos)
		{
			return 0f;
		}

		public static bool SameDirection(this float a, float b)
		{
			return false;
		}

		public static float PointDisperse01(int index, int baseV = 2)
		{
			return 0f;
		}

		public static float PointDisperse(int index, int baseV = 2)
		{
			return 0f;
		}

		public static float GetScaler(this Transform transform)
		{
			return 0f;
		}

		public static Vector3 PosFromMatrix(this Matrix4x4 m)
		{
			return default;
		}

		public static Quaternion RotFromMatrix(this Matrix4x4 m)
		{
			return default;
		}

		public static Vector3 ScaleFromMatrix(this Matrix4x4 m)
		{
			return default;
		}

		public static Bounds TransformBounding(Bounds b, Transform by)
		{
			return default;
		}

		public static Bounds TransformBounding(Bounds b, Matrix4x4 mx)
		{
			return default;
		}

		public static Bounds RotateBoundsByMatrix(this Bounds b, Quaternion rotation)
		{
			return default;
		}

		public static Bounds RotateLocalBounds(this Bounds b, Quaternion rotation)
		{
			return default;
		}

		public static int[] GetLayermaskValues(int mask, int optionsCount)
		{
			return null;
		}

		public static LayerMask GetLayerMaskUsingPhysicsProjectSettingsMatrix(int maskForLayer)
		{
			return default;
		}

		public static float DistanceTo_2D(Vector3 aPos, Vector3 bPos)
		{
			return 0f;
		}

		public static float DistanceTo_2DSqrt(Vector3 aPos, Vector3 bPos)
		{
			return 0f;
		}

		public static Vector2 GetAngleDirection2D(float angle)
		{
			return default;
		}

		public static Vector3 GetAngleDirection(float angle)
		{
			return default;
		}

		public static Vector3 GetAngleDirectionXZ(float angle)
		{
			return default;
		}

		public static Vector3 GetAngleDirectionZX(float angle)
		{
			return default;
		}

		public static Vector3 GetAngleDirectionXY(float angle, float radOffset = 0f, float secAxisRadOffset = 0f)
		{
			return default;
		}

		public static Vector3 GetAngleDirectionYX(float angle, float firstAxisRadOffset = 0f, float secAxisRadOffset = 0f)
		{
			return default;
		}

		public static Vector3 GetAngleDirectionYZ(float angle)
		{
			return default;
		}

		public static Vector3 GetAngleDirectionZY(float angle)
		{
			return default;
		}

		public static Vector3 V2ToV3TopDown(Vector2 v)
		{
			return default;
		}

		public static Vector2 V3ToV2(Vector3 a)
		{
			return default;
		}

		public static Vector2 V3TopDownDiff(Vector3 target, Vector3 me)
		{
			return default;
		}

		public static float GetAngleDeg(Vector3 v)
		{
			return 0f;
		}

		public static float GetAngleDeg(Vector2 v)
		{
			return 0f;
		}

		public static float GetAngleDeg(float x, float z)
		{
			return 0f;
		}

		public static float GetAngleRad(float x, float z)
		{
			return 0f;
		}

		public static float Rnd(float val, int dec = 0)
		{
			return 0f;
		}

		internal static float ManhattanTopDown2D(Vector3 probePos, Vector3 worldPosition)
		{
			return 0f;
		}

		internal static bool IsInSqureBounds2D(Vector3 probePos, Vector3 boundsPos, float boundsRange)
		{
			return false;
		}

		internal static bool IsInSqureBounds2D(Vector3 boundsAPos, float boundsAHalfRange, Vector3 boundsBPos, float boundsBHRange)
		{
			return false;
		}

		internal static Vector3 GetDirectionTowards(Vector3 me, Vector3 target)
		{
			return default;
		}
	}
}
