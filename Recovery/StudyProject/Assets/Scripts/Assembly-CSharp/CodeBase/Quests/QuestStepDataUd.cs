using Multicast.UserData;

namespace Codebase.Quests
{
	public class QuestStepDataUd : UdObject
	{
		public UdValue<bool> IsGiven { get; }

		public UdValue<bool> IsComplete { get; }

		public UdValue<bool> RewardClaimed { get; }

		public UdValue<int> RespawnAtUnixSeconds { get; }

		public QuestStepDataUd(UdArgs args) : base(args)
{
    IsGiven = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_given", true), false);
    IsComplete = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_complete", true), false);
    RewardClaimed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("reward_claimed", true), false);
    RespawnAtUnixSeconds = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("respawn_at_unix_seconds", true), 0);
}
	}
}
