using System.Collections.Generic;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Simulation.Forecast;

namespace Codebase.Events.Services
{
	public class CrystalEventRosterGenerator
	{
		private const int MinGeneratedBudgetPoints = 600;

		private static readonly int[] RarityPoints;

		private static readonly int[] MinRewardNumbersByRarity;

		private static readonly List<double>[] RewardSlots;

		public List<List<ForecastCrystalRewardEntry>> Generate(PlayerModel player, int locationNumber)
		{
			return null;
		}

		private Dictionary<int, double> BuildIslandPoints()
		{
			return null;
		}

		private List<CrystalRewardVariant> BuildRewardVariants(List<ItemModel> items)
		{
			return null;
		}

		private List<CrystalRewardPart> GetRewardParts()
		{
			return null;
		}

		private List<CrystalRewardCandidate> BuildCandidates(int rewardNumber, double target, List<CrystalRewardVariant> rewardVariants, double[] assigned)
		{
			return null;
		}

		private CrystalRewardCandidate ChooseCandidate(List<CrystalRewardCandidate> candidates)
		{
			return null;
		}

		private int GetRarityPoints(Rarity rarity)
		{
			return 0;
		}

		private int GetMinRewardNumber(Rarity rarity)
		{
			return 0;
		}

		private int GetRewardCount(double target, int points)
		{
			return 0;
		}
	}
}
