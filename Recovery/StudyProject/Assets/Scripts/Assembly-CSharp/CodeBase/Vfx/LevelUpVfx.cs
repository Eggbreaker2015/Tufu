using UnityEngine;

namespace Codebase.Vfx
{
	/// <summary>Plays the authored particle prefab, scaled from a radius-1, height-2 character (Pudge's agent body).</summary>
	public sealed class LevelUpVfx : MonoBehaviour
	{
		private ParticleSystem particles;
		private Transform target;
		private float radiusOverride;
		private Vector3 feetOffset;
		private bool followTarget;

		/// <param name="radius">Positive world-space radius overrides automatic sizing from the NavMeshAgent body.</param>
		public static LevelUpVfx Play(Transform target, float radius = -1f)
		{
			if (target == null) return null;
			LevelUpVfx prefab = Resources.Load<LevelUpVfx>("Vfx/LevelUpVfx");
			if (prefab == null)
			{
				Debug.LogWarning("LevelUpVfx: missing Resources/Vfx/LevelUpVfx prefab.");
				return null;
			}

			LevelUpVfx effect = Instantiate(prefab, target.position, Quaternion.identity);
			effect.target = target;
			effect.radiusOverride = radius;
			effect.followTarget = true;
			effect.FitTarget();
			effect.transform.position = target.position + effect.feetOffset;
			effect.PlayParticles();
			return effect;
		}

		private void Start()
		{
			// Scene instances keep their authored position, rotation and scale.
			if (particles == null) PlayParticles();
		}

		private void PlayParticles()
		{
			if (!TryGetComponent(out particles))
			{
				Debug.LogWarning("LevelUpVfx: rebuild the particle prefab using Tools/学习测试/重建升级特效预制体.", this);
				Destroy(gameObject);
				return;
			}
			particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			particles.Play(true);
		}

		// Re-fitted every frame, so the effect grows and shrinks with the character.
		private void FitTarget()
		{
			if (!CharacterVfxScale.TryGetBody(target, out float bodyRadius, out float bodyHeight))
			{
				bodyRadius = 0.5f;
				bodyHeight = 2f;
			}
			float radius = radiusOverride > 0f ? radiusOverride : bodyRadius;
			float height = Mathf.Max(bodyHeight, bodyRadius * 2f) * radius / bodyRadius;
			transform.localScale = new Vector3(radius, height * 0.5f, radius);
			feetOffset = Vector3.up * 0.035f;
		}

		private void LateUpdate()
		{
			if (followTarget)
			{
				if (target == null) { Destroy(gameObject); return; }
				FitTarget();
				transform.position = target.position + feetOffset;
			}
			// Include children and trails, so Inspector lifetime edits also affect cleanup.
			if (particles != null && !particles.IsAlive(true)) Destroy(gameObject);
		}
	}
}
