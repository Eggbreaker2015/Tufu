using System.Linq;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Enemy;
using Codebase.Player;
using Codebase.Player.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Inventory
{
	public class InventoryEquipAllButton : View
	{
		[SerializeField]
		private WeaponType WeaponType;

		[SerializeField]
		private WeaponType ArmorType;

		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject locker;

		[SerializeField]
		private Image weaponTypeImage;

		[SerializeField]
		private bool isEquipBest;

		[SerializeField]
		private Sprite equipBestSprite;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IconStyleConfig style;

		protected override void Awake()
		{
			// Native ELF VA 0x164c78c / 0x164dd0c.
			base.Awake();
			button.onClick.AddListener(() => { EquipWeapons(); EquipArmors(); });
		}

		public bool MatchesShortcut(WeaponType weaponType, bool isEquipBest)
		{
			// Native ELF VA 0x164c828.
			return this.isEquipBest == isEquipBest && (isEquipBest || WeaponType == weaponType);
		}

		public void Press()
		{
			// Native ELF VA 0x164c854.
			button.onClick.Invoke();
		}

		private void EquipWeapons()
		{
			// Native ELF VA 0x164c878 and original LINQ selectors 0x164dd24/0x164de58/0x164de80.
			foreach (var item in playerModel.EquippedWeapons.ToList())
			    if (!playerModel.ItemSlots.First(slot => slot.ItemModel == item).IsPinned) playerModel.UnequipItem(item);
			foreach (var item in playerModel.UnequippedItems.Where(item => !item.IsArmor)
			    .OrderByDescending(item => isEquipBest || item.Config.DamageType == WeaponType).ThenByDescending(item => item.DPS))
			    if (!playerModel.EquipItem(item)) break;
		}

		private void EquipArmors()
		{
			// Native ELF VA 0x164cffc; separately fill helmets and shields, retaining pinned slots.
			foreach (var item in playerModel.EquippedArmors.ToList())
			    if (!playerModel.ItemSlots.First(slot => slot.ItemModel == item).IsPinned) playerModel.UnequipItem(item);
			var items = playerModel.UnequippedItems.Where(item => item.IsArmor)
			    .OrderByDescending(item => isEquipBest || item.Config.DamageType == ArmorType).ThenByDescending(item => item.Armor).ToList();
			foreach (var item in items.Where(item => item.Config.SlotType == Codebase.Player.Inventory.ItemSlotType.Helmet)) if (!playerModel.EquipItem(item)) break;
			foreach (var item in items.Where(item => item.Config.SlotType == Codebase.Player.Inventory.ItemSlotType.Shield)) if (!playerModel.EquipItem(item)) break;
		}

		private void OnEnable()
		{
			// Native ELF VA 0x164db4c.
			Initialize();
		}

		protected override void Render()
		{
			// Native ELF VA 0x164db58. Availability deliberately uses WeaponType even for the equip-best button.
			bool available = playerModel.Items.Any(item => item.Config.DamageType == WeaponType);
			button.interactable = available; locker.SetActive(!available);
			weaponTypeImage.sprite = isEquipBest ? equipBestSprite : style.WeaponTypeStyles.First(icon => icon.WeaponType == WeaponType).Sprite;
		}
	}
}
