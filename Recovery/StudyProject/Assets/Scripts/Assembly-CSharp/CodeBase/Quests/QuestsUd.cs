using Multicast.UserData;

namespace Codebase.Quests
{
	public class QuestsUd : UdObject
	{
		public UdDict<QuestStepDataUd> Steps { get; }

		public UdDict<QuestChainDataUd> Chains { get; }

		public UdDictValue<bool> DeliveredTokens { get; }

		public QuestsUd(UdArgs args) : base(args)
{
    Steps = new global::Multicast.UserData.UdDict<global::Codebase.Quests.QuestStepDataUd>(Child("steps", true), childArgs => new global::Codebase.Quests.QuestStepDataUd(childArgs));
    Chains = new global::Multicast.UserData.UdDict<global::Codebase.Quests.QuestChainDataUd>(Child("chains", true), childArgs => new global::Codebase.Quests.QuestChainDataUd(childArgs));
    DeliveredTokens = new global::Multicast.UserData.UdDictValue<global::System.Boolean>(Child("delivered_tokens", true));
}
	}
}
