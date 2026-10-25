using System.Collections.Generic;
using System.Linq;

namespace Codebase.Enemy
{
	public class EnemiesModel
	{
		public List<EnemyUnit> Enemies { get; private set; } = new List<EnemyUnit>();

		public List<EnemyUnit> NonStaticEnemies { get; private set; } = new List<EnemyUnit>();

		public List<EnemySpawner> Spawners { get; private set; } = new List<EnemySpawner>();

		public List<EnemySpawner> PendingBootstrap { get; private set; } = new List<EnemySpawner>();

		public List<EnemySpawner> PendingEventMobBootstrap { get; private set; } = new List<EnemySpawner>();

		public IEnumerable<EnemySpawner> EventSpawners => Spawners.Where(spawner => spawner.Config.IsEvent);

		public void AddEnemy(EnemyUnit unit)
		{
			// Native ELF VA 0x14feaac; no duplicate suppression.
			Enemies.Add(unit);
			if (!unit.IsStatic) NonStaticEnemies.Add(unit);
		}

		public void RemoveEnemy(EnemyUnit unit)
		{
			// Native ELF VA 0x14feb38.
			Enemies.Remove(unit);
			if (!unit.IsStatic) NonStaticEnemies.Remove(unit);
		}
	}
}
