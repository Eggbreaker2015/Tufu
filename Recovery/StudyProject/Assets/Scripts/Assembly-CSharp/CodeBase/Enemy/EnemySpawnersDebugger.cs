using System.Collections.Generic;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Enemy
{
	public class EnemySpawnersDebugger : MonoBehaviour
	{
		[SerializeField]
		private bool considerNonRespawnable;

		[SerializeField]
		private bool considerStatic;

		[SerializeField]
		private List<StatType> statsCSVOrder;

		[SerializeField]
		private List<EnemySpawnerDebugInfo> info;

		[SerializeField]
		private List<DuplicateKeyInfo> duplicateKeys;

		private List<EnemySpawner> spawners;

		private List<EnemySpawner> FindAllSpawners()
		{
			return null;
		}

		public void UpdateGpms()
		{
		}

		public void FindDuplicateKeys()
		{
		}

		public void ExportToCsv()
		{
		}
	}
}
