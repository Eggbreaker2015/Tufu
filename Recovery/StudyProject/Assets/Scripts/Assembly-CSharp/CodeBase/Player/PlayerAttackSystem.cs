using System;
using Codebase.Player.Input;
using Codebase.Abilities.Effects;
using Codebase.Unit.Events;
using Codebase.Vibration.Events;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Meta;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Lofelt.NiceVibrations;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Player
{
	public class PlayerAttackSystem : MonoBehaviour
	{
		private const float ResetCooldownPostCastPointDelay = 0.15f;

		private const float AttackCancelRewindMultiplier = 3f;

		private const float TargetOutOfRangeRetainDuration = 0.2f;

		private const float AnimationCastPointSpeedFactor = 0.05f;

		private const float BuckshotCastPointSpeedMultiplier = 1f;

		private const float CuttingCastPointSpeedMultiplier = 0.5f;

		private const float StabbingCastPointSpeedMultiplier = 0f;

		private const float WeakEnemyAttackSpeedMultiplier = 2.5f;

		private const float WeakEnemyAttackRangeMultiplier = 3f;

		private const float EventGolemAttackSpeedMultiplier = 2.5f;

		private const float EventGolemAttackRangeMultiplier = 3f;

		private PlayerModel playerModel;

		private EnemiesModel enemiesModel;

		[SerializeField]
		private GameObject hitVfx;

		private readonly Dictionary<int, bool> weakEnemyByLocation = new Dictionary<int, bool>();

		private MetaStatsModel metaStatsModel;

		private MetaStat weakSpawnerExtraRangeStat;

		private MetaStat eventGolemExtraRangeStat;

		private float targetOutOfRangeTimer;

		private int lastUnlockedLocationNumber;

		private static float animationCastPointSpeedCoefficient;

		private readonly List<ItemModel> playerAttackItems = new List<ItemModel>(1);

		[Inject]
		private World world;

		private PlayerUnit PlayerUnit => playerModel.Unit;

		public GameObject HitVfx => hitVfx;

		// IsWeakEnemy/IsEventGolem need Start to have resolved the meta stats.
		public bool CanClassifyEnemies => weakSpawnerExtraRangeStat != null && eventGolemExtraRangeStat != null;

		[StaticReset]
		private static void ResetStaticState()
		{
			// Native ELF VA 0x143ffc4.
			animationCastPointSpeedCoefficient = 0f;
		}

		private void Start()
		{
			// Native ELF VA 0x1440024.
			playerModel = GameContext.Get<PlayerModel>();
			enemiesModel = GameContext.Get<EnemiesModel>();
			metaStatsModel = GameContext.Get<MetaStatsModel>();
			weakSpawnerExtraRangeStat = metaStatsModel.Get("weak_spawner_extra_range");
			eventGolemExtraRangeStat = metaStatsModel.Get("event_golem_extra_range");
		}

		private void Update()
		{
			// Native ELF VA 0x1440178; vector arguments reconstructed from bounded ARM64.
			weakEnemyByLocation.Clear();
			lastUnlockedLocationNumber = playerModel.LastUnlockedLocation.Number;
			float range = (float)playerModel.GetStatValue(Codebase.Unit.StatType.AttackRange);
			Vector3 position = PlayerUnit.transform.position;
			float deltaTime = Time.deltaTime;
			float speedTarget = GameContext.Get<Joystick>().GetInput().magnitude * (float)PlayerUnit.GetStatSpecificValue(Codebase.Unit.StatType.MovementSpeed) * 0.05f;
			float coefficient = Mathf.Clamp01(1f - Mathf.Exp(-4f * deltaTime));
			animationCastPointSpeedCoefficient += (speedTarget - animationCastPointSpeedCoefficient) * coefficient;
			bool hasTarget;
			if (PlayerUnit.CurrentTarget != null && !PlayerUnit.CurrentTarget.IsDead && SqrDistance(position, PlayerUnit.CurrentTarget.transform.position) < GetAttackRangeSqr(PlayerUnit.CurrentTarget, range))
			{
			    targetOutOfRangeTimer = 0f;
			    hasTarget = true;
			}
			else
			{
			    bool retain = false;
			    if (PlayerUnit.CurrentTarget != null && !PlayerUnit.CurrentTarget.IsDead)
			    {
			        targetOutOfRangeTimer += deltaTime;
			        retain = targetOutOfRangeTimer < 0.2f;
			    }
			    else targetOutOfRangeTimer = 0f;
			    if (retain) hasTarget = true;
			    else
			    {
			        PlayerUnit.CurrentTarget = null;
			        FindTarget(position, range);
			        hasTarget = PlayerUnit.CurrentTarget != null;
			    }
			}
			// Pet mode: only the player's own attack item cycles here; pets run in PetAttackSystem.
			foreach (ItemModel weapon in GetAttackItems())
			{
			    weapon.JustStartedAttack = false;
			    float attackSpeed = 1f;
			    foreach (var ability in weapon.AbilityStates) attackSpeed *= ability.GetWeaponStatMultiplier(WeaponBuffType.AttackSpeed);
			    attackSpeed *= weapon.AbilityAttackSpeedMultiplier;
			    if (IsAttackingWeakEnemy() || IsAttackingEventGolem()) attackSpeed *= 2.5f;
			    if (hasTarget) weapon.Cooldown -= attackSpeed * deltaTime;
			    else if (weapon.Cooldown > GetHitCooldownThreshold(weapon, playerModel))
			    {
			        weapon.Cooldown += attackSpeed * deltaTime * 3f;
			        if (weapon.Cooldown >= weapon.Config.AttackCooldown)
			        {
			            weapon.Cooldown = 0f;
			            weapon.AttackInProgress = false;
			        }
			    }
			    else
			    {
			        weapon.AttackInProgress = false;
			        weapon.Cooldown -= attackSpeed * deltaTime;
			    }
			    weapon.WearingTime += deltaTime;
			    if (weapon.ResetCooldown && !weapon.AttackInProgress && weapon.Cooldown <= weapon.Config.AnimationBackPoint * weapon.Config.AttackCooldown)
			    {
			        weapon.ResetCooldown = false;
			        weapon.AttackInProgress = true;
			        weapon.Cooldown = GetAnimationCastPoint(weapon) * weapon.Config.AttackCooldown + 0.15f;
			    }
			}
			if (PlayerUnit.IsDying) PlayerUnit.CurrentTarget = null;
			else if (hasTarget) Attack();
		}

		private void Attack()
		{
			// Native ELF VA 0x1441034: original cast crossing emits the actual Morpeh hit event.
			if (PlayerUnit.IsDying || PlayerUnit.CurrentTarget == null || PlayerUnit.IsDead || PlayerUnit.IsSleeping || !playerModel.CanMove) return;
			EnemyUnit target = PlayerUnit.CurrentTarget;
			float baseRange = (float)playerModel.GetStatValue(Codebase.Unit.StatType.AttackRange);
			// Pet mode: only the player's own attack item; its SlotIndex 0 keeps the weapon_1 base-range rule.
			foreach (ItemModel weapon in GetAttackItems())
			{
			    ItemSlotModel slot = playerModel.ItemSlots[weapon.SlotIndex];
			    if (slot.Key == "weapon_1" && SqrDistance(PlayerUnit.transform.position, target.transform.position) >= baseRange * baseRange) continue;
			    if (weapon.WearingTime < 1f) continue;
			    if (weapon.Cooldown <= 0f)
			    {
			        weapon.AttackInProgress = true;
			        weapon.JustStartedAttack = true;
			        weapon.Cooldown = weapon.Config.AttackCooldown;
			        continue;
			    }
			    float attackSpeed = 1f;
			    foreach (var ability in weapon.AbilityStates) attackSpeed *= ability.GetWeaponStatMultiplier(WeaponBuffType.AttackSpeed);
			    attackSpeed *= weapon.AbilityAttackSpeedMultiplier;
			    if (IsAttackingWeakEnemy() || IsAttackingEventGolem()) attackSpeed *= 2.5f;
			    if (!weapon.AttackInProgress || weapon.Cooldown > GetHitCooldownThreshold(weapon, playerModel) + attackSpeed * Time.deltaTime) continue;
			    weapon.AttackInProgress = false;
			    if (weapon.ResetCooldown)
			    {
			        weapon.ResetCooldown = false;
			        weapon.Cooldown = 0f;
			    }
			    World.Default.GetEvent<WeaponHitEvent>().NextFrame(new WeaponHitEvent
			    {
			        Source = PlayerUnit, Target = target, Weapon = weapon, DamageType = weapon.Config.DamageType
			    });
			    World.Default.GetRequest<HapticRequest>().Publish(new HapticRequest { preset = GetWeaponImpact(weapon.Config.DamageType) }, true);
			    GameObject vfx = Instantiate(hitVfx);
			    vfx.transform.position = target.transform.position + Vector3.up;
			    Destroy(vfx, 3f);
			}
		}

		private List<ItemModel> GetAttackItems()
		{
			if (!PetConfig.Enabled)
				return playerModel.EquippedWeapons;
			playerAttackItems.Clear();
			if (playerModel.PlayerAttackItem != null)
				playerAttackItems.Add(playerModel.PlayerAttackItem);
			return playerAttackItems;
		}

		private static HapticPatterns.PresetType GetWeaponImpact(WeaponType weaponType)
		{
			// Native ELF VA 0x1441930; original 4-entry RODATA table.
			switch (weaponType)
			{
			    case WeaponType.Stabbing: return (HapticPatterns.PresetType)0;
			    case WeaponType.Cutting: return (HapticPatterns.PresetType)8;
			    case WeaponType.Magic: return (HapticPatterns.PresetType)8;
			    case WeaponType.Buckshot: return (HapticPatterns.PresetType)5;
			    default: throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null);
			}
		}

		public static float GetAnimationCastPoint(ItemModel weapon)
		{
			// Native ELF VA 0x1440fa0; type factors [0, .5, 0, 1].
			float factor = weapon.Config.DamageType == WeaponType.Cutting ? 0.5f : weapon.Config.DamageType == WeaponType.Buckshot ? 1f : 0f;
			return 1f - (1f - weapon.Config.AnimationCastPoint) / (factor * animationCastPointSpeedCoefficient + 1f);
		}

		private void FindTarget(Vector3 playerPos, float range)
		{
			// Native ELF VA 0x1440b54: first eligible list entry, not nearest; only IsFar is skipped.
			foreach (EnemyUnit enemy in enemiesModel.Enemies)
			{
			    if (enemy.IsFar) continue;
			    if (GetAttackRangeSqr(enemy, range) < SqrDistance(playerPos, enemy.transform.position)) continue;
			    PlayerUnit.CurrentTarget = enemy;
			    return;
			}
		}

		private static float GetHitCooldownThreshold(ItemModel weapon, PlayerModel playerModel)
		{
			// Native ELF VA 0x1440f74; player argument is unused in native.
			return GetAnimationCastPoint(weapon) * weapon.Config.AttackCooldown;
		}

		private static float SqrDistance(Vector3 a, Vector3 b)
		{
			// Native ELF VA 0x1440a78, native float operation order.
			float x=a.x-b.x, y=a.y-b.y, z=a.z-b.z;
			return z*z+(x*x+y*y);
		}

		private float GetAttackRangeSqr(EnemyUnit enemy, float baseRange)
		{
			// Native ELF VA 0x1440a9c.
			float range = baseRange;
			// Pet mode: chain slots hold pets, so the player never gets the chain x3 range.
			if (!PetConfig.Enabled && ItemSlotKeys.HasChainWeaponEquipped(playerModel) && (IsWeakEnemy(enemy) || IsEventGolem(enemy))) range *= 3f;
			return range * range;
		}

		private bool IsAttackingWeakEnemy()
		{
			// Native ELF VA 0x1440de4.
			return PlayerUnit.CurrentTarget != null && IsWeakEnemy(PlayerUnit.CurrentTarget);
		}

		private bool IsAttackingEventGolem()
		{
			// Native ELF VA 0x1440eac.
			return PlayerUnit.CurrentTarget != null && IsEventGolem(PlayerUnit.CurrentTarget);
		}

		public bool IsWeakEnemy(EnemyUnit enemy)
		{
			// Native ELF VA 0x14419bc.
			var config = enemy.Config;
			if (config.IsVip || config.IsSpecial || config.IsEvent || config.LocationNumber >= lastUnlockedLocationNumber) return false;
			if (!weakEnemyByLocation.TryGetValue(config.LocationNumber, out bool weak))
			{
			    weak = weakSpawnerExtraRangeStat.HasModifierForParam(config.LocationNumber);
			    weakEnemyByLocation[config.LocationNumber] = weak;
			}
			return weak;
		}

		public bool IsEventGolem(EnemyUnit enemy)
		{
			// Native ELF VA 0x1441acc.
			return enemy.Config.IsEvent && enemy.Config.EventDef != null && (int)enemy.Config.EventDef.UnitType == 0 && eventGolemExtraRangeStat.GetValue(0) > 0.0;
		}
	}
}
