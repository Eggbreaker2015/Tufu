using Codebase.Player.Inventory;
using Codebase.Rage;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using Codebase.Abilities.Events;
using Codebase.Core;
using Codebase.Crystals;
using Codebase.Player.Models;
using Codebase.Unit;
using Codebase.Unit.Events;
using JetBrains.Annotations;
using Scellecs.Morpeh;
using Codebase.Abilities.Effects;
using Codebase.Meta;
using Codebase.Pet;

namespace Codebase.Abilities.Systems
{
	public class AbilityExecuteEventSystem : SystemBase
	{
		private Event<AbilityTriggerEvent> abilityEvent;
		private Event<WeaponHitEvent> weaponHitEvent;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private CrystalModel crystalModel;
		private UnitCore lastUnit;
		private readonly Stack<List<IAbilityOwner>> ownerListsPool;
		public AbilityExecuteEventSystem()
		{
			// Native ELF VA 0x15eb60c.
			ownerListsPool = new Stack<List<IAbilityOwner>>(2);
		}

		public override void OnAwake()
		{
			// Native ELF VA 0x15e82d8.
			abilityEvent = World.GetEvent<AbilityTriggerEvent>();
			weaponHitEvent = World.GetEvent<WeaponHitEvent>();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Native ELF VA 0x15e8368; preserve original stage order.
			ResetPerFrameState(deltaTime);
			ApplyCrystalLegendaryEffects();
			UpdatePassiveAbilities(deltaTime);
			ProcessTriggeredAbilities();
			SyncStatModifiers();
			GameContext.Get<RageModel>().Tick(deltaTime);
		}

		private void ResetPerFrameState(float deltaTime)
		{
			// Native ELF VA 0x15e8420. These are tick factors, not serialized ctor defaults.
			foreach (ItemModel item in playerModel.EquippedItems)
			{
				item.AbilityDamageMultiplier = 1f;
				item.AbilityDamageFlat = 0f;
				item.AbilityAttackSpeedMultiplier = 1f;
				item.AbilityCritMultiplier = 1f;
				item.AbilityCritChanceBonus = 0f;
				item.AbilitySuperCritChanceBonus = 0f;
				item.AbilitySuperCritDamageBonus = 0f;
				foreach (AbilityState state in item.AbilityStates)
				{
					state.DamageMultiplier = 1f;
					state.BonusValue = 0f;
					state.BonusStackLimit = 0;
					state.BonusChance = 0f;
					state.Cooldown -= deltaTime;
				}
			}

			// Pet mode: the player's own attack item is never equipped, so its tick factors are reset here.
			if (PetConfig.Enabled && playerModel.PlayerAttackItem != null)
				ResetPlayerAttackItem(playerModel.PlayerAttackItem, deltaTime);

			playerModel.AbilityHealthRegenMultiplier = 1f;
			playerModel.AbilityDamageMultiplier = 1f;
			playerModel.AbilityDamageFlat = 0f;
			playerModel.LethalIgnoreChance = 0f;
			playerModel.HasPerTargetLethalIgnore = false;
			playerModel.ThornsDamage = 0f;
			playerModel.BonusDamageToStunned = 0f;
			playerModel.TreatAsAllSlotsEmpty = false;
			playerModel.LightningDamageBonus = 0f;
			playerModel.BonusLightningChance = 0f;
			playerModel.BonusMarkChance = 0f;
			playerModel.BonusMaxMarks = 0;
			playerModel.DamagePerConsumedShockOnTarget = 0f;
		}

		private static void ResetPlayerAttackItem(ItemModel item, float deltaTime)
		{
			item.AbilityDamageMultiplier = 1f;
			item.AbilityDamageFlat = 0f;
			item.AbilityAttackSpeedMultiplier = 1f;
			item.AbilityCritMultiplier = 1f;
			item.AbilityCritChanceBonus = 0f;
			item.AbilitySuperCritChanceBonus = 0f;
			item.AbilitySuperCritDamageBonus = 0f;
			foreach (AbilityState state in item.AbilityStates)
			{
				state.DamageMultiplier = 1f;
				state.BonusValue = 0f;
				state.BonusStackLimit = 0;
				state.BonusChance = 0f;
				state.Cooldown -= deltaTime;
			}
		}

		private void ApplyCrystalLegendaryEffects()
		{
			// Native ELF VA 0x15e863c.
			RageModel rage = GameContext.Get<RageModel>();
			int rageLevel = rage == null ? 0 : rage.ActiveRageLevel;
			float damagePerRage = 0f;
			foreach (CrystalItemModel crystal in crystalModel.Crystals)
			{
				if (!crystal.IsEquipped || !crystal.HasLegendaryAbility) continue;
				double value = CrystalBalance.GetLegendaryAbilityValue(crystalModel.Config, crystal);
				switch (crystal.LegendaryAbility)
				{
					case LegendaryAbilityType.MaxMarksPerEnchant: playerModel.BonusMaxMarks += (int)value; break;
					case LegendaryAbilityType.DamagePerRageLevel: damagePerRage += (float)value; break;
					case LegendaryAbilityType.HammerLightningChancePerEnchant: playerModel.BonusLightningChance += (float)value / 100f; break;
				}
			}
			if (rageLevel > 0 && damagePerRage > 0f)
				playerModel.AbilityDamageMultiplier = (damagePerRage * rageLevel / 100f + 1f) * playerModel.AbilityDamageMultiplier;
		}

