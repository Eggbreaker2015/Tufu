using Codebase.Utilities;
using Codebase.Player.Models;
using Codebase.Abilities.Effects;
using Codebase.Abilities.Conditions;
using System.Linq;
using System.Collections.Generic;
using Codebase.Abilities;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.DailyProcs;
using Codebase.Player.Inventory;
using Codebase.Unit;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Inventory
{
	public class ItemAbilityView : View
	{
		private const string DailyLimitReachedColorHex = "D98B8B";

		public static readonly Color LockedIconTint = new Color(0.45f, 0.45f, 0.45f, 0.8f);

		[SerializeField]
		private GameObject withStarsAbilityLocker;

		[SerializeField]
		private RectTransform withStarsRoot;

		[SerializeField]
		private TMP_Text withStarsName;

		[SerializeField]
		private TMP_Text withStarsDescription;

		[SerializeField]
		private GameObject noStarsAbilityLocker;

		[SerializeField]
		private RectTransform noStarsRoot;

		[SerializeField]
		private TMP_Text noStarsName;

		[SerializeField]
		private TMP_Text noStarsDescription;

		[SerializeField]
		private ItemStarsView starsView;

		[Inject]
		private DailyProcsModel dailyProcsModel;

		private ItemModel model;

		private int abilityIndex;

		private bool showLevelToUnlock;

		private bool hideNextLevel;

		public void Setup(ItemModel itemModel)
		{
			// Native ELF VA 0x1653fec.
			Setup(itemModel,0,false,false);
		}

		public void Setup(ItemModel itemModel, int abilityIndex, bool showLevelToUnlock = false, bool hideNextLevel = false)
		{
			// Native ELF VA 0x16542c0.
			model = itemModel; this.abilityIndex = abilityIndex; this.showLevelToUnlock = showLevelToUnlock; this.hideNextLevel = hideNextLevel; Initialize(); Render();
		}

		public static string AppendAccumulatedStat(string description, AbilityDef def)
		{
			// Native ELF VA 0x16542fc; original float sum, first permanent effect types.
			if (AbilityDescriptionContext.IsEconomyAbility(def)) return description;
			List<StatType> types = ResolvePermanentStatTypes(def); if (types.Count == 0) return description;
			var unit = GameContext.Get<PlayerModel>()?.Unit; if (unit == null) return description;
			float sum = 0f; bool found = false, percent = false;
			foreach (var stat in unit.Stats) if (types.Contains(stat.Type)) foreach (var modifier in stat.Modifiers)
			    if (modifier.Name == def.Key && modifier.Source == ModifierSource.AbilityPermanent) {
			        sum += modifier.Value; found = true;
			        percent = modifier.ModifierType == (ModifierType)1 || modifier.ModifierType == (ModifierType)2 || modifier.ModifierType == (ModifierType)4 || modifier.ModifierType == (ModifierType)5;
			    }
			if (found) description += " <color=#8BF07A>(+" + (percent ? ((double)(sum * 100f)).Format() + "%" : ((double)sum).Format()) + ")</color>";
			return description;
		}

		public static List<StatType> ResolvePermanentStatTypes(AbilityDef def)
		{
			// Native ELF VA 0x165492c, first matching permanent effect.
			foreach (var effect in def.Effects) {
			    if (effect is AddStatEffect add && add.Source == ModifierSource.AbilityPermanent) return new List<StatType>{add.StatType};
			    if (effect is GrantRandomStatPerDistanceEffect random && random.Source == ModifierSource.AbilityPermanent) return random.StatPool;
			}
			return new List<StatType>();
		}

		public static bool HasReachedDailyLimit(AbilityDef def, DailyProcsModel dailyProcs)
		{
			// Native ELF VA 0x1654b1c.
			if (dailyProcs == null) dailyProcs = GameContext.Has<DailyProcsModel>() ? GameContext.Get<DailyProcsModel>() : null;
			if (dailyProcs == null) return false;
			foreach (var effect in def.Effects) {
			    if (effect is AddStatEffect add && add.UseDailyProcLimit && dailyProcs.HasReachedLimit(def.Key, add.DailyProcLimit)) return true;
			    if (effect is GrantRandomStatPerDistanceEffect random && dailyProcs.HasReachedLimit(def.Key, random.DailyLimit)) return true;
			}
			return false;
		}

		public static string GetTitle(AbilityDef def)
		{
			return LocalizationStrings.Get("ABILITY_NAME_" + def.Key);
		}

		public static string GetDescription(ItemModel model, AbilityDef def, bool showLevelToUnlock, bool hideNextLevel, DailyProcsModel dailyProcs = null)
		{
			bool unlocked = model.IsAbilityUnlocked(def);
			string text;
			if (unlocked) {
				text = LocalizationStrings.Get("ABILITY_DESCRIPTION_"+def.Key);
				var state = model.AbilityStates.FirstOrDefault(s => s.Def == def);
				if (state != null) {
					text = state.DescriptionContext.Resolve(text, hideNextLevel); text = AppendAccumulatedStat(text,def);
					if (HasReachedDailyLimit(def, dailyProcs)) text += " <color=#D98B8B>(" + LocalizationStrings.Get("ABILITY_DAILY_LIMIT_REACHED") + ")</color>";
				}
			} else if (showLevelToUnlock) text = LocalizationStrings.Get("ABILITY_CHEST_LEVEL_TO_UNLOCK").Replace("{value}",def.UnlockLevel.ToString());
			else if (model.IsPreDivine) text = LocalizationStrings.Get("ABILITY_UNLOCK_CONDITION").Replace("{value}",model.LevelsToEvolve.ToString());
			else text = LocalizationStrings.Get("ABILITY_UNLOCKS_AT") + "<br>" + ItemStarsView.BuildStarsSpriteText(def.UnlockLevel);
			return text;
		}

		public static string GetStarsLine(ItemModel model, AbilityDef def)
		{
			if (!model.IsAbilityUnlocked(def) || def.LevelScaling == null || def.LevelScaling.Count == 0) return null;
			var state = model.AbilityStates.FirstOrDefault(s => s.Def == def);
			if (state == null) return null;
			int next = state.GetNextStarLevel();
			return (LocalizationStrings.Get("ABILITY_STARS_DESCRIPTION") + " ").Replace("{level}", next.ToString()) + ItemStarsView.BuildStarsSpriteText(next);
		}

		public static bool HasCastSkill(AbilityDef def)
		{
			return def != null && def.Effects != null && def.Effects.Any(effect => effect is CastSkillEffect);
		}

		public static bool IsPetSignature(AbilityDef def)
		{
			return PetModeCondition.IsPetOnly(def) && !HasCastSkill(def);
		}

		protected override void Render()
		{
			// Native ELF VA 0x1654cb0; resolve original localized description through the real state.
			if (model == null || abilityIndex < 0 || abilityIndex >= model.Config.AbilityDefs.Count) return;
			AbilityDef def = model.Config.AbilityDefs[abilityIndex]; bool unlocked = model.IsAbilityUnlocked(def);
			string title = GetTitle(def), text = GetDescription(model, def, showLevelToUnlock, hideNextLevel, dailyProcsModel); bool hasStars = false;
			if (unlocked) hasStars = def.LevelScaling.Count > 0;
			withStarsRoot.gameObject.SetActive(hasStars); noStarsRoot.gameObject.SetActive(!hasStars);
			withStarsAbilityLocker.SetActive(!unlocked); noStarsAbilityLocker.SetActive(!unlocked);
			SetLabel(noStarsName, title); SetLabel(noStarsDescription, text);
			if (hasStars) { SetLabel(withStarsName, title); SetLabel(withStarsDescription, text); Injector.InjectInto(starsView); if(starsView != null) starsView.Setup(model,abilityIndex); }
			// Effect-category icon in front of the name; greyed out while the ability is locked.
			Sprite icon = AbilityIcons.Get(def);
			Color tint = unlocked ? Color.white : LockedIconTint;
			TextLeadingIcon.GetOrCreate(noStarsName).Set(icon, tint);
			if (hasStars) TextLeadingIcon.GetOrCreate(withStarsName).Set(icon, tint);
		}

		// Pet mode: the pet abilities' zh texts need the CJK fallback font, reapplied on every render (locale reapply resets it).
		private static void SetLabel(TMP_Text label, string value)
		{
			PetStatsText.SetText(label, value);
		}
	}
}
