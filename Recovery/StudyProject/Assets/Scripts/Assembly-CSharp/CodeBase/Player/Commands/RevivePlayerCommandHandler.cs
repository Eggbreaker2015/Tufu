using Codebase.Unit;
using Codebase.Analytics.Events;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player.Models;
using Codebase.Rage;

namespace Codebase.Player.Commands
{
	public struct RevivePlayerCommandHandler : ICommandHandler<RevivePlayerCommand>
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private EnemiesModel enemiesModel;

		[Inject]
		private RageModel rageModel;

		[Inject]
		private PetsModel petsModel;

		[Inject]
		private IAnalyticsWrapper analytics;

		public void Execute(RevivePlayerCommand command)
		{
// Native ELF VA 0x1469b28. Ordinary firecamp revival restores one HP, not full health.
            PlayerUnit unit = playerModel.Unit;
            unit.Agent.Warp(command.revivePosition);
            // Pet mode: every pet comes back with the player, with the same health rule (1 HP unless restoreHealth).
            if (PetConfig.Enabled) petsModel.ReviveAll(command.revivePosition, unit.transform.eulerAngles.y, command.restoreHealth);
            unit.CurrentHealth = command.restoreHealth ? unit.GetStatValue(StatType.Health) : 1.0;
            rageModel.ResetRage();
            foreach (EnemyUnit enemy in enemiesModel.Enemies)
            {
                enemy.PreviousHealth = enemy.CurrentHealth;
                enemy.CurrentHealth = enemy.GetStatValue(StatType.Health);
                if (enemy.AggroTimer > 0f) { enemy.AggroTimer = 0f; enemy.RetreatTimer = 3f; }
            }
            analytics.Send(new PlayerDieEvent(command.KillerName, playerModel.CurrentLocation.Number));
            playerModel.SetCurrentLocation(1);
            unit.LastDamageGet -= 2.0;
            playerModel.Data.DeathsCount.Value++;
		}
	}
}
