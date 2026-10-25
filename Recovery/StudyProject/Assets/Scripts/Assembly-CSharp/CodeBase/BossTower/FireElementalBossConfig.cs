using System;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class FireElementalBossConfig : BossBehaviorConfig
	{
		public float AreaInterval;

		public float AreaDuration;

		public float AreaDamage;

		public float AreaRadius;

		public float AreaDamageInterval;

		public float MeteorInterval;

		public float MeteorDelay;

		public float MeteorDamage;

		public float MeteorSpawnRadius;

		public float MeteorSpawnHeight;

		public float MeteorRadius;

		public GameObject AreaPrefab;

		public GameObject MeteorPrefab;
	}
}
