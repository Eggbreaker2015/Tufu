using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Rpg
{
	public static class HeroAi
	{
		public static bool MoveTo(NavMeshAgent agent, Vector3 destination)
		{
			if (FlatSqrDistance(agent.transform.position, destination) <= 0.3f * 0.3f)
			{
				if (agent.hasPath) agent.ResetPath();
				return false;
			}
			if (!agent.hasPath && !agent.pathPending || FlatSqrDistance(agent.destination, destination) > 0.5f * 0.5f)
				agent.SetDestination(destination);
			return true;
		}

		public static bool Chase(UnitCore unit, NavMeshAgent agent, EnemyUnit target, float deltaTime)
		{
			Vector3 position = unit.transform.position;
			Vector3 targetPosition = target.transform.position;
			float range = (float)unit.GetStatValue(StatType.AttackRange);
			if (FlatSqrDistance(position, targetPosition) > range * range) return MoveTo(agent, targetPosition);
			if (agent.hasPath) agent.ResetPath();
			Vector3 direction = targetPosition - position;
			direction.y = 0f;
			if (direction.sqrMagnitude > 0.0001f)
				unit.transform.rotation = Quaternion.RotateTowards(unit.transform.rotation, Quaternion.LookRotation(direction), 720f * deltaTime);
			return false;
		}

		public static float FlatSqrDistance(Vector3 a, Vector3 b)
		{
			float x = a.x - b.x;
			float z = a.z - b.z;
			return x * x + z * z;
		}

		public static EnemyUnit SelectFollowerTarget(HeroUnit self, HeroUnit leader, float engageSqr, EnemyUnit current)
		{
			if (IsValid(leader.CurrentTarget)) return leader.CurrentTarget;
			if (IsValid(current) && current.AggroTarget == self) return current;
			EnemyUnit aggressor = FindAggressor(self);
			if (aggressor != null) return aggressor;
			return IsValid(current) && FlatSqrDistance(leader.transform.position, current.transform.position) <= engageSqr ? current : FindNearest(leader.transform.position, engageSqr);
		}

		public static EnemyUnit FindAggressor(UnitCore unit)
		{
			EnemyUnit best = null;
			float bestSqr = float.MaxValue;
			foreach (EnemyUnit enemy in GameContext.Get<EnemiesModel>().Enemies)
			{
				if (!IsValid(enemy) || enemy.AggroTarget != unit) continue;
				float sqr = FlatSqrDistance(unit.transform.position, enemy.transform.position);
				if (sqr < bestSqr) { bestSqr = sqr; best = enemy; }
			}
			return best;
		}

		public static EnemyUnit FindNearest(Vector3 center, float maxSqr)
		{
			EnemyUnit best = null;
			float bestSqr = maxSqr;
			foreach (EnemyUnit enemy in GameContext.Get<EnemiesModel>().Enemies)
			{
				if (!IsValid(enemy)) continue;
				float sqr = FlatSqrDistance(center, enemy.transform.position);
				if (sqr <= bestSqr) { bestSqr = sqr; best = enemy; }
			}
			return best;
		}

		private static bool IsValid(EnemyUnit enemy) => enemy != null && !enemy.IsDead && !enemy.IsFar;
	}
}
