using Multicast.UserData;

namespace Codebase.Gates
{
	public class GatesUd : UdObject
	{
		public UdListObject<GateDataUd> Gates { get; }

		public GatesUd(UdArgs args) : base(args)
{
    Gates = new global::Multicast.UserData.UdListObject<global::Codebase.Gates.GateDataUd>(Child("gates", true), childArgs => new global::Codebase.Gates.GateDataUd(childArgs));
}
	}
}
