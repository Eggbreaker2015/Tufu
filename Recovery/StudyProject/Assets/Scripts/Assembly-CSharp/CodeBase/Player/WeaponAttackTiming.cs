namespace Codebase.Player
{
	public static class WeaponAttackTiming
	{
		public const float WindupDurationRatio = 1f / 3f;

		public const float TravelDurationRatio = 1f / 3f;

		public const float ReturnDurationRatio = 1f / 3f;

		public const float HitElapsedRatio = 2f / 3f;

		public const float HitCooldownRatio = 0.3333333f;

		public static float GetAnimationDuration(float attackCooldown, float animationDurationCap)
		{
			return 0f;
		}

		public static float GetTravelElapsedDuration(float attackCooldown, float animationDurationCap)
		{
			return 0f;
		}

		public static float GetTravelElapsedDuration(float attackCooldown, float animationDurationCap, float windupRatio, float travelRatio, float returnRatio)
		{
			return 0f;
		}

		public static float GetHitElapsedDuration(float attackCooldown, float animationDurationCap)
		{
			return 0f;
		}

		public static float GetHitElapsedDuration(float attackCooldown, float animationDurationCap, float hitTravelProgress)
		{
			return 0f;
		}

		public static float GetHitElapsedDuration(float attackCooldown, float animationDurationCap, float hitTravelProgress, float overtravelRatio)
		{
			return 0f;
		}

		public static float GetHitElapsedDuration(float attackCooldown, float animationDurationCap, float hitTravelProgress, float windupRatio, float travelRatio, float returnRatio)
		{
			return 0f;
		}

		public static float GetHitElapsedDuration(float attackCooldown, float animationDurationCap, float windupRatio, float travelRatio, float returnRatio)
		{
			return 0f;
		}

		public static float GetWindupElapsedDuration(float attackCooldown, float animationDurationCap)
		{
			return 0f;
		}

		public static float GetWindupElapsedDuration(float attackCooldown, float animationDurationCap, float windupRatio, float travelRatio, float returnRatio)
		{
			return 0f;
		}

		public static float GetHitCooldownThreshold(float attackCooldown, float animationDurationCap)
		{
			return 0f;
		}

		public static float GetHitCooldownThreshold(float attackCooldown, float animationDurationCap, float hitTravelProgress)
		{
			return 0f;
		}

		public static float GetHitCooldownThreshold(float attackCooldown, float animationDurationCap, float hitTravelProgress, float overtravelRatio)
		{
			return 0f;
		}

		public static float GetHitCooldownThreshold(float attackCooldown, float animationDurationCap, float hitTravelProgress, float windupRatio, float travelRatio, float returnRatio)
		{
			return 0f;
		}

		public static float GetHitCooldownThreshold(float attackCooldown, float animationDurationCap, float windupRatio, float travelRatio, float returnRatio)
		{
			return 0f;
		}

		public static float GetStrikeEndElapsedDuration(float attackCooldown, float animationDurationCap, float windupRatio, float travelRatio, float returnRatio, float overtravelRatio)
		{
			return 0f;
		}

		public static float GetHitCooldownThresholdAtStrikeEnd(float attackCooldown, float animationDurationCap, float windupRatio, float travelRatio, float returnRatio, float overtravelRatio)
		{
			return 0f;
		}

		private static float GetNormalizedWindupRatio(float windupRatio, float travelRatio, float returnRatio)
		{
			return 0f;
		}

		private static float GetNormalizedTravelRatio(float windupRatio, float travelRatio, float returnRatio)
		{
			return 0f;
		}
	}
}
