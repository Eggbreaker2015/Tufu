using Codebase.Core;
using Codebase.UI.World;
using Scellecs.Morpeh;

namespace Codebase.Crystals.Objective.Systems
{
	public class CrystalObjectiveBannerSystem : SystemBase
	{
		[Inject]
		private CrystalUnitScene scene;

		[Inject]
		private DynamicWorldCanvas canvas;

		private CrystalUnitBannerView banner;

		public override void OnAwake()
		{
		}

		public override void Dispose()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
