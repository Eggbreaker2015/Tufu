using System.Collections.Generic;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.BossTower.Systems
{
	public class BossHealthBarSystem : SystemBase
	{
		private struct TrackedEntry
		{
			public float HealthFraction;
			public double CurrentHealth;
			public double MaxHealth;
			public float DeathTimer;
			public string OriginalUnitGuid;
		}

		private const float DEATH_PAUSE_DURATION = 0.5f;
		private const float DEATH_FADE_DURATION = 0.8f;
		private const float DEATH_DISPLAY_DURATION = 1.3f;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<BossTowerBossTag> bossTagStash;
		[Inject]
		private Stash<GhostOriginComponent> ghostOriginStash;
		private Filter aliveFilter;
		private Filter deadFilter;
		private readonly Dictionary<int, TrackedEntry> tracked;
		private readonly Dictionary<int, string> guidById;
		private readonly HashSet<string> ghostParentGuids;
		private readonly HashSet<int> completedFades;
		private readonly List<int> trackedKeys;
		private readonly HashSet<int> seenIds;
		private readonly List<int> toRemove;
		private readonly List<BossHealthBarData> data;
		public IReadOnlyList<BossHealthBarData> Data
		{
			get
			{
				return data;
			}
		}

		public override void OnAwake()
		{
			aliveFilter = Filter.With<BossArenaUnitTag>().With<UnitComponent>().Without<BossMinionUnitMarker>().Without<UnitDeadMarker>().Build();
			deadFilter = Filter.With<BossArenaUnitTag>().With<UnitComponent>().Without<BossMinionUnitMarker>().With<UnitDeadMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			seenIds.Clear();
			ghostParentGuids.Clear();
			foreach (Entity entity in aliveFilter)
			{
				int id = entity.Id;
				seenIds.Add(id);
				completedFades.Remove(id);
				guidById[id] = bossTagStash.Get(entity).UnitGuid;
				var unit = unitStash.Get(entity).Core;
				double maximum = unit.GetStatValue(Codebase.Unit.StatType.Health, true), current = unit.CurrentHealth;
				float fraction = 0f;
				if (maximum > 0.0)
				{
					float ratio = (float)(current / maximum);
					if (ratio >= 0f)
						fraction = ratio <= 1f ? ratio : 1f;
				}

				string original = ghostOriginStash.Has(entity) ? ghostOriginStash.Get(entity).OriginalUnitGuid : null;
				if (original != null)
					ghostParentGuids.Add(original);
				tracked[id] = new TrackedEntry
				{
					HealthFraction = fraction,
					CurrentHealth = current,
					MaxHealth = maximum,
					DeathTimer = -1f,
					OriginalUnitGuid = original
				};
			}

			foreach (Entity entity in deadFilter)
			{
				int id = entity.Id;
				seenIds.Add(id);
				if (ghostParentGuids.Contains(bossTagStash.Get(entity).UnitGuid))
				{
					tracked.Remove(id);
					guidById.Remove(id);
					continue;
				}

				if (completedFades.Contains(id))
					continue;
				if (!tracked.TryGetValue(id, out TrackedEntry entry) || entry.DeathTimer < 0f)
					tracked[id] = new TrackedEntry
					{
						HealthFraction = 0f,
						CurrentHealth = 0.0,
						MaxHealth = unitStash.Get(entity).Core.GetStatValue(Codebase.Unit.StatType.Health, true),
						DeathTimer = 1.3f
					};
			}

			trackedKeys.Clear();
			trackedKeys.AddRange(tracked.Keys);
			toRemove.Clear();
			foreach (int id in trackedKeys)
			{
				TrackedEntry entry = tracked[id];
				if (!seenIds.Contains(id) && entry.DeathTimer < 0f)
					entry.DeathTimer = 1.3f;
				if (entry.DeathTimer >= 0f)
				{
					if (entry.DeathTimer - deltaTime <= 0f)
						toRemove.Add(id);
					else
					{
						entry.DeathTimer -= deltaTime;
						tracked[id] = entry;
					}
				}
			}

			foreach (int id in toRemove)
			{
				tracked.Remove(id);
				guidById.Remove(id);
				completedFades.Add(id);
			}

			data.Clear();
			foreach (var pair in tracked)
			{
				TrackedEntry entry = pair.Value;
				float alpha = 1f;
				if (entry.DeathTimer >= 0f)
				{
					float ratio = entry.DeathTimer / 0.8f;
					alpha = ratio >= 0f ? (ratio <= 1f ? ratio : 1f) : 0f;
				}

				data.Add(new BossHealthBarData { EntityId = pair.Key, UnitGuid = guidById.TryGetValue(pair.Key, out string guid) ? guid : pair.Key.ToString(), OriginalUnitGuid = entry.OriginalUnitGuid, HealthFraction = entry.HealthFraction, Alpha = alpha, CurrentHealth = entry.CurrentHealth, MaxHealth = entry.MaxHealth });
			}
		}

		public BossHealthBarSystem()
		{
			tracked = new Dictionary<int, TrackedEntry>();
			guidById = new Dictionary<int, string>();
			ghostParentGuids = new HashSet<string>();
			completedFades = new HashSet<int>();
			trackedKeys = new List<int>();
			seenIds = new HashSet<int>();
			toRemove = new List<int>();
			data = new List<BossHealthBarData>();
		}
	}
}
