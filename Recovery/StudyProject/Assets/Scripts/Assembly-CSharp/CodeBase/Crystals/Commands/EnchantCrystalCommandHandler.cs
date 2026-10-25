using System;
using Codebase.ABTests;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;

namespace Codebase.Crystals.Commands
{
	public class EnchantCrystalCommandHandler : ICommandHandler<EnchantCrystalCommand, EnchantAttempt>
	{
		[Inject]
		private CrystalModel crystalModel;
		[Inject]
		private IAnalyticsWrapper analytics;
		[Inject]
		private ABTestService abTestService;
		private readonly Random random;
		public EnchantAttempt Execute(EnchantCrystalCommand command)
		{
			var crystal = command.Crystal;
			int previous = crystal.EnchantLevel;
			if (crystalModel.IsAtMaxEnchantLevel(crystal) || (command.Guaranteed && !crystalModel.IsGuaranteeEnchantReady) || !crystalModel.TrySpendEnchantmentStone())
				return EnchantAttempt.Unavailable(previous);
			int extra = crystalModel.GetEnchantExtraAttempts(previous);
			int steps = unchecked(previous + 1);
			float chance = crystalModel.GetEnchantSuccessChance(crystal);
			bool firstCommon = crystal.Rarity == CrystalRarity.Common && !crystalModel.Data.FirstGuaranteedEnchantUsed.Value;
			if (firstCommon || command.Guaranteed)
			{
				if (!firstCommon)
					crystalModel.ConsumeGuaranteeEnchant();
				crystalModel.ApplyEnchantLevel(crystal);
				var guaranteed = new EnchantAttempt(EnchantAttemptStatus.Success, previous, steps, steps, steps, extra, chance);
				ReportAttempt(crystal, guaranteed, true);
				crystalModel.Data.FirstGuaranteedEnchantUsed.Value = true;
				return guaranteed;
			}

			crystalModel.RegisterEnchant();
			var rolls = new System.Collections.Generic.List<bool>();
			int completed = 0, remaining = extra;
			while (completed < steps)
			{
				// Native narrows random.NextDouble()*100 to Single before strict FCMP+B.GT.
				bool success = chance > (float)(random.NextDouble() * 100.0);
				rolls.Add(success);
				if (success)
				{
					completed++;
					continue;
				}

				if (remaining <= 0)
				{
					double cashback = GrantFailureDustCashback();
					var failure = new EnchantAttempt(EnchantAttemptStatus.Failure, previous, previous, steps, completed, extra, chance, rolls, cashback);
					ReportAttempt(crystal, failure, command.Guaranteed);
					return failure;
				}

				remaining--;
			}

			crystalModel.ApplyEnchantLevel(crystal);
			var result = new EnchantAttempt(EnchantAttemptStatus.Success, previous, steps, steps, completed, extra, chance, rolls);
			ReportAttempt(crystal, result, command.Guaranteed);
			return result;
		}

		private void ReportAttempt(CrystalItemModel crystal, EnchantAttempt attempt, bool guaranteed)
		{
			analytics.Send(new Codebase.Analytics.Events.CrystalEnchantAttemptEvent(crystal, attempt, guaranteed));
		}

		private double GrantFailureDustCashback()
		{
			float rpm = abTestService.GetEnchantFailureDustCashbackRpm();
			// Native B.LS returns for ordered nonpositive only; unordered NaN continues.
			if (rpm <= 0f)
				return 0.0;
			double cashback = crystalModel.CalculateMagicDustForRpm(rpm);
			crystalModel.AddMagicDust(cashback);
			return cashback;
		}

		public EnchantCrystalCommandHandler()
		{
			random = new Random();
		}
	}
}
