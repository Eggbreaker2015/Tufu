using System;
using UniMob;
using UnityEngine;

namespace Codebase.Services.Time
{
    public class TimeService : ITimeService, ILifetimeScope
    {
        private static string TimeOffset
        {
            get
            {
#if UNITY_EDITOR
                return RecoveryCompatibility.StudyTestProfile.TimeOffsetKey;
#else
                return "TimeSkipSeconds";
#endif
            }
        }
        private TimeSpan _cachedCheatOffset;
        private GameTime _cachedGameTime;
        private int _cachedGameTimeFrame;
        public Lifetime Lifetime { get; }
        public TimeSpan CheatOffset => _cachedCheatOffset;
        public GameTime Now
        {
            get
            {
                if (Atom.CurrentScope != null) Ticker.TickEveryFrame();
                var frame = UnityEngine.Time.frameCount;
                if (frame == _cachedGameTimeFrame) return _cachedGameTime;
                _cachedGameTimeFrame = frame;
                _cachedGameTime = GameTime.FromDateTime(Codebase.Simulation.Harness.SimulationSandbox.Active ? Codebase.Simulation.Harness.SimulationSandbox.ClockUtc : DateTime.UtcNow + _cachedCheatOffset);
                return _cachedGameTime;
            }
        }
        public TimeService(Lifetime lifetime)
        {
            Lifetime = lifetime;
            _cachedCheatOffset = LoadCheatOffset();
        }
        private TimeSpan LoadCheatOffset() => Codebase.Simulation.Harness.SimulationSandbox.Active ? TimeSpan.Zero : TimeSpan.Zero + TimeSpan.FromSeconds(PlayerPrefs.GetFloat(TimeOffset, 0f));
        public bool InPast(GameTime time) => time < Now;
        public bool InFuture(GameTime time) => time > Now;
        public void UpdateCheatOffset(float offset)
        {
            PlayerPrefs.SetFloat(TimeOffset, offset);
            PlayerPrefs.Save();
            _cachedCheatOffset = LoadCheatOffset();
        }
    }
}
