using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Skills
{
	// Side and radius queries. Enemy side: EnemiesModel.Enemies (no static or far units); friendly side: the hero plus living pets.
	public static class SkillTargetQuery
	{
		private static readonly List<UnitCore> Candidates = new List<UnitCore>(64);

		private static readonly List<UnitCore> Scratch = new List<UnitCore>(64);

		private static readonly Stack<List<UnitCore>> ListPool = new Stack<List<UnitCore>>();

		private static Vector3 sortCenter;

		private static readonly Comparison<UnitCore> ByDistance = (a, b) => FlatSqrDistance(a.transform.position, sortCenter).CompareTo(FlatSqrDistance(b.transform.position, sortCenter));

		// Effects collect into rented lists so nested resolution never shares one.
		public static List<UnitCore> RentList()
		{
			return ListPool.Count > 0 ? ListPool.Pop() : new List<UnitCore>(16);
		}

		public static void ReturnList(List<UnitCore> list)
		{
			list.Clear();
			ListPool.Push(list);
		}

		public static bool IsHostile(UnitCore a, UnitCore b)
		{
			return (a is EnemyUnit) != (b is EnemyUnit);
		}

		public static bool IsAlive(UnitCore unit)
		{
			return unit != null && !unit.IsDead && unit.gameObject.activeInHierarchy;
		}

		public static bool IsBoss(UnitCore unit)
		{
			return unit is EnemyUnit enemy && enemy.Config != null && (enemy.Config.IsBossFightPart || enemy.Config.IsEliteBoss);
		}

		public static bool Matches(UnitCore caster, UnitCore unit, SkillTargetFilter filter)
		{
			if (unit == null || caster == null)
				return false;
			if ((filter & SkillTargetFilter.Alive) != 0 && !IsAlive(unit))
				return false;
			bool self = unit == caster;
			bool relation = self ? (filter & SkillTargetFilter.Self) != 0 :
				IsHostile(caster, unit) ? (filter & SkillTargetFilter.Enemy) != 0 : (filter & SkillTargetFilter.Ally) != 0;
			if (!relation)
				return false;
			const SkillTargetFilter kinds = SkillTargetFilter.Hero | SkillTargetFilter.Pet | SkillTargetFilter.Boss;
			if ((filter & kinds) == 0)
				return true;
			return (filter & SkillTargetFilter.Hero) != 0 && unit is PlayerUnit || (filter & SkillTargetFilter.Pet) != 0 && unit is PetUnit ||
				(filter & SkillTargetFilter.Boss) != 0 && IsBoss(unit);
		}

		// Every unit on both sides that could be targeted, the caster included.
		public static List<UnitCore> GetCandidates(UnitCore caster)
		{
			Candidates.Clear();
			if (GameContext.Has<EnemiesModel>())
				foreach (EnemyUnit enemy in GameContext.Get<EnemiesModel>().Enemies)
					if (enemy != null && !enemy.IsStatic && (!enemy.IsFar || enemy == caster))
						Candidates.Add(enemy);
			PlayerModel playerModel = GameContext.Has<PlayerModel>() ? GameContext.Get<PlayerModel>() : null;
			PlayerUnit player = playerModel != null ? playerModel.Unit : null;
			if (player != null && !player.IsDying)
				Candidates.Add(player);
			if (PetConfig.Enabled && GameContext.Has<PetsModel>())
				foreach (PetModel pet in GameContext.Get<PetsModel>().Pets)
					if (pet.IsAlive && pet.Unit != null)
						Candidates.Add(pet.Unit);
			return Candidates;
		}

		public static void GatherInRadius(UnitCore caster, Vector3 center, float radius, SkillTargetFilter filter, List<UnitCore> result)
		{
			float radiusSqr = radius * radius;
			foreach (UnitCore unit in GetCandidates(caster))
				if (FlatSqrDistance(unit.transform.position, center) <= radiusSqr && Matches(caster, unit, filter))
					result.Add(unit);
		}

		public static int CountInRadius(UnitCore caster, Vector3 center, float radius, SkillTargetFilter filter)
		{
			Scratch.Clear();
			GatherInRadius(caster, center, radius, filter, Scratch);
			int count = Scratch.Count;
			Scratch.Clear();
			return count;
		}

		public static UnitCore Nearest(UnitCore caster, SkillTargetFilter filter, float maxRange)
		{
			Vector3 origin = caster.transform.position;
			float best = maxRange > 0f ? maxRange * maxRange : float.MaxValue;
			UnitCore result = null;
			foreach (UnitCore unit in GetCandidates(caster))
			{
				float distance = FlatSqrDistance(unit.transform.position, origin);
				if (distance <= best && Matches(caster, unit, filter))
				{
					best = distance;
					result = unit;
				}
			}
			return result;
		}

		public static UnitCore LowestHealthFraction(UnitCore caster, SkillTargetFilter filter, float maxRange)
		{
			Vector3 origin = caster.transform.position;
			float rangeSqr = maxRange > 0f ? maxRange * maxRange : float.MaxValue;
			double best = double.MaxValue;
			UnitCore result = null;
			foreach (UnitCore unit in GetCandidates(caster))
			{
				if (FlatSqrDistance(unit.transform.position, origin) > rangeSqr || !Matches(caster, unit, filter))
					continue;
				double fraction = GetHealthFraction(unit);
				if (fraction < best)
				{
					best = fraction;
					result = unit;
				}
			}
			return result;
		}

		// The hostile unit with the most hostiles (itself included) within radius of it.
		public static UnitCore Densest(UnitCore caster, float radius, float maxRange)
		{
			const SkillTargetFilter filter = SkillTargetFilter.Enemy | SkillTargetFilter.Alive;
			Vector3 origin = caster.transform.position;
			float rangeSqr = maxRange > 0f ? maxRange * maxRange : float.MaxValue;
			Scratch.Clear();
			foreach (UnitCore unit in GetCandidates(caster))
				if (FlatSqrDistance(unit.transform.position, origin) <= rangeSqr && Matches(caster, unit, filter))
					Scratch.Add(unit);
			float radiusSqr = radius * radius;
			int best = 0;
			UnitCore result = null;
			foreach (UnitCore unit in Scratch)
			{
				int count = 0;
				Vector3 position = unit.transform.position;
				foreach (UnitCore other in Scratch)
					if (FlatSqrDistance(other.transform.position, position) <= radiusSqr)
						count++;
				if (count > best)
				{
					best = count;
					result = unit;
				}
			}
			Scratch.Clear();
			return result;
		}

		public static double GetHealthFraction(UnitCore unit)
		{
			if (unit == null || !unit.HasStat(StatType.Health))
				return 1.0;
			double max = unit.GetStatValue(StatType.Health);
			return max > 0.0 ? unit.CurrentHealth / max : 1.0;
		}

		public static void SortByDistance(List<UnitCore> units, Vector3 center)
		{
			sortCenter = center;
			units.Sort(ByDistance);
		}

		public static float FlatSqrDistance(Vector3 a, Vector3 b)
		{
			float x = a.x - b.x;
			float z = a.z - b.z;
			return x * x + z * z;
		}

		public static float FlatDistance(UnitCore a, UnitCore b)
		{
			return Mathf.Sqrt(FlatSqrDistance(a.transform.position, b.transform.position));
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			Candidates.Clear();
			Scratch.Clear();
			ListPool.Clear();
		}
	}
}
