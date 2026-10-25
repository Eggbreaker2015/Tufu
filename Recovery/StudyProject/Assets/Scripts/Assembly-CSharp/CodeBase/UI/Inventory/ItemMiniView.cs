using Codebase.Utilities;
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
			abilitiesView.Setup(model,3,false); float height=defaultHeight+abilitiesView.GetHeight();
			if(model.HasStars) starsView.Setup(model); else height-=((RectTransform)starsView.transform).sizeDelta.y*.25f;
			mainStatText.text=LocalizationStrings.Get(model.IsArmor ? "STAT_Armor" : "STAT_Damage");
			starsView.gameObject.SetActive(model.HasStars); rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x,height);
			RenderItemPreview(model);
		}

		private void RenderItemPreview(ItemModel item)
		{
			// Native ELF VA 0x165571c; original preview is read-only, so initialize with isPreview=true.
			itemPreview.Initialize(item,true);
			itemPreviewTitle.text = LocalizationStrings.Get("ITEM_NAME_" + item.Config.Key);
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
	}
}
