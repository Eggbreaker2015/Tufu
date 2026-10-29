using Codebase.Utilities;
using Codebase.Player;
using System.Linq;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Localization;
using Codebase.Pet;
using Codebase.UI.Pets;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Inventory
{
	public class ItemMiniView : PopupView
	{
		[SerializeField]
		private ItemAbilitiesView abilitiesView;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private TMP_Text mainStatText;

		[SerializeField]
		private ItemStarsView starsView;

		[SerializeField]
		private InventoryItemView itemPreview;

		[SerializeField]
		private TMP_Text itemPreviewTitle;

		[SerializeField]
		private TMP_Text itemPreviewRarity;

		[SerializeField]
		private TMP_Text itemPreviewType;

		[Inject]
		private InventoryView inventoryView;

		[Inject]
		private PlayerModel playerModel;

		private float defaultHeight;

		private ItemModel model;

		private float colorOffset;

		// Pet mode: the item's pet stats sit in a block cloned from the type label, between the type row and the abilities.
		private const float PetStatsHeight = 300f;

		// Part of the block that goes into the existing gap above the abilities.
		private const float PetStatsOverlap = 60f;

		private TMP_Text petStatsText;

		private Vector2 abilitiesPosition;

		private float petStatsRefreshTime;

		private PetAbilitySummaryRow abilitySummary;

		protected override void Awake()
		{
			// Native ELF VA 0x16553a4.
			base.Awake(); defaultHeight = rectTransform.sizeDelta.y; closeButton.onClick.AddListener(Close);
		}

		private void OnEnable()
		{
			// Native ELF VA 0x1655454.
			Initialize(); Render();
		}

		private void LateUpdate()
		{
			// Native ELF VA 0x165547c.
			if (petStatsText != null && petStatsText.gameObject.activeSelf && Time.unscaledTime >= petStatsRefreshTime) RefreshPetStats();
			if (model == null || model.Config.Rarity != Rarity.Mythical) return;
			itemPreviewRarity.color = Color.HSVToRGB(colorOffset,.25f,1f,true);
			colorOffset += Time.deltaTime*.5f; if (colorOffset>1f) colorOffset-=1f;
		}

		public void Setup(ItemModel model)
		{
			// Native ELF VA 0x1655524.
			this.model = model;
		}

		protected override void Render()
		{
			// Native ELF VA 0x165552c.
			if (model == null) return;
			bool isPet = PetStats.TryGet(model, out PetStatsInfo pet);
			float height = defaultHeight;
			if (isPet) abilitiesView.gameObject.SetActive(false);
			else
			{
				abilitiesView.gameObject.SetActive(true);
				abilitiesView.Setup(model,3,false); height += abilitiesView.GetHeight();
			}
			if(model.HasStars) starsView.Setup(model); else height-=((RectTransform)starsView.transform).sizeDelta.y*.25f;
			height += RenderPetStats(model, isPet, pet);
			if (isPet) height += PetAbilitySummaryRow.Height;
			mainStatText.text=LocalizationStrings.Get(model.IsArmor ? "STAT_Armor" : "STAT_Damage");
			starsView.gameObject.SetActive(model.HasStars); rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x,height);
			RenderItemPreview(model);
		}

		private void RenderItemPreview(ItemModel item)
		{
			// Native ELF VA 0x165571c; original preview is read-only, so initialize with isPreview=true.
			itemPreview.Initialize(item,true);
			PetStatsText.SetItemName(itemPreviewTitle, item.Config);
			var styles=GameContext.Get<IconStyleConfig>();var items=GameContext.Get<ItemsProvider>();
			string iconName=styles.WeaponTypeStyles.First(style=>style.WeaponType==item.Config.DamageType).Sprite.name;
			Rarity rarity=item.Rarity;string color=items.GetRarityColorCode(rarity);
			// Preserve original stat getter evaluations; the popup has no separate numeric fields.
			double value=item.Damage,next=item.NextLevelDamage;
			var rarityStyle=styles.ItemRarityStyles.First(style=>style.Rarity==rarity);
			if(item.IsArmor){iconName=styles.Styles.First(style=>style.StatType==item.Config.DamageStatTypeFlat).Sprite.name;value=item.Armor;next=item.NextLevelArmor;}
			itemPreviewRarity.text=(rarityStyle.HasGradient ? "" : "<color="+color+">")+LocalizationStrings.Get(string.Format("ITEM_RARITY_{0}",rarity));
			itemPreviewType.text="<sprite name="+iconName+">"+LocalizationStrings.Get(string.Format("WEAPON_TYPE_{0}",item.Config.DamageType));
			itemPreviewRarity.enableVertexGradient=rarityStyle.HasGradient;itemPreviewRarity.colorGradient=rarityStyle.Gradient;itemPreviewRarity.color=Color.white;
		}

		// Returns the height the pet block adds to the popup; read-only popup, so no next-level values.
		private float RenderPetStats(ItemModel item, bool isPet, PetStatsInfo pet)
		{
			RectTransform abilities = (RectTransform)abilitiesView.transform;
			if (!isPet)
			{
			    if (petStatsText != null && petStatsText.gameObject.activeSelf)
			    {
			        petStatsText.gameObject.SetActive(false);
			        abilities.anchoredPosition = abilitiesPosition;
			    }
			    if (abilitySummary != null) abilitySummary.gameObject.SetActive(false);
			    return 0f;
			}
			if (petStatsText == null)
			{
			    abilitiesPosition = abilities.anchoredPosition;
			    petStatsText = PetStatsText.CreateBlock(itemPreviewType, rectTransform, "PetStats");
			    LocalizationFontApplier.RegisterLike(petStatsText, itemPreviewType);
			    petStatsText.textWrappingMode = TextWrappingModes.Normal;
			    petStatsText.fontSizeMax = 40f;
			    petStatsText.enableVertexGradient = false;
			    petStatsText.color = Color.white;
			    RectTransform block = petStatsText.rectTransform;
			    block.anchoredPosition = new Vector2(abilitiesPosition.x, abilitiesPosition.y + PetStatsOverlap);
			    block.sizeDelta = new Vector2(abilities.sizeDelta.x - 62f, PetStatsHeight);
			}
			PetStatsText.SetText(petStatsText, PetStatsText.Build(pet, false));
			petStatsText.gameObject.SetActive(true);
			petStatsRefreshTime = Time.unscaledTime + PetStatsText.RefreshInterval;
			float shift = PetStatsHeight - PetStatsOverlap;
			abilities.anchoredPosition = new Vector2(abilitiesPosition.x, abilitiesPosition.y - shift);
			if (abilitySummary == null)
			{
				abilitySummary = PetAbilitySummaryRow.Create(rectTransform, "PetAbilitySummary", mainStatText, PetConfig.Instance?.ButtonSprite);
				PetUiBuilder.SetRect((RectTransform)abilitySummary.transform, abilities.anchorMin, abilities.anchorMax, abilities.pivot, new Vector2(abilitiesPosition.x, abilitiesPosition.y - shift), new Vector2(abilities.sizeDelta.x - 62f, PetAbilitySummaryRow.Height));
			}
			abilitySummary.gameObject.SetActive(true);
			abilitySummary.Render(item);
			return shift;
		}

		// Live absolute values: the player's stats are not atoms the render reaction tracks.
		private void RefreshPetStats()
		{
			petStatsRefreshTime = Time.unscaledTime + PetStatsText.RefreshInterval;
			if (PetStats.TryGet(model, out PetStatsInfo pet))
			{
				PetStatsText.SetText(petStatsText, PetStatsText.Build(pet, false));
				if (abilitySummary != null) abilitySummary.Render(model);
			}
		}
	}
}
