using Codebase.Core;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Rage;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using System.Linq;

namespace Codebase.Player.Systems
{
	public class RageExtraHealthSystem : SystemBase
	{
		private const string ModifierName = "graph_rage_extra_health";
		private Filter filter;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private RageModel rageModel;
		[Inject]
		private MetaStatsModel metaStatsModel;
		private double lastAppliedValue = -1.0;
		public override void OnAwake()
		{
			filter = Filter.With<UnitComponent>().With<PlayerTag>().Without<UnitDeadMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			ForceApply();
		}

		public void ForceApply()
		{
			if (playerModel.Unit == null)
				return;
			foreach (Entity entity in filter)
			{
				UnitCore unit = unitStash.Get(entity).Core;
				double value = GetPercentBonus();
				if (System.Math.Abs(value - lastAppliedValue) >= 0.0001)
				{
					ApplyModifier(unit, value);
					lastAppliedValue = value;
				}
			}
		}

		private double GetPercentBonus()
		{
			int level = rageModel.ActiveRageLevel;
			if (level <= 0)
				return 0.0;
			double value = metaStatsModel.Get("rage_extra_health").Value;
			return value > 0.0 ? value * level : 0.0;
		}

		private void ApplyModifier(UnitCore unit, double value)
		{
			Stat health = unit.Stats.FirstOrDefault(stat => stat.Type == StatType.Health);
			if (health == null)
				return;
			StatModifier old = health.Modifiers.FirstOrDefault(modifier => modifier.Name == ModifierName);
			if (old != null)
				health.RemoveModifier(old);
			if (value > 0.0)
				health.AddModifier(new StatModifier { Name = ModifierName, StatType = StatType.Health, ModifierType = ModifierType.PercentAdditive, Source = ModifierSource.GraphUpgrade, Value = (float)value });
		}
	}
}
