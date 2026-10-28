using System;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Pets
{
	// One species in the collection grid: colored icon, name and level when owned; black silhouette and an unlock button when not.
	public class PetBookCell : MonoBehaviour
	{
		private static readonly Color SilhouetteColor = new Color(0f, 0f, 0f, 0.85f);

		private static readonly Color LockedFrameColor = new Color(0.45f, 0.45f, 0.45f, 1f);

		private static readonly Color DisabledButtonColor = new Color(0.55f, 0.55f, 0.55f, 1f);

		public Button Button;

		public Image Frame;

		public Image Icon;

		public Image Lock;

		public TMP_Text Level;

		public TMP_Text Name;

		public Button UnlockButton;

		public Image UnlockImage;

		public TMP_Text UnlockLabel;

		public TMP_Text Note;

		public PetDefinition Definition { get; private set; }

		public ItemConfig Config { get; private set; }

		// Owned item of this species, null when not owned.
		public ItemModel Item { get; private set; }

		public void Setup(PetDefinition definition, ItemConfig config, Action<PetBookCell> onClick, Action<PetBookCell> onUnlock)
		{
			Definition = definition;
			Config = config;
			if (Button != null) Button.onClick.AddListener(() => onClick?.Invoke(this));
			if (UnlockButton != null) UnlockButton.onClick.AddListener(() => onUnlock?.Invoke(this));
		}

		public void Render(ItemModel item, IconStyleConfig styles)
		{
			Item = item;
			if (Config == null) return;
			bool owned = item != null;
			Rarity rarity = owned ? item.Rarity : Config.Rarity;
			Sprite frame = FindRaritySprite(styles, rarity, owned && item.Evolved);
			PetUiBuilder.SetSprite(Frame, frame, owned ? new Color(0.3f, 0.35f, 0.5f, 1f) : new Color(0.2f, 0.2f, 0.22f, 1f));
			if (frame != null && !owned) Frame.color = LockedFrameColor;

			Sprite icon = owned ? item.CurrentIcon : Config.Icon;
			if (Icon != null)
			{
				Icon.sprite = icon;
				Icon.enabled = icon != null;
				Icon.color = owned ? Color.white : SilhouetteColor;
			}
			if (Lock != null) Lock.gameObject.SetActive(!owned && Lock.sprite != null);

			if (Level != null)
			{
				Level.gameObject.SetActive(owned);
				if (owned) PetStatsText.SetText(Level, "Lv." + PetStats.GetLevel(item));
			}

			bool mythical = Config.Rarity == Rarity.Mythical;
			int cost = owned ? 0 : PetEconomy.GetUnlockCost(Config);
			string name = Definition != null && !string.IsNullOrEmpty(Definition.DisplayName) ? Definition.DisplayName : LocalizationStrings.Get("ITEM_NAME_" + Config.Key);
			// Mythical species drop from the Boss Tower; with a shard cost the button stays below the hint.
			PetStatsText.SetText(Name, owned ? name : mythical && cost > 0 ? "<color=#FFB347>Boss塔掉落</color>" : "???");

			bool showUnlock = !owned && cost > 0;
			if (UnlockButton != null)
			{
				UnlockButton.gameObject.SetActive(showUnlock);
				if (showUnlock)
				{
					bool affordable = PetEconomy.CanUnlock(Config);
					UnlockButton.interactable = affordable;
					if (UnlockImage != null && UnlockImage.sprite != null) UnlockImage.color = affordable ? Color.white : DisabledButtonColor;
					PetStatsText.SetText(UnlockLabel, "解锁 " + (affordable ? cost.ToString() : "<color=#FF8080>" + cost + "</color>") + "碎片");
				}
			}
			if (Note != null)
			{
				Note.gameObject.SetActive(!showUnlock);
				if (!showUnlock)
					PetStatsText.SetText(Note, owned ? PetStatsText.DamageTypeName(Definition != null ? Definition.DamageType : Config.DamageType) : mythical ? "<color=#FFB347>Boss塔掉落</color>" : "宝箱掉落");
			}
		}

		private static Sprite FindRaritySprite(IconStyleConfig styles, Rarity rarity, bool evolved)
		{
			if (styles == null || styles.ItemRarityStyles == null) return null;
			foreach (ItemRarityStyle style in styles.ItemRarityStyles)
				if (style != null && style.Rarity == rarity)
					return evolved && style.EvolvedSprite != null ? style.EvolvedSprite : style.Sprite;
			return null;
		}
	}
}
