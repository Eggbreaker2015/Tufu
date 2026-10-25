using System.Runtime.CompilerServices;
using Codebase.Core;
using UniMob;
using UniMob.Core;
using Scellecs.Morpeh;

namespace Codebase.Rage
{
	public class RageModel : Model
	{
		private static readonly float[] Thresholds = new float[]
		{
			30f,
			100f,
			270f,
			270f
		};
		private const float DecayTimeout = 10f;
		[CompilerGenerated]
		private float __RagePoints__BackingField;
		[CompilerGenerated]
		private int __RageLevel__BackingField;
		[CompilerGenerated]
		private int __MaxAccessibleLevel__BackingField;
		[CompilerGenerated]
		private int __BossMaxLevelBonus__BackingField;
		private ComputedAtom<float> __RagePoints;
		private ComputedAtom<int> __RageLevel;
		private ComputedAtom<int> __MaxAccessibleLevel;
		private ComputedAtom<int> __BossMaxLevelBonus;
		private ComputedAtom<int> __EffectiveMaxLevel;
		private ComputedAtom<int> __ActiveRageLevel;
		public float RagePoints
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__RagePoints == null)
					__RagePoints = CodeGenAtom.CreatePooled(this, null, () => RagePoints, false);
				return __RagePoints.DirectEvaluate() ? __RagePoints__BackingField : __RagePoints.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__RagePoints != null && !__RagePoints.CompareAndInvalidate(value))
					return;
				__RagePoints__BackingField = value;
			}
		}

		public int RageLevel
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__RageLevel == null)
					__RageLevel = CodeGenAtom.CreatePooled(this, null, () => RageLevel, false);
				return __RageLevel.DirectEvaluate() ? __RageLevel__BackingField : __RageLevel.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__RageLevel != null && !__RageLevel.CompareAndInvalidate(value))
					return;
				__RageLevel__BackingField = value;
			}
		}

		public int MaxAccessibleLevel
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MaxAccessibleLevel == null)
					__MaxAccessibleLevel = CodeGenAtom.CreatePooled(this, null, () => MaxAccessibleLevel, false);
				return __MaxAccessibleLevel.DirectEvaluate() ? __MaxAccessibleLevel__BackingField : __MaxAccessibleLevel.Value;
			}

			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MaxAccessibleLevel != null && !__MaxAccessibleLevel.CompareAndInvalidate(value))
					return;
				__MaxAccessibleLevel__BackingField = value;
			}
		}

		private int BossMaxLevelBonus
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__BossMaxLevelBonus == null)
					__BossMaxLevelBonus = CodeGenAtom.CreatePooled(this, null, () => BossMaxLevelBonus, false);
				return __BossMaxLevelBonus.DirectEvaluate() ? __BossMaxLevelBonus__BackingField : __BossMaxLevelBonus.Value;
			}

			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__BossMaxLevelBonus != null && !__BossMaxLevelBonus.CompareAndInvalidate(value))
					return;
				__BossMaxLevelBonus__BackingField = value;
			}
		}

		public int EffectiveMaxLevel
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EffectiveMaxLevel == null)
					__EffectiveMaxLevel = CodeGenAtom.CreatePooled(this, null, () => EffectiveMaxLevel, false);
				return __EffectiveMaxLevel.DirectEvaluate() ? MaxAccessibleLevel + BossMaxLevelBonus : __EffectiveMaxLevel.Value;
			}
		}

		public int ActiveRageLevel
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ActiveRageLevel == null)
					__ActiveRageLevel = CodeGenAtom.CreatePooled(this, null, () => ActiveRageLevel, false);
				return __ActiveRageLevel.DirectEvaluate() ? (EffectiveMaxLevel > 0 ? RageLevel : 0) : __ActiveRageLevel.Value;
			}
		}

		public float TimeSinceLastRage { get; private set; }

		public RageModel(Lifetime lifetime) : base(lifetime)
		{
		}

		public void Tick(float deltaTime)
		{
			if (EffectiveMaxLevel <= 0)
				return;
			if (!(RagePoints > 0f || RageLevel > 0))
				return;
			TimeSinceLastRage += deltaTime;
			if (TimeSinceLastRage >= DecayTimeout)
				ClearRage();
		}

		public float GetThreshold(int level)
		{
			return level >= 0 && level < Thresholds.Length ? Thresholds[level] : float.MaxValue;
		}

		public bool TryAddRage(float amount)
		{
			if (EffectiveMaxLevel <= 0)
				return false;
			TimeSinceLastRage = 0f;
			World.Default.GetEvent<RageGainedEvent>().NextFrame(new RageGainedEvent { Amount = amount });
			if (RageLevel >= EffectiveMaxLevel)
			{
				RagePoints = 0f;
				return false;
			}

			RagePoints += amount;
			bool leveledUp = false;
			float threshold = GetThreshold(RageLevel);
			while (RagePoints >= threshold && RageLevel < EffectiveMaxLevel)
			{
				RagePoints -= threshold;
				RageLevel++;
				leveledUp = true;
				threshold = GetThreshold(RageLevel);
			}

			if (RageLevel >= EffectiveMaxLevel)
				RagePoints = 0f;
			return leveledUp;
		}

		public void ResetRage()
		{
			// Native ELF VA 0x141fff8; elapsed decay timer deliberately remains unchanged.
			RagePoints = 0f;
			RageLevel = 0;
		}

		public void DecreaseLevel(int amount = 1)
		{
			if (RageLevel <= 0)
				return;
			RageLevel = System.Math.Max(0, RageLevel - amount);
			RagePoints = 0f;
		}

		public void SetMaxAccessibleLevel(int level)
		{
			MaxAccessibleLevel = System.Math.Max(MaxAccessibleLevel, level);
		}

		public void SyncToWeaponAccess()
		{
			if (EffectiveMaxLevel <= 0)
				return;
			if (RageLevel > EffectiveMaxLevel)
			{
				RageLevel = EffectiveMaxLevel;
				RagePoints = 0f;
			}
		}

		public void SetBossMaxLevelBonus(int bonus)
		{
			BossMaxLevelBonus = bonus;
			SyncToWeaponAccess();
		}

		private void ClearRage()
		{
			// Native ELF VA 0x141fd0c.
			RagePoints = 0f;
			RageLevel = 0;
		}
	}
}
