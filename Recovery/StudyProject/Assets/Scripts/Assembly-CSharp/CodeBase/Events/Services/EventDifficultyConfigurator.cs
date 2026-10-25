using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.Events.Models;

namespace Codebase.Events.Services
{
	public class EventDifficultyConfigurator
	{
		public void PrepareEvent(EventModel model, EventDifficulty difficulty)
		{
			// Original APK ARM64 ELF VA 0x14e2bc4.
			if (model == null) return;
			EventDifficulty previous = model.Difficulty;
			model.Difficulty = difficulty;
			if ((int)previous == -1) GenerateUniqueWeaponTypesForDifficulties(model);
			EventDifficultySettings settings = model.Def.GetDifficultySettings(difficulty);
			if (settings == null) return;
			model.Data.WeaponType.Value = settings.AttackType;
			model.Data.ResistType.Value = settings.ResistType;
		}

		public void GenerateUniqueWeaponTypesForDifficulties(EventModel model)
		{
			// Original APK ARM64 ELF VA 0x14e2cb8.
			if (model.Def.WeaponResistMode == EventWeaponResistMode.LastUnlockedLocationPreferred)
			{
			    var location = GameContext.Get<PlayerModel>()?.LastUnlockedLocation;
			    WeaponType attack = location != null ? location.PreferredWeaponType : (WeaponType)1;
			    WeaponType resist = location != null ? location.PreferredArmorType : (WeaponType)1;
			    foreach (EventDifficultySettings settings in model.Def.DifficultySettings)
			    {
			        settings.AttackType = attack;
			        settings.ResistType = resist;
			    }
			    return;
			}
			var weapons = new List<WeaponType>(model.Def.PossibleWeapons);
			var resists = new List<WeaponType>(model.Def.PossibleResists);
			foreach (EventDifficulty difficulty in new[] { (EventDifficulty)0, (EventDifficulty)1, (EventDifficulty)2 })
			{
			    var settings = model.Def.GetDifficultySettings(difficulty);
			    if (settings == null) continue;
			    if (weapons.Count == 0) weapons = new List<WeaponType>(model.Def.PossibleWeapons);
			    if (resists.Count == 0) resists = new List<WeaponType>(model.Def.PossibleResists);
			    int weapon = UnityEngine.Random.Range(0, weapons.Count);
			    int resist = UnityEngine.Random.Range(0, resists.Count);
			    settings.AttackType = weapons[weapon];
			    settings.ResistType = resists[resist];
			    weapons.RemoveAt(weapon);
			    resists.RemoveAt(resist);
			}
		}
	}
}
