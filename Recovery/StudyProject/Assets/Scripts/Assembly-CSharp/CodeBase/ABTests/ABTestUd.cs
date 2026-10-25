using Multicast.UserData;

namespace Codebase.ABTests
{
	public class ABTestUd : UdObject
	{
		public UdDictValue<string> Tests { get; }

		public UdValue<string> AbData { get; }

		public UdValue<int> AbDataRevision { get; }

		public ABTestUd(UdArgs args) : base(args)
{
    Tests = new global::Multicast.UserData.UdDictValue<global::System.String>(Child("tests", true));
    AbData = new global::Multicast.UserData.UdValue<global::System.String>(Child("ab_data", true));
    AbDataRevision = new global::Multicast.UserData.UdValue<global::System.Int32>(Child("ab_data_revision", true));
}
	}
}
