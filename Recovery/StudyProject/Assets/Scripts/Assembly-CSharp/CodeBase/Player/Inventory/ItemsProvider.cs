using System.Collections.Generic;
using System.Linq;
using Codebase.Pet;
using UnityEngine;

namespace Codebase.Player.Inventory
{
	public class ItemsProvider : ScriptableObject
	{
		public List<ItemConfig> Items;

		public List<Color> RarityColors;

		// Random pick filter: pet-only species drop only in pet mode.
		public static bool IsAvailable(ItemConfig item) => item != null;

		public ItemConfig Get(string key)
		{
			return Items.First(item => item.Key == key);
		}

		public Color GetRarityColor(Rarity rarity)
		{
			return RarityColors[(int)rarity];
		}

		public string GetRarityColorCode(Rarity rarity)
		{
			Color color = GetRarityColor(rarity);
			return string.Format("#{0:X2}{1:X2}{2:X2}", Mathf.RoundToInt(color.r * 255f), Mathf.RoundToInt(color.g * 255f), Mathf.RoundToInt(color.b * 255f));
		}
	}
}
