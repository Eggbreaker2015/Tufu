using Codebase.Core;
using Codebase.Player.Models;
using Codebase.ProximityView.Components;
using Scellecs.Morpeh;

namespace Codebase.ProximityView.Systems
{
	public class ProximityViewDistanceSystem : SystemBase
	{
		private const float HideBuffer = 1f;

		[Inject]
		private Stash<ProximityViewComponent> proximityViewStash;

		[Inject]
		private Stash<ProximityViewNearMarker> nearMarkerStash;

		[Inject]
		private PlayerModel playerModel;

		private Filter filter;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
