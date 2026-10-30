using Codebase.Utilities;
using System;
using System.Collections.Generic;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using TMPro;
using UniMob;
using UnityEngine;

namespace Codebase.UI.Inventory
{
	public class InventoryCategoryBlock : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text titleText;

		[SerializeField]
		private Transform itemsGrid;

		[SerializeField]
		private InventoryItemView itemPrefab;

		private ViewContainer<InventoryItemView, ItemModel> itemsContainer;

		private Func<IEnumerable<ItemModel>> currentSource;

		public void Setup(Lifetime lifetime, string titleLocKey, Func<IEnumerable<ItemModel>> itemsSource)
		{
			// Native ELF VA 0x164c468; pooled category keeps its original source delegate binding.
			titleText.text = LocalizationStrings.Get(titleLocKey);
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) global::Codebase.UI.PetStatsText.SetText(titleText, "英雄");
			currentSource = itemsSource;
			if (itemsContainer != null) { itemsContainer.ForceRebuild(); return; }
			itemsContainer = new ViewContainer<InventoryItemView, ItemModel>(itemsGrid, itemPrefab)
			    .BindTo(lifetime, () => currentSource())
			    .Setup((view, item) => view.Initialize(item));
		}
	}
}
