using System;
using System.Collections.Generic;
using System.Globalization;
using Codebase.Abilities;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Localization;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Skills;
using Codebase.UI.Inventory;
using Codebase.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Pets
{
	public class PetAbilitiesView : PopupView
	{
		[Serializable]
		public class Layout
		{
			public RectTransform Panel;
			public TMP_Text Title;
			public Button CloseButton;
			public ScrollRect Scroll;
			public RectTransform Content;
		}

		private class Row
		{
			public Image Background;
			public Image Icon;
			public Image Lock;
			public TMP_Text Name;
			public TMP_Text Description;
			public TMP_Text Stars;
			public bool Unlocked;
			public string NameValue;
			public string DescriptionValue;
			public string StarsValue;
		}

		private const float HeaderHeight = 170f;
		private const float BottomInset = 24f;
		private static readonly Dictionary<string, IReadOnlyList<SkillDef>> speciesSkills = new Dictionary<string, IReadOnlyList<SkillDef>>(StringComparer.Ordinal);
		private static HashSet<string> skillLocalizationKeys;

		[SerializeField]
		private Layout layout = new Layout();

		private readonly List<Row> rows = new List<Row>();
		private ItemModel item;
		private int visibleCount = -1;

		internal Layout Elements => layout;

		public static IReadOnlyList<SkillDef> GetSpeciesSkills(ItemModel item)
		{
			if (item == null || !PetConfig.Enabled || string.IsNullOrEmpty(item.Config?.Key)) return Array.Empty<SkillDef>();
			string key = item.Config.Key;
			if (speciesSkills.TryGetValue(key, out IReadOnlyList<SkillDef> cached)) return cached;
			UnitSkillLoadout loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Pet, key);
			List<SkillDef> resolved = null;
			if (loadout?.Skills != null)
				foreach (SkillSlotDef slot in loadout.Skills)
				{
					if (slot == null || string.IsNullOrEmpty(slot.SkillKey)) continue;
					SkillDef def = SkillLibrary.FindSkill(slot.SkillKey);
					if (def == null) continue;
					if (resolved == null) resolved = new List<SkillDef>();
					resolved.Add(def);
				}
			cached = resolved != null ? (IReadOnlyList<SkillDef>)resolved : Array.Empty<SkillDef>();
			speciesSkills[key] = cached;
			return cached;
		}

		private static string GetSkillText(string key, string fallback)
		{
			if (skillLocalizationKeys == null)
			{
				skillLocalizationKeys = new HashSet<string>(LocalizationService.GetKeysWithPrefix("SKILL_NAME_"), StringComparer.Ordinal);
				skillLocalizationKeys.UnionWith(LocalizationService.GetKeysWithPrefix("SKILL_DESC_"));
			}
			if (!skillLocalizationKeys.Contains(key)) return fallback;
			string text = LocalizationStrings.Get(key);
			return string.IsNullOrEmpty(text) || text == key ? fallback : text;
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			speciesSkills.Clear();
			skillLocalizationKeys = null;
		}

		// Every non-hidden ability, locked ones included (the inventory list keeps only the first locked one); the unlocked signature goes last.
		public static List<int> GetAbilityIndices(ItemModel item)
		{
			var indices = new List<int>();
			int sigIndex = -1;
			var defs = item.Config.AbilityDefs;
			for (int i = 0; i < defs.Count; i++)
			{
				AbilityDef def = defs[i];
				if (global::Codebase.Abilities.Conditions.PetModeCondition.IsHidden(def)) continue;
				if (sigIndex < 0 && global::Codebase.Abilities.Conditions.PetModeCondition.IsPetOnly(def) && item.IsAbilityUnlocked(def)) { sigIndex = i; continue; }
				indices.Add(i);
			}
			if (sigIndex >= 0) indices.Add(sigIndex);
			return indices;
		}

		protected override void Awake()
		{
			base.Awake();
			if (layout.CloseButton != null) layout.CloseButton.onClick.AddListener(OnCloseClicked);
		}

		public override void Initialize()
		{
			item = null;
			FitPanel();
			base.Initialize();
		}

		public void Setup(ItemModel item)
		{
			this.item = item;
			SafeRender();
			if (layout.Scroll != null)
			{
				layout.Scroll.StopMovement();
				layout.Scroll.verticalNormalizedPosition = 1f;
			}
		}

		protected override void Render()
		{
			SafeRender();
		}

		private void SafeRender()
		{
			try
			{
				RenderAll();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void RenderAll()
		{
			if (item == null)
			{
				foreach (Row row in rows) row.Background.gameObject.SetActive(false);
				visibleCount = -1;
				return;
			}
			string name = PetConfig.Instance?.Find(item.Config)?.DisplayName;
			if (string.IsNullOrEmpty(name)) name = LocalizationStrings.Get("ITEM_NAME_" + item.Config.Key);
			PetStatsText.SetText(layout.Title, name + " 技能");
			List<int> indices = GetAbilityIndices(item);
			IReadOnlyList<SkillDef> skills = GetSpeciesSkills(item);
			int count = skills.Count + indices.Count;
			bool changed = visibleCount != count;
			while (rows.Count < count) rows.Add(CreateRow());
			for (int i = 0; i < rows.Count; i++)
			{
				Row row = rows[i];
				bool active = i < count;
				if (row.Background.gameObject.activeSelf != active) changed = true;
				row.Background.gameObject.SetActive(active);
				if (!active) continue;
				bool unlocked;
				string title;
				string description;
				string stars;
				Sprite icon;
				if (i < skills.Count)
				{
					SkillDef skill = skills[i];
					unlocked = true;
					title = GetSkillText("SKILL_NAME_" + skill.Key, skill.Key);
					description = GetSkillText("SKILL_DESC_" + skill.Key, string.Empty);
					stars = "<color=#7CE3FF>主动 · 冷却 " + skill.Cooldown.ToString("0.#", CultureInfo.InvariantCulture) + " 秒</color>";
					icon = AbilityIcons.Get(skill);
				}
				else
				{
					AbilityDef def = item.Config.AbilityDefs[indices[i - skills.Count]];
					unlocked = item.IsAbilityUnlocked(def);
					title = ItemAbilityView.GetTitle(def);
					if (ItemAbilityView.IsPetSignature(def)) title += "  <size=72%><color=#FFD54A>[专属]</color></size>";
					if (ItemAbilityView.HasCastSkill(def)) title += "  <size=72%><color=#7CE3FF>[主动]</color></size>";
					description = ItemAbilityView.GetDescription(item, def, false, false);
					stars = ItemAbilityView.GetStarsLine(item, def);
					icon = AbilityIcons.Get(def);
				}
				Sprite lockSprite = PetConfig.Instance != null ? PetConfig.Instance.LockSprite : null;
				bool showLock = !unlocked && lockSprite != null;
				if (row.NameValue != title || row.DescriptionValue != description || row.StarsValue != stars || row.Unlocked != unlocked || row.Lock.gameObject.activeSelf != showLock) changed = true;
				row.NameValue = title;
				row.DescriptionValue = description;
				row.StarsValue = stars;
				row.Unlocked = unlocked;
				PetStatsText.SetText(row.Name, title);
				PetStatsText.SetText(row.Description, description);
				PetStatsText.SetText(row.Stars, stars ?? string.Empty);
				row.Stars.gameObject.SetActive(!string.IsNullOrEmpty(stars));
				Color tint = unlocked ? Color.white : new Color(0.62f, 0.62f, 0.62f, 1f);
				row.Name.color = tint;
				row.Description.color = tint;
				row.Icon.sprite = icon;
				row.Icon.enabled = row.Icon.sprite != null;
				row.Icon.color = unlocked ? Color.white : ItemAbilityView.LockedIconTint;
				row.Lock.sprite = lockSprite;
				row.Lock.gameObject.SetActive(showLock);
				row.Background.color = new Color(0f, 0f, 0f, unlocked ? 0.3f : 0.18f);
			}
			visibleCount = count;
			if (!changed) return;
			LayoutRebuilder.ForceRebuildLayoutImmediate(layout.Content);
			float contentHeight = LayoutUtility.GetPreferredHeight(layout.Content);
			if (contentHeight <= 0f) contentHeight = layout.Content.rect.height;
			layout.Panel.sizeDelta = new Vector2(PetUiBuilder.PanelWidth, Mathf.Clamp(HeaderHeight + contentHeight + BottomInset, 640f, 1800f));
			FitPanel();
		}

		private Row CreateRow()
		{
			Row row = new Row();
			row.Background = PetUiBuilder.CreateImage(layout.Content, "Row" + rows.Count, null, new Color(0f, 0f, 0f, 0.3f));
			row.Background.raycastTarget = false;
			HorizontalLayoutGroup horizontal = row.Background.gameObject.AddComponent<HorizontalLayoutGroup>();
			horizontal.padding = new RectOffset(20, 20, 18, 18);
			horizontal.spacing = 20f;
			horizontal.childAlignment = TextAnchor.UpperLeft;
			horizontal.childControlWidth = true;
			horizontal.childControlHeight = true;
			horizontal.childForceExpandWidth = false;
			horizontal.childForceExpandHeight = false;
			row.Icon = PetUiBuilder.CreateImage(row.Background.transform, "Icon", null, Color.white);
			row.Icon.preserveAspect = true;
			row.Icon.raycastTarget = false;
			LayoutElement iconLayout = row.Icon.gameObject.AddComponent<LayoutElement>();
			iconLayout.minWidth = iconLayout.preferredWidth = 96f;
			iconLayout.minHeight = iconLayout.preferredHeight = 96f;
			iconLayout.flexibleWidth = 0f;
			row.Lock = PetUiBuilder.CreateImage(row.Icon.transform, "Lock", null, Color.white);
			row.Lock.preserveAspect = true;
			row.Lock.raycastTarget = false;
			PetUiBuilder.SetRect(row.Lock.rectTransform, Vector2.one, Vector2.one, Vector2.one, Vector2.zero, new Vector2(40f, 40f));
			RectTransform body = PetUiBuilder.CreateRect(row.Background.transform, "Body");
			VerticalLayoutGroup vertical = body.gameObject.AddComponent<VerticalLayoutGroup>();
			vertical.spacing = 6f;
			vertical.childControlWidth = true;
			vertical.childControlHeight = true;
			vertical.childForceExpandWidth = true;
			vertical.childForceExpandHeight = false;
			body.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
			row.Name = CreateRowText(body, "Name", 44f);
			row.Description = CreateRowText(body, "Description", 38f);
			row.Stars = CreateRowText(body, "Stars", 34f);
			return row;
		}

		private TMP_Text CreateRowText(Transform parent, string name, float size)
		{
			TMP_Text text = PetUiBuilder.CreateText(parent, name, size, TextAlignmentOptions.TopLeft);
			LocalizationFontApplier.RegisterLike(text, layout.Title);
			text.enableAutoSizing = false;
			text.textWrappingMode = TextWrappingModes.Normal;
			text.overflowMode = TextOverflowModes.Overflow;
			return text;
		}

		private void FitPanel()
		{
			if (layout.Panel == null || rectTransform == null) return;
			Rect rect = rectTransform.rect;
			float scale = Mathf.Min(1f, (rect.width - 24f) / PetUiBuilder.PanelWidth, (rect.height - 60f) / layout.Panel.sizeDelta.y);
			if (float.IsNaN(scale) || scale <= 0.1f) scale = 1f;
			layout.Panel.localScale = new Vector3(scale, scale, 1f);
		}

		private void OnCloseClicked()
		{
			PetUiBuilder.PlayTap();
			Close();
		}
	}
}
