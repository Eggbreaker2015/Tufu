using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Player.Models;
using Codebase.Player.Commands;

namespace Codebase.BossTower.Systems
{
	public class ExitBossArenaCommandHandler : ICommandHandler<ExitBossArenaCommand>
	{
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private BossRaidSystem bossRaidSystem;
		[Inject]
		private BossTowerModel bossTowerModel;
		public void Execute(ExitBossArenaCommand command)
		{
			if (playerModel.Unit == null)
				return;
			playerModel.InBossTowerArena = false;
			if (playerModel.Unit.IsDead)
			{
				App.ExecuteCommand(new PlayerDeathSequenceCommand());
				return;
			}

			bossRaidSystem.TeleportPlayerToTowerEntrance();
			playerModel.SavePositionOverride = null;
		}
	}
}
