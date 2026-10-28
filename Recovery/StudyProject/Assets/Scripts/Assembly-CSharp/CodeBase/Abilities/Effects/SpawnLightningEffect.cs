using CodeBase.Audio;
using Codebase.Player.Inventory;
using Codebase.Unit.Systems;
using Codebase.UI.Damage;
using Codebase.Camera;
using Codebase.Meta;
using Codebase.Core;
using Codebase.BossTower.Systems;
using Codebase.BossTower.Components;
using Codebase.Abilities.Vfx;
using Codebase.Abilities.Systems;
using Codebase.Abilities.Events;
using UnityEngine;
using System;
using Codebase.Abilities.Core;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Scellecs.Morpeh;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class SpawnLightningEffect : AbilityEffectBase
	{
		public float DamageMultiplier = 1f; // Native ctor 0x15f1f48.

		public override AbilityTrigger? IndependentTrigger => AbilityTrigger.OnHit; // Native 0x15f1698.

		public override void Execute(AbilityContext context)
		{
            // Native 0x15f16f8: nonpositive chance means unconditional, not disabled.
            float chance = context.State.GetChance() + context.PlayerModel.BonusLightningChance;
            if (chance > 0f && UnityEngine.Random.value >= chance) return;
            var target = AbilityTargetResolver.Resolve(context); // Pet mode: falls back to the attacker's target.
            if (target == null || target.IsDead) return;
            double damage = context.Damage * DamageMultiplier * (1f + context.PlayerModel.LightningDamageBonus);
            var meta = GameContext.Get<MetaStatsModel>();
            double empoweredChance = meta.Get("empowered_lightning").Value;
            bool empowered = empoweredChance > 0.0 && UnityEngine.Random.value < empoweredChance;
            if (empowered) damage *= 2.0 + meta.Get("lightning_damage").Value;
            var world = context.World;
            var died = world.GetEvent<UnitDiedEvent>();
            var vulnerability = world.GetStash<LightningVulnerabilityComponent>();
            var shock = world.GetStash<ShockComponent>();
            if (world.GetStash<VampireSpiritTag>().Has(target.EntityProvider.Entity)) GameContext.Get<VampireSpiritSystem>().ApplyChainLightning(damage, empowered);
            else ApplyLightningToUnit(target, damage, died, vulnerability, shock, empowered);
            GameContext.Get<AbilityExecuteEventSystem>().ProcessEventImmediate(new AbilityTriggerEvent {Trigger = AbilityTrigger.OnLightningHit, Target = target, Damage = damage, Item = context.Owner as ItemModel, Attacker = context.Attacker});
        }

		public static void ApplyLightningToUnit(UnitCore target, double damage, Event<UnitDiedEvent> unitDiedEvent, Stash<LightningVulnerabilityComponent> vulnStash, Stash<ShockComponent> shockStash, bool isEmpowered = false)
		{
            // Native 0x15f1ba4: damage is vulnerability-scaled before death, VFX and status work.
            Entity entity = target.EntityProvider.Entity;
            if (vulnStash.Has(entity)) damage *= vulnStash.Get(entity).DamageMultiplier;
            if (damage > 0.0) {
                bool wasDead = target.IsDead;
                target.CurrentHealth -= damage;
                target.LastDamageGet = Time.time;
                UnitDamageSystem.ProcessEnemyDeath(target, wasDead, unitDiedEvent);
            }
            GameContext.Get<AbilityVfxService>().Spawn(isEmpowered ? AbilityVfxConstants.MegaLightning : AbilityVfxConstants.ShockLightning, target);
            GameContext.Get<IAudioPlayer>().PlaySoundOnce("spawn_lightning");
            GameContext.Get<EnemyDamageEventsViewSystem>().CreateStrike(damage, isEmpowered);
            if (isEmpowered) GameContext.Get<CameraFollowSystem>().Shake(.15f, .21f);
            ShockComponent shock = shockStash.Get(entity, out bool exists);
            if (!exists) shock = default;
            shock.IsShocked = true; shock.ShockedTimer = 10f; shock.ConsumedDecayTimer = 10f;
            shockStash.Set(entity, shock);
        }
	}
}
