using Codebase.Enemy;
using Codebase.Player.Chain;
using UnityEngine;

namespace Codebase.Player
{
	internal class DevourAnimation
	{
		public EnemyUnit Enemy;

		public DevourSphere Sphere;

		public GameObject TentacleRoot;

		public Transform TentacleHead;

		public Transform Particles;

		public PlayerChainDeformer Deformer;

		public DevourState State;

		public float Elapsed;

		public Vector3 CatchStartSphereScale;

		public Vector3 CatchStartEnemyScale;

		public Material OriginalMaterial;
	}
}
