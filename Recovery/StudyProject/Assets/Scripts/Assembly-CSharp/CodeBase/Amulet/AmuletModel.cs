using System;
using System.Linq;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Player.Models;
using UniMob;
using UniMob.Core;

namespace Codebase.Amulet
{
	public class AmuletModel : Model<AmuletUd>
	{
		[Inject]
		private AmuletConfig config;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CurrencyModel currencyModel;

		private ComputedAtom<bool> __IsUnlocked;

		private ComputedAtom<bool> __CanUnlock;

		private ComputedAtom<bool> __IsUnlockOnboardingActive;

		private ComputedAtom<double> __TotalExperience;

		private ComputedAtom<int> __Level;

		private ComputedAtom<double> __CurrentLevelExperience;

		private ComputedAtom<double> __NextLevelExperience;

		private ComputedAtom<List<string>> __Souls;

		private ComputedAtom<float> __LevelProgress;

		private ComputedAtom<bool> __IsMaxLevel;

        public bool IsUnlocked
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsUnlocked == null) __IsUnlocked = CodeGenAtom.CreatePooled(this,null,()=>IsUnlocked,false);
                return __IsUnlocked.DirectEvaluate() ? Data.IsUnlocked.Value : __IsUnlocked.Value;
            }
            private set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsUnlocked != null && !__IsUnlocked.CompareAndInvalidate(value)) return;
                Data.IsUnlocked.Value = value;
            }
        }

		public bool CanUnlock
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CanUnlock == null) __CanUnlock = CodeGenAtom.CreatePooled(this,null,()=>CanUnlock,false);
                return __CanUnlock.DirectEvaluate() ? (!IsUnlocked && playerModel.LastUnlockedLocation.Number >= config.UnlockLocationNumber) : __CanUnlock.Value;
            }
        }

		public bool IsUnlockOnboardingActive
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsUnlockOnboardingActive == null) __IsUnlockOnboardingActive = CodeGenAtom.CreatePooled(this,null,()=>IsUnlockOnboardingActive,false);
                return __IsUnlockOnboardingActive.DirectEvaluate() ? (!IsUnlocked && CanUnlock) : __IsUnlockOnboardingActive.Value;
            }
        }

		public double EmptyAmuletCount => currencyModel.Get("empty_amulet");

		public double TotalExperience
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__TotalExperience == null) __TotalExperience = CodeGenAtom.CreatePooled(this,null,()=>TotalExperience,false);
                return __TotalExperience.DirectEvaluate() ? (Data.TotalExperience.Value) : __TotalExperience.Value;
            }
        }

		public int Level
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__Level == null) __Level = CodeGenAtom.CreatePooled(this,null,()=>Level,false);
                return __Level.DirectEvaluate() ? (CalculateLevel()) : __Level.Value;
            }
        }

		public double CurrentLevelExperience
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CurrentLevelExperience == null) __CurrentLevelExperience = CodeGenAtom.CreatePooled(this,null,()=>CurrentLevelExperience,false);
                return __CurrentLevelExperience.DirectEvaluate() ? (TotalExperience - GetExperienceForLevel(Level)) : __CurrentLevelExperience.Value;
            }
        }

		public double NextLevelExperience
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__NextLevelExperience == null) __NextLevelExperience = CodeGenAtom.CreatePooled(this,null,()=>NextLevelExperience,false);
                return __NextLevelExperience.DirectEvaluate() ? (config.GetRequiredExperience(Level + 1)) : __NextLevelExperience.Value;
            }
        }

		public List<string> Souls
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__Souls == null) __Souls = CodeGenAtom.CreatePooled(this,null,()=>Souls,false);
                return __Souls.DirectEvaluate() ? (currencyModel.KeysWithPrefix("soul.").Select(key=>key.Substring("soul.".Length)).ToList()) : __Souls.Value;
            }
        }

		public List<SoulDef> SoulDefs => config.SoulDefs;

		public float LevelProgress
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__LevelProgress == null) __LevelProgress = CodeGenAtom.CreatePooled(this,null,()=>LevelProgress,false);
                return __LevelProgress.DirectEvaluate() ? (IsMaxLevel ? 1f : NextLevelExperience > 0.0 ? (float)(CurrentLevelExperience / NextLevelExperience) : 0f) : __LevelProgress.Value;
            }
        }

		public bool IsMaxLevel
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsMaxLevel == null) __IsMaxLevel = CodeGenAtom.CreatePooled(this,null,()=>IsMaxLevel,false);
                return __IsMaxLevel.DirectEvaluate() ? (Level >= config.MaxLevel) : __IsMaxLevel.Value;
            }
        }

		public int MaxLevel => config.MaxLevel;

		public AmuletModel(Lifetime lifetime)
			: base(lifetime)
		{
		}

		public void Unlock()
		{
// Native ELF VA 0x15b434c: no new location or cost guard is introduced here.
            if (!IsUnlocked) IsUnlocked = true;
		}

		public bool TryFillWithSoul()
		{
// Native ELF VA 0x15b4374: huge soul is checked; only one empty_amulet is spent.
            if (EmptyAmuletCount <= 0.0 || !HasEnoughSouls("huge",1.0)) return false;
            currencyModel.TrySpend("empty_amulet",1.0);
            Unlock();
            return true;
		}

		public double GetSouls(string soulKey)
		{
return currencyModel.Get("soul." + soulKey);
		}

		public void AddSouls(string soulKey, double amount, bool isSimulation = false)
		{
// Original ELF VA 0x15b4514.
            if (amount <= 0.0) return;
            currencyModel.Add("soul." + soulKey,amount);
            if (!Data.DiscoveredSoulKeys.Contains(soulKey)) Data.DiscoveredSoulKeys.Add(soulKey);
            if (isSimulation) Data.TotalExperience.Value += (config.GetSoulDef(soulKey)?.BaseExperience ?? 1.0) * amount;
		}

		public void AddUpgradeExperience(string soulKey, double soulsSpent)
		{
// Native ELF VA 0x15b46a4; reward simulation's separate AddSouls path is unchanged.
            if (IsUnlocked && soulsSpent > 0.0) Data.TotalExperience.Value += config.GetBaseExperience(soulKey) * soulsSpent;
		}

		public bool TrySpendSouls(string soulKey, double amount)
		{
return currencyModel.TrySpend("soul." + soulKey,amount);
		}

		public bool HasEnoughSouls(string soulKey, double amount)
		{
return currencyModel.HasEnough("soul." + soulKey,amount);
		}

		public bool IsSoulDiscovered(string soulKey)
		{
return Data.DiscoveredSoulKeys.Contains(soulKey);
		}

		public void AddLevels(int amount)
		{
// Native ELF VA 0x15b4884, including native clamp and cumulative experience reset.
            if (amount < 1 || !IsUnlocked || IsMaxLevel) return;
            int next = Math.Min(Level + amount,config.MaxLevel);
            if (next <= Level) return;
            Data.TotalExperience.Value = GetExperienceForLevel(next);
		}

		public int GetLevelForExperience(double experience)
		{
// Native ELF VA 0x15b4960. Level 1 has zero required XP; thresholds advance the previous level.
            if (!(experience >= 0.0)) return 0;
            if (config.MaxLevel < 1) return 1;
            double cumulative = 0.0;
            int previous = 1;
            for (int level = 1; level <= config.MaxLevel; level++)
            {
                cumulative += config.GetRequiredExperience(level);
                if (experience < cumulative) return previous;
                previous = level;
            }
            return config.MaxLevel;
		}

		public double GetRequiredExperience(int level)
		{
return config.GetRequiredExperience(level);
		}

		private int CalculateLevel()
		{
return GetLevelForExperience(TotalExperience);
		}

		public double GetExperienceForLevel(int level)
		{
// Native ELF VA 0x15b3c30; no additional clamp or inferred balance formula.
            double cumulative = 0.0;
            for (int current = 1; current <= level; current++) cumulative += config.GetRequiredExperience(current);
            return cumulative;
		}
	}
}
