using System;
using System.Collections.Generic;
using Codebase.Abilities;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Localization;
using Codebase.Pet;
using Codebase.Skills;
using Codebase.Skills.Components;
using Codebase.Unit;
using Scellecs.Morpeh;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.World
{
	// Under an enemy health bar: a cast bar (localized skill name + wind-up fill, casts with CastPoint >= 0.3 s only)
	// and up to 3 buff icons (debuffs ringed red). Built in code once per pooled bar, no extra canvas target;
	// HealthBar.Initialize unbinds it and EnemyHealthBarSystem binds it to the unit on every spawn.
	public class EnemySkillStatusView : MonoBehaviour
	{
		private const float BarWidth = 200f;
		private const float BarHeight = 24f;
		private const float BarGap = 4f;
		private const float NameOverhang = 12f;
		private const float IconSize = 36f;
		private const float IconGap = 4f;
		private const float MinCastPoint = 0.3f;
		private const float BuffRefreshInterval = 0.1f;
		private const string NamePrefix = "SKILL_NAME_";
		private static readonly Color DebuffTint = new Color(1f, 0.2f, 0.15f, 0.95f);
		private static readonly Dictionary<string, string> nameCache = new Dictionary<string, string>(StringComparer.Ordinal);
		private static HashSet<string> nameKeys;
		private static bool subscribed;
		private static int localeVersion;

		private HealthBar owner;
		private UnitCore unit;
		private TMP_Text fontSource;
		private GameObject castBar;
		private RectTransform fill;
		private TMP_Text skillName;
		private AbilityBuffIconsView icons;
		private Scellecs.Morpeh.World world;
		private Stash<SkillCastingComponent> castingStash;
		private Stash<UnitBuffsComponent> buffsStash;
		private SkillDef shownDef;
		private int appliedLocaleVersion = -1;
		private float shownProgress = -1f;
		private float nextBuffRefresh;
		private readonly BuffDef[] addedBuffs = new BuffDef[AbilityBuffIconsView.MaxIcons];

		public static EnemySkillStatusView Create(HealthBar owner, RectTransform root, TMP_Text fontSource)
		{
			EnsureLocalization();
			RectTransform container = CreateRect("SkillStatus", root);
			container.anchorMin = container.anchorMax = new Vector2(0.5f, 0f);
			container.pivot = new Vector2(0.5f, 1f);
			container.anchoredPosition = Vector2.zero;
			container.sizeDelta = new Vector2(BarWidth, BarGap + BarHeight + IconGap + IconSize);
			container.SetAsLastSibling();
			EnemySkillStatusView view = container.gameObject.AddComponent<EnemySkillStatusView>();
			view.enabled = false;
			view.owner = owner;
			view.fontSource = fontSource;

			RectTransform bar = CreateRect("CastBar", container);
			bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 1f);
			bar.anchoredPosition = new Vector2(0f, -BarGap);
			bar.sizeDelta = new Vector2(BarWidth, BarHeight);
			Image background = bar.gameObject.AddComponent<Image>();
			background.color = new Color(0f, 0f, 0f, 0.65f);
			background.raycastTarget = false;
			view.castBar = bar.gameObject;

			view.fill = CreateRect("Fill", bar);
			Stretch(view.fill);
			view.fill.pivot = new Vector2(0f, 0.5f);
			view.fill.offsetMin = new Vector2(2f, 2f);
			view.fill.offsetMax = new Vector2(-2f, -2f);
			Image fillImage = view.fill.gameObject.AddComponent<Image>();
			fillImage.color = new Color(1f, 0.62f, 0.12f, 1f);
			fillImage.raycastTarget = false;

			RectTransform nameRect = CreateRect("Name", bar);
			Stretch(nameRect);
			// TMP's Ellipsis drops the whole text when one line is taller than the rect (22 pt lines are 26-32 units in every bound font).
			nameRect.offsetMin = new Vector2(4f, -NameOverhang);
			nameRect.offsetMax = new Vector2(-4f, NameOverhang);
			view.skillName = nameRect.gameObject.AddComponent<TextMeshProUGUI>();
			view.skillName.alignment = TextAlignmentOptions.Center;
			view.skillName.fontSize = 22f;
			view.skillName.enableAutoSizing = false;
			view.skillName.textWrappingMode = TextWrappingModes.NoWrap;
			view.skillName.overflowMode = TextOverflowModes.Ellipsis;
			view.skillName.color = Color.white;
			view.skillName.raycastTarget = false;
			view.skillName.text = string.Empty;
			view.ApplyNameFont();
			nameRect.gameObject.SetActive(false);
			view.castBar.SetActive(false);

			view.icons = AbilityBuffIconsView.Create(container, Vector2.zero, fontSource, Color.white, IconSize, SkillLibrary.SharedTelegraphTexture);
			RectTransform iconsRect = (RectTransform)view.icons.transform;
			iconsRect.anchorMin = iconsRect.anchorMax = new Vector2(0.5f, 1f);
			iconsRect.anchoredPosition = new Vector2(0f, -(BarGap + BarHeight + IconGap + IconSize / 2f));
			return view;
		}

		public void Bind(UnitCore unit)
		{
			if (!(unit is EnemyUnit))
			{
				Unbind();
				return;
			}
			this.unit = unit;
			HideCast();
			shownProgress = -1f;
			icons.Clear();
			Array.Clear(addedBuffs, 0, addedBuffs.Length);
			nextBuffRefresh = Time.unscaledTime;
			enabled = true;
		}

		public void Unbind()
		{
			unit = null;
			HideCast();
			icons.Clear();
			Array.Clear(addedBuffs, 0, addedBuffs.Length);
			enabled = false;
		}

		public bool IsBoundTo(UnitCore unit)
		{
			return this.unit == unit;
		}

		private void LateUpdate()
		{
			if (unit == null || unit.HealthBar != owner || unit.EntityProvider == null)
			{
				Unbind();
				return;
			}
			Scellecs.Morpeh.World current = Scellecs.Morpeh.World.Default;
			if (current == null) return;
			if (world != current)
			{
				world = current;
				castingStash = current.GetStash<SkillCastingComponent>();
				buffsStash = current.GetStash<UnitBuffsComponent>();
			}
			Entity entity = unit.EntityProvider.Entity;
			if (world.IsDisposed(entity))
			{
				HideCast();
				icons.Clear();
				return;
			}

			ref SkillCastingComponent cast = ref castingStash.Get(entity, out bool casting);
			SkillDef def = casting && !cast.Fired && cast.Slot != null ? cast.Slot.Def : null;
			if (def == null || !(def.CastPoint >= MinCastPoint))
				HideCast();
			else
			{
				if (shownDef != def || appliedLocaleVersion != localeVersion)
				{
					string name = GetSkillName(def);
					if (appliedLocaleVersion != localeVersion) ApplyNameFont();
					if (skillName.text != (name ?? string.Empty)) skillName.text = name ?? string.Empty;
					SetActive(skillName.gameObject, !string.IsNullOrEmpty(name));
					shownDef = def;
				}
				float progress = Mathf.Clamp01(cast.Elapsed / def.CastPoint);
				if (shownProgress != progress)
				{
					shownProgress = progress;
					fill.localScale = new Vector3(progress, 1f, 1f);
				}
				SetActive(castBar, true);
			}

			// Buff icons at ~10 Hz: keeps the radial sweeps from rebuilding every frame on every enemy.
			float now = Time.unscaledTime;
			if (now < nextBuffRefresh) return;
			nextBuffRefresh = now + BuffRefreshInterval;
			icons.Begin();
			int added = 0;
			ref UnitBuffsComponent buffs = ref buffsStash.Get(entity, out bool hasBuffs);
			if (hasBuffs && buffs.Buffs != null)
				for (int i = 0; i < buffs.Buffs.Count && added < addedBuffs.Length; i++)
				{
					BuffInstance instance = buffs.Buffs[i];
					if (instance == null || instance.Def == null) continue;
					BuffDef buff = instance.Def;
					bool duplicate = false;
					for (int j = 0; j < added; j++)
						if (addedBuffs[j] == buff) { duplicate = true; break; }
					if (duplicate) continue;
					Sprite sprite = AbilityIcons.Get(buff);
					if (sprite == null) continue;
					addedBuffs[added++] = buff;
					float remaining = buff.Duration > 0f ? instance.Remaining / buff.Duration : -1f;
					int stacks = buff.Stack == BuffStackMode.Stack && instance.Stacks > 1 ? instance.Stacks : 0;
					icons.Add(sprite, remaining, stacks, buff.IsDebuff ? DebuffTint : Color.clear);
				}
			Array.Clear(addedBuffs, 0, addedBuffs.Length);
			icons.End();
		}

		private void HideCast()
		{
			SetActive(castBar, false);
			shownDef = null;
		}

		private void ApplyNameFont()
		{
			if (fontSource != null)
			{
				skillName.font = fontSource.font;
				skillName.fontSharedMaterial = fontSource.fontSharedMaterial;
			}
			LocalizationFontApplier.RegisterLike(skillName, fontSource);
			if (PetConfig.Enabled || LocalizationService.CurrentLocale == "zh") PetStatsText.ApplyCjkFont(skillName);
			appliedLocaleVersion = localeVersion;
		}

		private static void EnsureLocalization()
		{
			if (!subscribed)
			{
				LocalizationService.OnLocaleChanged += OnLocaleChanged;
				subscribed = true;
			}
			if (nameKeys == null) nameKeys = new HashSet<string>(LocalizationService.GetKeysWithPrefix(NamePrefix), StringComparer.Ordinal);
		}

		private static string GetSkillName(SkillDef def)
		{
			if (string.IsNullOrEmpty(def.Key)) return null;
			EnsureLocalization();
			if (nameCache.TryGetValue(def.Key, out string name)) return name;
			string key = NamePrefix + def.Key;
			if (nameKeys.Contains(key))
			{
				try
				{
					name = LocalizationService.Get(key);
					if (string.IsNullOrEmpty(name) || name == key) name = null;
				}
				catch (IndexOutOfRangeException)
				{
					// A row with fewer than 13 locale values throws inside LocalizationService.Get.
					SkillLibrary.WarnOnce("locname:" + key, "[Skills] Incomplete localization row '" + key + "'");
					name = null;
				}
			}
			nameCache[def.Key] = name;
			return name;
		}

		private static void OnLocaleChanged()
		{
			nameCache.Clear();
			localeVersion++;
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			if (subscribed) LocalizationService.OnLocaleChanged -= OnLocaleChanged;
			subscribed = false;
			nameCache.Clear();
			nameKeys = null;
			localeVersion = 0;
		}

		private static RectTransform CreateRect(string name, RectTransform parent)
		{
			RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
			rect.SetParent(parent, false);
			return rect;
		}

		private static void Stretch(RectTransform rect)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = rect.offsetMax = Vector2.zero;
		}

		private static void SetActive(GameObject target, bool active)
		{
			if (target.activeSelf != active) target.SetActive(active);
		}
	}
}
