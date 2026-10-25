using Codebase.Enemy;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct BossRaidComponent : IComponent
	{
		public string BossKey;

		public string ArenaKey;

		public int Floor;

		public EnemyUnit BossUnit;

		public Vector3 ReturnPosition;

		public float RemainingTime;

		public float DelayToMakeSureBossIsDead;

		public bool FinishPending;

		public float FinishDelay;

		public bool FinishFightResult;

		public bool FinishFlowStarted;

		public int DeadAlliesCount;
	}
}
