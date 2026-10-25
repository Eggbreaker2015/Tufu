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
			var unit = playerModel.Unit;
			if (unit == null || unit.IsDead)
				return;
			unit.CurrentHealth = 0.0;
			unitDamageSystem.ProcessPlayerDeath("Unstack", unit);
		}
	}
}
