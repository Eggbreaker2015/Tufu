using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
	public class EnemySoulSystem : SystemBase
	{
		private Filter filter;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private AmuletUpgradesModel amuletUpgradesModel;

		[Inject]
		private AmuletModel amuletModel;

		private float timer;

		private const float CheckInterval = 1f;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
