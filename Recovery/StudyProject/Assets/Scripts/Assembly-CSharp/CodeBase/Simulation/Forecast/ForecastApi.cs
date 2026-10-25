using System;
using System.Collections.Generic;
using System.Threading;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using Cysharp.Threading.Tasks;

namespace Codebase.Simulation.Forecast
{
    public static class ForecastApi
    {
        // Original ARM64 ELF VA 0x13f6610 and BuildAsync's state-machine body.
        public static async UniTask<ForecastEventPlan> BuildEventPlanAsync(SimulationSystem simulationSystem, int minutesAhead, int stepMinutes, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats, int snapshotsPerFrame = 1, Action<float> onProgress = null, CancellationToken cancellationToken = default)
        {
            return (await BuildAsync(simulationSystem, minutesAhead, stepMinutes, input, presets, rewardAllowedStats, snapshotsPerFrame, onProgress, cancellationToken)).EventPlan;
        }
        public static async UniTask<ForecastBuildResult> BuildAsync(SimulationSystem simulationSystem, int minutesAhead, int stepMinutes, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats, int snapshotsPerFrame = 1, Action<float> onProgress = null, CancellationToken cancellationToken = default)
        {
            var snapshots = new List<ForecastSnapshotExtended>();
            var routine = ForecastSnapshots.BuildAsync(simulationSystem, minutesAhead, stepMinutes, input, presets, rewardAllowedStats, snapshots, onProgress, snapshotsPerFrame);
            try
            {
                while (routine.MoveNext())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally { (routine as IDisposable)?.Dispose(); }
            return new ForecastBuildResult(snapshots, ForecastSnapshots.BuildEventPlan(snapshots, input));
        }
    }
}
