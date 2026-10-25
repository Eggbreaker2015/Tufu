using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Scellecs.Morpeh;
using Codebase.Abilities.Effects;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.BossTower.Systems
{
	public class LichSkeletonDeathSystem : SystemBase
	{
		[Inject]
		private Stash<LichSkeletonTag> skeletonTagStash;

		[Inject]
		private Stash<LichBossComponent> lichStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<MarkComponent> markStash;

		[Inject]
		private PlayerModel playerModel;

		private Event<UnitDiedEvent> unitDiedEvent;

		private Filter lichFilter;

		public override void OnAwake()
		{
			unitDiedEvent = World.GetEvent<UnitDiedEvent>();
			lichFilter = Filter.With<LichBossComponent>().With<UnitComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Original ELF VA 0x156c9c4: includes dead Lich units until raid cleanup.
			foreach (Entity entity in lichFilter)
			{
				EnemyUnit unit = (EnemyUnit)unitStash.Get(entity).Core;
				int marks = markStash.Has(entity) ? markStash.Get(entity).Stacks : 0;
				unit.SpecialAdditionalCritChancePercent = marks * .01;
			}
			foreach (UnitDiedEvent death in unitDiedEvent.BatchedChanges)
				if (skeletonTagStash.Has(death.Entity))
				{
					Entity owner = skeletonTagStash.Get(death.Entity).OwnerLich;
					if (lichStash.Has(owner) && !(Random.value > lichStash.Get(owner).SkeletonDeathMarkChance))
						ApplyMarkEffect.ApplyMark(markStash, World, (EnemyUnit)unitStash.Get(owner).Core, playerModel, 1, true);
				}
		}
	}
}
