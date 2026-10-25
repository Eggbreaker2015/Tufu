using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Codebase.Cashback
{
	public class CashBackConfig : ScriptableObject
	{
		public bool Enabled = true;

		public int PeriodMinutes = 1;

		public List<CashbackLevelConfig> Levels = new List<CashbackLevelConfig>();

		public float GetPercent(int vipLevel)
		{
			if (!Enabled || Levels == null || Levels.Count == 0) return 0f;
			return Levels.Where(level => level != null && level.VipLevel <= vipLevel)
				.OrderByDescending(level => level.VipLevel).FirstOrDefault()?.Percent ?? 0f;
		}
	}
}
