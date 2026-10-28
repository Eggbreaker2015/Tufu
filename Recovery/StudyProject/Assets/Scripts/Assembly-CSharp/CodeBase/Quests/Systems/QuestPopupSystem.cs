using System.Collections.Generic;
using Codebase.Core;
using Codebase.Quests.Components;
using Codebase.UI;
using Codebase.UI.Quests;
using Codebase.UI.World;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Quests.Systems
{
	public class QuestPopupSystem : SystemBase
	{
		private struct ActivePopup
		{
			public QuestPopupView View;

			public Transform Anchor;
		}

		private const float UpdateInterval = 0.2f;

		private const float PopupHeightOffset = 8f;

		private const float PlaqueHeightOffset = 6f;

		[Inject]
		private Stash<QuestNpcComponent> npcStash;

		[Inject]
		private QuestModel questModel;

		[Inject]
		private QuestConfig questConfig;

		[Inject]
		private DynamicWorldCanvas worldCanvas;

		[Inject]
		private UiModel uiModel;

		private Filter filter;

		private float updateTimer;

		private readonly Dictionary<Entity, ActivePopup> activeViews;

		private readonly HashSet<Entity> trackedEntities;

		private readonly List<Entity> entitiesToRemove;

		private QuestPopupView prefab;

		public override void OnAwake()
		{
			// Original ELF 0x1430508.
			filter = Filter.With<QuestNpcComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Original ELF 0x14305bc.
			updateTimer += deltaTime;
			if (updateTimer < UpdateInterval) return;
			updateTimer = 0f;
			if (!uiModel.IsWorldUiVisible) { RemoveAllViews(); return; }
			trackedEntities.Clear();
			foreach (var entity in filter) {
			    trackedEntities.Add(entity);
			    var npc = npcStash.Get(entity);
			    if (!questModel.Popup.TryGetQuestPopupChain(npc, out var chain, out var displayMode)) {
			        RemoveView(entity);
			        continue;
			    }
			    if (activeViews.TryGetValue(entity, out var popup)) {
			        SyncAnchorPosition(popup.Anchor, ResolveAnchorTarget(npc), displayMode);
			        popup.View.Setup(chain, displayMode);
			    } else CreateView(entity, npc, chain, displayMode);
			}
			entitiesToRemove.Clear();
			foreach (var pair in activeViews) if (!trackedEntities.Contains(pair.Key)) entitiesToRemove.Add(pair.Key);
			foreach (var entity in entitiesToRemove) RemoveView(entity);
		}

		public override void Dispose()
		{
			// Original ELF 0x1431598.
			RemoveAllViews();
		}

		private void CreateView(Entity entity, QuestNpcComponent npc, QuestChainDef chain, QuestPopupDisplayMode displayMode)
		{
			// Original ELF 0x14313b8.
			var view = worldCanvas.CreateNew(GetPrefab());
			var anchor = CreateAnchor(ResolveAnchorTarget(npc), displayMode);
			view.gameObject.SetActive(true);
			worldCanvas.AddTarget(anchor, view.transform as RectTransform);
			Injector.InjectInto(view);
			view.Setup(chain, displayMode);
			activeViews[entity] = new ActivePopup { View = view, Anchor = anchor };
		}

		private Transform ResolveAnchorTarget(QuestNpcComponent npc)
		{
			// Original ELF 0x143124c.
			if (questModel.Prologue.IsZeroQuestNpc(npc)) {
			    var anchor = QuestNpcLocator.FindZeroQuestGiverPlaqueAnchor();
			    if (anchor != null) return anchor;
			}
			return npc.Transform;
		}

		private void SyncAnchorPosition(Transform anchor, Transform target, QuestPopupDisplayMode displayMode)
		{
			// Original ELF 0x1431300.
			anchor.position = target.position + Vector3.up * GetHeightOffset(displayMode);
		}

		private float GetHeightOffset(QuestPopupDisplayMode displayMode)
		{
			// Original ELF 0x1431790.
			return displayMode == QuestPopupDisplayMode.Plaque ? PlaqueHeightOffset : PopupHeightOffset;
		}

		private Transform CreateAnchor(Transform target, QuestPopupDisplayMode displayMode)
		{
			// Original ELF 0x14316ec.
			var anchor = new GameObject("QuestPopupAnchor").transform;
			SyncAnchorPosition(anchor, target, displayMode);
			return anchor;
		}

		private void RemoveAllViews()
		{
			// Original ELF 0x1430d70.
			entitiesToRemove.Clear();
			foreach (var entity in activeViews.Keys) entitiesToRemove.Add(entity);
			foreach (var entity in entitiesToRemove) RemoveView(entity);
		}

		private void RemoveView(Entity entity)
		{
			// Original ELF 0x1431144.
			if (!activeViews.Remove(entity, out var popup)) return;
			popup.View.DisposeView();
			worldCanvas.RemoveTarget(popup.Anchor);
			worldCanvas.Despawn(popup.View);
			Object.Destroy(popup.Anchor.gameObject);
		}

		private QuestPopupView GetPrefab()
		{
			// Original ELF 0x143159c.
			if (prefab != null) return prefab;
			var go = global::Codebase.Core.RecoveredAddressables.Load("Assets/Prefabs/UI/Mines/Quests/QuestPopup.prefab");
			prefab = go.GetComponent<QuestPopupView>();
			return prefab;
		}
	
		public QuestPopupSystem()
		{
			// Original ELF 0x14317a4.
			activeViews = new Dictionary<Entity, ActivePopup>();
			trackedEntities = new HashSet<Entity>();
			entitiesToRemove = new List<Entity>();
		}

	}
}
