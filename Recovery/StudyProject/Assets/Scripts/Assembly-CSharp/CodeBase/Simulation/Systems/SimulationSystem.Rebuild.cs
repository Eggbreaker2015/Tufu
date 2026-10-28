namespace Codebase.Simulation.Systems
{
	public partial class SimulationSystem
	{
		// ---- cross-module partial hooks (design 4.4, 4.6, 7.4) ----
		// Declared here, implemented by the owning unit's file; a hook without an implementation compiles away.
		// Call sites are placed by C3b. Not to be confused with SimulationHarness's RebuildJob* hooks.

		// Replay/checkpoint state for day `day`, right after its forced snapshot (C4, .Replay.cs).
		partial void RebuildApplyDayOverride(int day);

		// Stored DpsData/DefenseData caches after CalculateSimulationData (C4, .Replay.cs).
		partial void RebuildApplyReplayCaches();

		// Tick progress and heartbeat after every ExecuteTick; forwards to SimulationHarness.OnTickCompleted (C3b).
		partial void RebuildOnTickCompleted();

		// In-session reset between replay/checkpoint jobs (S1, .Reset.cs); sets `reset` when implemented.
		partial void RebuildResetForJob(ref bool reset);

		// Entry point for SimulationHarness, which cannot call a private partial method. False while S1 is absent.
		internal bool RebuildTryResetForJob()
		{
			bool reset = false;
			RebuildResetForJob(ref reset);
			return reset;
		}
	}
}
