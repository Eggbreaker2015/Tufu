using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Rewards;
using UniMob;
using UniMob.Core;
using System.Linq;
using Codebase.Player.Inventory;

namespace Codebase.BossTower
{
	public class BossModel : Model
	{
		[Inject]
		private BossTowerConfig config;
		internal static float? CheatOptimumTime;
		[CompilerGenerated]
		private bool? __IsUseEliteBoss__BackingField;
		[CompilerGenerated]
		private bool? __IsUseWeakenedBoss__BackingField;
		private ComputedAtom<int> __Floor;
		private ComputedAtom<bool> __IsSuperBoss;
		private ComputedAtom<bool> __IsWeakened;
		private ComputedAtom<bool> __HasRemainingDailyClaims;
		private ComputedAtom<int> __DailyClaims;
		private ComputedAtom<bool?> __IsUseEliteBoss;
		private ComputedAtom<bool?> __IsUseWeakenedBoss;
		public BossDef Def { get; }
		public BossUd Data { get; }
		public List<RewardBase> RewardsBaked { get; }
		public List<RewardBase> PreviousFloorRewardsBaked { get; }
		public MutableAtom<int> RewardsVersion { get; private set; }

		public int Floor
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Floor == null)
					__Floor = CodeGenAtom.CreatePooled(this, null, () => Floor, false);
				return __Floor.DirectEvaluate() ? Data.Floor.Value : __Floor.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Floor != null && !__Floor.CompareAndInvalidate(value))
					return;
				Data.Floor.Value = value;
			}
		}

		public bool IsSuperBoss
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsSuperBoss == null)
					__IsSuperBoss = CodeGenAtom.CreatePooled(this, null, () => IsSuperBoss, false);
				return __IsSuperBoss.DirectEvaluate() ? ((Floor > 0 && Floor % 10 == 0) || (IsUseEliteBoss.HasValue && IsUseEliteBoss.Value)) : __IsSuperBoss.Value;
			}
		}

		public bool IsWeakened
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsWeakened == null)
					__IsWeakened = CodeGenAtom.CreatePooled(this, null, () => IsWeakened, false);
				return __IsWeakened.DirectEvaluate() ? Data.IsWeakened.Value : __IsWeakened.Value;
			}
		}

		public bool HasRemainingDailyClaims
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__HasRemainingDailyClaims == null)
					__HasRemainingDailyClaims = CodeGenAtom.CreatePooled(this, null, () => HasRemainingDailyClaims, false);
				return __HasRemainingDailyClaims.DirectEvaluate() ? DailyClaims < 6 : __HasRemainingDailyClaims.Value;
			}
		}

		public int DailyClaims
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__DailyClaims == null)
					__DailyClaims = CodeGenAtom.CreatePooled(this, null, () => DailyClaims, false);
				return __DailyClaims.DirectEvaluate() ? Data.DailyClaims.Value : __DailyClaims.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__DailyClaims != null && !__DailyClaims.CompareAndInvalidate(value))
					return;
				Data.DailyClaims.Value = value;
			}
		}

		public string Key
		{
			get
			{
				return Def.Key;
			}
		}

		public bool IsUnlockedByDefault
		{
			get
			{
				return Def.isUnlockedByDefault;
			}
		}

		public bool? IsUseEliteBoss
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUseEliteBoss == null)
					__IsUseEliteBoss = CodeGenAtom.CreatePooled(this, null, () => IsUseEliteBoss, false);
				return __IsUseEliteBoss.DirectEvaluate() ? __IsUseEliteBoss__BackingField : __IsUseEliteBoss.Value;
			}

			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUseEliteBoss != null && !__IsUseEliteBoss.CompareAndInvalidate(value))
					return;
				__IsUseEliteBoss__BackingField = value;
			}
		}

		public bool? IsUseWeakenedBoss
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUseWeakenedBoss == null)
					__IsUseWeakenedBoss = CodeGenAtom.CreatePooled(this, null, () => IsUseWeakenedBoss, false);
				return __IsUseWeakenedBoss.DirectEvaluate() ? __IsUseWeakenedBoss__BackingField : __IsUseWeakenedBoss.Value;
			}

			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUseWeakenedBoss != null && !__IsUseWeakenedBoss.CompareAndInvalidate(value))
					return;
				__IsUseWeakenedBoss__BackingField = value;
			}
		}

		public BossModel(Lifetime lifetime, BossDef def, BossUd data) : base(lifetime)
		{
			Def = def;
			Data = data;
			RewardsBaked = new List<RewardBase>();
			PreviousFloorRewardsBaked = new List<RewardBase>();
			RewardsVersion = Atom.Value(Lifetime, 0);
		}

		public void IncrementFloor()
		{
			Floor++;
		}

		public void SetFloor(int floor)
		{
			Floor = System.Math.Max(0, floor);
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			CheatOptimumTime = null;
		}

		public float GetOptimumTime()
		{
			if (CheatOptimumTime.HasValue)
				return CheatOptimumTime.Value;
			int floor = Floor;
			float time = floor <= 30 ? 500f + (float)System.Math.Pow((double)(floor - 1) / 29.0, 1.15) * 4500f : 5000f + unchecked(floor * 13400 - 402000) / 60f;
			if (Def.Rewards.Any(reward => reward is ItemReward item && item.Def.SlotType == ItemSlotType.Shield))
				time += config.ArmorBossOptimumTimeOffsetMinutes;
			return time;
		}
	}
}
