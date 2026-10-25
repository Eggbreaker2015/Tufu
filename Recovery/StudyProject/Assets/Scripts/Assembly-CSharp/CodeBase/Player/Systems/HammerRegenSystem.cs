using Codebase.Core;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Player.Systems
{
	public class HammerRegenSystem : SystemBase
	{
		private const string ModifierName = "graph_hammer_regen";

		private Filter filter;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private MetaStatsModel metaStatsModel;

		private double lastAppliedValue;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private double GetFlatBonus(double maxHealth)
		{
			return 0.0;
		}

		private void ApplyModifier(UnitCore unit, double value)
		{
		}
	}
}
