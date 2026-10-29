using System.Collections.Generic;
using Codebase.Player.Input;
using Codebase.UI.Pets;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Codebase.Rpg.UI
{
	public class HeroSwitchHudView : MonoBehaviour
	{
		private class Entry
		{
			public HeroModel Hero;
			public Image Frame;
			public Image Icon;
			public Image Health;
			public TMP_Text Label;
		}

		private class DragBlocker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
		{
			public void OnBeginDrag(PointerEventData eventData) { }
			public void OnDrag(PointerEventData eventData) { }
			public void OnEndDrag(PointerEventData eventData) { }
		}

		private static Sprite healthSprite;
		private readonly List<Entry> entries = new List<Entry>();
		private HeroRosterModel roster;
		private HeroModel request;

		public static HeroSwitchHudView Create(Joystick joystick, HeroRosterModel roster)
		{
			if (!RpgModeConfig.Enabled || joystick == null) return null;
			if (joystick.transform.parent == null) return null;
			RectTransform root = PetUiBuilder.CreateRect(joystick.transform.parent, "HeroSwitchHud");
			PetUiBuilder.SetRect(root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(120f, 360f));
			HeroSwitchHudView view = root.gameObject.AddComponent<HeroSwitchHudView>();
			view.roster = roster;
			return view;
		}

		public HeroModel TakeRequest()
		{
			HeroModel result = request;
			request = null;
			return result;
		}

		private void Update()
		{
			if (!RpgModeConfig.Enabled) { Destroy(gameObject); return; }
			foreach (HeroModel hero in roster.Heroes)
				if (!entries.Exists(entry => entry.Hero == hero)) Add(hero);
			foreach (Entry entry in entries)
			{
				bool alive = entry.Hero.IsAlive;
				entry.Frame.color = !alive ? Color.gray : entry.Hero == roster.Controlled ? new Color(1f, 0.8f, 0.2f) : new Color(0.25f, 0.3f, 0.38f);
				entry.Icon.color = entry.Icon.sprite == null ? Color.clear : alive ? Color.white : Color.gray;
				entry.Label.color = alive ? Color.white : Color.gray;
				double maximum = entry.Hero.Unit != null ? entry.Hero.Unit.GetStatValue(StatType.Health) : 0;
				entry.Health.fillAmount = maximum > 0 ? Mathf.Clamp01((float)(entry.Hero.Unit.CurrentHealth / maximum)) : 0f;
			}
		}

		private void Add(HeroModel hero)
		{
			Image frame = PetUiBuilder.CreateImage(transform, "Hero" + hero.SlotOrder, null, Color.gray);
			PetUiBuilder.SetRect(frame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -hero.SlotOrder * 115f), new Vector2(110f, 105f));
			frame.gameObject.AddComponent<DragBlocker>();
			Button button = frame.gameObject.AddComponent<Button>();
			button.targetGraphic = frame;
			button.transition = Selectable.Transition.None;
			button.onClick.AddListener(() => request = hero);
			Sprite sprite = hero.Definition != null ? hero.Definition.Icon : null;
			Image icon = PetUiBuilder.CreateImage(frame.transform, "Icon", sprite, Color.clear);
			icon.raycastTarget = false;
			PetUiBuilder.SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(78f, 70f));
			TMP_Text label = PetUiBuilder.CreateText(frame.transform, "Name", 22f, TextAlignmentOptions.Center);
			label.text = sprite == null ? (hero.Definition != null ? hero.Definition.DisplayName : "Hero") : "";
			PetUiBuilder.SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(-8f, -20f));
			if (healthSprite == null) healthSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
			Image health = PetUiBuilder.CreateImage(frame.transform, "Health", healthSprite, Color.green);
			health.raycastTarget = false;
			health.type = Image.Type.Filled;
			health.fillMethod = Image.FillMethod.Horizontal;
			PetUiBuilder.SetRect(health.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(-10f, 8f));
			entries.Add(new Entry { Hero = hero, Frame = frame, Icon = icon, Health = health, Label = label });
		}
	}
}
