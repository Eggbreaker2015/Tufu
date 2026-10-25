using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct MeteorComponent : IComponent
	{
		public float Delay;

		public float Timer;

		public float Damage;

		public float Radius;

		public Vector3 TargetPosition;

		public GameObject Visual;

		public float SpawnHeight;
	}
}
