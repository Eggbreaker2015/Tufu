namespace Codebase.Simulation.Rebuild
{
    public enum HealthModel { RegenRate = 0, RegenToFull = 1 }

    public partial class SimulationRebuildParams
    {
        public HealthModel HealthModel = HealthModel.RegenRate;
        public bool CapIdleAtSessionEnd = false;
        public bool BreakEnRouteAtBoundary = false;
    }
}
