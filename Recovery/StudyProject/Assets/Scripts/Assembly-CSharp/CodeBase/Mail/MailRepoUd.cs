using Multicast.UserData;

namespace Codebase.Mail
{
	public class MailRepoUd : UdObject
	{
		public UdDict<MailUd> Items { get; }

		public UdValue<bool> PassedBalanceUpdate_1_2_0 { get; }

		public UdValue<bool> PassedStatusUpdate_1_3_0 { get; }

		public UdValue<bool> PassedUpdate_2_0_0 { get; }

		public UdValue<bool> PassedCompensationMail_2_0_0 { get; }

		public UdValue<bool> PassedUpdate_2_0_3 { get; }

		public UdValue<bool> PassedUpdate_2_0_4 { get; }

		public UdValue<int> CompensationLocation_2_0_0 { get; }

		public MailRepoUd(UdArgs args) : base(args)
{
    Items = new global::Multicast.UserData.UdDict<global::Codebase.Mail.MailUd>(Child("items", true), childArgs => new global::Codebase.Mail.MailUd(childArgs));
    PassedBalanceUpdate_1_2_0 = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("passed_balance_update_1_2_0", true));
    PassedStatusUpdate_1_3_0 = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("passed_status_update_1_3_0", true));
    PassedUpdate_2_0_0 = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("passed_update_2_0_0", true));
    PassedCompensationMail_2_0_0 = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("passed_compensation_mail_2_0_0", true));
    PassedUpdate_2_0_3 = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("passed_update_2_0_3", true));
    PassedUpdate_2_0_4 = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("passed_update_2_0_4", true));
    CompensationLocation_2_0_0 = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("compensation_location_2_0_0", true));
}
	}
}
