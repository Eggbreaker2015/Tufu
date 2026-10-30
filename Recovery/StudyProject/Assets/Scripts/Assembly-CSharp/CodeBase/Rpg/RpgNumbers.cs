using System;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.Rpg
{
	public enum RpgSpawnerKind { Normal, Elite, Boss, Static }
	public static class RpgNumbers
	{
		public static double Xp(int level) => RpgModeConfig.Instance.XpBase * Math.Pow(RpgModeConfig.Instance.XpGrowth, level - 1);
		public static double Need(int level) => (RpgModeConfig.Instance.XpNeedBase + RpgModeConfig.Instance.XpNeedPerLevel * level) * Xp(level);
		public static double Gold(int level) => RpgModeConfig.Instance.GoldBase * Math.Pow(RpgModeConfig.Instance.GoldGrowth, level - 1);
		public static int GrantedGold(int level, EnemySpawnerConfig config) => Math.Max(1, (int)Math.Round(Gold(level) * RpgSpawnerRules.GoldMultiplier(config), MidpointRounding.AwayFromZero));
	}
	public static class RpgStars
	{
		public static int FromCopies(int copies)
		{
			int star = 1;
			foreach (int threshold in RpgModeConfig.Instance.StarCopyThresholds)
				if (star < 5 && copies >= threshold) star++;
			return star;
		}
	}
	public static class RpgSpawnerRules
	{
		public static RpgSpawnerKind Kind(EnemySpawnerConfig config)
		{
			if (config.Gates != null && config.Gates.Count > 0) return RpgSpawnerKind.Boss;
			if (config.IsEliteBoss) return RpgSpawnerKind.Elite;
			if (config.IsStatic && !config.DisableAfterFirstClear) return RpgSpawnerKind.Static;
			return RpgSpawnerKind.Normal;
		}
		public static float GetCooldownSeconds(EnemySpawnerConfig config)
		{
			var c = RpgModeConfig.Instance;
			switch (Kind(config)) { case RpgSpawnerKind.Boss: return Mathf.Max(1, c.BossCooldown); case RpgSpawnerKind.Elite: return Mathf.Max(1, c.EliteCooldown); case RpgSpawnerKind.Static: return Mathf.Max(1, c.StaticCooldown); default: return Mathf.Max(1, c.NormalCooldown); }
		}
		public static float XpMultiplier(EnemySpawnerConfig config)
		{
			var c = RpgModeConfig.Instance;
			switch (Kind(config)) { case RpgSpawnerKind.Boss: return c.BossXpMultiplier; case RpgSpawnerKind.Elite: return c.EliteXpMultiplier; case RpgSpawnerKind.Static: return c.StaticXpMultiplier; default: return c.NormalXpMultiplier; }
		}
		public static float GoldMultiplier(EnemySpawnerConfig config)
		{
			var c = RpgModeConfig.Instance;
			switch (Kind(config)) { case RpgSpawnerKind.Boss: return c.BossGoldMultiplier; case RpgSpawnerKind.Elite: return c.EliteGoldMultiplier; case RpgSpawnerKind.Static: return c.StaticGoldMultiplier; default: return c.NormalGoldMultiplier; }
		}
	}
	public static class RpgEnemyLevel
	{
		public static int End(int island) { var c = RpgModeConfig.Instance; return c.IslandEnds[Mathf.Clamp(island - 1, 0, c.IslandEnds.Count - 1)]; }
		public static void CopyLevel(EnemySpawnerConfig target, EnemySpawnerConfig reference)
		{
			var starts = RpgModeConfig.Instance.IslandStarts;
			int start = starts[Mathf.Clamp(target.LocationNumber - 1, 0, starts.Count - 1)];
			int end = End(target.LocationNumber);
			target.RelativeOptimumTime = end == start ? 0 : (float)(Compute(reference) - start) / (end - start);
		}
		public static int Compute(EnemySpawnerConfig config)
		{
			var c = RpgModeConfig.Instance;
			int start = c.IslandStarts[Mathf.Clamp(config.LocationNumber - 1, 0, c.IslandStarts.Count - 1)];
			int end = End(config.LocationNumber);
			int level = RpgSpawnerRules.Kind(config) == RpgSpawnerKind.Boss ? end + 1 : Mathf.RoundToInt(start + config.RelativeOptimumTime * (end - start));
			if (config.RelativeOptimumTime > 1f) level = Math.Min(level, end + 1);
			return Mathf.Clamp(level, 1, c.LevelCap);
		}
	}
}
