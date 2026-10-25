using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.BossTower.Systems
{
	public class HydraMarkSyncSystem : SystemBase
	{
		[Inject]
		private Stash<MarkComponent> markStash;
		[Inject]
		private Stash<BossRaidComponent> raidStash;
		private Filter livingHydraFilter;
		private bool hasHydra;
		private int stacks;
		private int maxStacks;
		private Filter bossFightFilter;
		private int hydraCount;
		public override void OnAwake()
		{
			livingHydraFilter = Filter.With<HydraBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			bossFightFilter = Filter.With<BossRaidComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			hasHydra = false;
			hydraCount = 0;
			foreach (Entity entity in bossFightFilter)
				if (raidStash.Get(entity).BossKey == "hydra")
					hasHydra = true;
			bool found = false;
			MarkComponent best = default;
			foreach (Entity entity in livingHydraFilter)
			{
				hydraCount++;
				if (!markStash.Has(entity))
					continue;
				MarkComponent candidate = markStash.Get(entity);
				if (!found || candidate.Stacks > best.Stacks || (candidate.Stacks == best.Stacks && candidate.DecayTimer > best.DecayTimer))
				{
					best = candidate;
					found = true;
				}
			}

			stacks = found ? best.Stacks : 0;
			maxStacks = found ? best.MaxStacks : 0;
			if (found)
				foreach (Entity entity in livingHydraFilter)
					markStash.Set(entity, best);
		}

		public bool TryGetSharedMark(out int stacks, out int maxStacks, out int hydraCount)
		{
			stacks = this.stacks;
			maxStacks = this.maxStacks;
			hydraCount = this.hydraCount;
			return hasHydra;
		}
	}
}
