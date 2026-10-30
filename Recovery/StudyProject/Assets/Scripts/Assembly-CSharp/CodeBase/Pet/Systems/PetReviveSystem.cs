using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Models;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Pet.Systems
{
	// Cooldown revive next to the player; the fountain and the player's revive bring pets back sooner.
	public class PetReviveSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PetsModel petsModel;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!PetConfig.Enabled)
				return;
			PlayerUnit player = playerModel.Unit;
			// While the player dies, RevivePlayerCommand revives every pet at the fountain.
			if (player == null || player.IsDead)
				return;
			// RPG dungeons: fallen heroes stay down until the party leaves (or a revive skill brings them back).
			if (global::Codebase.Rpg.RpgModeConfig.Enabled && global::Codebase.Rpg.Dungeon.RpgDungeonSystem.BlocksRevive)
				return;
			float now = Time.time;
			Transform owner = global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel).transform;
			foreach (PetModel pet in petsModel.Pets)
				if (pet.State == PetState.Dead && pet.Unit != null && now >= pet.ReviveAt)
					petsModel.Revive(pet, owner.position, owner.eulerAngles.y);
		}
	}
}
