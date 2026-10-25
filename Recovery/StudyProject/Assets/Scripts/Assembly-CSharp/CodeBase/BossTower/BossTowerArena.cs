using UnityEngine;

namespace Codebase.BossTower
{
	public class BossTowerArena : MonoBehaviour
	{
		[SerializeField]
		private Transform bossSpawnPoint;

		[SerializeField]
		private Transform playerSpawnPoint;

		[SerializeField]
		private Transform[] spiritSpawnPoints;

		[SerializeField]
		private Bounds arenaBounds;

		public Transform BossSpawnPoint => this.bossSpawnPoint;

		public Transform PlayerSpawnPoint => this.playerSpawnPoint;

		public Transform[] SpiritSpawnPoints => this.spiritSpawnPoints;

		public Bounds ArenaBounds => default;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<BossTowerArena>(this);
        }

		public Vector3 GetRandomPointInArena()
		{
			return default;
		}

		public Vector3 GetHalfArenaCenter(bool leftHalf)
		{
			return default;
		}

		public Vector3[] GetArenaCorners()
		{
			return null;
		}

		public Vector3 GetRandomCorner()
		{
			return default;
		}

		private void OnDrawGizmosSelected()
		{
		}
	}
}
