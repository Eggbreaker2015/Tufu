using System.Collections.Generic;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Crystals
{
	[CreateAssetMenu(menuName = "Devour/CrystalStyleConfig")]
	public class CrystalStyleConfig : ScriptableObject
	{
		public List<CrystalVariantStyle> Variants = new List<CrystalVariantStyle>();

		public List<EnchantmentIconStyle> EnchantmentIcons = new List<EnchantmentIconStyle>();

		public List<RarityBackgroundStyle> RarityBackgrounds = new List<RarityBackgroundStyle>();

		public Sprite GetCrystalSprite(StatType baseStat, int enchantLevel)
		{
            // Native ELF0x152a118.
            return GetCrystalSprite(CrystalColors.GetForStat(baseStat), enchantLevel);
        }

		public Sprite GetCrystalSprite(CrystalColor color, int enchantLevel)
		{
            // Native ELF0x152a148.
            if (Variants.Count == 0) return null;
            CrystalVariantStyle variant = null;
            foreach (var entry in Variants) if (entry.Color == color) { variant = entry; break; }
            if (variant == null) variant = Variants[0];
            if (variant.SpritesByLevel == null || variant.SpritesByLevel.Count == 0) return null;
            int index = enchantLevel < 1 ? 0 : Mathf.Min(enchantLevel - 1, variant.SpritesByLevel.Count - 1);
            return variant.SpritesByLevel[index];
        }

		public Sprite GetEnchantmentIcon(int enchantLevel)
		{
            // Native ELF0x152a3dc.
            foreach (var style in EnchantmentIcons) if (style.Level == enchantLevel) return style.Sprite;
            return null;
        }

		public Sprite GetRarityBackground(CrystalRarity rarity)
		{
            // Native ELF0x152a59c.
            foreach (var style in RarityBackgrounds) if (style.Rarity == rarity) return style.Sprite;
            return null;
        }

		public Color GetRarityColor(CrystalRarity rarity)
		{
            // Native ELF0x152a75c.
            foreach (var style in RarityBackgrounds) if (style.Rarity == rarity) return style.Color;
            return Color.white;
        }

		public string GetRarityColorCode(CrystalRarity rarity)
		{
            // Native ELF0x152a954.
            Color color = GetRarityColor(rarity);
            return string.Format("#{0:X2}{1:X2}{2:X2}", Mathf.RoundToInt(color.r * 255f), Mathf.RoundToInt(color.g * 255f), Mathf.RoundToInt(color.b * 255f));
        }
	}
}
