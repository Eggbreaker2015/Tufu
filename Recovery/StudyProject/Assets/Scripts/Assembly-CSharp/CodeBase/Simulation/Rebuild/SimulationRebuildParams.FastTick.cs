namespace Codebase.Simulation.Rebuild
{
	// The state the fast phase's kill gate is evaluated on (design 4.5, P02).
	public enum KillState
	{
		// Every planning round of a day uses the day's start (rewards wait for the end-of-day flush).
		StartOfDay = 0,
		// A round after the first also sees the stat rewards accumulated by the rounds before it.
		AfterPass = 1
	}

	// Active minutes of a fast day (design 4.5, P03/P04); an output only, it never feeds back into growth (4.3).
	public enum ActiveMinutesModel
	{
		// A = Kappa * sum(clears * farmedTime) / 60
		FarmedTime = 0,
		// A = TourA * Tours * NN tour(farmed spawners) / speed / 60 + TourB * sum(clears * fightTime) / 60
		NnTour = 1
	}

	// Fast-phase parameters (unit C5); defaults mirror Balance/sim/params/defaults/fasttick.json.
	public partial class SimulationRebuildParams
	{
		public KillState KillState = KillState.StartOfDay;
		// P01, L in !(L * H < loss): fixed at 1.0, the A group's GetFastRPMConfig().Item2; the theta ablation only.
		public float KillLossMultiplier = 1f;
		// L from the AB table instead (the B group's 5): a policy extrapolation, marked in the manifest.
		public bool KillLossFromAb = false;
		public ActiveMinutesModel ActiveMinutesModel = ActiveMinutesModel.FarmedTime;
		public float Kappa = 0.245f;
		public float TourA = 1.018f;
		public float TourB = 0.78f;
		public int Tours = 3;
		// A gate cleared in a planning round opens its island at once and the day plans again (design 4.5).
		public bool PlanNewIslandsSameDay = true;
		// Diagnostic switches, off by default (regression tests assert it): no kill predicate (fightTime < 300
		// still applies), and the sensitivity probes' player-DPS and defence factors (fightTime / dps and
		// loss / (dps * defence) in the kill gate).
		public bool DiagnosticDisableKillPredicate = false;
		public float DiagnosticPlayerDpsScale = 1f;
		public float DiagnosticDefenseScale = 1f;

		// Null when usable, else the reason (FastTick checks it on its first call).
		internal string ValidateFastTick()
		{
			if (!System.Enum.IsDefined(typeof(KillState), KillState)) return "params.KillState " + (int)KillState + " is unknown";
			if (!System.Enum.IsDefined(typeof(ActiveMinutesModel), ActiveMinutesModel)) return "params.ActiveMinutesModel " + (int)ActiveMinutesModel + " is unknown";
			if (!(KillLossMultiplier > 0f)) return "params.KillLossMultiplier must be > 0";
			if (!(Kappa >= 0f) || !(TourA >= 0f) || !(TourB >= 0f) || Tours < 0) return "params.Kappa, TourA, TourB and Tours must be >= 0";
			if (!(DiagnosticPlayerDpsScale > 0f) || !(DiagnosticDefenseScale > 0f)) return "params.DiagnosticPlayerDpsScale and DiagnosticDefenseScale must be > 0";
			return null;
		}

		// Diagnostics or the AB kill loss on: the run is not the regression configuration.
		internal bool FastTickDiagnosticsOn => DiagnosticDisableKillPredicate || DiagnosticPlayerDpsScale != 1f || DiagnosticDefenseScale != 1f;
	}
}
