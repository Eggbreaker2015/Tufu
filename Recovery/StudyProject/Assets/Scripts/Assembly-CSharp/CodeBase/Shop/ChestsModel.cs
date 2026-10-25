using Codebase.Core;
using Codebase.Crystals;
using Codebase.Meta;
using Codebase.Player.Inventory;
using Codebase.Unit;
using Codebase.VIP;
using UniMob;
using UniMob.Core;
using UnityEngine;
using System;

namespace Codebase.Shop
{
	public class ChestsModel : Model
	{
		private const int FallbackLevel = 1;
		private const float ArmorCountDivider = 1.5f;
		private const int CrystalBlacksmithBonusValue = 3;
		private const int LegendaryArmorUnlockLocation = 14;
		private const string VipModifierName = "vip_chest_level";
		private const float EquipmentLegendaryBaseChance = 0.095f;
		private const float EquipmentLegendaryChancePerLevel = 0.005f;
		private const float CrystalLegendaryBaseChance = 0.035f;
		private const float CrystalLegendaryChancePerLevel = 0.005f;
		[Inject]
		private VipModel vipModel;
		[Inject]
		private MetaStatsModel metaStatsModel;
		private StatModifier vipModifier;
		private ComputedAtom<int> __Level;
		private ComputedAtom<float> __EquipmentLegendaryChance;
		private ComputedAtom<float> __CrystalLegendaryChance;
		public int Level
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Level == null)
					__Level = CodeGenAtom.CreatePooled(this, null, () => Level, false);
				if (!__Level.DirectEvaluate())
					return __Level.Value;
				var stat = metaStatsModel.Get("chest_level");
				return stat == null ? 1 : (int)stat.Value;
			}
		}

		public float EquipmentLegendaryChance
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EquipmentLegendaryChance == null)
					__EquipmentLegendaryChance = CodeGenAtom.CreatePooled(this, null, () => EquipmentLegendaryChance, false);
				if (!__EquipmentLegendaryChance.DirectEvaluate())
					return __EquipmentLegendaryChance.Value;
				return GetEquipmentLegendaryChance(Level);
			}
		}

		public float CrystalLegendaryChance
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CrystalLegendaryChance == null)
					__CrystalLegendaryChance = CodeGenAtom.CreatePooled(this, null, () => CrystalLegendaryChance, false);
				if (!__CrystalLegendaryChance.DirectEvaluate())
					return __CrystalLegendaryChance.Value;
				return GetCrystalLegendaryChance(Level);
			}
		}

		public int CrystalBlacksmithBonus => unchecked((int)3L);
		public int LegendaryArmorUnlockLocationNumber => unchecked((int)14L);

		public int CrystalChestEnchantLevel
		{
			get
			{
				var stat = metaStatsModel.Get("crystal_chest_ench_level");
				return stat == null ? 0 : (int)stat.Value;
			}
		}

		public ChestsModel(Lifetime lifetime) : base(lifetime)
		{
		}

		public void Populate()
		{
			Atom.Reaction(Lifetime, () => vipModel.Level, ApplyVipModifier);
		}

		public float GetEquipmentLegendaryChance(int level)
		{
			return Mathf.Clamp01(EquipmentLegendaryBaseChance + level * EquipmentLegendaryChancePerLevel);
		}

		public float GetCrystalLegendaryChance(int level)
		{
			return Mathf.Clamp01(CrystalLegendaryBaseChance + level * CrystalLegendaryChancePerLevel);
		}

		public float GetRareChestRarityChance(Rarity rarity, int level)
		{
			float chance = GetEquipmentLegendaryChance(level);
			return rarity == Rarity.Legendary ? chance : 1f - chance;
		}

		public float GetCrystalChestRarityChance(Rarity rarity, int level)
		{
			float chance = GetCrystalLegendaryChance(level);
			return rarity == Rarity.Legendary ? chance : 1f - chance;
		}

		private void ApplyVipModifier(int vipLevel)
		{
			var stat = metaStatsModel.Get("chest_level");
			if (stat == null)
				return;
			if (vipModifier != null)
			{
				stat.RemoveModifier(vipModifier);
				vipModifier = null;
			}

			if (vipLevel > 0)
			{
				vipModifier = new StatModifier
				{
					Name = VipModifierName,
					ModifierType = ModifierType.Flat,
					Source = ModifierSource.Vip,
					Value = vipLevel
				};
				stat.AddModifier(vipModifier);
			}
		}

		public bool IsRareChest(string chestKey)
		{
			return chestKey == "royal" || chestKey == "royal_armor";
		}

		public bool IsCrystalChest(string chestKey)
		{
			return chestKey == "crystal";
		}

		public int GetEquipmentDropCopies(string chestKey, ItemType itemType)
		{
			return GetEquipmentDropCopies(chestKey, itemType, Level);
		}

		public int GetEquipmentDropCopies(string chestKey, ItemType itemType, int level)
		{
			int copies = IsRareChest(chestKey) ? unchecked(level + 4) : unchecked(level * 2 + 8);
			return itemType == ItemType.Armor ? (int)Math.Ceiling((float)copies / ArmorCountDivider) : copies;
		}

		public Rarity RollRareEquipmentRarity()
		{
			float roll = UnityEngine.Random.value;
			return roll < EquipmentLegendaryChance ? Rarity.Legendary : Rarity.Epic;
		}

		public CrystalRarity RollCrystalRarity()
		{
			float roll = UnityEngine.Random.value;
			return roll < CrystalLegendaryChance ? CrystalRarity.Legendary : CrystalRarity.Epic;
		}
	}
}
