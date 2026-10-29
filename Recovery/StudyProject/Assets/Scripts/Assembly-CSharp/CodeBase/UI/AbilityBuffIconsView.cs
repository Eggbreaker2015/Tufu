using System.Collections.Generic;
using Codebase.Abilities;
using Codebase.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	// A row of small ability icons above a hotbar slot, one per active buff: a dark sweep shows the elapsed
	// part of a timed buff and a corner number shows its stacks. Built in code, so the slot prefab is unchanged.
	public class AbilityBuffIconsView : MonoBehaviour
	{
		public const int MaxIcons = 3;

		public const float DefaultIconSize = 44f;

		private const float Spacing = 4f;

		// Soft ring behind a marked (debuff) icon, relative to the icon size.
		private const float HaloScale = 1.35f;

		private static readonly Color SweepColor = new Color(0f, 0f, 0f, 0.6f);

		private class Entry
		{
			public GameObject Root;
			public RectTransform Rect;
			public RawImage Halo;
			public Color DisplayedTint;
			public Image Icon;
			public Image Sweep;
			public TMP_Text Stacks;
			public int DisplayedStacks = -1;
		}

		private readonly List<Entry> entries = new List<Entry>();

		private TMP_Text fontSource;

		private Color stacksColor;

		private int used;

		private int shown = -1;

		private float iconSize = DefaultIconSize;

		private Texture haloTexture;

		// iconSize: hotbar slots use the default; haloTexture: optional ring drawn behind icons added with a tint.
		public static AbilityBuffIconsView Create(RectTransform parent, Vector2 anchoredPosition, TMP_Text fontSource, Color stacksColor, float iconSize = DefaultIconSize, Texture haloTexture = null)
		{
			GameObject go = new GameObject("AbilityBuffIcons", typeof(RectTransform));
			RectTransform rect = (RectTransform)go.transform;
			rect.SetParent(parent, false);
			rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(MaxIcons * iconSize + (MaxIcons - 1) * Spacing, iconSize);
			rect.anchoredPosition = anchoredPosition;
			rect.SetAsLastSibling();
			AbilityBuffIconsView view = go.AddComponent<AbilityBuffIconsView>();
			view.iconSize = iconSize;
			view.haloTexture = haloTexture;
			view.fontSource = fontSource;
			view.stacksColor = stacksColor;
			return view;
		}

		public void Begin()
		{
			used = 0;
		}

		// remaining01 < 0 means the buff has no timer (stacks only); stacks <= 0 hides the number.
		public void Add(AbilityDef def, float remaining01, int stacks)
		{
			Add(AbilityIcons.Get(def), remaining01, stacks, Color.clear);
		}

		// tint.a > 0 marks debuffs with a soft halo, or tints the icon when no halo texture was given.
		public void Add(Sprite sprite, float remaining01, int stacks, Color tint)
		{
			if (used >= MaxIcons)
				return;
			Entry entry = used < entries.Count ? entries[used] : CreateEntry();
			used++;
			if (entry.Icon.sprite != sprite)
			{
				entry.Icon.sprite = entry.Sweep.sprite = sprite;
				entry.Icon.enabled = sprite != null;
			}
			bool halo = tint.a > 0f && haloTexture != null && sprite != null;
			if (entry.Halo.enabled != halo) entry.Halo.enabled = halo;
			if (entry.DisplayedTint != tint)
			{
				entry.DisplayedTint = tint;
				entry.Halo.color = tint;
				Color iconColor = tint.a > 0f && haloTexture == null ? Color.Lerp(Color.white, tint, 0.5f) : Color.white;
				iconColor.a = 1f;
				entry.Icon.color = iconColor;
			}
			bool timed = remaining01 >= 0f && sprite != null;
			if (entry.Sweep.enabled != timed)
				entry.Sweep.enabled = timed;
			if (timed)
				entry.Sweep.fillAmount = 1f - Mathf.Clamp01(remaining01);
			if (entry.DisplayedStacks != stacks)
			{
				entry.DisplayedStacks = stacks;
				if (stacks > 0) entry.Stacks.SetText("{0}", stacks);
				else entry.Stacks.text = string.Empty;
			}
		}

		public void End()
		{
			if (shown == used)
				return;
			shown = used;
			float total = used * iconSize + Mathf.Max(0, used - 1) * Spacing;
			for (int i = 0; i < entries.Count; i++)
			{
				bool visible = i < used;
				entries[i].Root.SetActive(visible);
				if (visible)
					entries[i].Rect.anchoredPosition = new Vector2(-total / 2f + iconSize / 2f + i * (iconSize + Spacing), 0f);
			}
		}

		public void Clear()
		{
			Begin();
			End();
		}

		private Entry CreateEntry()
		{
			Entry entry = new Entry();
			entry.Root = new GameObject("Buff", typeof(RectTransform));
			entry.Rect = (RectTransform)entry.Root.transform;
			entry.Rect.SetParent(transform, false);
			entry.Rect.anchorMin = entry.Rect.anchorMax = entry.Rect.pivot = new Vector2(0.5f, 0.5f);
			entry.Rect.sizeDelta = new Vector2(iconSize, iconSize);
			GameObject halo = new GameObject("Halo", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
			RectTransform haloRect = (RectTransform)halo.transform;
			haloRect.SetParent(entry.Rect, false);
			haloRect.anchorMin = haloRect.anchorMax = haloRect.pivot = new Vector2(0.5f, 0.5f);
			haloRect.sizeDelta = Vector2.one * (iconSize * HaloScale);
			entry.Halo = halo.GetComponent<RawImage>();
			entry.Halo.raycastTarget = false;
			entry.Halo.texture = haloTexture;
			entry.Halo.enabled = false;
			entry.Icon = CreateImage(entry.Rect, "Icon");
			entry.Icon.enabled = false;
			entry.Sweep = CreateImage(entry.Rect, "Sweep");
			entry.Sweep.color = SweepColor;
			entry.Sweep.type = Image.Type.Filled;
			entry.Sweep.fillMethod = Image.FillMethod.Radial360;
			entry.Sweep.fillOrigin = (int)Image.Origin360.Top;
			entry.Sweep.fillClockwise = true;
			entry.Stacks = CreateStacksText(entry.Rect);
			entry.Root.SetActive(false);
			entries.Add(entry);
			return entry;
		}

		private static Image CreateImage(RectTransform parent, string name)
		{
			GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			RectTransform rect = (RectTransform)go.transform;
			rect.SetParent(parent, false);
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = rect.offsetMax = Vector2.zero;
			Image image = go.GetComponent<Image>();
			image.raycastTarget = false;
			image.preserveAspect = true;
			return image;
		}

		private TMP_Text CreateStacksText(RectTransform parent)
		{
			GameObject go = new GameObject("Stacks", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
			RectTransform rect = (RectTransform)go.transform;
			rect.SetParent(parent, false);
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = new Vector2(0f, -6f * iconSize / DefaultIconSize);
			rect.offsetMax = new Vector2(6f * iconSize / DefaultIconSize, 0f);
			TMP_Text text = go.GetComponent<TextMeshProUGUI>();
			if (fontSource != null)
			{
				text.font = fontSource.font;
				text.fontSharedMaterial = fontSource.fontSharedMaterial;
				LocalizationFontApplier.RegisterLike(text, fontSource);
			}
			text.fontSize = 24f * iconSize / DefaultIconSize;
			text.alignment = TextAlignmentOptions.BottomRight;
			text.textWrappingMode = TextWrappingModes.NoWrap;
			text.overflowMode = TextOverflowModes.Overflow;
			text.color = stacksColor;
			text.raycastTarget = false;
			text.text = string.Empty;
			return text;
		}
	}
}
