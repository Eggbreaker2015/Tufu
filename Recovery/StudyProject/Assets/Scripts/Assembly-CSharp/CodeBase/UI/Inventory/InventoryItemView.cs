using CodeBase.Audio;
using Codebase.Core;
using System.Linq;
using System.Collections.Generic;
using AssetKits.ParticleImage;
using Codebase.Core.UI;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Inventory
{
	public class InventoryItemView : View
	{
		[SerializeField]
		private TMP_Text levelField;

		[SerializeField]
		private TMP_Text rarityField;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Image rarityBg;

		[SerializeField]
		private Image rarityBgLarge;

		[SerializeField]
		private Image typeAndRarity;

		[SerializeField]
		private Image selected;

		[SerializeField]
		private Button button;

		[SerializeField]
		private bool isPreview;

		[SerializeField]
		private bool hideLevel;

		[SerializeField]
		private bool showInfo;

		[SerializeField]
		private GameObject notification;

		[SerializeField]
		private GameObject info;

		[SerializeField]
		private ParticleImage divineParticles;

		[SerializeField]
		private List<ParticleImage> mythicalParticles;

		private ItemModel model;

		private PlayerModel playerModel;

		private bool enableNotification;

		private bool forceBaseRarity;

		private bool showInfoDynamic;

		private bool IsItemSelected => playerModel.SelectedItem == model; // Native 0x164f124.

		protected override void Awake()
		{
			// Native ELF VA 0x164f154.
			base.Awake();
			playerModel = GameContext.Get<PlayerModel>();
		}

		public void Initialize(ItemModel model, bool enableNotification = true, bool forceBaseRarity = false)
		{
			// Native ELF VA 0x164b578 with original click lambdas 0x164facc/0x164fc00.
			base.Initialize();
			this.model = model;
			this.enableNotification = enableNotification;
			this.forceBaseRarity = forceBaseRarity;
			if (!isPreview)
			{
			    button.onClick.RemoveAllListeners();
			    button.onClick.AddListener(() => { GameContext.Get<IAudioPlayer>().PlaySoundOnce("light_tap"); GameContext.Get<InventoryView>().SelectItem(this.model); });
			}
			if (showInfo)
			{
			    button.onClick.RemoveAllListeners();
			    button.onClick.AddListener(() => {
			        if (!showInfoDynamic) return;
			        GameContext.Get<IAudioPlayer>().PlaySoundOnce("light_tap");
			        GameContext.Get<UINavigator>().Open<ItemMiniView>("Assets/Prefabs/UI/ItemMiniView.prefab", true).Setup(this.model);
			    });
			}
			showInfoDynamic = true;
			Render();
		}

		protected override void Render()
		{
			// Native ELF VA 0x164f1d8; float s2 from Rect width and the -25 literal checked in ARM64.
			if (model == null)
			{
			    levelField.text = "";
			    rarityField.text = "";
			    icon.enabled = false;
			    SetRarityBgVisible(false, false);
			    selected.enabled = false;
			    divineParticles.enabled = false;
			    mythicalParticles.ForEach(particle => particle.enabled = false);
			    notification.SetActive(false);
			    return;
			}
			selected.enabled = playerModel != null && IsItemSelected && !isPreview;
			bool divine = model.IsDivine && !forceBaseRarity;
			Rarity rarity = divine ? Rarity.Divine : model.Config.Rarity;
			divineParticles.enabled = divine;
			mythicalParticles.ForEach(particle => particle.enabled = model.Config.Rarity == Rarity.Mythical);
			if (RectTransform != null) mythicalParticles.Last().startSize = new SeparatedMinMaxCurve(RectTransform.rect.width - 25f);
			IconStyleConfig styles = GameContext.Get<IconStyleConfig>();
			ItemRarityStyle rarityStyle = styles.ItemRarityStyles.First(style => style.Rarity == model.Config.Rarity);
			Sprite raritySprite = divine ? rarityStyle.EvolvedSprite : rarityStyle.Sprite;
			levelField.text = string.Format("{0}", forceBaseRarity ? model.Copies : model.DisplayLevel);
			rarityField.text = rarity.ToString() ?? "";
			icon.sprite = forceBaseRarity ? model.Config.Icon : model.CurrentIcon;
			icon.enabled = true;
			ApplyRarityBg(rarityStyle, raritySprite);
			levelField.enabled = !hideLevel;
			typeAndRarity.sprite = (model.IsArmor ? styles.ArmorTypeMinimizedStyles : styles.WeaponTypeMinimizedStyles)
			    .First(style => style.WeaponType == model.Config.DamageType).Sprite;
			typeAndRarity.color = GameContext.Get<ItemsProvider>().RarityColors[(int)rarity];
			typeAndRarity.enabled = rarity != Rarity.Mythical;
			notification.SetActive((model.ShowNotification || model.CanEvolve) && enableNotification && !isPreview);
			info.SetActive(showInfo && showInfoDynamic);
		}

		private void ApplyRarityBg(ItemRarityStyle rarityStyle, Sprite sprite)
		{
			// Native ELF VA 0x164f9f8.
			rarityBg.sprite = sprite;
			rarityBgLarge.sprite = sprite;
			bool large = rarityStyle.UseLargeRarityBg && rarityBgLarge != null;
			SetRarityBgVisible(!large, large);
		}

		private void SetRarityBgVisible(bool normalVisible, bool largeVisible)
		{
			// Native ELF VA 0x164f998.
			rarityBg.gameObject.SetActive(normalVisible);
			rarityBgLarge.gameObject.SetActive(largeVisible);
		}

		public void SetInfoBtnVisible(bool visible)
		{
			this.showInfoDynamic = visible;
		}
	}
}
