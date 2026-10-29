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
		private const float AgentRadius = 0.5f;

		private const float AgentHeight = 2f;

		private const float AgentAngularSpeed = 720f;

		private const float AgentAcceleration = 20f;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PetsModel petsModel;

		[Inject]
		private Stash<PetTag> petTagStash;

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
		}

		private void Spawn(ItemSlotModel slot, int slotOrder, PlayerUnit player)
		{
			ItemModel item = slot.ItemModel;
			PetDefinition definition = PetConfig.Instance.Find(item.Config);
			PetModel pet = new PetModel(slot, item, definition, slotOrder);
			Transform owner = player.transform;
			PetsModel.TrySamplePosition(PetsModel.GetFollowPosition(owner.position, owner.eulerAngles.y, slotOrder), owner.position, out Vector3 position);

			// Order matters: UnitCore.Awake reads the EntityProvider and Start needs the agent.
			GameObject root = new GameObject("Pet " + item.Key);
			root.transform.SetPositionAndRotation(position, owner.rotation);
			root.AddComponent<EntityProvider>();
			NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
			agent.radius = AgentRadius;
			agent.height = AgentHeight;
			agent.angularSpeed = AgentAngularSpeed;
			agent.acceleration = AgentAcceleration;
			agent.speed = (float)player.GetStatSpecificValue(StatType.MovementSpeed) * PetConfig.Instance.FollowSpeedMultiplier;
			PetUnit unit = root.AddComponent<PetUnit>();
			Transform healthBarPoint = new GameObject("HealthBarPoint").transform;
			healthBarPoint.SetParent(root.transform, false);
			Transform visualSlot = new GameObject("VisualSlot").transform;
			visualSlot.SetParent(root.transform, false);
			unit.Setup(item, definition, agent, healthBarPoint, visualSlot);
			unit.AttachVisual(definition);
			unit.Stats = new List<Stat>();
			unit.BuildStatLookup();
			petTagStash.Set(unit.EntityProvider.Entity);
			Injector.InjectInto(unit);
			UnitSkillLoadout loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Pet, item.Key);
			if (loadout != null && GameContext.Has<SkillService>())
				GameContext.Get<SkillService>().Install(unit, unit.EntityProvider.Entity, loadout);
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
			if (unit == null)
				return;
			if (unit.HealthBar != null)
			{
				unit.Dispose();
				unit.HealthBar = null;
			}
			// The EntityProvider does not remove its entity on destroy; without this NavMeshSyncSystem
			// keeps reading the destroyed NavMeshAgent (MissingReferenceException every frame).
			Entity entity = unit.EntityProvider.Entity;
			if (!World.IsDisposed(entity))
			{
				if (GameContext.Has<SkillService>())
				{
					GameContext.Get<SkillService>().ClearBuffs(entity);
					GameContext.Get<SkillService>().CancelCast(entity);
				}
				World.RemoveEntity(entity);
			}
			Object.Destroy(unit.gameObject);
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
