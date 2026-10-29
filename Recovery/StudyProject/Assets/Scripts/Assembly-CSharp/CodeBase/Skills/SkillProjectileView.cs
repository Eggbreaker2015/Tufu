using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Skills
{
	// Runtime-built projectile for SkillVfxEntry entries without a Prefab but with a ProjectileTexture.
	// WorldVfxService.SpawnTracking moves the root (a silent ParticleSystem, so it can be pooled like any vfx prefab);
	// this view turns the textured quad along the movement, facing the camera, and clears the trail on every reuse.
	public class SkillProjectileView : MonoBehaviour
	{
		private const string ShaderName = "Sprites/Default";

		public TrailRenderer Trail;

		private Vector3 lastPosition;

		private Vector3 direction;

		private static readonly Dictionary<SkillVfxEntry, ParticleSystem> Templates = new Dictionary<SkillVfxEntry, ParticleSystem>();

		private static GameObject templateHolder;

		private static Mesh quadMesh;

		private static UnityEngine.Camera cachedCamera;

		private static Material trailMaterial;

		// Pooled instances are positioned before SetActive(true), so the old trail is dropped at the new start.
		private void OnEnable()
		{
			lastPosition = transform.position;
			direction = Vector3.zero;
			if (Trail != null)
				Trail.Clear();
		}

		public void Aim(Vector3 worldDirection)
		{
			if (worldDirection.sqrMagnitude < 1e-6f)
				return;
			direction = worldDirection.normalized;
			lastPosition = transform.position;
			Orient();
			if (Trail != null)
				Trail.Clear();
		}

		private void LateUpdate()
		{
			Vector3 position = transform.position;
			Vector3 delta = position - lastPosition;
			lastPosition = position;
			if (delta.sqrMagnitude > 1e-8f)
				direction = delta.normalized;
			Orient();
		}

		// Axial billboard: local +Z along the flight, the quad's normal (local +Y) towards the camera.
		private void Orient()
		{
			if (direction == Vector3.zero)
				return;
			if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
				cachedCamera = UnityEngine.Camera.main;
			Vector3 toCamera = cachedCamera != null ? cachedCamera.transform.position - transform.position : Vector3.up;
			Vector3 normal = Vector3.ProjectOnPlane(toCamera, direction);
			if (normal.sqrMagnitude < 1e-6f)
				normal = Vector3.ProjectOnPlane(Vector3.up, direction);
			if (normal.sqrMagnitude < 1e-6f)
				normal = Vector3.forward;
			transform.rotation = Quaternion.LookRotation(direction, normal);
		}

		// One template per entry, kept under an inactive DontDestroyOnLoad holder: active itself (so pooled clones
		// come out active), but never awake or rendered.
		public static ParticleSystem GetTemplate(SkillVfxEntry entry)
		{
			if (entry == null || entry.ProjectileTexture == null)
				return null;
			if (Templates.TryGetValue(entry, out ParticleSystem cached) && cached != null)
				return cached;
			Shader shader = Shader.Find(ShaderName);
			if (shader == null)
			{
				SkillLibrary.WarnOnce("projectile-shader", "[Skills] Shader '" + ShaderName + "' not found; projectile '" + entry.Key + "' is invisible");
				return null;
			}
			if (templateHolder == null)
			{
				templateHolder = new GameObject("[Skill Projectile Templates]");
				templateHolder.SetActive(false);
				Object.DontDestroyOnLoad(templateHolder);
			}
			float scale = entry.Scale > 0f ? entry.Scale : 1f;
			float length = Mathf.Max(0.05f, entry.ProjectileLength) * scale;
			float width = Mathf.Max(0.02f, entry.ProjectileWidth) * scale;

			var root = new GameObject("Projectile_" + entry.Key);
			root.transform.SetParent(templateHolder.transform, false);
			ParticleSystem particle = root.AddComponent<ParticleSystem>();
			particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = particle.main;
			main.playOnAwake = false;
			main.loop = true;
			main.maxParticles = 1;
			ParticleSystem.EmissionModule emission = particle.emission;
			emission.enabled = false;
			ParticleSystemRenderer particleRenderer = root.GetComponent<ParticleSystemRenderer>();
			if (particleRenderer != null)
				particleRenderer.enabled = false;

			var body = new GameObject("Body");
			body.transform.SetParent(root.transform, false);
			body.transform.localScale = new Vector3(width, 1f, length);
			body.AddComponent<MeshFilter>().sharedMesh = GetQuadMesh();
			MeshRenderer bodyRenderer = body.AddComponent<MeshRenderer>();
			bodyRenderer.sharedMaterial = new Material(shader) { name = "Projectile_" + entry.Key, mainTexture = entry.ProjectileTexture, color = entry.ProjectileTint };
			bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			bodyRenderer.receiveShadows = false;

			TrailRenderer trail = null;
			if (entry.TrailTime > 0f && entry.TrailWidth > 0f)
			{
				var tail = new GameObject("Trail");
				tail.transform.SetParent(root.transform, false);
				tail.transform.localPosition = new Vector3(0f, 0f, -length * 0.35f);
				trail = tail.AddComponent<TrailRenderer>();
				trail.sharedMaterial = GetTrailMaterial(shader);
				trail.time = entry.TrailTime;
				trail.minVertexDistance = 0.05f;
				trail.widthMultiplier = entry.TrailWidth * scale;
				trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
				Color head = entry.TrailColor;
				Color end = head;
				end.a = 0f;
				var gradient = new Gradient();
				gradient.SetKeys(new[] { new GradientColorKey(head, 0f), new GradientColorKey(head, 1f) }, new[] { new GradientAlphaKey(head.a, 0f), new GradientAlphaKey(0f, 1f) });
				trail.colorGradient = gradient;
				trail.numCapVertices = 2;
				trail.alignment = LineAlignment.View;
				trail.autodestruct = false;
				trail.emitting = true;
				trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				trail.receiveShadows = false;
			}

			root.AddComponent<SkillProjectileView>().Trail = trail;
			Templates[entry] = particle;
			return particle;
		}

		private static Material GetTrailMaterial(Shader shader)
		{
			if (trailMaterial == null)
				trailMaterial = new Material(shader) { name = "Projectile Trail" };
			return trailMaterial;
		}

		// Unit quad in the local XZ plane (normal +Y), centred; U runs along +Z so a right-pointing texture points forward.
		private static Mesh GetQuadMesh()
		{
			if (quadMesh != null)
				return quadMesh;
			quadMesh = new Mesh { name = "Projectile Quad" };
			quadMesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) };
			quadMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
			quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
			quadMesh.RecalculateNormals();
			quadMesh.RecalculateBounds();
			return quadMesh;
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			Templates.Clear();
			templateHolder = null;
			quadMesh = null;
			cachedCamera = null;
			trailMaterial = null;
		}
	}
}
