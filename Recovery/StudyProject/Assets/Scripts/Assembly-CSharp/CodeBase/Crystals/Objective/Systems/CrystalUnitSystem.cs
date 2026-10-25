using Codebase.Core;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Crystals.Objective.Systems
{
	public class CrystalUnitSystem : SystemBase
	{
		private const double RegenBaseFraction = 0.01;
		private const double RegenMaxFraction = 0.15;
		private const double RegenExponent = 2.0;
		private const float HalfHpShakeThreshold = 0.5f;
		private const float MaxShakeMagnitude = 0.1f;
		private const float ShakeFrequency = 30f;
		private Filter regenFilter;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private CrystalUnitScene scene;
		public override void OnAwake()
		{
			regenFilter = Filter.With<UnitComponent>().With<CrystalUnitTag>().Without<UnitDeadMarker>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (var entity in regenFilter)
			{
				var core = unitStash.Get(entity).Core;
				double maximum = core.GetStatValue(StatType.Health, true);
				// Original FCMP+B.LS excludes ordered <=0; unordered NaN continues.
				if (maximum <= 0.0)
					continue;
				double missing = System.Math.Clamp(1.0 - core.CurrentHealth / maximum, 0.0, 1.0);
				core.CurrentHealth = System.Math.Min(core.CurrentHealth + maximum * (System.Math.Pow(missing, 2.0) * 0.15 + 0.01) * (double)deltaTime, maximum);
				float fraction = (float)(core.CurrentHealth / maximum);
				scene.UpdateHpColors(fraction);
				UpdateLowHpShake(core, fraction, entity.Id);
			}
		}

		private void UpdateLowHpShake(UnitCore core, float hpFraction, int entityId)
		{
			if (hpFraction <= 0.5f)
			{
				float seed = (float)entityId * 0.137f;
				float phase = UnityEngine.Time.time * 30f;
				float x = UnityEngine.Mathf.PerlinNoise(seed, phase), y = UnityEngine.Mathf.PerlinNoise(phase, seed), z = UnityEngine.Mathf.PerlinNoise(seed + 1f, phase + 1f);
				float magnitude = UnityEngine.Mathf.Clamp01((hpFraction - 0.5f) * -2f) * 0.1f;
				core.VisualRoot.localPosition = new UnityEngine.Vector3((x - 0.5f + x - 0.5f) * magnitude, (y - 0.5f + y - 0.5f) * magnitude, magnitude * (z - 0.5f + z - 0.5f));
			}
			else
				core.VisualRoot.localPosition = UnityEngine.Vector3.zero;
		}
	}
}
