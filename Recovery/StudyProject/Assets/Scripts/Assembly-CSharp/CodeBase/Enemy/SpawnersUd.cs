using Multicast.UserData;

namespace Codebase.Enemy
{
	public class SpawnersUd : UdObject
	{
		public UdDict<SpawnerDataUd> Spawners { get; }

		public SpawnersUd(UdArgs args) : base(args)
{
    Spawners = new global::Multicast.UserData.UdDict<global::Codebase.Enemy.SpawnerDataUd>(Child("spawners_v3", true), childArgs => new global::Codebase.Enemy.SpawnerDataUd(childArgs));
}
	}
}
