using System;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	[Serializable]
	public struct VampireSpawnPointEntry
	{
		public Transform Point;

		public GameObject Vfx;

		public RotationActivationEffect RotationEffect;
	}
}
