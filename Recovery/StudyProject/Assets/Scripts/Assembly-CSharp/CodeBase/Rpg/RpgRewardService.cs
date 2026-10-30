using System;
using CodeBase.Audio;
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
			Compute(enemy, config, out double xp, out int gold);
			Grant(enemy.transform.position, xp, gold);
		}
		// Dungeon kills grant these on the spot too and also tally them for the clear bonus.
		public static void Compute(EnemyUnit enemy, EnemySpawnerConfig config, out double xp, out int gold)
		{
			var progress = GameContext.Get<RpgProgressModel>();
			var factors = RpgModeConfig.Instance.XpDifferenceFactors;
			int difference = Math.Max(0, progress.Level - enemy.RpgLevel);
			xp = RpgNumbers.Xp(enemy.RpgLevel) * RpgSpawnerRules.XpMultiplier(config) * factors[Math.Min(difference, factors.Count - 1)];
			gold = RpgNumbers.GrantedGold(enemy.RpgLevel, config);
		}
		public static void Grant(Vector3 position, double xp, int gold)
		{
			var progress = GameContext.Get<RpgProgressModel>();
			xp = progress.AddXp(xp);
			GameContext.Get<CurrencyModel>().Add(CurrencyKeys.RpgGold, gold);
			if (xp > 0 || gold > 0) GameContext.Get<IAudioPlayer>().PlaySoundOnce("devour");
			Granted?.Invoke(position, xp, gold);
		}
		public static void Quest(int island, float multiplier, Vector3 position)
		{
			double xp = multiplier * RpgNumbers.Xp(RpgEnemyLevel.End(island));
			xp = GameContext.Get<RpgProgressModel>().AddXp(xp);
			if (xp > 0) GameContext.Get<IAudioPlayer>().PlaySoundOnce("devour");
			Granted?.Invoke(position, xp, 0);
		}
	}
}
