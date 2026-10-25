using System.Collections.Generic;

namespace Codebase.Simulation.Forecast
{
    public readonly struct ForecastBuildResult
    {
        public readonly List<ForecastSnapshotExtended> Snapshots;
        public readonly ForecastEventPlan EventPlan;
        public ForecastBuildResult(List<ForecastSnapshotExtended> snapshots, ForecastEventPlan eventPlan)
        {
            Snapshots = snapshots;
            EventPlan = eventPlan;
        }
    }
}
