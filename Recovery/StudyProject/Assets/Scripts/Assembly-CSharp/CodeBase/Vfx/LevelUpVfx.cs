using UnityEngine;

namespace Codebase.Vfx
{
	/// <summary>Plays the authored particle prefab, scaled from a radius-1, height-2 character.</summary>
	public sealed class LevelUpVfx : MonoBehaviour
	{
		private ParticleSystem particles;
		private Transform target;
		private Vector3 feetOffset;
		private bool followTarget;

		/// <param name="radius">Positive world-space radius overrides automatic renderer bounds sizing.</param>
		/// <param name="visualRoot">Optional model root, to exclude UI and unrelated child renderers.</param>
		public static LevelUpVfx Play(Transform target, float radius = -1f, Transform visualRoot = null)
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
			effect.followTarget = true;
			effect.MeasureTarget(visualRoot != null ? visualRoot : target, radius);
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

		private void MeasureTarget(Transform visualRoot, float radiusOverride)
		{
			Bounds bounds = default;
			bool hasBounds = false;
			foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>())
			{
				if (!renderer.enabled || !(renderer is SkinnedMeshRenderer || renderer is MeshRenderer)) continue;
				if (renderer.GetComponentInParent<LevelUpVfx>() != null) continue;
				if (hasBounds) bounds.Encapsulate(renderer.bounds);
				else { bounds = renderer.bounds; hasBounds = true; }
			}

			float measuredRadius = hasBounds ? Mathf.Max(0.15f, bounds.extents.x, bounds.extents.z) : 0.5f;
			float radius = radiusOverride > 0f ? radiusOverride : measuredRadius;
			float height = hasBounds ? Mathf.Max(bounds.size.y, measuredRadius * 2f) : 2f;
			height *= radius / measuredRadius;
			transform.localScale = new Vector3(radius, height * 0.5f, radius);
			feetOffset = Vector3.up * 0.035f;
			if (hasBounds)
				feetOffset += new Vector3(bounds.center.x - target.position.x,
					Mathf.Max(0f, bounds.min.y - target.position.y), bounds.center.z - target.position.z);
		}

		private void LateUpdate()
		{
			if (followTarget)
			{
				if (target == null) { Destroy(gameObject); return; }
				transform.position = target.position + feetOffset;
			}
			// Include children and trails, so Inspector lifetime edits also affect cleanup.
			if (particles != null && !particles.IsAlive(true)) Destroy(gameObject);
		}
	}
}
