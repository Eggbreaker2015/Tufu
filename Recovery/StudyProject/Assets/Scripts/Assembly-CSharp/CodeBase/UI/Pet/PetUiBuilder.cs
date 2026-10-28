using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Pet;
using Codebase.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Pets
{
	// Pet mode UI built in code: the collection popup template and its inventory entry button (no prefab, no scene edit).
	public static class PetUiBuilder
	{
		public const float PanelWidth = 1020f;

		public const float PanelHeight = 1880f;

		// Five 180 px cells with 22.5 px spacing fill the 990 px page.
		public const float PageInset = 15f;

		public const float CellWidth = 180f;

		public const float CellHeight = 292f;

		public const int Columns = 5;

		private const string EntryButtonName = "PetBookButton";

		private static readonly Color PanelColor = new Color(0.1f, 0.12f, 0.19f, 0.98f);

		private static readonly Color ButtonColor = new Color(0.25f, 0.55f, 0.95f, 1f);

		private static readonly Color CloseColor = new Color(0.85f, 0.3f, 0.3f, 1f);

		private static readonly Color BlockColor = new Color(0f, 0f, 0f, 0.3f);

		private static GameObject holder;

		private static PetCollectionView template;

		[StaticReset]
		private static void ResetStaticState()
		{
			holder = null;
			template = null;
		}

		public static T Find<T>() where T : class
		{
			return GameContext.Has<T>() ? GameContext.Get<T>() : null;
		}

		public static void PlayTap()
		{
			Find<IAudioPlayer>()?.PlaySoundOnce("tap");
		}

		public static void OpenCollection()
		{
			if (!PetConfig.Enabled) return;
			UINavigator navigator = Find<UINavigator>();
			PetCollectionView view = GetTemplate();
			if (navigator == null || view == null) return;
			PlayTap();
			navigator.OpenTemplate(view, true);
		}

		// Built once under an inactive holder, so the template's Awake never runs; the navigator's pool clones it.
		public static PetCollectionView GetTemplate()
		{
			if (template != null && holder != null) return template;
			if (holder != null) Object.Destroy(holder);
			holder = new GameObject("PetUiTemplates", typeof(RectTransform));
			holder.SetActive(false);
			UINavigator navigator = Find<UINavigator>();
			if (navigator != null) holder.transform.SetParent(navigator.transform, false);
			template = BuildCollectionView(holder.transform);
			return template;
		}

		// The 图鉴 button left of the Stats panel header, mirroring its Info_Button.
		public static void InstallInventoryEntry(InventoryView inventoryView)
		{
			if (!PetConfig.Enabled || inventoryView == null) return;
			Transform stats = inventoryView.transform.Find("Content/Stats");
			if (stats == null)
			{
				Debug.LogWarning("[Pet] Inventory Stats panel not found; no collection entry button.");
				return;
			}
			if (stats.Find(EntryButtonName) != null) return;
			PetConfig config = PetConfig.Instance;
			RectTransform info = stats.Find("Info_Button") as RectTransform;
			Image infoImage = info != null ? info.GetComponent<Image>() : null;
			Sprite buttonSprite = config.ButtonSprite != null ? config.ButtonSprite : infoImage != null ? infoImage.sprite : null;
			Image image = CreateImage(stats, EntryButtonName, buttonSprite, ButtonColor);
			RectTransform rect = image.rectTransform;
			Vector2 size = info != null ? info.sizeDelta : new Vector2(84f, 92f);
			Vector2 position = info != null ? new Vector2(-info.anchoredPosition.x, info.anchoredPosition.y) : new Vector2(78f, -4f);
			SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), info != null ? info.pivot : new Vector2(0.5f, 0.5f), position, size);
			Button button = image.gameObject.AddComponent<Button>();
			button.targetGraphic = image;
			if (config.BookIconSprite != null)
			{
				Image icon = CreateImage(rect, "Icon", config.BookIconSprite, Color.white);
				icon.preserveAspect = true;
				icon.raycastTarget = false;
				SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 9f), new Vector2(64f, 64f));
			}
			// Label beside the button; a child, so tapping it also presses the button.
			TMP_Text label = CreateText(rect, "Label", 38f, TextAlignmentOptions.Left);
			label.raycastTarget = true;
			SetRect(label.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 4f), new Vector2(120f, 64f));
			image.gameObject.AddComponent<PetCollectionEntryButton>().Setup(button, label);
		}

		private static PetCollectionView BuildCollectionView(Transform parent)
		{
			PetConfig config = PetConfig.Instance;
			GameObject root = new GameObject("PetCollection", typeof(RectTransform), typeof(CanvasGroup));
			root.transform.SetParent(parent, false);
			RectTransform rootRect = (RectTransform)root.transform;
			Stretch(rootRect, Vector2.zero, Vector2.zero);
			PetCollectionView view = root.AddComponent<PetCollectionView>();
			PetCollectionView.Layout layout = view.Elements;

			Image panel = CreateImage(rootRect, "Panel", config != null ? config.PanelSprite : null, PanelColor);
			layout.Panel = panel.rectTransform;
			SetRect(layout.Panel, Center, Center, Center, Vector2.zero, new Vector2(PanelWidth, PanelHeight));

			layout.Title = CreateText(layout.Panel, "Title", 64f, TextAlignmentOptions.Center);
			SetRect(layout.Title.rectTransform, Top, Top, Top, new Vector2(0f, -30f), new Vector2(600f, 100f));
			layout.Title.text = "宠物图鉴";

			Image close = CreateImage(layout.Panel, "Close", config != null ? config.CloseButtonSprite : null, CloseColor);
			SetRect(close.rectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-18f, -18f), new Vector2(170f, 110f));
			layout.CloseButton = close.gameObject.AddComponent<Button>();
			layout.CloseButton.targetGraphic = close;
			if (config != null && config.CloseIconSprite != null)
			{
				Image icon = CreateImage(close.rectTransform, "Icon", config.CloseIconSprite, Color.white);
				icon.raycastTarget = false;
				icon.preserveAspect = true;
				SetRect(icon.rectTransform, Center, Center, Center, new Vector2(0f, 8f), new Vector2(64f, 64f));
			}
			else
			{
				TMP_Text cross = CreateText(close.rectTransform, "Icon", 60f, TextAlignmentOptions.Center);
				Stretch(cross.rectTransform, Vector2.zero, Vector2.zero);
				cross.text = "X";
			}

			layout.CollectionTab = CreateTab(layout.Panel, "CollectionTab", new Vector2(-165f, -150f), out layout.CollectionTabImage, out layout.CollectionTabLabel);
			layout.SynergyTab = CreateTab(layout.Panel, "SynergyTab", new Vector2(165f, -150f), out layout.SynergyTabImage, out layout.SynergyTabLabel);

			// Collection page: progress, shards and bonuses above the species grid.
			RectTransform collection = CreateRect(layout.Panel, "CollectionPage");
			Stretch(collection, new Vector2(PageInset, 24f), new Vector2(-PageInset, -270f));
			layout.CollectionPage = collection.gameObject;
			layout.Progress = CreateText(collection, "Progress", 46f, TextAlignmentOptions.Left);
			SetRect(layout.Progress.rectTransform, new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 60f));
			layout.Shards = CreateText(collection, "Shards", 46f, TextAlignmentOptions.Right);
			SetRect(layout.Shards.rectTransform, new Vector2(0.45f, 1f), Vector2.one, Vector2.one, new Vector2(-10f, 0f), new Vector2(-10f, 60f));
			layout.Bonus = CreateText(collection, "Bonus", 34f, TextAlignmentOptions.TopLeft);
			layout.Bonus.textWrappingMode = TextWrappingModes.Normal;
			SetRect(layout.Bonus.rectTransform, new Vector2(0f, 1f), Vector2.one, Top, new Vector2(0f, -68f), new Vector2(-20f, 136f));
			layout.CollectionScroll = CreateScroll(collection, out layout.CollectionContent);
			Stretch((RectTransform)layout.CollectionScroll.transform, Vector2.zero, new Vector2(0f, -214f));

			// Synergy page: a hint and one block per damage type.
			RectTransform synergy = CreateRect(layout.Panel, "SynergyPage");
			Stretch(synergy, new Vector2(PageInset, 24f), new Vector2(-PageInset, -270f));
			layout.SynergyPage = synergy.gameObject;
			TMP_Text hint = CreateText(synergy, "Hint", 34f, TextAlignmentOptions.TopLeft);
			hint.textWrappingMode = TextWrappingModes.Normal;
			SetRect(hint.rectTransform, new Vector2(0f, 1f), Vector2.one, Top, new Vector2(0f, 0f), new Vector2(-20f, 120f));
			var blocks = new System.Collections.Generic.List<TMP_Text>();
			for (int i = 0; i < PetSynergyTab.Types.Length; i++)
			{
				Image block = CreateImage(synergy, "Block" + i, null, BlockColor);
				block.raycastTarget = false;
				SetRect(block.rectTransform, new Vector2(0f, 1f), Vector2.one, Top, new Vector2(0f, -132f - i * 356f), new Vector2(0f, 332f));
				TMP_Text text = CreateText(block.rectTransform, "Text", 38f, TextAlignmentOptions.TopLeft);
				Stretch(text.rectTransform, new Vector2(24f, 14f), new Vector2(-24f, -14f));
				blocks.Add(text);
			}
			layout.Synergy = synergy.gameObject.AddComponent<PetSynergyTab>();
			layout.Synergy.Setup(hint, blocks);
			synergy.gameObject.SetActive(false);
			return view;
		}

		private static Button CreateTab(RectTransform parent, string name, Vector2 position, out Image image, out TMP_Text label)
		{
			image = CreateImage(parent, name, null, Color.gray);
			SetRect(image.rectTransform, Top, Top, Top, position, new Vector2(310f, 100f));
			Button button = image.gameObject.AddComponent<Button>();
			button.targetGraphic = image;
			label = CreateText(image.rectTransform, "Label", 46f, TextAlignmentOptions.Center);
			Stretch(label.rectTransform, new Vector2(10f, 6f), new Vector2(-10f, -6f));
			return button;
		}

		private static ScrollRect CreateScroll(RectTransform parent, out RectTransform content)
		{
			Image background = CreateImage(parent, "Scroll", null, new Color(0f, 0f, 0f, 0.25f));
			ScrollRect scroll = background.gameObject.AddComponent<ScrollRect>();
			RectTransform viewport = CreateRect(background.rectTransform, "Viewport");
			Stretch(viewport, Vector2.zero, Vector2.zero);
			viewport.gameObject.AddComponent<RectMask2D>();
			content = CreateRect(viewport, "Content");
			SetRect(content, new Vector2(0f, 1f), Vector2.one, Top, Vector2.zero, Vector2.zero);
			VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 12f;
			layout.padding = new RectOffset(0, 0, 12, 30);
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = false;
			ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			scroll.content = content;
			scroll.viewport = viewport;
			scroll.horizontal = false;
			scroll.vertical = true;
			scroll.movementType = ScrollRect.MovementType.Elastic;
			scroll.scrollSensitivity = 40f;
			return scroll;
		}

		// Rarity header of the collection grid.
		public static TMP_Text CreateGroupHeader(Transform parent)
		{
			TMP_Text header = CreateText(parent, "Header", 44f, TextAlignmentOptions.Left);
			header.enableAutoSizing = false;
			header.margin = new Vector4(10f, 0f, 10f, 0f);
			LayoutElement element = header.gameObject.AddComponent<LayoutElement>();
			element.minHeight = 64f;
			element.preferredHeight = 64f;
			return header;
		}

		public static RectTransform CreateGrid(Transform parent)
		{
			RectTransform grid = CreateRect(parent, "Grid");
			GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
			layout.cellSize = new Vector2(CellWidth, CellHeight);
			layout.spacing = new Vector2(22.5f, 20f);
			layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			layout.constraintCount = Columns;
			layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
			layout.startAxis = GridLayoutGroup.Axis.Horizontal;
			layout.childAlignment = TextAnchor.UpperLeft;
			return grid;
		}

		public static PetBookCell CreateCell(Transform parent)
		{
			PetConfig config = PetConfig.Instance;
			RectTransform root = CreateRect(parent, "Cell");
			root.sizeDelta = new Vector2(CellWidth, CellHeight);
			PetBookCell cell = root.gameObject.AddComponent<PetBookCell>();

			cell.Frame = CreateImage(root, "Frame", null, Color.gray);
			SetRect(cell.Frame.rectTransform, Top, Top, Top, Vector2.zero, new Vector2(CellWidth, CellWidth));
			cell.Button = root.gameObject.AddComponent<Button>();
			cell.Button.targetGraphic = cell.Frame;
			cell.Button.transition = Selectable.Transition.None;

			cell.Icon = CreateImage(cell.Frame.rectTransform, "Icon", null, Color.white);
			cell.Icon.raycastTarget = false;
			cell.Icon.preserveAspect = true;
			Stretch(cell.Icon.rectTransform, new Vector2(16f, 16f), new Vector2(-16f, -16f));

			cell.Lock = CreateImage(cell.Frame.rectTransform, "Lock", config != null ? config.LockSprite : null, Color.clear);
			cell.Lock.raycastTarget = false;
			cell.Lock.preserveAspect = true;
			SetRect(cell.Lock.rectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-6f, -6f), new Vector2(52f, 52f));

			cell.Level = CreateText(cell.Frame.rectTransform, "Level", 34f, TextAlignmentOptions.BottomRight);
			SetRect(cell.Level.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-18f, 44f));

			cell.Name = CreateText(root, "Name", 34f, TextAlignmentOptions.Center);
			SetRect(cell.Name.rectTransform, new Vector2(0f, 1f), Vector2.one, Top, new Vector2(0f, -184f), new Vector2(0f, 48f));

			cell.UnlockImage = CreateImage(root, "Unlock", config != null ? config.ButtonSprite : null, ButtonColor);
			SetRect(cell.UnlockImage.rectTransform, Top, Top, Top, new Vector2(0f, -234f), new Vector2(176f, 58f));
			cell.UnlockButton = cell.UnlockImage.gameObject.AddComponent<Button>();
			cell.UnlockButton.targetGraphic = cell.UnlockImage;
			cell.UnlockLabel = CreateText(cell.UnlockImage.rectTransform, "Label", 30f, TextAlignmentOptions.Center);
			Stretch(cell.UnlockLabel.rectTransform, new Vector2(6f, 4f), new Vector2(-6f, -4f));

			cell.Note = CreateText(root, "Note", 28f, TextAlignmentOptions.Center);
			cell.Note.color = new Color(0.75f, 0.75f, 0.75f, 1f);
			SetRect(cell.Note.rectTransform, new Vector2(0f, 1f), Vector2.one, Top, new Vector2(0f, -234f), new Vector2(0f, 58f));
			return cell;
		}

		public static RectTransform CreateRect(Transform parent, string name)
		{
			GameObject go = new GameObject(name, typeof(RectTransform));
			go.layer = parent != null ? parent.gameObject.layer : go.layer;
			RectTransform rect = (RectTransform)go.transform;
			rect.SetParent(parent, false);
			return rect;
		}

		public static Image CreateImage(Transform parent, string name, Sprite sprite, Color fallbackColor)
		{
			RectTransform rect = CreateRect(parent, name);
			rect.gameObject.AddComponent<CanvasRenderer>();
			Image image = rect.gameObject.AddComponent<Image>();
			SetSprite(image, sprite, fallbackColor);
			return image;
		}

		// Sliced when the sprite has borders; a plain colored rect when the sprite is missing.
		public static void SetSprite(Image image, Sprite sprite, Color fallbackColor)
		{
			if (image == null) return;
			image.sprite = sprite;
			image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
			image.color = sprite != null ? Color.white : fallbackColor;
		}

		public static TMP_Text CreateText(Transform parent, string name, float size, TextAlignmentOptions alignment)
		{
			RectTransform rect = CreateRect(parent, name);
			rect.gameObject.AddComponent<CanvasRenderer>();
			TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
			if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
			text.richText = true;
			text.raycastTarget = false;
			text.color = Color.white;
			text.alignment = alignment;
			text.textWrappingMode = TextWrappingModes.NoWrap;
			text.overflowMode = TextOverflowModes.Overflow;
			text.enableAutoSizing = true;
			text.fontSizeMax = size;
			text.fontSizeMin = Mathf.Min(size, 16f);
			text.fontSize = size;
			text.text = string.Empty;
			return text;
		}

		public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
		{
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.pivot = pivot;
			rect.anchoredPosition = position;
			rect.sizeDelta = size;
		}

		public static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.pivot = Center;
			rect.offsetMin = offsetMin;
			rect.offsetMax = offsetMax;
		}

		private static Vector2 Center => new Vector2(0.5f, 0.5f);

		private static Vector2 Top => new Vector2(0.5f, 1f);
	}
}
