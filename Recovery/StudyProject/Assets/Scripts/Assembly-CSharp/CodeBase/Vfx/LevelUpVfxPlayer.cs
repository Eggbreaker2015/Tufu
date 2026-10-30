using System;
using Codebase.Rage;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Scellecs.Morpeh.Collections;
using UnityEngine;

namespace Codebase.Vfx
{
	/// <summary>Connects the reusable visual to the player's real rage level-up event.</summary>
	public sealed class LevelUpVfxPlayer : MonoBehaviour
	{
		private World world;
		private Filter players;
		private Stash<UnitComponent> unitStash;
		private IDisposable subscription;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Bootstrap()
		{
			if (FindAnyObjectByType<LevelUpVfxPlayer>() != null) return;
			GameObject listener = new GameObject("Level-up VFX events");
			DontDestroyOnLoad(listener);
			listener.AddComponent<LevelUpVfxPlayer>();
		}

		private void Update()
		{
			EnsureSubscription();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (UnityEngine.Input.GetKeyDown(KeyCode.L)) PlayOnPlayer();
#endif
		}

		private void EnsureSubscription()
		{
			World current = World.Default;
			if (current == world && subscription != null && !current.IsNullOrDisposed()) return;
			ReleaseSubscription();
			if (current.IsNullOrDisposed()) return;
			world = current;
			players = world.Filter.With<PlayerTag>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			unitStash = world.GetStash<UnitComponent>();
			subscription = world.GetEvent<RageLevelUpEvent>().Subscribe(OnRageLevelUp);
		}

		private void OnRageLevelUp(FastList<RageLevelUpEvent> changes)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return;
			// One visual for the batch, including a hit which crosses several thresholds.
			for (int i = 0; i < changes.length; i++)
			{
				if (changes[i].RageLevel <= 0) continue;
				PlayOnPlayer();
				break;
			}
		}

		public bool PlayOnPlayer()
		{
			EnsureSubscription();
			if (world.IsNullOrDisposed() || players == null) return false;
			foreach (Entity entity in players)
			{
				var unit = unitStash.Get(entity).Core;
				if (unit == null || !unit.isActiveAndEnabled) continue;
				return LevelUpVfx.Play(unit.transform, visualRoot: unit.VisualRoot) != null;
			}
			return false;
		}

		private void ReleaseSubscription()
		{
			subscription?.Dispose();
			subscription = null;
			players = null;
			unitStash = null;
			world = null;
		}

		private void OnDisable()
		{
			ReleaseSubscription();
		}
	}
}
