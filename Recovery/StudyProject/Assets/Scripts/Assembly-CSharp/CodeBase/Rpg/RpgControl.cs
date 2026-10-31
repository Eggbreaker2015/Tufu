using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Rpg
{
	public static class RpgControl
	{
		public static UnitCore ControlledUnit(PlayerModel pm)
		{
			HeroRosterModel roster = GameContext.Has<HeroRosterModel>() ? GameContext.Get<HeroRosterModel>() : null;
			if (roster == null) return pm.Unit;
			if (roster.InWipe) return pm.Unit;
			if (roster.Controlled == null || !roster.Controlled.IsAlive) roster.ForceControl(roster.FirstLiving());
			return roster.Controlled != null ? roster.Controlled.Unit : null;
		}

		public static UnitCore ControlledUnitOrNull(PlayerModel pm) => pm == null ? null : ControlledUnit(pm);

		public static bool ShouldTeleport(bool offMesh, bool distance, bool pathFailure) => offMesh || distance && (Time.time >= GameContext.Get<HeroRosterModel>().GraceUntil) || pathFailure;

		public static bool IsControlled(UnitCore unit) => unit is HeroUnit hero && hero.IsControlled;

		public static bool IsHero(UnitCore unit) => unit is HeroUnit;

		public static bool IsAiHero(UnitCore unit) => unit is HeroUnit hero && !hero.IsControlled;
	}
}
