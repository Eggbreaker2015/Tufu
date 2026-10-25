using Multicast.UserData;

namespace Codebase.SecretChest
{
	public class SecretChestsUd : UdObject
	{
		public UdDict<SecretChestDataUd> Chests { get; }

		public SecretChestsUd(UdArgs args) : base(args)
{
    Chests = new global::Multicast.UserData.UdDict<global::Codebase.SecretChest.SecretChestDataUd>(Child("chests", true), childArgs => new global::Codebase.SecretChest.SecretChestDataUd(childArgs));
}
	}
}
