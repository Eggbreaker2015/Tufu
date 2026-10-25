using Codebase.Buildings;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Player.Models;

namespace Codebase.Player.Commands
{
	public struct PlayerDeathSequenceCommandHandler : ICommandHandler<PlayerDeathSequenceCommand>
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private FirecampSystem firecampSystem;

		public void Execute(PlayerDeathSequenceCommand command)
		{
// Native ELF VA 0x1469888, including vector args from bounded ARM64.
            playerModel.Unit.StartDeathSequence(command.Killer, firecampSystem.transform.position, firecampSystem.GetRandomJumpOutPoint(), firecampSystem.JumpOutEffect, firecampSystem.UnderFirecampEffect, playerModel);
		}
	}
}
