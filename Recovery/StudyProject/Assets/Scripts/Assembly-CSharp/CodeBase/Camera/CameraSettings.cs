using System;

namespace Codebase.Camera
{
	[Serializable]
	public struct CameraSettings
	{
		public float Distance;

		public float AngleOfAttack;

		public float Inertia;

		public float Yaw;

		public float HeightSmoothTime;
	}
}
