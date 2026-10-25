using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Player.Models;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Codebase.Unit.Systems;
using Codebase.Vfx;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
	public class CreeperExplosionSystem : SystemBase
	{
		private Filter filter;

		private Event<UnitDiedEvent> unitDiedEvent;

		[Inject]
		private Stash<CreeperMobComponent> creeperStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private EnemyCastSystem enemyCastSystem;

		[Inject]
		private UnitDamageSystem unitDamageSystem;

		[Inject]
		private WorldVfxService worldVfxService;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void StartExplosion(Entity entity, ref CreeperMobComponent creeper)
		{
		}

		private void Explode(Entity entity)
		{
		}

		private void FinalizeDeath(UnitCore unit, ref CreeperMobComponent creeper)
		{
		}
	}
}
