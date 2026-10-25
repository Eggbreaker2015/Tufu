using Codebase.Unit;
using Codebase.Utilities;
using System.Linq;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player;
using Codebase.Services.Damage;
using UnityEngine;

namespace Codebase.UI.Damage
{
	public class EnemyDamageEventsViewSystem : MonoBehaviour
	{
		[SerializeField]
		private DamageEventView prefab;

		[SerializeField]
		private List<DamageEventView> views;

		[Inject]
		private IconStyleConfig style;

		public void Awake()
		{
            global::Codebase.Core.GameContext.Set(this);
            foreach (DamageEventView view in views) view.Hide();
        }

		public void Create(DamageResult data)
		{
			// Native ELF VA 0x1671ce0.
			DamageEventView view = views.FirstOrDefault(v => v.IsFinished);
			if (view == null) return;
			string text = "<color=#FF5757>" + data.Damage.Format();
			if (data.HasDodged)
			    text = "<sprite name=" + style.Styles.First(v => v.StatType == StatType.Evasion).Sprite.name + "><color=#27A5FF> " + LocalizationStrings.Get("DAMAGE_EVENT_MISS");
			else if (data.HasBlocked) text = "<sprite name=atlas_109><color=#00BBFF> " + data.Damage.Format();
			view.Setup(text);
		}

		public void CreateLethalIgnore()
		{
			// Native ELF VA 0x1672060.
			DamageEventView view = views.FirstOrDefault(v => v.IsFinished);
			if (view == null) return;
			view.Setup("<color=#FFFFFF>" + LocalizationStrings.Get("DAMAGE_EVENT_LETHAL_IGNORE"));
		}

		public void CreateStrike(double damage, bool isEmpowered = false)
		{
			// Native ELF VA 0x1672208.
			DamageEventView view = views.FirstOrDefault(v => v.IsFinished);
			if (view == null) return;
			string sprite = isEmpowered ? "atlas_117" : "atlas_116";
			string color = isEmpowered ? "#EFFE1E" : "#1EBBFE";
			view.Setup("<sprite name=" + sprite + "><color=" + color + "> " + damage.Format());
		}

		public void CreateReflection(double damage, string color = "#FF5757")
		{
			// Native ELF VA 0x1672534.
			DamageEventView view = views.FirstOrDefault(v => v.IsFinished);
			if (view == null) return;
			view.Setup("<sprite name=atlas_128><color=" + color + "> " + damage.Format());
		}
	}
}
