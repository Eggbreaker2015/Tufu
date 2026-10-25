using Codebase.BossTower.Components;
using Scellecs.Morpeh.Providers;
using UnityEngine;

namespace Codebase.BossTower
{
	public class BossArenaProvider : MonoProvider<BossArenaComponent>
	{
		[SerializeField]
		private string arenaKey;
		[SerializeField]
		private Transform bossSpawnPoint;
		[SerializeField]
		private Transform playerSpawnPoint;
		[SerializeField]
		private Transform[] spiritSpawnPoints;
		[SerializeField]
		private Bounds arenaBounds;
		private void Start()
		{
			ref BossArenaComponent arena = ref GetData();
			arena.ArenaKey = arenaKey;
			arena.BossSpawnPoint = bossSpawnPoint;
			arena.PlayerSpawnPoint = playerSpawnPoint;
			arena.SpiritSpawnPoints = spiritSpawnPoints;
			arena.ArenaBounds = arenaBounds;
		}

		private void OnDrawGizmosSelected()
		{
		}
	}
}
