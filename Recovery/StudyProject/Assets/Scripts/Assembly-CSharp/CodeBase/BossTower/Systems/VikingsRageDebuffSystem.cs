using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Meta;
using Codebase.Rage;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Codebase.Unit;
using Codebase.Enemy;
using Codebase.Player.Commands;
using DG.Tweening;
using UnityEngine;

namespace Codebase.BossTower.Systems
{
	public class VikingsRageDebuffSystem : SystemBase
	{
		private const string DebuffName = "VikingRageDebuff";
		private Filter aliveFilter;
		private Event<RageLevelUpEvent> rageLevelUpEvent;
		[Inject]
		private RageModel rageModel;
		[Inject]
		private MetaStatsModel metaStatsModel;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<ScaleComponent> scaleStash;
		[Inject]
		private Stash<BuffStunComponent> stunStash;
		[Inject]
		private Stash<VikingsBossComponent> vikingsStash;
		public override void OnAwake()
		{
			aliveFilter = Filter.With<VikingsBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			rageLevelUpEvent = World.GetEvent<RageLevelUpEvent>();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (RageLevelUpEvent data in rageLevelUpEvent.BatchedChanges)
			{
				ApplyRageDebuff(data.RageLevel);
				ApplyVikingsStun();
			}
		}

		public void OnVikingDied()
		{
			rageModel.DecreaseLevel(1);
		}

		private void ApplyVikingsStun()
		{
			float duration = (float)metaStatsModel.Get("boss_tower_vikings").Value;
			if (duration <= 0f)
				return;
			foreach (Entity entity in aliveFilter)
			{
				UnitCore unit = unitStash.Get(entity).Core;
				stunStash.Set(entity, new BuffStunComponent { Duration = duration });
				((EnemyUnit)unit).ApplyStunAnimation(true);
			}
		}

		private void ApplyRageDebuff(int rageLevel)
		{
			foreach (Entity entity in aliveFilter)
			{
				UnitCore unit = unitStash.Get(entity).Core;
				VikingsBossComponent data = vikingsStash.Get(entity);
				App.ExecuteCommand(new AddTemporaryModifierCommand { Unit = unit, Name = DebuffName, StatType = StatType.Damage, ModifierType = ModifierType.PreAdditivePostMultiplicative, Source = ModifierSource.AbilityTemporary, Value = -(data.DebuffPerRageLevel * rageLevel), Duration = data.DebuffDuration, RemoveDuplicates = true });
				if (!scaleStash.Has(entity))
					continue;
				ScaleComponent scale = scaleStash.Get(entity);
				float reduction = Mathf.Min(data.ScaleStep * rageLevel, .6f);
				App.ExecuteCommand(new AddTemporaryModifierCommand { Unit = unit, Name = DebuffName, StatType = StatType.VisualScaleMultiplier, ModifierType = ModifierType.Flat, Source = ModifierSource.AbilityTemporary, Value = -reduction, Duration = data.DebuffDuration, RemoveDuplicates = true });
				unit.transform.DOScale(scale.OriginalScale * (1f - reduction), .35f).SetEase((Ease)30);
			}
		}
	}
}
