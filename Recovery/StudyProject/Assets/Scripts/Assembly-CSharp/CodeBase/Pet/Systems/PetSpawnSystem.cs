using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Skills;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Scellecs.Morpeh.Providers;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Pet.Systems
{
	// One pet per equipped weapon slot; polls the slots like PlayerWeaponView.Update.
	public class PetSpawnSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PetsModel petsModel;

		private readonly List<PetModel> removed = new List<PetModel>();

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!PetConfig.Enabled)
			{
				if (petsModel.Pets.Count > 0)
					DespawnAll();
				// Pet mode off: no pet-sourced buff stays on the hero (also from pets unequipped before the switch).
				if (GameContext.Has<SkillService>())
					GameContext.Get<SkillService>().ClearPetSourceBuffs(playerModel.Unit);
				return;
			}

			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return;

			PlayerUnit player = playerModel.Unit;
			if (player == null)
				return;
			removed.Clear();
			foreach (PetModel pet in petsModel.Pets)
				if (pet.Unit == null || !pet.Slot.IsEquipped || pet.Slot.ItemModel != pet.Item)
					removed.Add(pet);
			foreach (PetModel pet in removed)
				Despawn(pet);

			int slotOrder = 0;
			foreach (ItemSlotModel slot in playerModel.ItemSlots)
			{
				if (slot.SlotType != ItemSlotType.Weapon)
					continue;
				int order = slotOrder++;
				if (!slot.IsEquipped || petsModel.GetBySlot(slot) != null)
					continue;
				Spawn(slot, order, player);
			}

			foreach (PetModel pet in petsModel.Pets)
			{
				if (pet.Unit == null)
					continue;
				int level = PetStats.GetSkillLevel(pet.Item);
				if (level == pet.SkillLevel)
					continue;
				if (GameContext.Has<SkillService>())
					GameContext.Get<SkillService>().SetSkillLevel(pet.Unit, level);
				pet.SkillLevel = level;
			}
		}

		private void Spawn(ItemSlotModel slot, int slotOrder, PlayerUnit player)
		{
			ItemModel item = slot.ItemModel;
			PetDefinition definition = PetConfig.Instance.Find(item.Config);
			PetModel pet = new PetModel(slot, item, definition, slotOrder);
			Transform owner = player.transform;
			PetsModel.TrySamplePosition(PetsModel.GetFollowPosition(owner.position, owner.eulerAngles.y, slotOrder), owner.position, out Vector3 position);

			PetUnit unit = PetUnitFactory.Create(item, definition, position, owner.rotation, (float)player.GetStatSpecificValue(StatType.MovementSpeed) * PetConfig.Instance.FollowSpeedMultiplier);
			UnitSkillLoadout loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Pet, item.Key);
			int skillLevel = PetStats.GetSkillLevel(item);
			if (loadout != null && GameContext.Has<SkillService>())
				GameContext.Get<SkillService>().Install(unit, unit.EntityProvider.Entity, loadout, levelOverride: skillLevel);
			pet.SkillLevel = skillLevel;
			pet.Unit = unit;
			petsModel.Pets.Add(pet);
			petsModel.RestoreDeath(pet);
		}

		private void Despawn(PetModel pet)
		{
			petsModel.RememberRevive(pet);
			petsModel.Pets.Remove(pet);
			PetUnit unit = pet.Unit;
			pet.Unit = null;
			PetUnitFactory.Destroy(unit);
		}

		private void DespawnAll()
		{
			removed.Clear();
			removed.AddRange(petsModel.Pets);
			foreach (PetModel pet in removed)
				Despawn(pet);
			petsModel.ClearPendingRevives();
		}
	}
}
