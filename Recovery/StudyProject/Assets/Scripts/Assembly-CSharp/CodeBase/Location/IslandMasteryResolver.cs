using Codebase.Crystals.Objective;
using System.Collections.Generic;
using Codebase.Enemy;

namespace Codebase.Location
{
	public class IslandMasteryResolver
	{
		private readonly EnemiesModel enemiesModel;

		private readonly Dictionary<int, List<EnemySpawner>> eligibleSpawnersByLocation;

		private readonly List<int> trackedLocationNumbers;

		private int indexedSpawnerCount;

		public IslandMasteryResolver(EnemiesModel enemiesModel)
		{
			eligibleSpawnersByLocation = new Dictionary<int, List<EnemySpawner>>();
			trackedLocationNumbers = new List<int>();
			indexedSpawnerCount = -1;
			this.enemiesModel = enemiesModel;
		}

		public IReadOnlyList<int> GetTrackedLocationNumbers()
		{
			EnsureSpawnerIndex();
			return trackedLocationNumbers;
		}

		public bool IsFullyCleared(int locationNumber)
		{
			EnsureSpawnerIndex();
			if (!eligibleSpawnersByLocation.TryGetValue(locationNumber, out List<EnemySpawner> spawners)) return false;
			foreach (EnemySpawner spawner in spawners)
			    if (!IsSpawnerCleared(spawner)) return false;
			return true;
		}

		public bool IsEligibleSpawner(EnemySpawner spawner)
		{
			var config = spawner.Config;
			if (config.IsEvent || config.IsVip || config.IsSpecial || config.IsDummy || config.IsBossFightPart || config.DisableAfterFirstClear) return false;
			if (config.Prefab is CrystalUnit || config.Prefab.TryGetComponent<CreeperMobProvider>(out _)) return false;
			return spawner.ChargesTotal > 0;
		}

		public bool IsSpawnerCleared(EnemySpawner spawner)
		{
			if (!spawner.IsBootstrapComplete || spawner.AnyoneAlive) return false;
			if (spawner.Config.DisableAfterFirstClear) return spawner.HasBeenClearedOnce && !spawner.IsEnabled;
			return spawner.ChargesTotal > 0 && spawner.ChargesLeft == 0;
		}

		private void EnsureSpawnerIndex()
		{
			List<EnemySpawner> spawners = enemiesModel.Spawners;
			if (indexedSpawnerCount == spawners.Count) return;
			eligibleSpawnersByLocation.Clear();
			trackedLocationNumbers.Clear();
			foreach (EnemySpawner spawner in spawners)
			{
			    if (!IsEligibleSpawner(spawner)) continue;
			    int location = spawner.Config.LocationNumber;
			    if (!eligibleSpawnersByLocation.TryGetValue(location, out List<EnemySpawner> list))
			    {
			        list = new List<EnemySpawner>();
			        eligibleSpawnersByLocation.Add(location, list);
			        trackedLocationNumbers.Add(location);
			    }
			    list.Add(spawner);
			}
			trackedLocationNumbers.Sort();
			indexedSpawnerCount = spawners.Count;
		}
	}
}
