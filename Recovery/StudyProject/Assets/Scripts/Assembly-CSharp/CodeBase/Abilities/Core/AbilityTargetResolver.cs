using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Inventory;

namespace Codebase.Abilities.Core
{
	// Pet mode: the enemy an ability acts on. With pet mode off it returns what the original code read.
	public static class AbilityTargetResolver
	{
		// ctx.Target, then the attacker's target, the owner pet's target, the player's target.
		public static EnemyUnit Resolve(AbilityContext context)
		{
			if (context.Target != null)
				return context.Target;
			return ResolveAttackerTarget(context);
		}

		// Same order without ctx.Target, for code that originally read the player's current target directly.
		public static EnemyUnit ResolveAttackerTarget(AbilityContext context)
		{
			PlayerUnit player = context.PlayerModel != null ? context.PlayerModel.Unit : null;
			if (!PetConfig.Enabled)
				return player != null ? player.CurrentTarget : null;
			// A known attacker decides, even without a target; passive contexts have none and use the owner pet.
			if (context.Attacker is PetUnit pet && pet != null)
				return pet.CurrentTarget;
			if (context.Attacker is PlayerUnit attacker && attacker != null)
				return attacker.CurrentTarget;
			PetUnit ownerPet = GetOwnerPetUnit(context.Owner);
			if (ownerPet != null)
				return ownerPet.CurrentTarget;
			return player != null ? player.CurrentTarget : null;
		}

		// The alive pet of a weapon owner; null for armor owners, dead pets and with pet mode off.
		public static PetUnit GetOwnerPetUnit(IAbilityOwner owner)
		{
			if (!PetConfig.Enabled || !(owner is ItemModel item) || item.Config.SlotType != ItemSlotType.Weapon)
				return null;
			PetModel pet = GameContext.Get<PetsModel>().Get(item);
			return pet != null && pet.IsAlive ? pet.Unit : null;
		}
	}
}
