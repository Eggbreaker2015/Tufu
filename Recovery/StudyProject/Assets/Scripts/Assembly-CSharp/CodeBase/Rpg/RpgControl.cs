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
			return RpgModeConfig.Enabled && roster?.Controlled?.Unit != null ? roster.Controlled.Unit : pm.Unit;
		}

		public static UnitCore ControlledUnitOrNull(PlayerModel pm) => pm == null ? null : ControlledUnit(pm);

		public static bool ShouldTeleport(bool offMesh, bool distance, bool pathFailure) => offMesh || distance && (!RpgModeConfig.Enabled || Time.time >= GameContext.Get<HeroRosterModel>().GraceUntil) || pathFailure;

		public static bool IsControlled(UnitCore unit) => RpgModeConfig.Enabled ? unit is HeroUnit hero && hero.IsControlled : unit is PlayerUnit;

		public static bool IsHero(UnitCore unit) => RpgModeConfig.Enabled && unit is HeroUnit;

		public static bool IsAiHero(UnitCore unit) => RpgModeConfig.Enabled && unit is HeroUnit hero && !hero.IsControlled;
	}
}
