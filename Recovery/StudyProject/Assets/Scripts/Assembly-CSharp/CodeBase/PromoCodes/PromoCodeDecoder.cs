using System.Collections.Generic;
using Codebase.Player.Inventory;
using Codebase.Rewards;

namespace Codebase.PromoCodes
{
	public static class PromoCodeDecoder
	{
		private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

		private const int XorKey = 21473;

		private const int MixMult = 12679;

		private const int MixInv = 443447;

		private const int MixMask = 1048575;

		private const int CodeLength = 4;

		public static bool TryDecode(string code, ItemsProvider items, out List<RewardBase> rewards)
		{
			rewards = null;
			return false;
		}
	}
}
