using Codebase.Core.Flows;
using UnityEngine;

namespace Codebase.Quests.Flows
{
	public struct QuestMobSpawnedFlow : IFlow
	{
		public Transform TargetTransform;

		public ParticleSystem SpawnVfx;
	}
}
