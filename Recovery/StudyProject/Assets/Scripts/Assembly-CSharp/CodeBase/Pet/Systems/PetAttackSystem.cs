using Codebase.Abilities.Effects;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Unit;
using Codebase.Unit.Events;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Pet.Systems
{
	// Per-pet copy of PlayerAttackSystem's cooldown state machine; hits come from the pet with its own item.
	public class PetAttackSystem : SystemBase
	{
		private const float ResetCooldownPostCastPointDelay = 0.15f;

		private const float AttackCancelRewindMultiplier = 3f;

		private const float TargetOutOfRangeRetainDuration = 0.2f;

		private const float WeakEnemyAttackSpeedMultiplier = 2.5f;

		private const float MinWearingTime = 1f;

		private const float HitVfxLifetime = 3f;

		// Share of the attack cycle the attack animation bool stays on, as in EnemyUnit.Tick.
		private const float AttackAnimationPortion = 0.5f;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PetsModel petsModel;

		private EnemiesModel enemiesModel;

		private Event<WeaponHitEvent> weaponHitEvent;

		private PlayerUnit boundPlayer;

		private PlayerAttackSystem playerAttack;

		public override void OnAwake()
		{
			weaponHitEvent = World.GetEvent<WeaponHitEvent>();
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!PetConfig.Enabled)
				return;
			PlayerUnit player = playerModel.Unit;
			if (player == null)
				return;
			BindPlayer(player);
			if (enemiesModel == null)
				enemiesModel = GameContext.Get<EnemiesModel>();
			bool paused = !playerModel.CanMove || player.IsDying || player.IsDead || player.IsSleeping;
			float engageRadius = PetConfig.Instance.EngageRadius;
			float engageSqr = engageRadius * engageRadius;
			foreach (PetModel pet in petsModel.Pets)
			{
				ItemModel item = pet.Item;
				item.JustStartedAttack = false;
				if (!pet.IsAlive)
					continue;
				PetUnit unit = pet.Unit;
				if (paused)
					SetTarget(pet, null);
				else
					SelectTarget(pet, player, engageSqr);
				EnemyUnit target = unit.CurrentTarget;
				bool hasTarget = UpdateTargetInRange(pet, target, deltaTime);
				float attackSpeed = GetAttackSpeed(pet, target);
				float hitThreshold = GetHitCooldownThreshold(item);
				if (hasTarget)
					item.Cooldown -= attackSpeed * deltaTime;
				else if (item.Cooldown > hitThreshold)
				{
					item.Cooldown += attackSpeed * deltaTime * AttackCancelRewindMultiplier;
					if (item.Cooldown >= item.Config.AttackCooldown)
					{
						item.Cooldown = 0f;
						item.AttackInProgress = false;
					}
				}
				else
				{
					item.AttackInProgress = false;
					item.Cooldown -= attackSpeed * deltaTime;
				}

				item.WearingTime += deltaTime;
				if (item.ResetCooldown && !item.AttackInProgress && item.Cooldown <= item.Config.AnimationBackPoint * item.Config.AttackCooldown)
				{
					item.ResetCooldown = false;
					item.AttackInProgress = true;
					item.Cooldown = hitThreshold + ResetCooldownPostCastPointDelay;
				}

				bool engaged = !paused && hasTarget;
				if (engaged)
					Attack(unit, item, target, attackSpeed, hitThreshold, deltaTime);
				unit.SetAttackAnimation(engaged && item.Cooldown > item.Config.AttackCooldown * AttackAnimationPortion, attackSpeed / item.Config.AttackCooldown);
			}
		}

		private void BindPlayer(PlayerUnit player)
		{
			if (boundPlayer == player)
				return;
			boundPlayer = player;
			playerAttack = player.GetComponent<PlayerAttackSystem>();
			if (player.GetComponent<PlayerClawView>() == null)
				player.gameObject.AddComponent<PlayerClawView>();
		}

		private void Attack(PetUnit unit, ItemModel item, EnemyUnit target, float attackSpeed, float hitThreshold, float deltaTime)
		{
			// Mirrors PlayerAttackSystem.Attack for one item, without the weapon_1 range rule and haptics.
			if (item.WearingTime < MinWearingTime)
				return;
			if (item.Cooldown <= 0f)
			{
				item.AttackInProgress = true;
				item.JustStartedAttack = true;
				item.Cooldown = item.Config.AttackCooldown;
				return;
			}

			if (!item.AttackInProgress || item.Cooldown > hitThreshold + attackSpeed * deltaTime)
				return;
			item.AttackInProgress = false;
			if (item.ResetCooldown)
			{
				item.ResetCooldown = false;
				item.Cooldown = 0f;
			}

			weaponHitEvent.NextFrame(new WeaponHitEvent
			{
				Source = unit, Target = target, Weapon = item, DamageType = item.Config.DamageType
			});
			SpawnHitVfx(target);
		}

		private void SpawnHitVfx(EnemyUnit target)
		{
			if (playerAttack == null || playerAttack.HitVfx == null)
				return;
			GameObject vfx = Object.Instantiate(playerAttack.HitVfx);
			vfx.transform.position = target.transform.position + Vector3.up;
			Object.Destroy(vfx, HitVfxLifetime);
		}

		private float GetAttackSpeed(PetModel pet, EnemyUnit target)
		{
			ItemModel item = pet.Item;
			float attackSpeed = 1f;
			foreach (var ability in item.AbilityStates)
				attackSpeed *= ability.GetWeaponStatMultiplier(WeaponBuffType.AttackSpeed);
			attackSpeed *= item.AbilityAttackSpeedMultiplier;
			if (pet.Definition != null)
				attackSpeed *= pet.Definition.AttackSpeedMultiplier;
			// Weak enemies and event golems are judged on the pet's own target.
			if (IsValid(target) && playerAttack != null && playerAttack.CanClassifyEnemies && (playerAttack.IsWeakEnemy(target) || playerAttack.IsEventGolem(target)))
				attackSpeed *= WeakEnemyAttackSpeedMultiplier;
			return attackSpeed;
		}

		private static float GetHitCooldownThreshold(ItemModel item)
		{
			// Fixed cast point: the player's joystick-driven coefficient does not apply to pets.
			return item.Config.AnimationCastPoint * item.Config.AttackCooldown;
		}

		private static bool UpdateTargetInRange(PetModel pet, EnemyUnit target, float deltaTime)
		{
			if (!IsValid(target))
			{
				pet.TargetOutOfRangeTimer = 0f;
				return false;
			}

			float range = (float)pet.Unit.GetStatValue(StatType.AttackRange);
			if (FlatSqrDistance(pet.Unit.transform.position, target.transform.position) <= range * range)
			{
				pet.TargetOutOfRangeTimer = 0f;
				return true;
			}

			pet.TargetOutOfRangeTimer += deltaTime;
			return pet.TargetOutOfRangeTimer < TargetOutOfRangeRetainDuration;
		}

		private void SelectTarget(PetModel pet, PlayerUnit player, float engageSqr)
		{
			// Priority: the player's target, then an enemy fighting this pet, then the enemy nearest the player.
			PetUnit unit = pet.Unit;
			EnemyUnit current = unit.CurrentTarget;
			EnemyUnit next;
			if (IsValid(player.CurrentTarget))
				next = player.CurrentTarget;
			else if (IsValid(current) && current.AggroTarget == unit)
				next = current;
			else
			{
				next = FindAggressor(unit);
				if (next == null)
					next = IsValid(current) && FlatSqrDistance(player.transform.position, current.transform.position) <= engageSqr ? current : FindNearest(player.transform.position, engageSqr);
			}

			SetTarget(pet, next);
		}

		private static void SetTarget(PetModel pet, EnemyUnit target)
		{
			if (pet.Unit.CurrentTarget == target)
				return;
			pet.Unit.CurrentTarget = target;
			pet.TargetOutOfRangeTimer = 0f;
		}

		private EnemyUnit FindAggressor(PetUnit unit)
		{
			Vector3 position = unit.transform.position;
			EnemyUnit best = null;
			float bestSqr = float.MaxValue;
			foreach (EnemyUnit enemy in enemiesModel.Enemies)
			{
				if (enemy == null || enemy.AggroTarget != unit || !IsValid(enemy))
					continue;
				float sqr = FlatSqrDistance(position, enemy.transform.position);
				if (sqr < bestSqr)
				{
					bestSqr = sqr;
					best = enemy;
				}
			}
			return best;
		}

		private EnemyUnit FindNearest(Vector3 center, float maxSqr)
		{
			EnemyUnit best = null;
			float bestSqr = maxSqr;
			foreach (EnemyUnit enemy in enemiesModel.Enemies)
			{
				if (!IsValid(enemy))
					continue;
				float sqr = FlatSqrDistance(center, enemy.transform.position);
				if (sqr <= bestSqr)
				{
					bestSqr = sqr;
					best = enemy;
				}
			}
			return best;
		}

		private static bool IsValid(EnemyUnit enemy)
		{
			return enemy != null && !enemy.IsDead && !enemy.IsFar;
		}

		private static float FlatSqrDistance(Vector3 a, Vector3 b)
		{
			float x = a.x - b.x;
			float z = a.z - b.z;
			return x * x + z * z;
		}
	}
}
