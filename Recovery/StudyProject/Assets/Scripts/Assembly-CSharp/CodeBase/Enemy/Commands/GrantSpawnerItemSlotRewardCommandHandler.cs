using System.Linq;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Player.Models;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerItemSlotRewardCommandHandler : ICommandHandler<GrantSpawnerItemSlotRewardCommand, string>
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private World world;

		public string Execute(GrantSpawnerItemSlotRewardCommand command)
		{
			return null;
        }
	}
}
