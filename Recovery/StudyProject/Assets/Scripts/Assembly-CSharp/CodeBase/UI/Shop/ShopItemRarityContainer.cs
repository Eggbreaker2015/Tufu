using Codebase.Core;
using System.Collections.Generic;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.UI.Shop
{
	public class ShopItemRarityContainer : BaseView
	{
		[SerializeField]
		private Transform content;

		[SerializeField]
		private ShopRarityItemView rarityItemPrefab;

		private readonly List<ShopRarityItemView> spawnedItems = new List<ShopRarityItemView>(); // Native ctor 0x162724c.

		public void SetupChestDrops(ChestDef chestDef)
		{
            // Native 0x1626cac: preserve every original drop entry, including duplicate rarities.
            Clear();
            foreach (var drop in chestDef.Drops) SpawnRarityItem(drop.Rarity);
		}

		private void SpawnRarityItem(Rarity rarity)
		{
            // Native 0x16270a8: list insertion precedes dependency injection/setup.
            var item = Instantiate(rarityItemPrefab, content);
            spawnedItems.Add(item);
            Injector.InjectInto(item);
            item.SetRarity(rarity);
            item.gameObject.SetActive(true);
		}

		private void Clear()
		{
            // Native 0x1626e70 deliberately destroys components, not their entire GameObjects.
            foreach (var item in spawnedItems) Destroy(item);
            spawnedItems.Clear();
		}
	}
}
