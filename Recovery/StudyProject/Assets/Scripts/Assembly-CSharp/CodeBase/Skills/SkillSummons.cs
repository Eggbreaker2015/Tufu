using System.Collections.Generic;
using Codebase.BossTower;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Skills.Effects;
using Codebase.Unit;
using Codebase.Unit.Components;
using DG.Tweening;
using Scellecs.Morpeh;
using UniMob;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Skills
{
	public class SkillSummons
	{
		private struct Record
		{
			public EnemyUnit Unit;

			public UnitCore Owner;

			public string Key;

			public float ExpireTime;

			public string VfxKey;
		}

		private readonly List<Record> records = new List<Record>();

		public bool IsSummon(UnitCore unit)
		{
			if (!SkillTargetQuery.IsAlive(unit))
				return false;
			for (int i = 0; i < records.Count; i++)
				if (records[i].Unit == unit)
					return true;
			return false;
		}

		public int CountAlive(UnitCore owner, string key)
		{
			int count = 0;
			BossMinionService minions = GameContext.Has<BossMinionService>() ? GameContext.Get<BossMinionService>() : null;
			for (int i = 0; i < records.Count; i++)
			{
				Record record = records[i];
				if (record.Owner == owner && record.Key == key && SkillTargetQuery.IsAlive(record.Unit) && minions != null && minions.IsSummon(record.Unit))
					count++;
			}
			return count;
		}

		public void Summon(SkillService service, in SkillCastContext context, SummonEffect def)
		{
			string key = def.SummonKey;
			if (!(context.Caster is EnemyUnit caster) || caster.Config == null)
			{
				SkillLibrary.WarnOnce("summon-caster:" + key, "[Skills] Summon '" + key + "' requires an enemy caster with config");
				return;
			}
			if (IsSummon(caster))
			{
				SkillLibrary.WarnOnce("summon-recursion:" + key, "[Skills] Summon '" + key + "' skipped: no recursion");
				return;
			}
			World world = World.Default;
			if (caster.EntityProvider != null && !world.IsDisposed(caster.EntityProvider.Entity) && world.GetStash<BossArenaUnitTag>().Has(caster.EntityProvider.Entity))
			{
				SkillLibrary.WarnOnce("summon-arena:" + key, "[Skills] Summon '" + key + "' skipped: boss tower arena units do not summon");
				return;
			}
			SkillSummonEntry entry = SkillLibrary.FindSummon(key);
			EnemyUnit prefab = entry != null && entry.Prefab != null ? entry.Prefab.GetComponent<EnemyUnit>() : null;
			if (prefab == null)
			{
				SkillLibrary.WarnOnce("summon-prefab:" + key, "[Skills] Summon '" + key + "' requires a prefab with EnemyUnit");
				return;
			}
			if (!GameContext.Has<BossMinionService>())
				return;
			BossMinionService minionService = GameContext.Get<BossMinionService>();
			int count = Mathf.Max(1, def.Count);
			if (def.MaxAlive > 0)
				count = Mathf.Min(count, def.MaxAlive - CountAlive(caster, key));
			if (count <= 0)
				return;
			var stats = new List<Stat>();
			List<Stat> sourceStats = caster.Stats != null && caster.Stats.Count > 0 ? caster.Stats : caster.Config.Stats;
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) sourceStats = caster.Config.Stats;
			if (sourceStats != null)
				foreach (Stat stat in sourceStats)
				{
					double value = stat.BaseValue;
					// A zero health scale would spawn a corpse.
					if (stat.Type == StatType.Health)
						value = System.Math.Max(1.0, value * def.HealthScale);
					else if (stat.Type == StatType.Damage)
						value *= Mathf.Max(0f, def.DamageScale);
					stats.Add(new Stat(Lifetime.Eternal, value, stat.Type));
				}
			EnemySpawnerConfig source = caster.Config;
			var config = new EnemySpawnerConfig
			{
				Key = source.Key,
				Prefab = prefab,
				Stats = stats,
				WeaponType = source.WeaponType,
				ArmorConfig = source.ArmorConfig,
				AggroRange = source.AggroRange,
				AggroDuration = Mathf.Max(source.AggroDuration, 5f),
				LocationNumber = source.LocationNumber,
				Difficulty = source.Difficulty,
				AverageFightTime = source.AverageFightTime,
				StatRewards = new List<StatModifier>()
			};
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) global::Codebase.Rpg.RpgEnemyLevel.CopyLevel(config, caster.Config);
			Quaternion rotation = Quaternion.LookRotation(SkillTargetQuery.GetFlatDirection(caster, null), Vector3.up);
			for (int i = 0; i < count; i++)
			{
				Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, def.Radius);
				Vector3 candidate = caster.transform.position + new Vector3(offset.x, 0f, offset.y);
				if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas) && !NavMesh.SamplePosition(caster.transform.position, out hit, 2f, NavMesh.AllAreas))
					continue;
				Vector3 position = hit.position;
				EnemyUnit unit = minionService.SpawnSummon(prefab, position, rotation, config);
				// Initialize re-enters InstallForEnemy; records must never be enumerated across a spawn.
				for (int j = records.Count - 1; j >= 0; j--)
					if (records[j].Unit == unit)
						records.RemoveAt(j);
				if (unit.Agent != null)
				{
					unit.Agent.enabled = true;
					if (unit.Agent.isOnNavMesh)
						unit.Agent.Warp(position);
					world.GetStash<NavMeshAgentActiveMarker>().Set(unit.EntityProvider.Entity);
				}
				unit.LastDestination = null;
				unit.AggroTimer = unit.AggroDuration;
				service.RemoveSummonSkills(unit);
				if (!string.IsNullOrEmpty(def.VfxKey))
					service.SpawnVfx(def.VfxKey, position, Quaternion.identity, 1f);
				unit.VisualRoot.DOScale(Vector3.one, 0.5f).From(Vector3.zero, true, false).SetEase(Ease.InOutQuad);
				records.Add(new Record { Unit = unit, Owner = caster, Key = key, ExpireTime = def.Lifetime > 0f ? Time.time + def.Lifetime : 0f, VfxKey = def.VfxKey });
			}
		}

		public void Tick(SkillService service)
		{
			BossMinionService minions = GameContext.Has<BossMinionService>() ? GameContext.Get<BossMinionService>() : null;
			for (int i = records.Count - 1; i >= 0; i--)
			{
				Record record = records[i];
				if (!SkillTargetQuery.IsAlive(record.Unit) || minions == null || !minions.IsSummon(record.Unit))
				{
					records.RemoveAt(i);
					continue;
				}
				if (!SkillTargetQuery.IsAlive(record.Owner) || record.ExpireTime > 0f && Time.time >= record.ExpireTime)
					Despawn(i, service, minions);
			}
		}

		public void DespawnOwnedBy(UnitCore owner)
		{
			SkillService service = GameContext.Has<SkillService>() ? GameContext.Get<SkillService>() : null;
			BossMinionService minions = GameContext.Has<BossMinionService>() ? GameContext.Get<BossMinionService>() : null;
			for (int i = records.Count - 1; i >= 0; i--)
				if (records[i].Owner == owner)
					Despawn(i, service, minions);
		}

		public void Clear()
		{
			SkillService service = GameContext.Has<SkillService>() ? GameContext.Get<SkillService>() : null;
			BossMinionService minions = GameContext.Has<BossMinionService>() ? GameContext.Get<BossMinionService>() : null;
			for (int i = records.Count - 1; i >= 0; i--)
				Despawn(i, service, minions);
		}

		private void Despawn(int index, SkillService service, BossMinionService minions)
		{
			Record record = records[index];
			records.RemoveAt(index);
			if (!SkillTargetQuery.IsAlive(record.Unit) || minions == null || !minions.IsSummon(record.Unit))
				return;
			if (service != null && !string.IsNullOrEmpty(record.VfxKey))
				service.SpawnVfx(record.VfxKey, record.Unit.transform.position, Quaternion.identity, 1f);
			record.Unit.VisualRoot.DOKill();
			minions.Despawn(record.Unit);
		}
	}
}
