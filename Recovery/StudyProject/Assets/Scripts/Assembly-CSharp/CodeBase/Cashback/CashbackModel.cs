using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Unit;
using Codebase.VIP;
using UniMob;

namespace Codebase.Cashback
{
	public class CashbackModel : Model<CashbackUd>
	{
		public const string MailKeyPrefix = "cashback_";

		[Inject]
		private CashBackConfig config;

		[Inject]
		private VipModel vipModel;

		public int PeriodId => Data.PeriodId.Value;

		public List<CashbackStatEntry> EarnedStats
		{
			get
			{
				var result = new List<CashbackStatEntry>();
				foreach (CashbackStatEntryUd entry in Data.EarnedStats)
					result.Add(new CashbackStatEntry { Type = (StatType)entry.StatType.Value, Value = entry.Value.Value });
				return result;
			}
		}

		public CashbackModel(Lifetime lifetime)
			: base(lifetime)
		{
		}

		public void AddEarnedStat(StatModifier modifier)
		{
			// Native ELF VA 0x154cdb4. B.LS rejects <= 0 and deliberately lets NaN through.
			bool enabled = config.Enabled;
			if (modifier == null || !enabled || modifier.Value <= 0f || modifier.ModifierType != ModifierType.Flat ||
				modifier.Name == "vip.cashback" || ((uint)modifier.StatType & 0xfffffffeU) == 30U) return;
			foreach (CashbackStatEntryUd entry in Data.EarnedStats)
				if (entry.StatType.Value == (int)modifier.StatType)
				{
					entry.Value.Value += (double)modifier.Value;
					return;
				}
			CashbackStatEntryUd created = Data.EarnedStats.Create();
			created.StatType.Value = (int)modifier.StatType;
			created.Value.Value = (double)modifier.Value;
		}

		public void SetPeriodId(int value)
		{
			Data.PeriodId.Value = value;
		}

		public void ResetEarnedStats()
		{
			Data.EarnedStats.Clear();
		}

		public float GetPercent()
		{
			return config.GetPercent(vipModel.Level);
		}

		public bool IsCashbackMail(string mailKey)
		{
			return !string.IsNullOrEmpty(mailKey) && mailKey.StartsWith(MailKeyPrefix, StringComparison.Ordinal);
		}
	}
}
