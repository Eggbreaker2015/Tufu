using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Codebase.Services.Time
{
    [Serializable]
    public struct GameTime : IEquatable<GameTime>, IComparable<GameTime>
    {
        public DateTime time;
        [PublicAPI] public GameTime Date => FromDateTime(time.Date);
        public static GameTime FromDateTime(DateTime dateTime) => new GameTime { time = dateTime };
        public bool Equals(GameTime other) => time.Equals(other.time);
        public int CompareTo(GameTime other) => time.CompareTo(other.time);
        public override bool Equals(object obj) => obj is GameTime other && Equals(other);
        public override int GetHashCode() => time.GetHashCode();
        public override string ToString() => time.ToString();
        [PublicAPI] public double GetRemaining(GameTime now) => (Date - now).TotalSeconds;
        [PublicAPI] public DateTime ToDateTime() => time;
        [PublicAPI] public GameTime Add(TimeSpan span) => FromDateTime(time + span);
        [PublicAPI] public GameTime AddSeconds(float seconds) => FromDateTime(time + TimeSpan.FromSeconds(seconds));
        [PublicAPI] public static bool TryParse(string format, out GameTime result)
        {
            if (DateTime.TryParseExact(format, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                result = FromDateTime(parsed);
                return true;
            }
            result = default;
            return false;
        }
        public static TimeSpan operator -(GameTime a, GameTime b) => a.time - b.time;
        public static bool operator <(GameTime a, GameTime b) => a.time < b.time;
        public static bool operator >(GameTime a, GameTime b) => a.time > b.time;
    }
}
