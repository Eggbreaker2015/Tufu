using UnityEngine;
using Codebase.Enemy;
using Codebase.Player;
using Codebase.Abilities.Effects;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Scellecs.Morpeh;

namespace Codebase.Abilities.Systems
{
	public class PiercingWeaponMarkSystem : SystemBase
	{
		private Event<WeaponHitEvent> hitEvent;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private Stash<MarkComponent> markStash;

		public override void OnAwake()
		{
            // Native 0x15ebbb0: bounded ARM64 assigns the actual scheduled hit stream.
            hitEvent = World.GetEvent<WeaponHitEvent>();
        }

		public override void OnUpdate(float deltaTime)
		{
            // Native 0x15ebc08: scheduled source/player, stabbing type, alive enemy and owned all-piercing effect gates.
            foreach (var hit in hitEvent.ScheduledChanges) {
                // Pet mode: pet hits also mark.
                if (!(hit.Source is PlayerUnit || PetConfig.Enabled && hit.Source is PetUnit) || hit.DamageType != WeaponType.Stabbing || !(hit.Target is EnemyUnit target) || target.IsDead || !HasPiercingMarkAbility()) continue;
                if (UnityEngine.Random.value < .1f + playerModel.BonusMarkChance) ApplyMarkEffect.ApplyMark(markStash, World, target, playerModel);
            }
        }

		private bool HasPiercingMarkAbility()
		{
            // Native 0x15ebe40: original nested ownership traversal, no substituted global flag.
            foreach (var item in playerModel.EquippedItems)
                foreach (var state in item.AbilityStates)
                    foreach (var effect in state.Def.Effects)
                        if (effect is ApplyMarkEffect mark && mark.AppliesToAllPiercingWeapons) return true;
            return false;
        }
	}
}
