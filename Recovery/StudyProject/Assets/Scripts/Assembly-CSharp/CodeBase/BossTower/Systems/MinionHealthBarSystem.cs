using Codebase.Unit;
using System.Collections.Generic;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.BossTower.Systems
{
	public class MinionHealthBarSystem : SystemBase
	{
		private struct TrackedEntry
		{
			public float HealthFraction;

			public float DeathTimer;
		}

		private const float DEATH_PAUSE_DURATION = 0.5f;

		private const float DEATH_FADE_DURATION = 0.8f;

		private const float DEATH_DISPLAY_DURATION = 1.3f;

		[Inject]
		private Stash<BossArenaUnitTag> arenaTagStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		private Filter aliveFilter;

		private Filter deadFilter;

		private readonly Dictionary<string, TrackedEntry> tracked;

		private readonly HashSet<string> completedFades;

		private readonly List<string> trackedKeys;

		private readonly HashSet<string> seenGuids;

		private readonly List<string> toRemove;

		private readonly List<MinionHealthBarData> data;

		// Native ELF VA 0x156f7c4.
		public IReadOnlyList<MinionHealthBarData> Data => data;

        // Native ELF VA 0x15706c0.
        public MinionHealthBarSystem()
        {
            tracked = new Dictionary<string, TrackedEntry>();
            completedFades = new HashSet<string>();
            trackedKeys = new List<string>();
            seenGuids = new HashSet<string>();
            toRemove = new List<string>();
            data = new List<MinionHealthBarData>();
        }

		public override void OnAwake()
		{
			// Native ELF VA 0x156f7cc. The dead filter deliberately does not require UnitComponent.
			aliveFilter = Filter.With<BossArenaUnitHasHealthBarMarker>().With<BossArenaUnitTag>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			deadFilter = Filter.With<BossArenaUnitHasHealthBarMarker>().With<BossArenaUnitTag>().With<UnitDeadMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Native ELF VA 0x156fa08; predicate closure 0x157088c.
			seenGuids.Clear();
			foreach (Entity entity in aliveFilter)
			{
			    string guid = arenaTagStash.Get(entity).UnitGuid;
			    seenGuids.Add(guid);
			    UnitCore unit = unitStash.Get(entity).Core;
			    float maximum = (float)unit.GetStatValue(StatType.Health, true);
			    float fraction = 0f;
			    if (maximum > 0f)
			    {
			        // Original native converts each operand to float before division.
			        float ratio = (float)unit.CurrentHealth / maximum;
			        fraction = ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio);
			    }
			    tracked[guid] = new TrackedEntry { HealthFraction = fraction, DeathTimer = -1f };
			}
			foreach (Entity entity in deadFilter)
			{
			    string guid = arenaTagStash.Get(entity).UnitGuid;
			    seenGuids.Add(guid);
			    if (completedFades.Contains(guid)) continue;
			    if (!tracked.TryGetValue(guid, out TrackedEntry entry) || entry.DeathTimer < 0f)
			        tracked[guid] = new TrackedEntry { HealthFraction = 0f, DeathTimer = DEATH_DISPLAY_DURATION };
			}
			completedFades.RemoveWhere(guid => !seenGuids.Contains(guid));
			trackedKeys.Clear(); trackedKeys.AddRange(tracked.Keys); toRemove.Clear();
			foreach (string guid in trackedKeys)
			{
			    TrackedEntry entry = tracked[guid];
			    if (!seenGuids.Contains(guid) && entry.DeathTimer < 0f) entry.DeathTimer = DEATH_DISPLAY_DURATION;
			    if (entry.DeathTimer >= 0f)
			    {
			        if (entry.DeathTimer - deltaTime <= 0f) toRemove.Add(guid);
			        else { entry.DeathTimer -= deltaTime; tracked[guid] = entry; }
			    }
			}
			foreach (string guid in toRemove) { tracked.Remove(guid); completedFades.Add(guid); }
			data.Clear();
			foreach (var pair in tracked)
			{
			    TrackedEntry entry = pair.Value;
			    float alpha = 1f;
			    if (entry.DeathTimer >= 0f)
			    {
			        float ratio = entry.DeathTimer / DEATH_FADE_DURATION;
			        alpha = ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio);
			    }
			    data.Add(new MinionHealthBarData { UnitGuid = pair.Key, HealthFraction = entry.HealthFraction, Alpha = alpha });
			}
		}
	}
}
