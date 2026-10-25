using Multicast.UserData;

namespace Codebase.Quests
{
	public class QuestChainDataUd : UdObject
	{
		public UdValue<bool> RewardClaimed { get; }

		public UdValue<int> RewardClaimAtUnixSeconds { get; }

		public QuestChainDataUd(UdArgs args) : base(args)
{
    RewardClaimed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("reward_claimed", true), false);
    RewardClaimAtUnixSeconds = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("reward_claim_at_unix_seconds", true), 0);
}
	}
}
