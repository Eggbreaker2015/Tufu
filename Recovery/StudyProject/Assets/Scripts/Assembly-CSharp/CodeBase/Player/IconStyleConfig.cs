using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Codebase.Player
{
	public class IconStyleConfig : ScriptableObject
	{
		public List<StatIconStyle> Styles;

		public List<ResistEfficiencyStyle> ResistStyles;

		public List<WeaponTypeStyle> WeaponTypeStyles;

		public List<WeaponTypeStyle> WeaponTypeMinimizedStyles;

		public List<WeaponTypeStyle> ArmorTypeMinimizedStyles;

		public List<ItemRarityStyle> ItemRarityStyles;

		public List<SlotTypeStyle> SlotTypeStyles;

		public MinimapStyle MinimapStyle;

		public StarsStyle StarsStyle;

		public Sprite GemsSprite;

		public Sprite VipPointsSprite;

		public Sprite LevelPassSprite;

		public Sprite NoAdsSprite;

		[FormerlySerializedAs("SoulOfferSprite")]
		public Sprite SoulPassSprite;

		[FormerlySerializedAs("CrystalOfferSprite")]
		public Sprite CrystalPassSprite;

		public Sprite ChaoticSoulOfferSprite;

		public Sprite ItemSlotUnlockSprite;

		public Sprite MineUnlockSprite;

		public void Fill()
		{
		}
	}
}
