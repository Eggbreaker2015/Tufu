using System;
using Codebase.Abilities.Effects;
using Codebase.Player.Inventory;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Abilities
{
	public class AbilityState
	{
		private AbilityDescriptionContext descriptionContext;

		public AbilityDef Def { get; private set; }

		public int Stacks { get; set; }

		public float Duration { get; set; }

		public float Cooldown { get; set; }

		public int HitsCounter { get; set; }

		public float MovedDistance { get; set; }

		public float DamageMultiplier { get; set; }

		public UnitCore CurrentTarget { get; set; }

		public bool AttackInProgress { get; set; }

		public bool GuaranteedCrit { get; set; }

		public float EmpoweredHit { get; set; }

		public bool DoubleLoot { get; set; }

		public float BonusChance { get; set; }

		public float BonusValue { get; set; }

		public int BonusStackLimit { get; set; }

		public float NextProcBonusChance { get; set; }

		public float NextProcBonusValue { get; set; }

		public int NextProcBonusStackLimit { get; set; }

		public float TemporaryDamageBonus { get; set; }

		public float TemporaryDamageDuration { get; set; }

		public float TemporaryAttackSpeedBonus { get; set; }

		public float TemporaryAttackSpeedDuration { get; set; }

		public float TemporaryCritBonus { get; set; }

		public float TemporaryCritDuration { get; set; }

		public float TemporaryCritChanceBonus { get; set; }

		public float TemporaryCritChanceDuration { get; set; }

		// Native ELF VA 0x15e2fe8: cache the description context on first access.
		public AbilityDescriptionContext DescriptionContext => descriptionContext ?? (descriptionContext = new AbilityDescriptionContext(this));

		private ItemModel ItemModel { get; set; }

		// Native ELF VA 0x15e306c.
		public int Stars => ItemModel.IsPreDivine ? 0 : ItemModel.Level;

		public bool HasNextUpgrade
		{
			get
			{
				// Native ELF VA 0x15e1590: a later configured tier, independent of affects flags.
				int stars = Stars;
				foreach (AbilityLevelScale scale in Def.LevelScaling)
					if (scale.Level > stars) return true;
				return false;
			}
		}

		public AbilityState(AbilityDef def, ItemModel itemModel)
		{
			// Native ELF VA 0x15e30ac.
			Def = def;
			ItemModel = itemModel;
			DamageMultiplier = 1f;
		}

		public float GetValue()
		{
			// Native ELF VA 0x15e30fc.
			return GetBaseValue() + BonusValue + NextProcBonusValue;
		}

		public float GetBaseValue()
		{
			// Native ELF VA 0x15e2440, tail to 0x15e3120.
			return GetCurrentOverride(scale => scale.ValueOverride, Def.Value, Def.AffectsValue);
		}

		public float CalculateScaledValue(float value)
		{
			// Native ELF VA 0x15e3368; preserve the approximate-zero branch and float32 order.
			float current = GetBaseValue();
			return Mathf.Approximately(Def.Value, 0f) ? value : (current / Def.Value) * value;
		}

		public int GetNextStarLevel()
		{
			// Native ELF VA 0x15e342c: nearest later tier, or the current star count.
			int stars = Stars, next = int.MaxValue;
			foreach (AbilityLevelScale scale in Def.LevelScaling)
				if (scale.Level > stars && scale.Level < next) next = scale.Level;
			return next == int.MaxValue ? stars : next;
		}

		public float GetNextStarValue()
		{
			// Native ELF VA 0x15e2534.
			return GetNextOverride(scale => scale.ValueOverride, Def.Value, Def.AffectsValue);
		}

		public float GetChance()
		{
			// Native ELF VA 0x15e3874.
			return GetBaseChance() + BonusChance + NextProcBonusChance;
		}

		public float GetBaseChance()
		{
			// Native ELF VA 0x15e2bc0.
			return GetCurrentOverride(scale => scale.ChanceOverride, Def.Chance, Def.AffectsChance);
		}

		public float GetNextStarChance()
		{
			// Native ELF VA 0x15e2cb4.
			return GetNextOverride(scale => scale.ChanceOverride, Def.Chance, Def.AffectsChance);
		}

		public int GetStackLimit()
		{
			// Native ELF VA 0x15e3898.
			return GetBaseStackLimit() + BonusStackLimit + NextProcBonusStackLimit;
		}

		public int GetBaseStackLimit()
		{
			// Native ELF VA 0x15e27c0: native conversion truncates toward zero.
			return (int)GetCurrentOverride(scale => scale.StacksOverride, Def.StackLimit, Def.AffectsStacks);
		}

		public int GetNextStarStackLimit()
		{
			// Native ELF VA 0x15e28d4.
			return (int)GetNextOverride(scale => scale.StacksOverride, Def.StackLimit, Def.AffectsStacks);
		}

		public int GetBaseMaxMarksCount()
		{
			// Native ELF VA 0x15e2a5c.
			return SumMaxMarksCount(GetBaseValue());
		}

		public int GetNextStarMaxMarksCount()
		{
			// Native ELF VA 0x15e2a74.
			return SumMaxMarksCount(GetNextStarValue());
		}

		private int SumMaxMarksCount(float abilityValue)
		{
			// Native ELF VA 0x15e38bc: count every IncreaseMaxMarksEffect; scaling is not applied here.
			int total = 0;
			foreach (var effect in Def.Effects)
				if (effect is IncreaseMaxMarksEffect marks)
					total += marks.UseAbilityValue ? (int)abilityValue : marks.Count;
			return total;
		}

		private float GetCurrentOverride(Func<AbilityLevelScale, float> selector, float baseValue, bool affects)
		{
			// Native ELF VA 0x15e3120; select greatest configured Level <= Stars.
			if (!affects) return baseValue;
			int stars = Stars, bestLevel = -1;
			float result = baseValue;
			foreach (AbilityLevelScale scale in Def.LevelScaling)
			    if (scale.Level <= stars && bestLevel < scale.Level)
			    {
			        result = selector(scale);
			        bestLevel = scale.Level;
			    }
			return result;
		}

		private float GetNextOverride(Func<AbilityLevelScale, float> selector, float baseValue, bool affects)
		{
			// Native ELF VA 0x15e360c: negative next overrides use the current tier instead.
			if (!affects) return baseValue;
			int stars = Stars, nextLevel = int.MaxValue;
			float result = -1f;
			foreach (AbilityLevelScale scale in Def.LevelScaling)
				if (scale.Level > stars && scale.Level < nextLevel)
				{
					result = selector(scale);
					nextLevel = scale.Level;
				}
			return result < 0f ? GetCurrentOverride(selector, baseValue, true) : result;
		}

		public float GetWeaponStatMultiplier(WeaponBuffType buffType)
		{
			// Native ELF VA 0x15e39f8; float32 operations preserve the native stack math.
			foreach (var effect in Def.Effects)
			    if (effect is WeaponStatStackEffect stack && stack.BuffType == buffType)
			        return (GetBaseValue() + BonusValue + NextProcBonusValue) * Stacks + 1f;
			return 1f;
		}
	}
}
