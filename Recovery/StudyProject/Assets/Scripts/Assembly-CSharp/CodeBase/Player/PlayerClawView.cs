using Codebase.Abilities.Effects;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using UnityEngine;

namespace Codebase.Player
{
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

		public void SetAttackAnimation(bool attacking, float speed)
		{
			if (animator == null || playerModel == null) return;
			var hero = GameContext.Get<global::Codebase.Rpg.HeroRosterModel>().Get(playerModel.Unit);
			if (hero == null || hero.AttackItem == null) return;
			ItemModel item = hero.AttackItem;
			float cooldown = item.Config.AttackCooldown;
			float multiplier = speed * cooldown;
			float attackStart = item.Config.AnimationCastPoint * cooldown;
			float remaining = cooldown - attackStart;
			float hitPoint = PlayerWeaponView.GetAttackHitPoint(item.Config.DamageType);
			float handSpeed = remaining <= 0f ? multiplier * attackEndSpeed : remaining <= multiplier * LongHandCastThreshold ? multiplier * hitPoint / remaining : (item.Cooldown <= multiplier * HandCastFastHalfDuration + attackStart ? hitPoint / LongHandCastThreshold : multiplier * hitPoint * .5f / (remaining - multiplier * HandCastFastHalfDuration));
			if (item.Cooldown <= cooldown * item.Config.AnimationBackPoint) handSpeed = multiplier * attackEndSpeed;
			if (!string.IsNullOrEmpty(handSpeedKey)) animator.SetFloat(handSpeedKey, Mathf.Min(handSpeed, MaxHandSpeed));
			if (hero.AttackItem.JustStartedAttack && !string.IsNullOrEmpty(attackAnimationKey))
			{
				animator.SetFloat(AttackType, (float)ToAttackType(hero.AttackItem.Config.DamageType));
				animator.SetTrigger(attackAnimationKey);
			}
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
