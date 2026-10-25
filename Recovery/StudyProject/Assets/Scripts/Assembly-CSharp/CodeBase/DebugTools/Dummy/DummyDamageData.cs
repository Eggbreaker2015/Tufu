namespace Codebase.DebugTools.Dummy
{
	public class DummyDamageData
	{
		public double TotalDamage;

		public double LastHitDamage;

		public double Dps;

		public float TimeSinceLastHit;

		public bool IsInCombat;

		private float firstHitTime;

		private float lastHitTime;

		private int hitCount;

		private const float ResetTimeout = 5f;

		public void RecordHit(double damage)
		{
		}

		public void Tick(float deltaTime)
		{
		}

		private void Reset()
		{
		}
	}
}