		private void UpdatePassiveAbilities(float deltaTime)
		{
			// Native ELF VA 0x15e89a0: boost pass precedes the regular pass for each owner.
			foreach (ItemModel item in playerModel.EquippedItems)
			{
				UpdatePassiveEffectsForOwner(item, deltaTime, true);
				UpdatePassiveEffectsForOwner(item, deltaTime, false);
			}
		}

		private void UpdatePassiveEffectsForOwner(IAbilityOwner owner, float deltaTime, bool boostsOnly)
		{
			// Native ELF VA 0x15e8cc8. Passive definitions do not require HasPassiveTick.
			foreach (AbilityState state in owner.AbilityStates)
			{
				AbilityContext context = RentContext(owner, state, deltaTime);
				bool passive = state.Def.Trigger == AbilityTrigger.Passive;
				bool active = passive && EvaluateConditions(state.Def, context);
				foreach (IAbilityEffect effect in state.Def.Effects)
				{
					if (effect.IndependentTrigger.HasValue || (effect is IAbilityBoostEffect) != boostsOnly) continue;
					if (active) effect.UpdatePassive(context, deltaTime);
					else if (passive)
					{
						if (!boostsOnly) effect.OnPassiveInactive(context);
					}
					else if (effect.HasPassiveTick) effect.UpdatePassive(context, deltaTime);
				}
				ReleaseContext(context);
			}
		}

		private void ProcessTriggeredAbilities()
		{
			// Native ELF VA 0x15e8a7c: process the scheduled event snapshot for this frame.
			foreach (AbilityTriggerEvent data in abilityEvent.ScheduledChanges) ProcessTriggerEvent(data);
		}

		private void ProcessTriggerEvent(AbilityTriggerEvent data)
		{
			// Native ELF VA 0x15e9450; retain the owner snapshot across effect-driven equipment changes.
			List<IAbilityOwner> owners = RentOwnerList();
			List<ItemModel> equipped = playerModel.EquippedItems;
			float economyBonus = ComputeEconomyProcBonus();
			foreach (ItemModel item in equipped) owners.Add(item);
			if (data.Item != null && !owners.Contains(data.Item)) owners.Add(data.Item);
			if (data.ItemUnequipped && data.Item != null) DeactivatePassiveEffects(data.Item);
			foreach (IAbilityOwner owner in owners)
			{
				if (!owner.HasAbility) continue;
				if (ShouldProcessForOwner(owner, data)) ProcessIndependentEffects(owner, data);
				foreach (AbilityState state in owner.AbilityStates)
				{
					if (state.Def.Trigger != data.Trigger || !ShouldProcessForOwner(owner, data)) continue;
					AbilityContext context = RentContext(owner, state, data);
					RecomputeBoostsForTrigger(owner, state, data);
					if (economyBonus > 0f && IsEconomyAbility(state.Def)) state.BonusChance += economyBonus;
					if (EvaluateConditions(state.Def, context))
					{
						ExecuteEffects(state.Def, context);
						// Pet mode: remember the attacker's target; with pet mode off this is the player's target.
						state.CurrentTarget = AbilityTargetResolver.ResolveAttackerTarget(context);
					}
					ReleaseContext(context);
				}
			}
			ReleaseOwnerList(owners);
		}

		private void RecomputeBoostsForTrigger(IAbilityOwner owner, AbilityState targetState, AbilityTriggerEvent data)
		{
			// Native ELF VA 0x15ea284: only boosts addressing this owner's target state index.
			int index = owner.AbilityStates.IndexOf(targetState);
			if (index < 0) return;
			targetState.BonusValue = 0f;
			targetState.BonusStackLimit = 0;
			targetState.BonusChance = 0f;
			foreach (AbilityState state in owner.AbilityStates)
				foreach (IAbilityEffect effect in state.Def.Effects)
					if (effect is IAbilityBoostEffect boost && boost.TargetAbilityIndex == index && !effect.IndependentTrigger.HasValue)
					{
						AbilityContext context = RentContext(owner, state, data);
						if (state.Def.Trigger != AbilityTrigger.Passive || EvaluateConditions(state.Def, context)) effect.UpdatePassive(context, 0f);
						ReleaseContext(context);
					}
		}

		private void DeactivatePassiveEffects(IAbilityOwner owner)
		{
			// Native ELF VA 0x15e9a28.
			foreach (AbilityState state in owner.AbilityStates)
			{
				if (state.Def.Trigger != AbilityTrigger.Passive) continue;
				AbilityContext context = RentContext(owner, state);
				foreach (IAbilityEffect effect in state.Def.Effects)
					if (!effect.IndependentTrigger.HasValue) effect.OnPassiveInactive(context);
				ReleaseContext(context);
			}
		}

