using Multicast.UserData;

namespace Codebase.SecretChest
{
	public class SecretChestDataUd : UdObject
	{
		public UdValue<bool> IsOpened { get; }

		public SecretChestDataUd(UdArgs args) : base(args)
{
    IsOpened = new global::Multicast.UserData.UdValue<global::System.Boolean>(Child("is_opened", true), false);
}
	}
}
