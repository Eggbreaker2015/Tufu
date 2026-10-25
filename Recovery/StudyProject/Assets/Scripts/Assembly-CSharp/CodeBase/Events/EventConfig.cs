using System.Linq;
using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.UI;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Events
{
	[CreateAssetMenu(menuName = "Codebase/Events/Event Config")]
	public class EventConfig : ScriptableObject
	{
		[KeysDropdown(typeof(EventKeys))]
		public string Key;

		[KeysDropdown(typeof(EventCurrencies))]
		public List<string> Currencies;

		[Min(1f)]
		public int RequiredLocationNumber;

		[Header("Schedule")]
		public List<EventTimeInterval> StartIntervals;

		[Tooltip("Days between event occurrences (0 = daily, 1 = every other day, etc.)")]
		[Min(0f)]
		public int IntervalDays;

		public List<WeaponType> PossibleWeapons;

		public List<WeaponType> PossibleResists;

		public EventWeaponResistMode WeaponResistMode;

		public CrystalEventRosterConfig CrystalRoster;

		public List<EventDifficultySettings> DifficultySettings;

		[Header("Boosters")]
		public List<EventBoosterDef> Boosters;

		[Header("Views")]
		[KeysDropdown(typeof(UIConstants))]
		public string EventStartViewKey;

		[KeysDropdown(typeof(UIConstants))]
		public string EventProgressViewKey;

		[KeysDropdown(typeof(UIConstants))]
		public string EventEndViewKey;

		[KeysDropdown(typeof(UIConstants))]
		public string EventTutorialViewKey;

		[KeysDropdown(typeof(UIConstants))]
		public string EventTimeIsOverViewKey;

		public EventDifficultySettings GetDifficultySettings(EventDifficulty difficulty)
		{
			// Original APK ARM64 ELF VA 0x14d9198.
			return DifficultySettings.FirstOrDefault(item => item.Difficulty == difficulty);
		}

		public bool HasDifficulty(EventDifficulty difficulty)
		{
			// Original APK ARM64 ELF VA 0x14d9288.
			return DifficultySettings.Exists(item => item.Difficulty == difficulty);
		}
	}
}
