using Multicast.UserData;

namespace Codebase.Mail
{
	public class MailUd : UdObject
	{
		public UdValue<string> Guid { get; }

		public UdValue<string> MailKey { get; }

		public UdValue<string> TemplateKey { get; }

		public UdValue<string> Description { get; }

		public UdValue<bool> Viewed { get; }

		public UdValue<bool> Claimed { get; }

		public UdValue<bool> Deleted { get; }

		public UdValue<bool> ArchiveOnClaim { get; }

		public UdValue<string> ReceiveTimeString { get; }

		public UdValue<string> ExpirationTimeString { get; }

		public UdValue<int> Gems { get; }

		public UdValue<int> InAppValue { get; }

		public UdValue<int> BoosterTokens { get; }

		public UdValue<string> BoosterTokenProductId { get; }

		public UdListValue<string> Attachment { get; }

		public UdListValue<string> TextArgs { get; }

		public UdListObject<StatRewardUd> StatRewards { get; }

		public UdListValue<string> Rewards { get; }

		public MailUd(UdArgs args) : base(args)
{
    Guid = new global::Multicast.UserData.UdValue<global::System.String>(Child("guid", true), global::System.Guid.NewGuid().ToString());
    MailKey = new global::Multicast.UserData.UdValue<global::System.String>(Child("mail_key", true));
    TemplateKey = new global::Multicast.UserData.UdValue<global::System.String>(Child("template_key", true));
    Description = new global::Multicast.UserData.UdValue<global::System.String>(Child("description", true));
    Viewed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("viewed", true));
    Claimed = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("claimed", true));
    Deleted = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("deleted", true));
    ArchiveOnClaim = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("arch", true));
    ReceiveTimeString = new global::Multicast.UserData.UdValue<global::System.String>(Child("receive_time_string", true));
    ExpirationTimeString = new global::Multicast.UserData.UdValue<global::System.String>(Child("expiration_time_string", true));
    Gems = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("gems", true));
    InAppValue = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("in_app_value", true));
    BoosterTokens = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("booster_tokens", true));
    BoosterTokenProductId = new global::Multicast.UserData.UdValue<global::System.String>(Child("booster_token_product_id", true));
    Attachment = new global::Multicast.UserData.UdListValue<global::System.String>(Child("attachment", true));
    TextArgs = new global::Multicast.UserData.UdListValue<global::System.String>(Child("text_args", true));
    StatRewards = new global::Multicast.UserData.UdListObject<global::Codebase.Mail.StatRewardUd>(Child("stat_rewards", true), childArgs => new global::Codebase.Mail.StatRewardUd(childArgs));
    Rewards = new global::Multicast.UserData.UdListValue<global::System.String>(Child("rewards", true));
}
	}
}
