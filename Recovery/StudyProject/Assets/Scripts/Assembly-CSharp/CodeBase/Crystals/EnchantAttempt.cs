using System.Collections.Generic;

namespace Codebase.Crystals
{
	public readonly struct EnchantAttempt
	{
		public readonly EnchantAttemptStatus Status;
		public readonly int PrevLevel;
		public readonly int NewLevel;
		public readonly int StepCount;
		public readonly int StepsCompleted;
		public readonly int ExtraAttemptsTotal;
		public readonly float SuccessChance;
		public readonly IReadOnlyList<bool> Rolls;
		public readonly double DustCashback;
		public bool Performed
		{
			get
			{
				return Status != EnchantAttemptStatus.Unavailable;
			}
		}

		public bool IsSuccess
		{
			get
			{
				return Status == EnchantAttemptStatus.Success;
			}
		}

		public bool IsFailure
		{
			get
			{
				return Status == EnchantAttemptStatus.Failure;
			}
		}

		public EnchantAttempt(EnchantAttemptStatus status, int prevLevel, int newLevel, int stepCount, int stepsCompleted, int extraAttemptsTotal = 0, float successChance = 0f, IReadOnlyList<bool> rolls = null, double dustCashback = 0.0)
		{
			Status = status;
			PrevLevel = prevLevel;
			NewLevel = newLevel;
			StepCount = stepCount;
			StepsCompleted = stepsCompleted;
			ExtraAttemptsTotal = extraAttemptsTotal;
			SuccessChance = successChance;
			Rolls = rolls;
			DustCashback = dustCashback;
		}

		public static EnchantAttempt Unavailable(int level)
		{
			return new EnchantAttempt(EnchantAttemptStatus.Unavailable, level, level, 0, 0);
		}
	}
}
