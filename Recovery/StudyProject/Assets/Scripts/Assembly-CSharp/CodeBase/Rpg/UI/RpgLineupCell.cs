using Codebase.Core;
using Codebase.Player.Inventory;
using Codebase.UI;
using Codebase.UI.Inventory;
using Codebase.UI.Pets;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.Rpg.UI
{
	public class RpgLineupCell : MonoBehaviour
	{
		private HeroRosterModel roster;
		private ItemSlotModel slot;
		private bool protagonist;
		private Image highlight;
		private Image health;
		private Image icon;
		private TMP_Text label;
		private static Sprite whiteSprite;

		public static void CreateProtagonist(Transform parent)
		{
			Image frame = PetUiBuilder.CreateImage(parent, "Protagonist", null, new Color(0.18f, 0.22f, 0.29f, 1f));
			frame.rectTransform.sizeDelta = new Vector2(152f, 152f);
			LayoutElement layout = frame.gameObject.AddComponent<LayoutElement>();
			layout.preferredWidth = 152f;
			Button button = frame.gameObject.AddComponent<Button>();
			button.targetGraphic = frame;
			button.transition = Selectable.Transition.None;
			RpgLineupCell cell = frame.gameObject.AddComponent<RpgLineupCell>();
			cell.roster = GameContext.Get<HeroRosterModel>();
			cell.protagonist = true;
			HeroDefinition definition = RpgModeConfig.Instance.Heroes.Find(hero => hero != null && hero.IsProtagonist);
			Sprite sprite = definition != null ? definition.Icon : null;
			cell.icon = PetUiBuilder.CreateImage(frame.transform, "Icon", sprite, Color.clear);
			cell.icon.raycastTarget = false;
			cell.icon.preserveAspect = true;
			PetUiBuilder.Stretch(cell.icon.rectTransform, new Vector2(12f, 20f), new Vector2(-12f, -12f));
			cell.label = PetUiBuilder.CreateText(frame.transform, "Name", 26f, TextAlignmentOptions.Center);
			PetUiBuilder.Stretch(cell.label.rectTransform, new Vector2(8f, 20f), new Vector2(-8f, -12f));
			PetStatsText.SetText(cell.label, sprite == null ? (definition != null && !string.IsNullOrEmpty(definition.DisplayName) ? definition.DisplayName : "主角") : string.Empty);
			cell.BuildOverlay();
			button.onClick.AddListener(() => {
				if (!GameContext.Get<InventoryView>().IsOpened) cell.roster.Request(cell.FindHero());
			});
		}

		public static void AddSlot(Transform parent, ItemSlotModel slot)
		{
			RectTransform root = PetUiBuilder.CreateRect(parent, "RpgLineupOverlay");
			PetUiBuilder.Stretch(root, Vector2.zero, Vector2.zero);
			RpgLineupCell cell = root.gameObject.AddComponent<RpgLineupCell>();
			cell.roster = GameContext.Get<HeroRosterModel>();
			cell.slot = slot;
			cell.BuildOverlay();
		}

		public static void RequestSlot(ItemSlotModel slot)
		{
			HeroRosterModel roster = GameContext.Get<HeroRosterModel>();
			roster.Request(roster.Heroes.Find(hero => hero.Pet != null && hero.Pet.Slot == slot));
		}

		private HeroModel FindHero()
		{
			return roster.Heroes.Find(hero => protagonist ? hero.IsProtagonist : hero.Pet != null && hero.Pet.Slot == slot);
		}

		private void BuildOverlay()
		{
			if (whiteSprite == null) whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
			highlight = PetUiBuilder.CreateImage(transform, "Controlled", whiteSprite, Color.white);
			highlight.color = new Color(1f, 0.8f, 0.2f, 0.4f);
			highlight.raycastTarget = false;
			PetUiBuilder.Stretch(highlight.rectTransform, Vector2.zero, Vector2.zero);
			Image background = PetUiBuilder.CreateImage(transform, "HealthBackground", whiteSprite, Color.white);
			background.color = new Color(0f, 0f, 0f, 0.8f);
			background.raycastTarget = false;
			PetUiBuilder.SetRect(background.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(-12f, 10f));
			health = PetUiBuilder.CreateImage(background.transform, "Fill", whiteSprite, Color.white);
			health.color = Color.green;
			health.raycastTarget = false;
			health.type = Image.Type.Filled;
			health.fillMethod = Image.FillMethod.Horizontal;
			PetUiBuilder.Stretch(health.rectTransform, new Vector2(1f, 1f), new Vector2(-1f, -1f));
		}

		private void Update()
		{
			HeroModel hero = FindHero();
			highlight.enabled = hero != null && roster.Controlled == hero;
			health.transform.parent.gameObject.SetActive(hero != null);
			if (hero == null) return;
			double maximum = hero.Unit != null ? hero.Unit.GetStatValue(StatType.Health) : 0;
			health.fillAmount = maximum > 0 && hero.IsAlive ? Mathf.Clamp01((float)(hero.Unit.CurrentHealth / maximum)) : 0f;
			if (icon != null) icon.color = icon.sprite == null ? Color.clear : hero.IsAlive ? Color.white : Color.gray;
			if (label != null) label.color = hero.IsAlive ? Color.white : Color.gray;
		}
	}
}
