using System;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.Rpg
{
	public static class RpgRewardService
	{
		public static event Action<Vector3, double, int> Granted;
		[StaticReset]
		private static void Reset() { Granted = null; }
		public static void Kill(EnemyUnit enemy, EnemySpawnerConfig config)
		{
			var progress = GameContext.Get<RpgProgressModel>();
			var factors = RpgModeConfig.Instance.XpDifferenceFactors;
			int difference = Math.Max(0, progress.Level - enemy.RpgLevel);
			double xp = RpgNumbers.Xp(enemy.RpgLevel) * RpgSpawnerRules.XpMultiplier(config) * factors[Math.Min(difference, factors.Count - 1)];
			int gold = RpgNumbers.GrantedGold(enemy.RpgLevel, config);
			xp = progress.AddXp(xp);
			GameContext.Get<CurrencyModel>().Add(CurrencyKeys.RpgGold, gold);
			Granted?.Invoke(enemy.transform.position, xp, gold);
		}
		public static void Quest(int island, float multiplier, Vector3 position)
		{
			double xp = multiplier * RpgNumbers.Xp(RpgEnemyLevel.End(island));
			xp = GameContext.Get<RpgProgressModel>().AddXp(xp);
			Granted?.Invoke(position, xp, 0);
		}
	}
}
