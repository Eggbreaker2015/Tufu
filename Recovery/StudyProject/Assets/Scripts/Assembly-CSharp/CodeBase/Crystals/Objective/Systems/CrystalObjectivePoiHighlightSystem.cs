using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Map.Components;
using Scellecs.Morpeh;

namespace Codebase.Crystals.Objective.Systems
{
	public class CrystalObjectivePoiHighlightSystem : SystemBase
	{
		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private CrystalConfig crystalConfig;

		[Inject]
		private Stash<EnemySpawnerComponent> spawnerStash;

		[Inject]
		private Stash<PointOfInterestComponent> poiStash;

		private Filter filter;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
