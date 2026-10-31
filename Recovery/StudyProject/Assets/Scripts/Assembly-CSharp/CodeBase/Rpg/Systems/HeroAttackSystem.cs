using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Unit;
using Codebase.Unit.Events;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Rpg.Systems
{
	public class HeroAttackSystem : SystemBase
	{
		[Inject] private PlayerModel playerModel;
		[Inject] private HeroRosterModel roster;
		[Inject] private global::Codebase.Skills.SkillService skillService;
		private Event<WeaponHitEvent> weaponHitEvent;

		public override void OnAwake() => weaponHitEvent = World.GetEvent<WeaponHitEvent>();

		public override void OnUpdate(float deltaTime)
		{
			PlayerUnit player = playerModel.Unit;
			if (player == null) return;
			HeroUnit leader = RpgControl.ControlledUnit(playerModel) as HeroUnit;
			bool paused = !playerModel.CanMove || player.IsSleeping;
			float engage = PetConfig.Instance.EngageRadius;
			foreach (HeroModel hero in roster.Heroes)
			{
				HeroUnit unit = hero.Unit;
				ItemModel item = hero.AttackItem;
				if (unit == null || item == null) continue;
				item.JustStartedAttack = false;
				if (!hero.IsAlive) { unit.SetAttackAnimation(false, 1f); continue; }
				if (skillService.IsStunned(unit)) { unit.SetAttackAnimation(false, 1f); continue; }
				if (skillService.IsCasting(unit)) { unit.SetAttackAnimation(skillService.IsInCastPoint(unit), 1f); continue; }
				float range = (float)unit.GetStatValue(StatType.AttackRange);
				EnemyUnit target = unit.CurrentTarget;
				if (paused) target = null;
				else if (unit.IsControlled)
				{
					if (!Valid(target) || HeroAi.FlatSqrDistance(unit.transform.position, target.transform.position) > range * range)
						target = HeroAi.FindNearest(unit.transform.position, range * range);
				}
				else if (leader != null) target = HeroAi.SelectFollowerTarget(unit, leader, engage * engage, target);
				else target = null;
				if (target != unit.CurrentTarget) { unit.CurrentTarget = target; hero.TargetOutOfRangeTimer = 0f; }
				hero.AttackTarget = target;
				bool inRange = Valid(target) && HeroAi.FlatSqrDistance(unit.transform.position, target.transform.position) <= range * range;
				if (inRange || !Valid(target)) hero.TargetOutOfRangeTimer = 0f;
				else hero.TargetOutOfRangeTimer += deltaTime;
				bool hasTarget = inRange || Valid(target) && hero.TargetOutOfRangeTimer < 0.2f;
				float speed = Mathf.Max(0f, (float)unit.GetStatValue(StatType.AttackSpeed)) / skillService.GetAttackIntervalFactor(unit);
				float cooldown = item.Config.AttackCooldown;
				if (cooldown <= 0f) continue;
				float threshold = item.Config.AnimationCastPoint * cooldown;
				if (hasTarget) hero.AttackCooldown -= speed * deltaTime;
				else if (hero.AttackCooldown > threshold)
				{
					hero.AttackCooldown += speed * deltaTime * 3f;
					if (hero.AttackCooldown >= cooldown) { hero.AttackCooldown = 0f; hero.AttackInProgress = false; }
				}
				else { hero.AttackInProgress = false; hero.AttackCooldown -= speed * deltaTime; }
				item.WearingTime += deltaTime;
				if (!paused && hasTarget && speed > 0f && item.WearingTime >= 1f)
				{
					if (hero.AttackCooldown <= 0f)
					{
						hero.AttackInProgress = true;
						item.JustStartedAttack = true;
						hero.AttackCooldown = cooldown;
					}
					else if (hero.AttackInProgress && hero.AttackCooldown <= threshold + speed * deltaTime)
					{
						hero.AttackInProgress = false;
						weaponHitEvent.NextFrame(new WeaponHitEvent { Source = unit, Target = target, Weapon = item, DamageType = item.Config.DamageType });
						PlayerAttackSystem old = player.GetComponent<PlayerAttackSystem>();
						if (old != null && old.HitVfx != null)
						{
							GameObject vfx = Object.Instantiate(old.HitVfx, target.transform.position + Vector3.up, Quaternion.identity);
							Object.Destroy(vfx, 3f);
						}
					}
				}
				item.Cooldown = hero.AttackCooldown;
				unit.SetAttackAnimation(!paused && hasTarget && hero.AttackCooldown > cooldown * 0.5f, speed / cooldown);
			}
		}

		private static bool Valid(EnemyUnit enemy) => enemy != null && !enemy.IsDead && !enemy.IsFar;
	}
}
