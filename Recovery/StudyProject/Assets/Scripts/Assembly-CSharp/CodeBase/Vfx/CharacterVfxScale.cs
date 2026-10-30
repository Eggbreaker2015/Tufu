using Codebase.Core;
using Codebase.Player;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Vfx
{
	/// <summary>Sizes effects shown on a character relative to the default-size player, so player visuals are unchanged at factor 1.</summary>
	public static class CharacterVfxScale
	{
		private const float MinFactor = 0.35f;

		private const float MaxFactor = 4f;

		// Pudge's health bar height at scale 1, used until a player exists to measure.
		private const float FallbackReferenceHeight = 4f;

		private static float referenceHeight;

		/// <summary>1 for transforms that are not characters.</summary>
		public static float Factor(Transform target)
		{
			if (target == null) return 1f;
			UnitCore unit = target.GetComponentInParent<UnitCore>();
			float height = Height(unit);
			if (height <= 0f) return 1f;
			return Mathf.Clamp(height / ReferenceHeight(), MinFactor, MaxFactor);
		}

		// The health bar point is placed per character above its head and scales with it; renderer bounds are
		// unusable here (culled units disable their renderers, skinned bounds are padded).
		private static float Height(UnitCore unit)
		{
			if (unit == null) return 0f;
			if (unit.HealthBarPoint != null)
			{
				float height = unit.HealthBarPoint.position.y - unit.transform.position.y;
				if (height > 0.1f) return height;
			}
			return unit.Agent != null ? unit.Agent.height * Mathf.Abs(unit.transform.lossyScale.y) : 0f;
		}

		private static float ReferenceHeight()
		{
			if (referenceHeight > 0f) return referenceHeight;
			PlayerUnit player = Object.FindAnyObjectByType<PlayerUnit>();
			float height = Height(player);
			if (height <= 0f) return FallbackReferenceHeight;
			// The player's height at its spawn scale, so buffs that grow the player also grow its effects.
			float current = Mathf.Abs(player.transform.localScale.y), original = Mathf.Abs(player.OriginalScale.y);
			if (current < 0.0001f || original < 0.0001f) return FallbackReferenceHeight;
			referenceHeight = height * original / current;
			return referenceHeight;
		}

		/// <summary>The character's NavMeshAgent body (radius, height) in world units, following its current scale.</summary>
		public static bool TryGetBody(Transform target, out float radius, out float height)
		{
			radius = height = 0f;
			UnitCore unit = target != null ? target.GetComponentInParent<UnitCore>() : null;
			if (unit == null || unit.Agent == null) return false;
			// Not renderer bounds: skinned bounds are padded (Pudge's T-pose box is 7 m wide).
			Vector3 scale = unit.transform.lossyScale;
			radius = unit.Agent.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
			height = unit.Agent.height * Mathf.Abs(scale.y);
			return radius > 0.01f && height > 0.01f;
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			referenceHeight = 0f;
		}
	}
}
