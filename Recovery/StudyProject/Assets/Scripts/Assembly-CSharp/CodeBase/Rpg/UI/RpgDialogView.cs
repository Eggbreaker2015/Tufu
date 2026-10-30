using System;
using Codebase.Core;
using Codebase.Player.Input;
using Codebase.UI;
using Codebase.UI.Pets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.Rpg.UI
{
	// Code-built modal under HudContainer: a full-screen dimmer that blocks the joystick, a title, a body and 1-2 buttons.
	public class RpgDialogView : MonoBehaviour
	{
		private static Sprite whiteSprite;

		public static RpgDialogView Show(string title, string body, string primary, Action onPrimary, string secondary = null, Action onSecondary = null)
		{
			if (!GameContext.Has<Joystick>()) return null;
			Transform parent = GameContext.Get<Joystick>().transform.parent;
			if (parent == null) return null;
			if (whiteSprite == null) whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
			Image dimmer = PetUiBuilder.CreateImage(parent, "RpgDialog", whiteSprite, Color.white);
			dimmer.color = new Color(0f, 0f, 0f, 0.6f);
			PetUiBuilder.Stretch(dimmer.rectTransform, Vector2.zero, Vector2.zero);
			dimmer.transform.SetAsLastSibling();
			RpgDialogView view = dimmer.gameObject.AddComponent<RpgDialogView>();
			view.Build(title, body, primary, onPrimary, secondary, onSecondary);
			return view;
		}

		public void Close()
		{
			if (this != null) Destroy(gameObject);
		}

		private void Build(string title, string body, string primary, Action onPrimary, string secondary, Action onSecondary)
		{
			Image panel = PetUiBuilder.CreateImage(transform, "Panel", whiteSprite, Color.white);
			panel.color = new Color(0.1f, 0.12f, 0.18f, 0.97f);
			PetUiBuilder.SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 760f));
			TMP_Text titleText = PetUiBuilder.CreateText(panel.transform, "Title", 64f, TextAlignmentOptions.Center);
			titleText.color = new Color(1f, 0.82f, 0.3f, 1f);
			PetUiBuilder.SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(-60f, 100f));
			PetStatsText.SetText(titleText, title);
			TMP_Text bodyText = PetUiBuilder.CreateText(panel.transform, "Body", 40f, TextAlignmentOptions.Top);
			bodyText.textWrappingMode = TextWrappingModes.Normal;
			bodyText.enableAutoSizing = true;
			bodyText.lineSpacing = 12f;
			PetUiBuilder.Stretch(bodyText.rectTransform, new Vector2(60f, 170f), new Vector2(-60f, -150f));
			PetStatsText.SetText(bodyText, body);
			bool two = !string.IsNullOrEmpty(secondary);
			CreateButton(panel.transform, primary, new Color(0.22f, 0.62f, 0.28f, 1f), two ? 190f : 0f, () => { Close(); onPrimary?.Invoke(); });
			if (two) CreateButton(panel.transform, secondary, new Color(0.35f, 0.38f, 0.45f, 1f), -190f, () => { Close(); onSecondary?.Invoke(); });
		}

		private static void CreateButton(Transform parent, string label, Color color, float x, Action onClick)
		{
			Image image = PetUiBuilder.CreateImage(parent, "Button", whiteSprite, Color.white);
			image.color = color;
			PetUiBuilder.SetRect(image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 45f), new Vector2(320f, 100f));
			Button button = image.gameObject.AddComponent<Button>();
			button.targetGraphic = image;
			button.onClick.AddListener(() => { PetUiBuilder.PlayTap(); onClick(); });
			TMP_Text text = PetUiBuilder.CreateText(image.transform, "Label", 44f, TextAlignmentOptions.Center);
			PetUiBuilder.Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
			PetStatsText.SetText(text, label);
		}
	}
}
