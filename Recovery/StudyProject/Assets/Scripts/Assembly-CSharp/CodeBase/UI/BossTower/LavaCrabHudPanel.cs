using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class LavaCrabHudPanel : MonoBehaviour
	{
		private const float BlinkDuration = 2f;

		private const float BlinkSpeedMin = 0.5f;

		private const float BlinkSpeedMax = 4.5f;

		[SerializeField]
		private HitCellsBar hitCellsBar;

		[SerializeField]
		private GameObject ignitedIndicator;

		[SerializeField]
		private Image ignitedNormalImage;

		[SerializeField]
		private Image ignitedWhiteImage;

		[Inject]
		private Scellecs.Morpeh.World world;

		[Inject]
		private Stash<LavaCrabBossComponent> lavaCrabStash;

		[Inject]
		private Stash<EnemyCastingState> castingStash;

		private Filter bossFilter;

		private void Start()
		{
			// Original ELF VA 0x13c8338.
			lavaCrabStash = world.GetStash<LavaCrabBossComponent>();
			castingStash = world.GetStash<EnemyCastingState>();
			bossFilter = world.Filter.With<LavaCrabBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
		}

		private void LateUpdate()
		{
			// Original ELF VA 0x13c84c8 renders the first live crab only.
			foreach (Entity entity in bossFilter)
			{
				LavaCrabBossComponent crab = lavaCrabStash.Get(entity);
				bool ignited = castingStash.Has(entity);
				hitCellsBar.SetMax(crab.HitsToExplode);
				hitCellsBar.SetValue(crab.CurrentHits);
				hitCellsBar.transform.localScale = crab.HitsToExplode < 11 ? Vector3.one : Vector3.one * .7f;
				ignitedIndicator.SetActive(ignited);
				UpdateIgnitedFlash(entity, ignited);
				return;
			}
		}

		private void UpdateIgnitedFlash(Entity entity, bool ignited)
		{
			// Original ELF VA 0x13c870c; unscaled sine has no extra PI factor.
			if (!ignited)
			{
				ignitedNormalImage.enabled = true;
				ignitedWhiteImage.enabled = false;
				return;
			}
			EnemyCastingState cast = castingStash.Get(entity);
			float progress = 1f - Mathf.Clamp01((cast.Duration + cast.StartTime - Time.time) * .5f);
			float frequency = Mathf.Lerp(BlinkSpeedMin, BlinkSpeedMax, progress * progress * progress);
			float wave = Mathf.Sin(Time.unscaledTime * frequency);
			ignitedNormalImage.enabled = !(wave > 0f);
			ignitedWhiteImage.enabled = wave > 0f;
		}
	}
}
