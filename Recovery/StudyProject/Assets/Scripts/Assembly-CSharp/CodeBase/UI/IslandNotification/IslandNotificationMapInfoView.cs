using Codebase.Utilities;
using System.Linq;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.IslandNotification
{
	public class IslandNotificationMapInfoView : MonoBehaviour
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text text;

		[Inject]
		private IconStyleConfig iconStyle;

		public void Setup(WeaponType weaponType, string key, Color color)
		{
			// Native ELF VA 0x16488e4; original icon branch uses the raw key argument.
			text.text = LocalizationStrings.Get(key); text.color = color;
			ResistEfficiencyStyle style = iconStyle.ResistStyles.First(entry => entry.WeaponType == weaponType);
			icon.sprite = key == "ISLAND_WEAKNESS" ? style.Effective.Medium : style.Ineffective.High;
		}
	}
}
