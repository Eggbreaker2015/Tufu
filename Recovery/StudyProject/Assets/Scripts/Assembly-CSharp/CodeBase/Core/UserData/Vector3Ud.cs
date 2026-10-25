using MessagePack;
using UnityEngine;

namespace Codebase.Core.UserData
{
	[MessagePackObject(false)]
	public struct Vector3Ud
	{
		[Key(0)]
		public float X;

		[Key(1)]
		public float Y;

		[Key(2)]
		public float Z;

		public Vector3Ud(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public Vector3Ud(Vector3 v)
		{
			X = v.x;
			Y = v.y;
			Z = v.z;
		}

		public Vector3 ToVector3()
		{
			return new Vector3(X, Y, Z);
		}

		public static implicit operator Vector3Ud(Vector3 v)
		{
			return new Vector3Ud(v);
		}

		public static implicit operator Vector3(Vector3Ud v)
		{
			return v.ToVector3();
		}
	}
}
