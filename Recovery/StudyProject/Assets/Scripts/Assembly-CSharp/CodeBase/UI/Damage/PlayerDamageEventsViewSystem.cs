using Codebase.Unit;
using Codebase.Utilities;
using System.Linq;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player;
using Codebase.Services.Damage;
using Codebase.Unit.Events;
using UnityEngine;

namespace Codebase.UI.Damage
{
	public class PlayerDamageEventsViewSystem : MonoBehaviour
	{
		public enum StatusEffect
		{
			Heal = 0,
			Stun = 1
		}

		[SerializeField]
		private DamageEventView prefab;

		[SerializeField]
		private List<DamageEventView> eventViews;

		[SerializeField]
		private List<DamageEventView> damageViews;

		[Inject]
		private IconStyleConfig style;

		public void Awake()
		{
            global::Codebase.Core.GameContext.Set(this);
            foreach (DamageEventView view in eventViews) view.Hide();
            foreach (DamageEventView view in damageViews) view.Hide();
        }

		public void Create(WeaponHitEvent data, DamageResult result)
		{
			// Native ELF VA 0x1672b10; the original weapon slot selects a fixed damage view.
			DamageEventView view = damageViews[data.Weapon.SlotIndex];
			string text = "<sprite name=" + GetResistSpriteName(result.Resist, GetStyle(data.DamageType)) + "> " + result.Damage.Format();
			bool special = false;
			if (result.HasDodged)
			    text = "<sprite name=" + style.Styles.First(v => v.StatType == StatType.Evasion).Sprite.name + "><color=#27A5FF> " + LocalizationStrings.Get("DAMAGE_EVENT_MISS");
			else if (result.IsSuperCrit || result.IsCrit)
			{
			    StatType type = result.IsSuperCrit ? StatType.SuperCritDamagePercent : StatType.CritDamagePercent;
			    text += "<sprite name=" + style.Styles.First(v => v.StatType == type).Sprite.name + ">";
			    special = true;
			}
			view.Setup(text, special);
		}

		public void Create(StatusEffect statusEffect)
		{
			// Native ELF VA 0x1673144; status events reuse the first view if every event view is busy.
			DamageEventView view = eventViews.FirstOrDefault(v => v.IsFinished);
			if (view == null) view = eventViews[0];
			string text = string.Empty;
			bool special = false;
			if (statusEffect == StatusEffect.Heal) text = "<sprite name=atlas_51><color=#46FF52>" + LocalizationStrings.Get("DAMAGE_EVENT_HEAL");
			else if (statusEffect == StatusEffect.Stun)
			{
			    text = "<sprite name=atlas_52><color=#FFCC01>" + LocalizationStrings.Get("DAMAGE_EVENT_STUN");
			    special = true;
			}
			view.Setup(text, special);
		}

		private ResistEfficiencyStyle GetStyle(WeaponType type)
		{
			// Native ELF VA 0x1672f5c.
			return style.ResistStyles.First(v => v.WeaponType == type);
		}

		private string GetResistSpriteName(double resist, ResistEfficiencyStyle style)
		{
			// Native ELF VA 0x1673044; exact six thresholds verified from bounded ARM64 and rodata.
			if (!(resist <= 0.1))
			{
			    if (!(resist <= 0.6)) return style.Ineffective.High.name;
			    if (!(resist <= 0.3)) return style.Ineffective.Medium.name;
			    return style.Ineffective.Low.name;
			}
			if (resist >= -0.3)
			{
			    if (resist >= -0.1) return style.Effective.Neutral.name;
			    return style.Effective.Low.name;
			}
			if (resist >= -0.6) return style.Effective.Medium.name;
			return style.Effective.High.name;
		}
	}
}
