using System.Collections.Generic;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Utilities.ObjectPool;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class VampireMinionHudPanel : MonoBehaviour
	{
		[SerializeField]
		private Transform barsContainer;

		[SerializeField]
		private MinionHealthBarView barPrefab;

		[SerializeField]
		private VisibilityAnimator barsVisibility;

		[SerializeField]
		private VisibilityAnimator spiritTimerVisibility;

		[SerializeField]
		private Slider spiritTimerSlider;

		[Inject]
		private MinionHealthBarSystem minionHealthBarSystem;

		[Inject]
		private VampireSpiritSystem vampireSpiritSystem;

		private readonly ObjectPool<MinionHealthBarView> pool;

		private readonly Dictionary<string, MinionHealthBarView> bars;

		private readonly HashSet<string> activeGuids;

		private readonly List<string> toRemove;

        // Native ELF VA 0x13c9548.
        public VampireMinionHudPanel()
        {
            pool = new ObjectPool<MinionHealthBarView>();
            bars = new Dictionary<string, MinionHealthBarView>();
            activeGuids = new HashSet<string>();
            toRemove = new List<string>();
        }

		private void Start()
		{
			// Native ELF VA 0x13c89e4.
			spiritTimerVisibility.SetVisibleImmediate(false);
		}

		private void LateUpdate()
		{
			// Native ELF VA 0x13c8a00. Removal and visibility precede creation, as in the original.
			IReadOnlyList<MinionHealthBarData> current = minionHealthBarSystem.Data;
			activeGuids.Clear();
			foreach (MinionHealthBarData entry in current) activeGuids.Add(entry.UnitGuid);
			toRemove.Clear();
			foreach (string guid in bars.Keys) if (!activeGuids.Contains(guid)) toRemove.Add(guid);
			foreach (string guid in toRemove) { pool.Despawn(bars[guid]); bars.Remove(guid); }
			if (vampireSpiritSystem.TryGetSpiritTimerData(out float progress))
			{
			    barsVisibility.SetVisible(false);
			    spiritTimerVisibility.SetVisible(true);
			    spiritTimerSlider.value = progress;
			}
			else
			{
			    barsVisibility.SetVisible(bars.Count > 0);
			    spiritTimerVisibility.SetVisible(false);
			}
			foreach (MinionHealthBarData entry in current)
			{
			    if (!bars.TryGetValue(entry.UnitGuid, out MinionHealthBarView bar))
			    {
			        bar = pool.Spawn(barPrefab, Vector3.zero, barsContainer);
			        bars[entry.UnitGuid] = bar;
			    }
			    bar.SetData(entry);
			}
		}
	}
}
