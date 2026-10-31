using Codebase.UI.Inventory;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Core;
using System.Linq;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.UI
{
	public class ItemsHotbarView : MonoBehaviour
	{
		[SerializeField]
		private ItemSlotView prefab;

		[SerializeField]
		private GameObject separator;

		[SerializeField]
		private CanvasGroup canvasGroup;

		private UiModel uiModel;

		private void Start()
		{
			{
				uiModel = GameContext.Get<UiModel>();
				separator.SetActive(false);
				global::Codebase.Rpg.UI.RpgLineupCell.CreateProtagonist(transform);
				foreach (ItemSlotModel slot in GameContext.Get<PlayerModel>().ItemSlots)
					if (slot.Key == "weapon_1" || slot.Key == "weapon_2" || slot.Key == "weapon_3" || slot.Key == "weapon_4") Instantiate(prefab, transform).Initialize(slot);
				canvasGroup.blocksRaycasts = true;
				return;
			}
		}

		private void Update()
		{
			// Native ELF VA 0x15d735c; original desktop shortcuts and raycast block.
			{
				canvasGroup.blocksRaycasts = true;
				return;
			}
		}

		private static void PressEquipButton(WeaponType weaponType, bool isEquipBest)
		{
			// Native ELF VA 0x15d7444, predicate 0x15f54cc.
			FindObjectsByType<InventoryEquipAllButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			    .First(button => button.MatchesShortcut(weaponType, isEquipBest)).Press();
		}
	}
}
