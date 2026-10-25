using Multicast.UserData;

namespace Codebase.Economy
{
	public class CurrencyUd : UdObject
	{
		public UdDictValue<double> Balances { get; }

		public CurrencyUd(UdArgs args) : base(args)
{
    Balances = new global::Multicast.UserData.UdDictValue<global::System.Double>(Child("balances", true));
}
	}
}
