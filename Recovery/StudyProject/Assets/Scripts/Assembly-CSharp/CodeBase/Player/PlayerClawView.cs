using Codebase.Abilities.Effects;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using UnityEngine;

namespace Codebase.Player
{
	// Pet mode: swings Pudge's right hand for the player's own attack item; added at runtime by PetAttackSystem.
	public class PlayerClawView : MonoBehaviour
	{
		private const string HandSlotKey = "weapon_1";

		private const float LongHandCastThreshold = 0.4f;

		private const float HandCastFastHalfDuration = 0.2f;

		private const float MaxHandSpeed = 4f;

		private static readonly int AttackType = Animator.StringToHash("AttackType");

		private PlayerModel playerModel;

		private Animator animator;

		private string attackAnimationKey;

		private string handSpeedKey;

		private float attackEndSpeed;

		private void Start()
		{
			playerModel = GameContext.Get<PlayerModel>();
			// Reuse the character animator and keys authored on the weapon_1 hand view.
			foreach (PlayerWeaponView view in GetComponentsInChildren<PlayerWeaponView>(true))
			{
				if (view.SlotKey != HandSlotKey || view.CharacterAnimator == null)
					continue;
				animator = view.CharacterAnimator;
				attackAnimationKey = view.AttackAnimationKey;
				handSpeedKey = view.HandSpeedKey;
				attackEndSpeed = view.AttackEndSpeed;
				break;
			}
		}

		private void Update()
		{
			if (!PetConfig.Enabled || animator == null || playerModel == null)
				return;
			ItemModel item = playerModel.PlayerAttackItem;
			if (item == null)
				return;
			if (!string.IsNullOrEmpty(handSpeedKey))
				animator.SetFloat(handSpeedKey, GetHandSpeed(item));
			if (item.JustStartedAttack && !string.IsNullOrEmpty(attackAnimationKey))
			{
				animator.SetFloat(AttackType, (float)ToAttackType(item.Config.DamageType));
				animator.SetTrigger(attackAnimationKey);
			}
		}

		private float GetHandSpeed(ItemModel item)
		{
			// Same curve as the weapon_1 hand in PlayerWeaponView.Update.
			if (global::Codebase.Skills.FriendlyDebuffs.IsStunned(playerModel.Unit))
				return 0f;
			float multiplier = 1f;
			foreach (var ability in item.AbilityStates)
				multiplier *= ability.GetWeaponStatMultiplier(WeaponBuffType.AttackSpeed);
			multiplier *= item.AbilityAttackSpeedMultiplier;
			multiplier /= global::Codebase.Skills.FriendlyDebuffs.AttackIntervalFactor(playerModel.Unit);
			float cooldown = item.Config.AttackCooldown;
			float attackStart = PlayerAttackSystem.GetAnimationCastPoint(item) * cooldown;
			float remaining = cooldown - attackStart;
			float hitPoint = PlayerWeaponView.GetAttackHitPoint(item.Config.DamageType);
			float speed = remaining <= multiplier * LongHandCastThreshold ? multiplier * hitPoint / remaining : (item.Cooldown <= multiplier * HandCastFastHalfDuration + attackStart ? hitPoint / LongHandCastThreshold : multiplier * hitPoint * .5f / (remaining - multiplier * HandCastFastHalfDuration));
			if (item.Cooldown <= cooldown * item.Config.AnimationBackPoint)
				speed = multiplier * attackEndSpeed;
			return Mathf.Min(speed, MaxHandSpeed);
		}

		private static WeaponAttackType ToAttackType(WeaponType damageType)
		{
			switch (damageType)
			{
				case WeaponType.Stabbing:
					return WeaponAttackType.Pierce;
				case WeaponType.Buckshot:
					return WeaponAttackType.Smash;
				default:
					return WeaponAttackType.Cut;
			}
		}
	}
}
