using Multicast.UserData;

namespace Codebase.Enemy
{
    public class SpawnerDataUd : UdArrayObject
    {
        public UdValue<int> LastSpawnTime { get; }
        public UdValue<int> SavedCount { get; }
        public UdValue<int> RemainingCharges { get; }
        public UdValue<int> NextBigSpawnTime { get; }
        public UdValue<int> ClearedOnce { get; }
        public UdValue<int> ChargesSyncPeriod { get; }
        public UdValue<int> KnownMaxCharges { get; }
        public SpawnerDataUd(UdArgs args) : base(args)
        {
            LastSpawnTime = new UdValue<int>(Child("last_spawn_time", true));
            SavedCount = new UdValue<int>(Child("saved_count", true));
            RemainingCharges = new UdValue<int>(Child("remaining_charges", true), -1);
            NextBigSpawnTime = new UdValue<int>(Child("next_big_spawn_time", true));
            ClearedOnce = new UdValue<int>(Child("cleared_once", true));
            ChargesSyncPeriod = new UdValue<int>(Child("charges_sync_period", true));
            KnownMaxCharges = new UdValue<int>(Child("known_max_charges", true), -1);
        }
    }
}
