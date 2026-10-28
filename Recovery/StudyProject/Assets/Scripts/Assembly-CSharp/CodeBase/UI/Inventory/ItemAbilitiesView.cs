using Codebase.Core;
using System.Collections.Generic;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using Codebase.Utilities.ObjectPool;
using UnityEngine;

namespace Codebase.UI.Inventory
{
	public class ItemAbilitiesView : View
	{
		[SerializeField]
		private Transform container;

		[SerializeField]
		private ItemAbilityView abilityPrefab;

		[SerializeField]
		private float elementHeight;

		[SerializeField]
		private float spacing;

		private readonly ObjectPool<ItemAbilityView> pool = new ObjectPool<ItemAbilityView>(); // Native 0x16541dc.

		private readonly List<ItemAbilityView> spawnedViews = new List<ItemAbilityView>();

		private ItemModel model;

		private int maxCount = int.MaxValue;

		private List<int> customIndices;

		private bool showLevelToUnlock;

		private bool hideNextLevel;

		protected override void Awake()
		{
			// Native ELF VA 0x1653968.
			base.Awake();
			if (container == null) container = transform;
		}

		public void Setup(ItemModel itemModel, int maxAbilitiesCount = 2147483647, bool showLevelToUnlock = false)
		{
			// Native ELF VA 0x1653a04.
			model = itemModel; maxCount = maxAbilitiesCount; customIndices = null; this.showLevelToUnlock = showLevelToUnlock; hideNextLevel = false;
			Initialize(); Render();
		}

		public void SetupSpecific(ItemModel itemModel, List<int> abilityIndices, bool hideNextLevel = false)
		{
			// Native ELF VA 0x1653a64; original preserves prior showLevelToUnlock.
			model = itemModel; maxCount = int.MaxValue; customIndices = abilityIndices; this.hideNextLevel = hideNextLevel;
			Initialize(); Render();
		}

		public float GetHeight()
		{
			// Native ELF VA 0x1653ac4; original returns -spacing for an empty list.
			int count = (customIndices ?? GetVisibleAbilityIndices(model)).Count;
			return elementHeight * count + spacing * (count - 1);
		}

		protected override void Render()
		{
			// Native ELF VA 0x1653b4c.
			if (model == null) { DespawnAll(); return; }
			List<int> indices = GetEffectiveIndices(model); EnsureCount(indices.Count);
			for (int i = 0; i < indices.Count; i++) { spawnedViews[i].transform.SetSiblingIndex(i); spawnedViews[i].Setup(model, indices[i], showLevelToUnlock, hideNextLevel); }
		}

		private List<int> GetEffectiveIndices(ItemModel itemModel)
		{
			// Native ELF VA 0x1653b38.
			return customIndices ?? GetVisibleAbilityIndices(itemModel);
		}

		private List<int> GetVisibleAbilityIndices(ItemModel itemModel)
		{
			// Native ELF VA 0x1654048; every unlocked ability plus the first locked ability, then cap count.
			// The unlocked pet-only signature (appended last in AbilityDefs) keeps a reserved slot so the cap never cuts it.
			var indices = new List<int>(); bool sawLocked = false; int sigIndex = -1;
			var defs = itemModel.Config.AbilityDefs;
			for (int i = 0; i < defs.Count; i++) {
			    var def = defs[i];
			    if (global::Codebase.Abilities.Conditions.PetModeCondition.IsHidden(def)) continue;
			    if (sigIndex < 0 && global::Codebase.Abilities.Conditions.PetModeCondition.IsPetOnly(def) && itemModel.IsAbilityUnlocked(def)) { sigIndex = i; continue; }
			    if (itemModel.IsAbilityUnlocked(def)) indices.Add(i);
			    else { if (!sawLocked) indices.Add(i); sawLocked = true; }
			}
			int room = sigIndex >= 0 ? System.Math.Max(0, maxCount - 1) : maxCount;
			if (indices.Count > room) indices.RemoveRange(room, indices.Count - room);
			if (sigIndex >= 0 && maxCount > 0) indices.Add(sigIndex);
			return indices;
		}

		private void EnsureCount(int count)
		{
			// Native ELF VA 0x1653dfc.
			while (spawnedViews.Count < count) { ItemAbilityView view = pool.Spawn(abilityPrefab, Vector3.zero, container); Injector.InjectInto(view); spawnedViews.Add(view); }
			for (int i = spawnedViews.Count - 1; i >= count; i--) { var view = spawnedViews[i]; spawnedViews.RemoveAt(i); pool.Despawn(view); }
		}

		private void DespawnAll()
		{
			// Native ELF VA 0x1653cd0.
			foreach (var view in spawnedViews) pool.Despawn(view);
			spawnedViews.Clear();
		}
	}
}

