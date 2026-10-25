using Codebase.Core;
using UniMob;

namespace Codebase.PromoCodes
{
	public class PromoCodesModel : Model<PromoCodesUd>
	{
		public bool HasClaimedHashReward => false;

		public PromoCodesModel(Lifetime lifetime)
			: base(default(Lifetime))
		{
		}

		public bool IsUsed(string normalizedCode)
		{
			return false;
		}

		public void MarkUsed(string normalizedCode)
		{
		}

		public void MarkHashRewardClaimed()
		{
		}
	}
}
