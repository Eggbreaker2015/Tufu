using Codebase.Core;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Rage;
using Scellecs.Morpeh;
using Codebase.Player.Inventory;
using Codebase.Abilities;
using Codebase.Abilities.Events;
using Codebase.Abilities.Systems;
using Codebase.Pet;

namespace Codebase.Player.Systems
{
	public class RageExtraHitSystem : SystemBase
	{
		private Event<RageGainedEvent> rageGainedEvent;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private MetaStatsModel metaStatsModel;
		private float accumulatedRage;
		public override void OnAwake()
		{
			rageGainedEvent = World.GetEvent<RageGainedEvent>();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (RageGainedEvent data in rageGainedEvent.BatchedChanges)
			{
				double threshold = metaStatsModel.Get("rage_extra_hit").Value;
				if (!(threshold > 0.0 && data.Amount > 0f))
					continue;
				accumulatedRage += data.Amount;
				while (accumulatedRage >= threshold)
				{
					accumulatedRage -= (float)threshold;
					ResetHighestDamageWeaponCooldown();
				}
			}
		}

		private void ResetHighestDamageWeaponCooldown()
		{
			// Pet mode: only living pets compete, and the player's own attack also gets the extra hit.
			bool petMode = PetConfig.Enabled;
			PetsModel petsModel = petMode ? GameContext.Get<PetsModel>() : null;
			if (petMode && playerModel.PlayerAttackItem != null)
				playerModel.PlayerAttackItem.ResetCooldown = true;
			ItemModel best = null;
			double bestDamage = double.MinValue;
			foreach (ItemModel item in playerModel.EquippedWeapons)
			{
				if (petMode && !petsModel.IsPetAlive(item))
					continue;
				if (item.Damage > bestDamage)
				{
					bestDamage = item.Damage;
					best = item;
				}
			}

			if (best == null)
				return;
			best.ResetCooldown = true;
			GameContext.Get<AbilityExecuteEventSystem>().ProcessEventImmediate(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnCooldownReset, Item = best });
		}
	}
}
