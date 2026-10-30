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
			if (global::Codebase.Rpg.RpgModeConfig.Enabled)
			{
				uiModel = GameContext.Get<UiModel>();
				separator.SetActive(false);
				global::Codebase.Rpg.UI.RpgLineupCell.CreateProtagonist(transform);
				foreach (ItemSlotModel slot in GameContext.Get<PlayerModel>().ItemSlots)
					if (slot.Key == "weapon_1" || slot.Key == "weapon_2" || slot.Key == "weapon_3" || slot.Key == "weapon_4") Instantiate(prefab, transform).Initialize(slot);
				canvasGroup.blocksRaycasts = true;
				return;
			}
			// Native ELF VA 0x15d6f94 and predicate 0x15f54a8.
			PlayerModel player = GameContext.Get<PlayerModel>();
			uiModel = GameContext.Get<UiModel>();
			foreach (ItemSlotModel slot in player.ItemSlots) Instantiate(prefab, transform).Initialize(slot);
			separator.transform.SetSiblingIndex(player.ItemSlots.FindLastIndex(slot => slot.SlotType == ItemSlotType.Weapon) + 1);
		}

		private void Update()
		{
			// Native ELF VA 0x15d735c; original desktop shortcuts and raycast block.
			if (global::Codebase.Rpg.RpgModeConfig.Enabled)
			{
				canvasGroup.blocksRaycasts = true;
				return;
			}
			else
			if (Input.GetKeyDown(KeyCode.Alpha1)) PressEquipButton((WeaponType)1, false);
			else if (Input.GetKeyDown(KeyCode.Alpha2)) PressEquipButton((WeaponType)0, false);
			else if (Input.GetKeyDown(KeyCode.Alpha3)) PressEquipButton((WeaponType)3, false);
			else if (Input.GetKeyDown(KeyCode.Alpha4)) PressEquipButton((WeaponType)4, true);
			if (canvasGroup.blocksRaycasts == uiModel.BlockItemsHotbar) canvasGroup.blocksRaycasts = !uiModel.BlockItemsHotbar;
		}

		private static void PressEquipButton(WeaponType weaponType, bool isEquipBest)
		{
			// Native ELF VA 0x15d7444, predicate 0x15f54cc.
			FindObjectsByType<InventoryEquipAllButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			    .First(button => button.MatchesShortcut(weaponType, isEquipBest)).Press();
		}
	}
}
