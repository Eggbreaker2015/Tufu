using System;
using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Services.Time;
using Codebase.Unit;

namespace Codebase.Rewards
{
    [Serializable]
    public class StatReward : RewardBase
    {
        public bool IsRandom;
        public bool UseGpm;
        public bool CustomModifier;
        public bool DailyRotation;
        public List<StatType> AllowedStats;
        public List<DailyRotationStat> DailyRotationStats;
        public string SaveKey;
        public StatType Type;
        public float Value;
        public string Name;
        public ModifierSource Source;
        public ModifierType ModifierType;
        [NonSerialized] public BakedStatReward BakedData;

        public StatType? GetResolvedType()
        {
            // Original ARM64 ELF VA 0x1419a2c, original game-time UTC date vs install UTC date.
            if (!DailyRotation) return Type;
            var install = DateTimeOffset.FromUnixTimeSeconds((long)GameContext.Get<UserModel>().installTime.Value).UtcDateTime.Date;
            var now = GameContext.Get<ITimeService>().Now.ToDateTime().ToUniversalTime().Date;
            int index = (now - install).Days % DailyRotationStats.Count;
            var entry = DailyRotationStats[index];
            return entry.Empty ? (StatType?)null : entry.Type;
        }
    }
}
