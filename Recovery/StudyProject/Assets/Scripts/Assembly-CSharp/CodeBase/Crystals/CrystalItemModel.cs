using Codebase.Core;
using Codebase.Unit;
using UniMob;
using UniMob.Core;

namespace Codebase.Crystals
{
	public class CrystalItemModel : Model
	{
		private readonly CrystalItemUd data;
		private ComputedAtom<int> __Id;
		private ComputedAtom<int> __Level;
		private ComputedAtom<int> __Variant;
		private ComputedAtom<CrystalRarity> __Rarity;
		private ComputedAtom<StatType> __BaseStatType;
		private ComputedAtom<double> __BaseStatValue;
		private ComputedAtom<int> __EnchantLevel;
		private ComputedAtom<bool> __IsProtected;
		private ComputedAtom<string> __EquippedSlotKey;
		private ComputedAtom<bool> __IsEquipped;
		private ComputedAtom<bool> __IsNew;
		private ComputedAtom<StatType> __SpecialStatType;
		private ComputedAtom<double> __SpecialStatValue;
		private ComputedAtom<bool> __HasSpecialStat;
		private ComputedAtom<LegendaryAbilityType> __LegendaryAbility;
		private ComputedAtom<bool> __HasLegendaryAbility;
		private ComputedAtom<bool> __IsLegendary;
		private ComputedAtom<double> __SecondaryStatValue;
		private ComputedAtom<StatType> __SecondaryStatType;
		private ComputedAtom<bool> __HasSecondaryStat;
		public int Id
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Id == null)
					__Id = CodeGenAtom.CreatePooled(this, null, () => Id, false);
				return __Id.DirectEvaluate() ? data.Id.Value : __Id.Value;
			}
		}

		public int Level
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Level == null)
					__Level = CodeGenAtom.CreatePooled(this, null, () => Level, false);
				return __Level.DirectEvaluate() ? data.Level.Value : __Level.Value;
			}
		}

		public int Variant
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Variant == null)
					__Variant = CodeGenAtom.CreatePooled(this, null, () => Variant, false);
				return __Variant.DirectEvaluate() ? data.Variant.Value : __Variant.Value;
			}
		}

		public CrystalRarity Rarity
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Rarity == null)
					__Rarity = CodeGenAtom.CreatePooled(this, null, () => Rarity, false);
				return __Rarity.DirectEvaluate() ? (CrystalRarity)data.Rarity.Value : __Rarity.Value;
			}
		}

		public StatType BaseStatType
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__BaseStatType == null)
					__BaseStatType = CodeGenAtom.CreatePooled(this, null, () => BaseStatType, false);
				return __BaseStatType.DirectEvaluate() ? (StatType)data.BaseStatType.Value : __BaseStatType.Value;
			}
		}

		public double BaseStatValue
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__BaseStatValue == null)
					__BaseStatValue = CodeGenAtom.CreatePooled(this, null, () => BaseStatValue, false);
				return __BaseStatValue.DirectEvaluate() ? data.BaseStatValue.Value : __BaseStatValue.Value;
			}
		}

		public int EnchantLevel
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EnchantLevel == null)
					__EnchantLevel = CodeGenAtom.CreatePooled(this, null, () => EnchantLevel, false);
				return __EnchantLevel.DirectEvaluate() ? data.EnchantLevel.Value : __EnchantLevel.Value;
			}
		}

		public bool IsProtected
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsProtected == null)
					__IsProtected = CodeGenAtom.CreatePooled(this, null, () => IsProtected, false);
				return __IsProtected.DirectEvaluate() ? data.IsProtected.Value : __IsProtected.Value;
			}
		}

		public string EquippedSlotKey
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EquippedSlotKey == null)
					__EquippedSlotKey = CodeGenAtom.CreatePooled(this, null, () => EquippedSlotKey, false);
				return __EquippedSlotKey.DirectEvaluate() ? data.EquippedSlotKey.Value : __EquippedSlotKey.Value;
			}
		}

		public bool IsEquipped
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsEquipped == null)
					__IsEquipped = CodeGenAtom.CreatePooled(this, null, () => IsEquipped, false);
				return __IsEquipped.DirectEvaluate() ? !string.IsNullOrEmpty(EquippedSlotKey) : __IsEquipped.Value;
			}
		}

		public bool IsNew
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsNew == null)
					__IsNew = CodeGenAtom.CreatePooled(this, null, () => IsNew, false);
				return __IsNew.DirectEvaluate() ? data.IsNew.Value : __IsNew.Value;
			}
		}

		public StatType SpecialStatType
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__SpecialStatType == null)
					__SpecialStatType = CodeGenAtom.CreatePooled(this, null, () => SpecialStatType, false);
				return __SpecialStatType.DirectEvaluate() ? (StatType)data.SpecialStatType.Value : __SpecialStatType.Value;
			}
		}

		public double SpecialStatValue
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__SpecialStatValue == null)
					__SpecialStatValue = CodeGenAtom.CreatePooled(this, null, () => SpecialStatValue, false);
				return __SpecialStatValue.DirectEvaluate() ? data.SpecialStatValue.Value : __SpecialStatValue.Value;
			}
		}

		public bool HasSpecialStat
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__HasSpecialStat == null)
					__HasSpecialStat = CodeGenAtom.CreatePooled(this, null, () => HasSpecialStat, false);
				return __HasSpecialStat.DirectEvaluate() ? data.SpecialStatType.Value >= 0 : __HasSpecialStat.Value;
			}
		}

		public LegendaryAbilityType LegendaryAbility
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__LegendaryAbility == null)
					__LegendaryAbility = CodeGenAtom.CreatePooled(this, null, () => LegendaryAbility, false);
				return __LegendaryAbility.DirectEvaluate() ? (LegendaryAbilityType)data.LegendaryAbility.Value : __LegendaryAbility.Value;
			}
		}

		public bool HasLegendaryAbility
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__HasLegendaryAbility == null)
					__HasLegendaryAbility = CodeGenAtom.CreatePooled(this, null, () => HasLegendaryAbility, false);
				return __HasLegendaryAbility.DirectEvaluate() ? LegendaryAbility != LegendaryAbilityType.None : __HasLegendaryAbility.Value;
			}
		}

		public bool IsLegendary
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsLegendary == null)
					__IsLegendary = CodeGenAtom.CreatePooled(this, null, () => IsLegendary, false);
				return __IsLegendary.DirectEvaluate() ? Rarity == CrystalRarity.Legendary : __IsLegendary.Value;
			}
		}

		public double SecondaryStatValue
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__SecondaryStatValue == null)
					__SecondaryStatValue = CodeGenAtom.CreatePooled(this, null, () => SecondaryStatValue, false);
				return __SecondaryStatValue.DirectEvaluate() ? data.SecondaryStatValue.Value : __SecondaryStatValue.Value;
			}
		}

		public StatType SecondaryStatType
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__SecondaryStatType == null)
					__SecondaryStatType = CodeGenAtom.CreatePooled(this, null, () => SecondaryStatType, false);
				return __SecondaryStatType.DirectEvaluate() ? (StatType)data.SecondaryStatType.Value : __SecondaryStatType.Value;
			}
		}

		public bool HasSecondaryStat
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__HasSecondaryStat == null)
					__HasSecondaryStat = CodeGenAtom.CreatePooled(this, null, () => HasSecondaryStat, false);
				return __HasSecondaryStat.DirectEvaluate() ? data.SecondaryStatType.Value >= 0 : __HasSecondaryStat.Value;
			}
		}

		public CrystalItemModel(Lifetime lifetime, CrystalItemUd data) : base(lifetime)
		{
			this.data = data;
		}

		public void SetProtected(bool value)
		{
			data.IsProtected.Value = value;
		}

		public void SetIsNew(bool value)
		{
			data.IsNew.Value = value;
		}

		public void MarkSeen()
		{
			data.IsNew.Value = false;
		}

		public void Equip(string slotKey)
		{
			data.EquippedSlotKey.Value = slotKey;
		}

		public void Unequip()
		{
			data.EquippedSlotKey.Value = string.Empty;
		}

		public void SetEnchantLevel(int level)
		{
			data.EnchantLevel.Value = level;
		}

		public void SetSpecialStat(StatType type, double value)
		{
			data.SpecialStatType.Value = (int)type;
			data.SpecialStatValue.Value = value;
		}

		public void SetSpecialStatValue(double value)
		{
			data.SpecialStatValue.Value = value;
		}

		public void SetBaseStatValue(double value)
		{
			data.BaseStatValue.Value = value;
		}

		public void SetSecondaryStat(StatType type, double value)
		{
			data.SecondaryStatType.Value = (int)type;
			data.SecondaryStatValue.Value = value;
		}

		public void SetSecondaryStatValue(double value)
		{
			data.SecondaryStatValue.Value = value;
		}
	}
}
