using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player.Inventory;
using Codebase.Skills;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UniMob;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Pet
{
	public class PetsModel : Model
	{
		public const int RingSlots = 5;

		public const float FollowRadius = 2.5f;

		private const float SampleRadius = 4f;

		// Time the death animation plays before the pet's visual is hidden.
		private const float DeathHideDelay = 2f;

		private const float DefaultReviveCooldown = 15f;

		private const float DefaultReviveHealthFraction = 0.2f;

		public List<PetModel> Pets { get; } = new List<PetModel>();

		private IEnumerable<PetModel> RosterPets
		{
			get
			{
				foreach (var hero in GameContext.Get<global::Codebase.Rpg.HeroRosterModel>().Heroes)
					if (hero.Pet != null) yield return hero.Pet;
			}
		}

		// Revive times of dead pets whose unit was despawned (item unequipped or moved), so re-equipping keeps the cooldown.
		private readonly Dictionary<ItemModel, float> pendingRevives = new Dictionary<ItemModel, float>();

		private PetStatInheritance inheritance;

		// Shared by stat sync and the UI (PetStats.TryGet).
		public PetStatInheritance Inheritance
		{
			get
			{
				if (inheritance == null)
					inheritance = new PetStatInheritance(GameContext.Get<ItemsProvider>());
				return inheritance;
			}
		}

		public IEnumerable<PetModel> AlivePets
		{
			get
			{
				foreach (PetModel pet in RosterPets)
					if (pet.IsAlive)
						yield return pet;
			}
		}

		public PetsModel(Lifetime lifetime)
			: base(lifetime)
		{
		}

		public PetModel Get(ItemModel item)
		{
			if (item == null)
				return null;
			foreach (PetModel pet in RosterPets)
				if (pet.Item == item)
					return pet;
			return null;
		}

		public PetModel Get(PetUnit unit)
		{
			if (unit == null)
				return null;
			foreach (PetModel pet in RosterPets)
				if (pet.Unit == unit)
					return pet;
			return null;
		}

		public bool IsPetAlive(ItemModel item)
		{
			PetModel pet = Get(item);
			return pet != null && pet.IsAlive;
		}

		// Weapons whose pet is alive, in the original order (for "highest damage weapon" picks).
		public List<ItemModel> GetAliveWeapons(List<ItemModel> weapons)
		{
			List<ItemModel> result = new List<ItemModel>(weapons.Count);
			foreach (ItemModel weapon in weapons)
				if (IsPetAlive(weapon))
					result.Add(weapon);
			return result;
		}

		public PetModel GetBySlot(ItemSlotModel slot)
		{
			foreach (PetModel pet in RosterPets)
				if (pet.Slot == slot)
					return pet;
			return null;
		}

		public static Vector3 GetFollowPosition(Vector3 center, float yaw, int slotOrder)
		{
			// Ring around the owner, starting behind it and spreading by slot order.
			float angle = yaw + 180f + slotOrder * (360f / RingSlots);
			return center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * FollowRadius;
		}

		public static bool TrySamplePosition(Vector3 desired, Vector3 fallback, out Vector3 position)
		{
			if (NavMesh.SamplePosition(desired, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas) ||
				NavMesh.SamplePosition(fallback, out hit, SampleRadius, NavMesh.AllAreas))
			{
				position = hit.position;
				return true;
			}
			position = fallback;
			return false;
		}

		public void Warp(PetModel pet, Vector3 center, float yaw)
		{
			if (pet.Unit == null)
				return;
			TrySamplePosition(GetFollowPosition(center, yaw, pet.SlotOrder), center, out Vector3 position);
			NavMeshAgent agent = pet.Unit.Agent;
			if (agent != null && agent.enabled)
			{
				agent.Warp(position);
				if (agent.isOnNavMesh)
					agent.ResetPath();
			}
			else
				pet.Unit.transform.position = position;
			pet.PathFailTimer = 0f;
		}

		// The item stays equipped and keeps its cooldown state; the pet is revived later (cooldown, fountain or player revive).
		public void OnPetDied(PetModel pet)
		{
			PetConfig config = PetConfig.Instance;
			SetDead(pet, Time.time + (config != null ? config.ReviveCooldown : DefaultReviveCooldown), DeathHideDelay);
		}

		public void RememberRevive(PetModel pet)
		{
			if (pet.State == PetState.Dead && pet.Item != null && pet.ReviveAt > Time.time)
				pendingRevives[pet.Item] = pet.ReviveAt;
		}

		// A respawned pet whose item died less than a cooldown ago comes back dead, hidden at once.
		public bool RestoreDeath(PetModel pet)
		{
			if (pet.Item == null || !pendingRevives.TryGetValue(pet.Item, out float reviveAt))
				return false;
			pendingRevives.Remove(pet.Item);
			if (reviveAt <= Time.time)
				return false;
			SetDead(pet, reviveAt, 0f);
			return true;
		}

		public void ClearPendingRevives()
		{
			pendingRevives.Clear();
		}

		private void SetDead(PetModel pet, float reviveAt, float hideDelay)
		{
			pet.State = PetState.Dead;
			pet.ReviveAt = reviveAt;
			pet.PathFailTimer = 0f;
			pet.TargetOutOfRangeTimer = 0f;
			PetUnit unit = pet.Unit;
			if (unit == null)
				return;
			unit.CurrentTarget = null;
			if (GameContext.Has<SkillService>())
			{
				GameContext.Get<SkillService>().ClearForcedMovement(unit);
				GameContext.Get<SkillService>().CancelCast(unit);
			}
			Entity entity = unit.EntityProvider.Entity;
			World world = World.Default;
			world.GetStash<UnitDeadMarker>().Set(entity);
			world.GetStash<NavMeshAgentActiveMarker>().Remove(entity);
			NavMeshAgent agent = unit.Agent;
			if (agent != null && agent.enabled)
			{
				if (agent.isOnNavMesh)
					agent.ResetPath();
				agent.enabled = false;
			}
			unit.SetWalking(false);
			unit.SetAttackAnimation(false, 1f);
			unit.SetDeadAnimation(true);
			unit.HideVisualAfter(hideDelay);
		}

		// Undoes OnPetDied: back on the follow ring, agent and markers restored, attack state reset.
		// A pet comes back with PetConfig.ReviveHealthFraction of its max health unless full health is restored.
		public void Revive(PetModel pet, Vector3 center, float yaw, bool restoreHealth = false)
		{
			PetUnit unit = pet.Unit;
			if (unit == null || pet.State == PetState.Alive)
				return;
			ClearSkillBuffs(unit);
			if (GameContext.Has<SkillService>())
				GameContext.Get<SkillService>().ResetCaster(unit);
			pet.State = PetState.Alive;
			pet.ReviveAt = 0f;
			pet.TargetOutOfRangeTimer = 0f;
			unit.CurrentTarget = null;
			double maximum = unit.GetStatValue(StatType.Health);
			PetConfig config = PetConfig.Instance;
			double fraction = config != null ? config.ReviveHealthFraction : DefaultReviveHealthFraction;
			unit.CurrentHealth = restoreHealth ? maximum : Math.Max(maximum * fraction, Math.Min(1.0, maximum));
			ResetAttackState(pet.Item);

			// Placed while the agent is still off, so it is enabled on the NavMesh.
			Warp(pet, center, yaw);
			Entity entity = unit.EntityProvider.Entity;
			World world = World.Default;
			world.GetStash<UnitDeadMarker>().Remove(entity);
			NavMeshAgent agent = unit.Agent;
			if (agent != null)
			{
				agent.enabled = true;
				agent.Warp(unit.transform.position);
				world.GetStash<NavMeshAgentActiveMarker>().Set(entity);
			}

			unit.ShowVisual();
			unit.SetWalking(false);
			unit.SetAttackAnimation(false, 1f);
			unit.SetDeadAnimation(false);
			unit.PlaySpawnAnimation();
		}

		// Fountain: brings back every dead pet next to the player.
		public void ReviveDead(Vector3 center, float yaw)
		{
			pendingRevives.Clear();
			foreach (PetModel pet in RosterPets)
				if (pet.State == PetState.Dead)
					Revive(pet, center, yaw);
		}

		// Player revive and arena entry: dead pets come back and all are moved and lose their target.
		// restoreHealth follows the player's: full health for everyone, otherwise dead pets get the revive fraction and living ones keep theirs.
		public void ReviveAll(Vector3 center, float yaw, bool restoreHealth)
		{
			pendingRevives.Clear();
			foreach (PetModel pet in RosterPets)
			{
				PetUnit unit = pet.Unit;
				if (unit == null)
					continue;
				if (pet.State == PetState.Dead)
				{
					Revive(pet, center, yaw, restoreHealth);
					continue;
				}

				ClearSkillBuffs(unit);
				if (GameContext.Has<SkillService>())
					GameContext.Get<SkillService>().CancelCast(unit);
				if (restoreHealth)
					unit.CurrentHealth = unit.GetStatValue(StatType.Health);
				unit.CurrentTarget = null;
				pet.TargetOutOfRangeTimer = 0f;
				Warp(pet, center, yaw);
			}
		}

		private static void ClearSkillBuffs(PetUnit unit)
		{
			if (unit.EntityProvider != null && GameContext.Has<SkillService>())
				GameContext.Get<SkillService>().ClearBuffs(unit.EntityProvider.Entity);
		}

		private static void ResetAttackState(ItemModel item)
		{
			if (item == null)
				return;
			item.WearingTime = 0f;
			item.Cooldown = 0f;
			item.AttackInProgress = false;
			item.JustStartedAttack = false;
			item.ResetCooldown = false;
		}

		public void WarpAll(Vector3 center, float yaw = 0f)
		{
			foreach (PetModel pet in RosterPets)
			{
				if (pet.Unit == null)
					continue;
				pet.Unit.CurrentTarget = null;
				Warp(pet, center, yaw);
			}
		}
	}
}
