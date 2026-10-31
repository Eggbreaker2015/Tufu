using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Unit.Systems;
using Scellecs.Morpeh;

namespace Codebase.Player.Systems
{
	public class UnstackSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private UnitDamageSystem unitDamageSystem;
		private bool pendingUnstack;
		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!pendingUnstack)
				return;
			pendingUnstack = false;
			KillPlayer();
		}

		public void RequestUnstack()
		{
			pendingUnstack = true;
		}

		private void KillPlayer()
		{
			var unit = global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel);
			if (unit == null || unit.IsDead)
				return;
			unit.CurrentHealth = 0.0;
			GameContext.Get<global::Codebase.Rpg.HeroDeathService>().OnHealthDepleted(GameContext.Get<global::Codebase.Rpg.HeroRosterModel>().Get(unit), "Unstack");
		}
	}
}
