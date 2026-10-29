using System;
using Codebase.Vfx;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Defaults for a radius-1, height-2 character. Existing particle prefabs are never auto-overwritten.</summary>
[InitializeOnLoad]
internal static class LevelUpVfxPrefabBuilder
{
	private const string PrefabPath = "Assets/Resources/Vfx/LevelUpVfx.prefab";
	private const string MenuPath = "Tools/学习测试/重建升级特效预制体";
	private const float Duration = 2f;

	static LevelUpVfxPrefabBuilder()
	{
		EditorApplication.delayCall += BuildIfNeeded;
		EditorApplication.playModeStateChanged += OnPlayModeChanged;
	}

	private static void OnPlayModeChanged(PlayModeStateChange state)
	{
		if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += BuildIfNeeded;
	}

	private static void BuildIfNeeded()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode) return;
		if (EditorApplication.isCompiling || EditorApplication.isUpdating)
		{
			EditorApplication.delayCall += BuildIfNeeded;
			return;
		}
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
		if (prefab == null) return;
		foreach (ParticleSystem system in prefab.GetComponentsInChildren<ParticleSystem>(true))
			if (system.transform != prefab.transform) return;
		Rebuild();
	}

	[MenuItem(MenuPath)]
	private static void Rebuild()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
		GameObject root = null;
		try
		{
			// Resolve every dependency before touching the existing prefab.
			Material beam = LoadMaterial("Beam");
			Material ring = LoadMaterial("Ring");
			Material runes = LoadMaterial("Runes");
			Material halo = LoadMaterial("Halo");
			Material star = LoadMaterial("Star");
			root = new GameObject("LevelUpVfx");
			ParticleSystem controller = root.AddComponent<ParticleSystem>();
			Configure(controller, null, Duration, 1);
			controller.GetComponent<ParticleSystemRenderer>().enabled = false;
			root.AddComponent<LevelUpVfx>();

			CreateBeam(root, "Light column - outer glow", beam, 5.2f, 3.3f,
				new Color(1.45f, 1.15f, 0.65f, 0.5f));
			CreateBeam(root, "Light column - bright core", beam, 2.3f, 3.3f,
				new Color(1.45f, 1.25f, 0.85f, 0.65f));

			ParticleSystem wave = CreateGround(root, "Expanding shockwave", ring, 0.7f, 8f, 0.002f,
				new Color(1.6f, 1.2f, 0.6f, 1f));
			SetSize(wave, Sample(t => Mathf.Lerp(0.15f, 1f, 1f - (1f - t) * (1f - t))));
			SetColor(wave, Alpha(t => Mathf.Pow(1f - t, 1.5f), 0f, 0.15f, 0.3f, 0.45f, 0.6f, 0.75f, 0.9f, 1f));

			ParticleSystem runeCircle = CreateGround(root, "Rotating runes", runes, Duration, 3.8f, 0.008f,
				new Color(1.35f, 1.15f, 0.7f, 0.85f));
			SetColor(runeCircle, Alpha(t => Mathf.Clamp01(t * Duration / 0.12f) *
				(1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.15f, Duration, t * Duration))),
				0f, 0.03f, 0.06f, 0.575f, 0.68f, 0.7875f, 0.9f, 1f));
			var runeRotation = runeCircle.rotationOverLifetime;
			runeRotation.enabled = true;
			runeRotation.z = 22f * Mathf.Deg2Rad;

			ParticleSystem glow = CreateGround(root, "Ground glow", halo, Duration, 4f, 0.004f,
				new Color(1.2f, 0.9f, 0.4f, 0.38f));
			SetColor(glow, Alpha(t => Rise(t) * Fade(t)));
			CreateSparks(root, "Spiral stars", star, halo, 100f, 0.12f, true);
			CreateSparks(root, "Ascending motes", halo, halo, 65f, 0.065f, false);
			CreateFlash(root, halo);

			controller.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			// Overwrite the same asset path to preserve its existing .meta GUID.
			GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
			if (!success) throw new InvalidOperationException("Could not save " + PrefabPath);
			Debug.Log("升级特效：已重建粒子预制体。选中根节点，用 Scene 的 Particle Effect 面板预览；默认值之后只会在手动重建时覆盖。", saved);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		finally
		{
			if (root != null) Object.DestroyImmediate(root);
		}
	}

	[MenuItem(MenuPath, true)]
	private static bool CanRebuild()
	{
		return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
	}

	private static Material LoadMaterial(string name)
	{
		string path = "Assets/Material/LevelUp" + name + "_ADD.mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (material == null) throw new InvalidOperationException("Missing level-up material: " + path);
		return material;
	}

	private static ParticleSystem Create(GameObject root, string name, Material material, float duration, int maxParticles)
	{
		GameObject layer = new GameObject(name);
		layer.layer = root.layer;
		layer.transform.SetParent(root.transform, false);
		ParticleSystem particles = layer.AddComponent<ParticleSystem>();
		Configure(particles, material, duration, maxParticles);
		return particles;
	}

	private static void Configure(ParticleSystem particles, Material material, float duration, int maxParticles)
	{
		particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		var main = particles.main;
		main.playOnAwake = true;
		main.loop = false;
		main.duration = duration;
		main.startLifetime = duration;
		main.startSpeed = 0f;
		main.startSize = 1f;
		main.startColor = Color.white;
		main.gravityModifier = 0f;
		main.simulationSpace = ParticleSystemSimulationSpace.Local;
		main.scalingMode = ParticleSystemScalingMode.Hierarchy;
		main.maxParticles = maxParticles;
		main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
		main.stopAction = ParticleSystemStopAction.None; // Runtime waits for every child and trail.
		var shape = particles.shape;
		shape.enabled = false;
		var emission = particles.emission;
		emission.enabled = false;
		emission.rateOverTime = 0f;
		emission.rateOverDistance = 0f;
		var renderer = particles.GetComponent<ParticleSystemRenderer>();
		renderer.sharedMaterial = material;
		renderer.renderMode = ParticleSystemRenderMode.Billboard;
		renderer.maxParticleSize = 10f; // Large columns/rings must not be clamped to half the viewport.
		renderer.shadowCastingMode = ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.lightProbeUsage = LightProbeUsage.Off;
		renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
		renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
	}

	private static void Burst(ParticleSystem particles)
	{
		var emission = particles.emission;
		emission.enabled = true;
		emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
	}

	private static void CreateBeam(GameObject root, string name, Material material, float width, float height, Color tint)
	{
		ParticleSystem particles = Create(root, name, material, Duration, 1);
		var main = particles.main;
		main.startSize3D = true;
		main.startSizeX = width;
		main.startSizeY = height;
		main.startSizeZ = 1f;
		main.startColor = tint;
		var renderer = particles.GetComponent<ParticleSystemRenderer>();
		renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
		renderer.pivot = new Vector3(0f, 0.5f, 0f); // Offset the quad upward by half its height; base stays at the feet.
		var size = particles.sizeOverLifetime;
		size.enabled = true;
		size.separateAxes = true;
		size.x = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
			new Keyframe(0f, 1f), new Keyframe(0.425f, 1f), new Keyframe(0.975f, 0.08f), new Keyframe(1f, 0.08f)));
		size.y = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
			new Keyframe(0f, 0f), new Keyframe(0.05f, 1f), new Keyframe(1f, 1f)));
		size.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));
		SetColor(particles, Alpha(t => Rise(t) * Fade(t)));
		Burst(particles);
	}

	private static ParticleSystem CreateGround(GameObject root, string name, Material material, float lifetime,
		float diameter, float lift, Color tint)
	{
		ParticleSystem particles = Create(root, name, material, lifetime, 1);
		particles.transform.localPosition = Vector3.up * lift;
		var main = particles.main;
		main.startSize = diameter;
		main.startColor = tint;
		particles.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
		Burst(particles);
		return particles;
	}

	private static void CreateSparks(GameObject root, string name, Material material, Material trailMaterial,
		float rate, float startSize, bool withTrails)
	{
		ParticleSystem particles = Create(root, name, material, 0.95f, 160);
		var main = particles.main;
		main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 0.9f);
		main.startSize = new ParticleSystem.MinMaxCurve(startSize, startSize * 2.2f);
		main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
		var emission = particles.emission;
		emission.enabled = true;
		emission.rateOverTime = rate;
		var shape = particles.shape;
		shape.enabled = true;
		shape.shapeType = ParticleSystemShapeType.Circle;
		shape.rotation = new Vector3(90f, 0f, 0f);
		shape.radius = 0.95f;
		shape.radiusThickness = 0.75f;
		var velocity = particles.velocityOverLifetime;
		velocity.enabled = true;
		velocity.space = ParticleSystemSimulationSpace.Local;
		// Each XYZ group must use the same MinMaxCurve mode (TwoConstants).
		velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
		velocity.y = new ParticleSystem.MinMaxCurve(2.4f, 3.4f);
		velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
		velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
		velocity.orbitalY = new ParticleSystem.MinMaxCurve(3f, 6f);
		velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
		velocity.radial = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
		Gradient gradient = new Gradient();
		gradient.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.64f, 0.2f), 0f),
			new GradientColorKey(new Color(1f, 0.94f, 0.67f), 0.45f),
			new GradientColorKey(new Color(1f, 0.72f, 0.3f), 1f) },
			new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, 0.1f),
				new GradientAlphaKey(0.85f, 0.7f), new GradientAlphaKey(0f, 1f) });
		SetColor(particles, gradient);
		var rotation = particles.rotationOverLifetime;
		rotation.enabled = true;
		rotation.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
		SetSize(particles, new ParticleSystem.MinMaxCurve(1f,
			new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.2f, 1f), new Keyframe(0.4f, 0.65f),
				new Keyframe(0.6f, 1f), new Keyframe(1f, 0f))));
		particles.GetComponent<ParticleSystemRenderer>().maxParticleSize = 0.4f;
		if (!withTrails) return;
		var trails = particles.trails;
		trails.enabled = true;
		trails.ratio = 0.6f;
		trails.lifetime = 0.12f;
		trails.dieWithParticles = true;
		trails.worldSpace = false;
		trails.minVertexDistance = 0.025f;
		trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.35f, 1f, 0f));
		particles.GetComponent<ParticleSystemRenderer>().trailMaterial = trailMaterial;
	}

	private static void CreateFlash(GameObject root, Material halo)
	{
		ParticleSystem particles = Create(root, "Opening white-gold flash", halo, 0.22f, 1);
		particles.transform.localPosition = Vector3.up * 0.7f;
		var main = particles.main;
		main.startSize = 6f;
		main.startColor = new Color(1f, 0.98f, 0.85f, 1f);
		SetColor(particles, Alpha(t => 1f - t));
		SetSize(particles, new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f)));
		Burst(particles);
	}

	private static float Rise(float t) => Mathf.SmoothStep(0f, 1f, t * Duration / 0.1f);
	private static float Fade(float t) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1.95f, t * Duration));

	private static ParticleSystem.MinMaxCurve Sample(Func<float, float> evaluate)
	{
		const int steps = 4;
		Keyframe[] keys = new Keyframe[steps + 1];
		for (int i = 0; i <= steps; i++)
		{
			float t = (float)i / steps;
			float before = Mathf.Max(0f, t - 0.001f);
			float after = Mathf.Min(1f, t + 0.001f);
			float tangent = (evaluate(after) - evaluate(before)) / (after - before);
			keys[i] = new Keyframe(t, evaluate(t), tangent, tangent);
		}
		return new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
	}

	private static Gradient Alpha(Func<float, float> evaluate, params float[] times)
	{
		// Unity gradients support eight alpha keys. Include each timing boundary explicitly.
		if (times.Length == 0) times = new[] { 0f, 0.025f, 0.05f, 0.425f, 0.7f, 0.85f, 0.975f, 1f };
		GradientAlphaKey[] keys = new GradientAlphaKey[times.Length];
		for (int i = 0; i < times.Length; i++) keys[i] = new GradientAlphaKey(evaluate(times[i]), times[i]);
		Gradient gradient = new Gradient();
		gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, keys);
		return gradient;
	}

	private static void SetColor(ParticleSystem particles, Gradient gradient)
	{
		var color = particles.colorOverLifetime;
		color.enabled = true;
		color.color = new ParticleSystem.MinMaxGradient(gradient);
	}

	private static void SetSize(ParticleSystem particles, ParticleSystem.MinMaxCurve curve)
	{
		var size = particles.sizeOverLifetime;
		size.enabled = true;
		size.size = curve;
	}
}
