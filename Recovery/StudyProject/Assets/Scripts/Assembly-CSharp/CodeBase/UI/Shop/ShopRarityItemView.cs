using Codebase.Core;
using Codebase.Player.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ShopRarityItemView : MonoBehaviour
	{
		[SerializeField]
		private Image rarityIcon;

		[Inject]
		private ItemsProvider itemsProvider;

		public void SetRarity(Rarity rarity)
		{
            // Native 0x16271b8.
            rarityIcon.color = itemsProvider.RarityColors[(int)rarity];
		}
	}
}
