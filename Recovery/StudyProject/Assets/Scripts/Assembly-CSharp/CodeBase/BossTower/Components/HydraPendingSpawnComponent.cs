using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct HydraPendingSpawnComponent : IComponent
	{
		public HydraBossComponent HydraData;

		public string BossFightKey;

		public Vector3 StartPosition;

		public Vector3 TargetPosition;

		public EnemyUnit ChildPrefab;

		public EnemySpawnerConfig ChildConfig;

		public List<Stat> ChildStats;

		public int Index;

		public bool HasMark;

		public int MarkStacks;

		public float ProjectileDelay;
	}
}
