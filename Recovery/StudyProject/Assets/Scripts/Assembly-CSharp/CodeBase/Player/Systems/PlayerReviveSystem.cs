using Codebase.Unit.Components;
using Codebase.Player.Commands;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Player.Models;
using Scellecs.Morpeh;

namespace Codebase.Player.Systems
{
	public class PlayerReviveSystem : SystemBase
	{
		private Event<PlayerDeathEvent> playerDeathEvent;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private PlayerModel playerModel;

		private Filter playersFilter;

		public override void OnAwake()
		{
// Native ELF VA 0x1455c64.
            playerDeathEvent = World.GetEvent<PlayerDeathEvent>();
            playersFilter = World.Filter.With<PlayerTag>().With<UnitDeadMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
// Native ELF VA 0x1455da4. No synthetic death event or immediate health override.
            if (playerModel.InBossTowerArena) return;
            foreach (Entity entity in playersFilter)
            {
                if (playerModel.Unit.IsReviving || playerModel.Unit.IsDeathAnimationActive) return;
                App.ExecuteCommand(new PlayerDeathSequenceCommand { Killer = playerModel.Unit.KillerName });
            }
		}
	}
}