		private void ProcessIndependentEffects(IAbilityOwner owner, AbilityTriggerEvent data)
		{
			// Native ELF VA 0x15e9d54: independent triggers bypass the definition's conditions.
			foreach (AbilityState state in owner.AbilityStates)
				foreach (IAbilityEffect effect in state.Def.Effects)
					if (effect.IndependentTrigger.HasValue && effect.IndependentTrigger.Value == data.Trigger)
					{
						AbilityContext context = RentContext(owner, state, data);
						effect.Execute(context);
						ReleaseContext(context);
					}
		}

		private bool ShouldProcessForOwner(IAbilityOwner owner, AbilityTriggerEvent data)
		{
			// Native ELF VA 0x15e9d1c; source-local trigger mask is 0xC0115.
			uint trigger = (uint)data.Trigger;
			return ReferenceEquals(owner, data.Item) || trigger >= 20 || ((0x3feeaU >> (int)trigger) & 1U) != 0;
		}

		private bool EvaluateConditions(AbilityDef def, AbilityContext context)
		{
			// Native ELF VA 0x15e920c; empty/null conditions succeed for both modes.
			if (def.Conditions == null || def.Conditions.Count == 0) return true;
			if ((int)def.ConditionMode == 0)
			{
				foreach (IAbilityCondition condition in def.Conditions) if (!condition.Evaluate(context)) return false;
				return true;
			}
			foreach (IAbilityCondition condition in def.Conditions) if (condition.Evaluate(context)) return true;
			return false;
		}

		private void ExecuteEffects(AbilityDef def, AbilityContext context)
		{
			// Native ELF VA 0x15ea7fc.
			if (def.Effects == null) return;
			foreach (IAbilityEffect effect in def.Effects)
				if (!effect.IndependentTrigger.HasValue) effect.Execute(context);
			context.State.NextProcBonusChance = 0f;
			context.State.NextProcBonusValue = 0f;
			context.State.NextProcBonusStackLimit = 0;
		}

		private AbilityContext RentContext(IAbilityOwner owner, AbilityState state, float deltaTime = 0f)
		{
			// Native ELF VA 0x15e9130.
			AbilityContext context = AbilityContext.Rent();
			context.World = World;
			context.Owner = owner;
			context.State = state;
			context.PlayerModel = playerModel;
			context.DeltaTime = deltaTime;
			context.Trigger = state.Def.Trigger;
			return context;
		}

		private AbilityContext RentContext(IAbilityOwner owner, AbilityState state, AbilityTriggerEvent data)
		{
			// Native ELF VA 0x15ea178.
			AbilityContext context = AbilityContext.Rent();
			context.World = World;
			context.Owner = owner;
			context.State = state;
			context.PlayerModel = playerModel;
			context.Target = data.Target;
			context.EnemySpawner = data.EnemySpawner;
			context.Damage = data.Damage;
			context.Trigger = data.Trigger;
			context.SourceItem = data.Item;
			context.ItemUnequipped = data.ItemUnequipped;
			context.Attacker = data.Attacker; // Pet mode: new field.
			return context;
		}

		private List<IAbilityOwner> RentOwnerList()
		{
			// Native ELF VA 0x15e98c0.
			return ownerListsPool.Count > 0 ? ownerListsPool.Pop() : new List<IAbilityOwner>(8);
		}

		private void ReleaseOwnerList(List<IAbilityOwner> list)
		{
			// Native ELF VA 0x15ea9e4.
			list.Clear();
			ownerListsPool.Push(list);
		}

		private void ReleaseContext(AbilityContext context)
		{
			// Native ELF VA 0x15e93f8.
			AbilityContext.Release(context);
		}

		private void SyncStatModifiers()
		{
			// Native ELF VA 0x15e8bcc.
			UnitCore unit = playerModel.Unit;
			if (unit == null) return;
			if (lastUnit != unit)
			{
				AddStatPassiveEffect.ClearOnUnitChange();
				lastUnit = unit;
			}
			AddStatPassiveEffect.SyncModifiers(unit);
		}

		[PublicAPI]
		public void FireEvent(AbilityTriggerEvent data)
		{
			// Native ELF VA 0x15eb50c.
			if (abilityEvent == null)
				abilityEvent = World.GetEvent<AbilityTriggerEvent>();
			abilityEvent.NextFrame(data);
		}

		[PublicAPI]
		public void ProcessEventImmediate(AbilityTriggerEvent data)
		{
			ProcessTriggerEvent(data);
		}

		private float ComputeEconomyProcBonus()
		{
			// Native ELF VA 0x15e997c, identical lookup to the original description helper.
			MetaStat stat = GameContext.Get<MetaStatsModel>()?.Get("economy_item_abilities_chance");
			return stat == null ? 0f : (float)stat.Value;
		}

		private static bool IsEconomyAbility(AbilityDef def)
		{
			// Native ELF VA 0x15ea664 matches 0x15e21a0 exactly in condition/effect classification.
			return AbilityDescriptionContext.IsEconomyAbility(def);
		}
	}
}
