using Codebase.Utilities;
using Codebase.Unit;
using Codebase.Player;
using System.Linq;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Inventory
{
	public class InventoryItemContextMenu : View
	{
		[SerializeField]
		private ItemAbilitiesView abilitiesView;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button evolveButton;

		[SerializeField]
		private Button equipButton;

		[SerializeField]
		private TMP_Text equipButtonText;

		[SerializeField]
		private TMP_Text evolveButtonText;

		[SerializeField]
		private GameObject evolveButtonError;

		[SerializeField]
		private GameObject evolveNotification;

		[SerializeField]
		private TMP_Text mainStatText;

		[SerializeField]
		private ItemStarsView starsView;

		[SerializeField]
		private InventoryItemView itemPreview;

		[SerializeField]
		private TMP_Text itemPreviewTitle;

		[SerializeField]
		private TMP_Text itemPreviewField;

		[SerializeField]
		private TMP_Text itemPreviewEquipText;

		[SerializeField]
		private TMP_Text itemPreviewRarity;

		[SerializeField]
		private TMP_Text itemPreviewType;

		[SerializeField]
		private TMP_Text itemPreviewDamage;

		[SerializeField]
		private TMP_Text itemPreviewDamageNext;

		[SerializeField]
		private TMP_Text itemPreviewStatPerCopyLabel;

		[SerializeField]
		private TMP_Text itemPreviewStatPerCopyValue;

		[SerializeField]
		private TMP_Text itemPreviewAttackSpeed;

		[SerializeField]
		private GameObject itemPreviewAttackSpeedRoot;

		[SerializeField]
		private TMP_Text itemEvolveWarningText;

		[SerializeField]
		private Image equipButtonImage;

		[SerializeField]
		private Sprite equipButtonSprite;

		[SerializeField]
		private Sprite cancelButtonSprite;

		[Inject]
		private InventoryView inventoryView;

		[Inject]
		private PlayerModel playerModel;

		private float defaultHeight;

		private float colorOffset;

		protected override void Awake()
		{
			// Native ELF VA 0x164df78; original split-code branch continues to all three button listeners.
			base.Awake();
			defaultHeight = RectTransform.sizeDelta.y;
			closeButton.onClick.AddListener(Close);
			equipButton.onClick.AddListener(Equip);
			evolveButton.onClick.AddListener(Evolve);
		}

		private void OnEnable()
		{
			// Native ELF VA 0x164e0d0.
			Initialize();
			Render();
		}

		private void Evolve()
		{
			// Native ELF VA 0x164e0f8.
			ItemModel item = playerModel.SelectedItem;
			if (item != null && !item.TryEvolve()) evolveButtonError.SetActive(true);
		}

		private void Equip()
		{
			// Native ELF VA 0x164e144.
			inventoryView.Equip();
		}

		private void Close()
		{
			// Native ELF VA 0x164e15c.
			playerModel.SelectedItem = null;
		}

		private void LateUpdate()
		{
			// Native ELF VA 0x164e178; original mythical text hue cycles at 0.5 per second.
			if (playerModel.SelectedItem == null || playerModel.SelectedItem.Config.Rarity != Rarity.Mythical) return;
			itemPreviewRarity.color = Color.HSVToRGB(colorOffset, 0.25f, 1f, true);
			colorOffset += Time.deltaTime * 0.5f;
			if (colorOffset > 1f) colorOffset -= 1f;
		}

		protected override void Render()
		{
			// Native ELF VA 0x164e22c; labels are original ELF literals, not new strings.
			ItemModel item = playerModel.SelectedItem;
			if (item == null) return;
			float height = defaultHeight;
			abilitiesView.Setup(item, int.MaxValue, false);
			if (item.HasUnlockableAbility) height += abilitiesView.GetHeight();
			if (item.HasStars) starsView.Setup(item);
			else height -= ((RectTransform)starsView.transform).sizeDelta.y * 0.25f;
			mainStatText.text = LocalizationStrings.Get(item.IsArmor ? "STAT_Armor" : "STAT_Damage");
			starsView.gameObject.SetActive(item.HasStars);
			RectTransform.sizeDelta = new Vector2(RectTransform.sizeDelta.x, height);
			evolveButton.gameObject.SetActive(true);
			if (evolveNotification != null) evolveNotification.SetActive(item.CanEvolve);
			equipButtonText.text = LocalizationStrings.Get(playerModel.EquippedItems.Contains(item) ? "INVENTORY_UNEQUIP" : "INVENTORY_EQUIP");
			evolveButtonText.text = LocalizationStrings.Get("INVENTORY_EVOLVE") + " " + string.Format("{0}{1}<size=45><color=#DDC0FF>/{2}", item.CanEvolve ? "" : "<color=#FF4876>", item.Copies, item.LevelsToEvolve);
			RenderItemPreview(item);
			itemEvolveWarningText.text = LocalizationStrings.Get("INVENTORY_EVOLVE_REQUIREMENT").Replace("{value}", item.LevelsToEvolve.ToString());
		}

		private void RenderItemPreview(ItemModel item)
		{
			// Native ELF VA 0x164e6ec and its three original typed lookup predicates.
			itemPreview.Initialize(item);
			itemPreviewTitle.text = LocalizationStrings.Get("ITEM_NAME_" + item.Config.Key);
			bool cancel = inventoryView.UnequipState;
			itemPreviewEquipText.text = LocalizationStrings.Get(cancel ? "INVENTORY_CANCEL" : playerModel.EquippedItems.Contains(item) ? "INVENTORY_UNEQUIP" : "INVENTORY_EQUIP");
			IconStyleConfig styles = GameContext.Get<IconStyleConfig>();
			ItemsProvider items = GameContext.Get<ItemsProvider>();
			string iconName = styles.WeaponTypeStyles.First(style => style.WeaponType == item.Config.DamageType).Sprite.name;
			Rarity rarity = item.Rarity;
			ItemRarityStyle rarityStyle = styles.ItemRarityStyles.First(style => style.Rarity == rarity);
			string color = items.GetRarityColorCode(rarity);
			double value = item.Damage;
			double next = item.NextLevelDamage;
			if (item.IsArmor)
			{
			    iconName = styles.Styles.First(style => style.StatType == item.Config.DamageStatTypeFlat).Sprite.name;
			    value = item.Armor;
			    next = item.NextLevelArmor;
			}
			itemPreviewRarity.text = (rarityStyle.HasGradient ? "" : "<color=" + color + ">") + LocalizationStrings.Get(string.Format("ITEM_RARITY_{0}", rarity));
			itemPreviewType.text = "<sprite name=" + iconName + ">" + LocalizationStrings.Get(string.Format("WEAPON_TYPE_{0}", item.Config.DamageType));
			itemPreviewDamage.text = "<sprite name=" + iconName + ">" + value.Format();
			itemPreviewDamageNext.text = next.Format();
			itemPreviewAttackSpeed.text = item.Config.AttackCooldown.ToString() + "s";
			itemPreviewStatPerCopyLabel.text = LocalizationStrings.Get(item.IsArmor ? "ITEMS_ArmorPerCopy" : "ITEMS_DamagePerCopy");
			itemPreviewStatPerCopyValue.text = "<sprite name=" + iconName + "> " + item.StatPerCopy.Format();
			itemPreviewAttackSpeedRoot.SetActive(!item.IsArmor);
			itemPreviewRarity.enableVertexGradient = rarityStyle.HasGradient;
			itemPreviewRarity.colorGradient = rarityStyle.Gradient;
			itemPreviewRarity.color = Color.white;
			equipButtonImage.sprite = cancel ? cancelButtonSprite : equipButtonSprite;
		}

		public void SelectStat(string title, string value)
		{
			// Native ELF VA 0x164ef94.
			itemPreviewTitle.text = title;
			itemPreviewField.text = value;
			playerModel.SelectedItem = null;
		}
	}
}
