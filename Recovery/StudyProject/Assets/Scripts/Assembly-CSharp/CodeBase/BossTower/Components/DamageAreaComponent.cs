using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct DamageAreaComponent : IComponent
	{
		public float Duration;

		public float DamagePerSecond;

		public float Radius;

		public float DamageTimer;

		public float DamageInterval;

		public Vector3 Position;

		public GameObject Visual;
	}
}
