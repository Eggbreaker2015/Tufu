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
            string key = command.Spawner.Config.SlotReward;
            if (string.IsNullOrWhiteSpace(key)) return null;
            var slot = playerModel.ItemSlots.FirstOrDefault(value => value.Key == key);
            if (slot == null)
            {
                UnityEngine.Debug.LogError("Slot " + key + " does not exits", command.Spawner);
                return null;
            }
            if (slot.IsUnlocked) return null;
            slot.IsUnlocked = true;
            return slot.Key;
        }
	}
}
