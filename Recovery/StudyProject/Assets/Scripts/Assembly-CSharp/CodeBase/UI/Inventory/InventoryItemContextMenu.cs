using Codebase.Utilities;
using Codebase.Unit;
using Codebase.Player;
using System.Linq;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Localization;
using Codebase.Pet;
using Codebase.UI.Pets;
using Codebase.Pet.Commands;
using CodeBase.Audio;
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

		// Pet mode: the item's pet stats sit in a block cloned from the stat label, between the stats and the abilities.
		private const float PetStatsHeight = 340f;

		// Row under the pet block: the 万能碎片 button and the shard balance.
		private const float ShardRowHeight = 112f;

		// Part of the block that goes into the existing gap above the abilities.
		private const float PetStatsOverlap = 24f;

		private TMP_Text petStatsText;

		private Vector2 abilitiesPosition;

		private float petStatsRefreshTime;

		private RectTransform shardRow;

		private Button shardButton;

		private Image shardButtonImage;

		private TMP_Text shardButtonText;

		private TMP_Text shardCountText;

		private PetAbilitySummaryRow abilitySummary;

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
			if (petStatsText != null && petStatsText.gameObject.activeSelf && Time.unscaledTime >= petStatsRefreshTime) RefreshPetStats(playerModel.SelectedItem);
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
			if (PetStats.TryGet(item, out _)) abilitiesView.gameObject.SetActive(false);
			else
			{
				abilitiesView.gameObject.SetActive(true);
				abilitiesView.Setup(item, int.MaxValue, false);
				if (item.HasUnlockableAbility) height += abilitiesView.GetHeight();
			}
			if (item.HasStars) starsView.Setup(item);
			else height -= ((RectTransform)starsView.transform).sizeDelta.y * 0.25f;
			height += RenderPetStats(item);
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
			PetStatsText.SetItemName(itemPreviewTitle, item.Config);
			bool cancel = inventoryView.UnequipState;
			itemPreviewEquipText.text = LocalizationStrings.Get(cancel ? "INVENTORY_CANCEL" : playerModel.EquippedItems.Contains(item) ? "INVENTORY_UNEQUIP" : "INVENTORY_EQUIP");
			bool isPet = PetStats.TryGet(item, out PetStatsInfo pet);
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
			// Pet mode: the damage row shows the pet's attack (own + player share) and its next-level value.
			if (isPet)
			{
			    value = pet.Attack;
			    next = pet.NextAttack;
			}
			itemPreviewDamage.text = "<sprite name=" + iconName + ">" + value.Format();
			itemPreviewDamageNext.text = next.Format();
			itemPreviewAttackSpeed.text = item.Config.AttackCooldown.ToString() + "s";
			itemPreviewStatPerCopyLabel.text = LocalizationStrings.Get(item.IsArmor ? "ITEMS_ArmorPerCopy" : "ITEMS_DamagePerCopy");
			itemPreviewStatPerCopyValue.text = "<sprite name=" + iconName + "> " + (isPet ? PetStatsText.AttackPerCopy(pet) : item.StatPerCopy).Format();
			itemPreviewAttackSpeedRoot.SetActive(!item.IsArmor);
			itemPreviewRarity.enableVertexGradient = rarityStyle.HasGradient;
			itemPreviewRarity.colorGradient = rarityStyle.Gradient;
			itemPreviewRarity.color = Color.white;
			equipButtonImage.sprite = cancel ? cancelButtonSprite : equipButtonSprite;
		}

		// Returns the height the pet block adds to the menu; hides it and restores the abilities outside pet mode or for armor.
		private float RenderPetStats(ItemModel item)
		{
			RectTransform abilities = (RectTransform)abilitiesView.transform;
			if (!PetStats.TryGet(item, out PetStatsInfo pet))
			{
			    if (petStatsText != null && petStatsText.gameObject.activeSelf)
			    {
			        petStatsText.gameObject.SetActive(false);
			        abilities.anchoredPosition = abilitiesPosition;
			    }
			    if (shardRow != null) shardRow.gameObject.SetActive(false);
			    if (abilitySummary != null) abilitySummary.gameObject.SetActive(false);
			    return 0f;
			}
			if (petStatsText == null)
			{
			    abilitiesPosition = abilities.anchoredPosition;
			    petStatsText = PetStatsText.CreateBlock(mainStatText, RectTransform, "PetStats");
			    LocalizationFontApplier.RegisterLike(petStatsText, mainStatText);
			    petStatsText.textWrappingMode = TextWrappingModes.Normal;
			    petStatsText.fontSizeMax = 40f;
			    RectTransform block = petStatsText.rectTransform;
			    block.anchoredPosition = new Vector2(abilitiesPosition.x, abilitiesPosition.y + PetStatsOverlap);
			    block.sizeDelta = new Vector2(abilities.sizeDelta.x - 2f * mainStatText.rectTransform.anchoredPosition.x, PetStatsHeight);
			}
			if (shardRow == null) CreateShardRow();
			if (abilitySummary == null)
			{
				abilitySummary = PetAbilitySummaryRow.Create(RectTransform, "PetAbilitySummary", evolveButtonText, shardButtonImage.sprite);
				RectTransform block = petStatsText.rectTransform;
				PetUiBuilder.SetRect((RectTransform)abilitySummary.transform, block.anchorMin, block.anchorMax, new Vector2(0.5f, 1f), new Vector2(block.anchoredPosition.x, shardRow.anchoredPosition.y - ShardRowHeight), new Vector2(block.sizeDelta.x, PetAbilitySummaryRow.Height));
			}
			abilitySummary.gameObject.SetActive(true);
			PetStatsText.SetText(petStatsText, BuildPetStats(pet, item));
			petStatsText.gameObject.SetActive(true);
			shardRow.gameObject.SetActive(true);
			RenderShardRow(item);
			if (abilitySummary != null) abilitySummary.Render(item);
			petStatsRefreshTime = Time.unscaledTime + PetStatsText.RefreshInterval;
			float shift = PetStatsHeight - PetStatsOverlap + ShardRowHeight;
			abilities.anchoredPosition = new Vector2(abilitiesPosition.x, abilitiesPosition.y - shift);
			return shift + PetAbilitySummaryRow.Height;
		}

		// Live absolute values: the player's stats are not atoms the render reaction tracks.
		private void RefreshPetStats(ItemModel item)
		{
			petStatsRefreshTime = Time.unscaledTime + PetStatsText.RefreshInterval;
			if (!PetStats.TryGet(item, out PetStatsInfo pet)) return;
			PetStatsText.SetText(petStatsText, BuildPetStats(pet, item));
			RenderShardRow(item);
			if (abilitySummary != null) abilitySummary.Render(item);
			IconStyleConfig styles = GameContext.Get<IconStyleConfig>();
			string iconName = styles.WeaponTypeStyles.First(style => style.WeaponType == item.Config.DamageType).Sprite.name;
			itemPreviewDamage.text = "<sprite name=" + iconName + ">" + pet.Attack.Format();
			itemPreviewDamageNext.text = pet.NextAttack.Format();
			itemPreviewStatPerCopyValue.text = "<sprite name=" + iconName + "> " + PetStatsText.AttackPerCopy(pet).Format();
		}

		private static string BuildPetStats(PetStatsInfo pet, ItemModel item)
		{
			string stats = PetStatsText.Build(pet, PetStatsText.Next(pet), false);
			string synergy = PetStatsText.SynergyLine(item);
			return string.IsNullOrEmpty(synergy) ? stats : stats + "\n" + synergy;
		}

		// Button and balance built from the menu's own button / label so they keep the menu's look.
		private void CreateShardRow()
		{
			RectTransform block = petStatsText.rectTransform;
			GameObject row = new GameObject("PetShardRow", typeof(RectTransform));
			shardRow = (RectTransform)row.transform;
			shardRow.SetParent(RectTransform, false);
			shardRow.anchorMin = block.anchorMin;
			shardRow.anchorMax = block.anchorMax;
			shardRow.pivot = new Vector2(0.5f, 1f);
			shardRow.anchoredPosition = new Vector2(block.anchoredPosition.x, block.anchoredPosition.y - PetStatsHeight);
			shardRow.sizeDelta = new Vector2(block.sizeDelta.x, ShardRowHeight);

			GameObject button = new GameObject("ShardButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
			RectTransform buttonRect = (RectTransform)button.transform;
			buttonRect.SetParent(shardRow, false);
			buttonRect.anchorMin = new Vector2(0f, 0.5f);
			buttonRect.anchorMax = new Vector2(0f, 0.5f);
			buttonRect.pivot = new Vector2(0f, 0.5f);
			buttonRect.anchoredPosition = Vector2.zero;
			buttonRect.sizeDelta = new Vector2(430f, 100f);
			shardButtonImage = button.GetComponent<Image>();
			Image evolveImage = evolveButton.GetComponent<Image>();
			Sprite sprite = PetConfig.Instance != null && PetConfig.Instance.ButtonSprite != null ? PetConfig.Instance.ButtonSprite : evolveImage != null ? evolveImage.sprite : null;
			shardButtonImage.sprite = sprite;
			shardButtonImage.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
			shardButtonImage.color = sprite != null ? Color.white : new Color(0.25f, 0.55f, 0.95f, 1f);
			shardButton = button.GetComponent<Button>();
			shardButton.targetGraphic = shardButtonImage;
			shardButton.onClick.AddListener(UseShards);

			shardButtonText = PetStatsText.CreateBlock(evolveButtonText, buttonRect, "Label");
			LocalizationFontApplier.RegisterLike(shardButtonText, evolveButtonText);
			RectTransform label = shardButtonText.rectTransform;
			label.anchorMin = Vector2.zero;
			label.anchorMax = Vector2.one;
			label.pivot = new Vector2(0.5f, 0.5f);
			label.offsetMin = new Vector2(12f, 8f);
			label.offsetMax = new Vector2(-12f, -8f);
			shardButtonText.alignment = TextAlignmentOptions.Center;
			shardButtonText.fontSizeMax = 44f;
			shardButtonText.gameObject.SetActive(true);

			shardCountText = PetStatsText.CreateBlock(mainStatText, shardRow, "ShardCount");
			LocalizationFontApplier.RegisterLike(shardCountText, mainStatText);
			RectTransform count = shardCountText.rectTransform;
			count.anchorMin = new Vector2(0f, 0.5f);
			count.anchorMax = new Vector2(1f, 0.5f);
			count.pivot = new Vector2(0f, 0.5f);
			count.offsetMin = new Vector2(450f, -40f);
			count.offsetMax = new Vector2(0f, 40f);
			shardCountText.alignment = TextAlignmentOptions.Left;
			shardCountText.fontSizeMax = 40f;
			shardCountText.gameObject.SetActive(true);
		}

		private void RenderShardRow(ItemModel item)
		{
			if (shardRow == null || item == null) return;
			int amount = PetEconomy.GetShardSpendAmount(item);
			bool capped = PetEconomy.IsEvolutionCapped(item);
			shardButton.interactable = amount > 0;
			if (shardButtonImage.sprite != null) shardButtonImage.color = amount > 0 ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
			PetStatsText.SetText(shardButtonText, capped ? "已达进化上限" : "万能碎片 +" + amount);
			PetStatsText.SetText(shardCountText, "持有万能碎片 <color=#7CE3FF>" + PetEconomy.Shards + "</color>");
		}

		private void UseShards()
		{
			ItemModel item = playerModel.SelectedItem;
			int amount = PetEconomy.GetShardSpendAmount(item);
			if (amount <= 0) return;
			GameContext.Get<IAudioPlayer>().PlaySoundOnce("tap");
			App.ExecuteCommand(new UseUniversalShardCommand { Item = item, Amount = amount });
			RenderShardRow(item);
			if (abilitySummary != null) abilitySummary.Render(item);
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
