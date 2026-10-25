using Scellecs.Morpeh;

namespace Codebase.Enemy.Components
{
	public struct EnemyCanEscapeMarker : IComponent
	{
		public float EscapeDistance;

		public float ThreatCooldown;

		public float TimeSinceLastThreat;

		public float StuckTimerMax;

		public float StuckTimer;
	}
}
