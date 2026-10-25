using System.Collections.Generic;
using Codebase.Core;
using Codebase.Map.Components;
using Codebase.Player;
using Codebase.Quests.Components;
using Scellecs.Morpeh;
using Scellecs.Morpeh.Collections;
using UnityEngine;

namespace Codebase.Quests.Systems
{
	public class QuestMiniMapPoiSystem : SystemBase
	{
		private const int InitialPoiCapacity = 16;

		[Inject]
		private Stash<QuestNpcComponent> npcStash;

		[Inject]
		private QuestModel questModel;

		[Inject]
		private QuestConfig questConfig;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		private Filter filter;

		private readonly Dictionary<Entity, PointOfInterest> poiByEntity = new Dictionary<Entity, PointOfInterest>();

		private readonly HashSet<Entity> trackedEntities = new HashSet<Entity>();

		private readonly List<Entity> entitiesToRemove = new List<Entity>();

		private readonly Color white = Color.white;

		private float refreshTimer;

		public FastList<PointOfInterest> PointsOfInterest { get; private set; }

		public int PointsOfInterestVersion { get; private set; }

		public override void OnAwake()
		{
			filter = World.Filter.With<QuestNpcComponent>().Build();
			PointsOfInterest = new FastList<PointOfInterest>(16);
			PointsOfInterestVersion = 0;
		}

		public override void OnUpdate(float deltaTime)
		{
			float remaining = refreshTimer - deltaTime;
			refreshTimer = remaining <= 0f ? .5f : remaining;
			bool changed = remaining <= 0f;
			trackedEntities.Clear();
			int count = 0;
			foreach (Entity entity in filter)
			{
			    trackedEntities.Add(entity);
			    ref QuestNpcComponent npc = ref npcStash.Get(entity);
			    var chain = questModel.Popup.GetActiveChainForNpc(npc);
			    if (chain == null || !questModel.Popup.ShouldShowNpcOnMinimap(chain))
			    {
			        if (poiByEntity.Remove(entity)) changed = true;
			        continue;
			    }
			    var giver = questConfig.GetGiverOfChain(chain);
			    Sprite sprite = giver != null ? giver.MinimapIcon : null;
			    if (sprite == null)
			    {
			        if (poiByEntity.Remove(entity)) changed = true;
			        continue;
			    }
			    if (!poiByEntity.TryGetValue(entity, out PointOfInterest poi))
			    {
			        poi = new PointOfInterest(default);
			        poiByEntity.Add(entity, poi);
			        changed = true;
			    }
			    bool highlighted = questModel.Popup.ShouldHighlightQuestNpc(chain);
			    Vector3 position = npc.Transform.position;
			    if (poi.Component.Sprite != sprite || poi.Component.isHighlighted != highlighted || !((poi.Component.WorldPosition - position).sqrMagnitude < 1e-10f)) changed = true;
			    poi.Component.WorldPosition = position;
			    poi.Component.Sprite = sprite;
			    poi.Component.Color = white;
			    poi.Component.Size = 1.95f;
			    poi.Component.Priority = 4;
			    poi.Component.isHighlighted = highlighted;
			    poi.Component.highlightColor = giver.OverrideMinimapHighlightColor ? giver.MinimapHighlightColor : iconStyleConfig.MinimapStyle.PoiHighlightColor;
			    poi.Component.IsDirty = true;
			    poi.Component.SourceVersion++;
			    if (count >= PointsOfInterest.data.Length)
			        System.Array.Resize(ref PointsOfInterest.data, PointsOfInterest.data.Length == 0 ? 16 : PointsOfInterest.data.Length * 2);
			    PointsOfInterest.data[count++] = poi;
			}
			entitiesToRemove.Clear();
			foreach (var pair in poiByEntity)
			    if (!trackedEntities.Contains(pair.Key)) entitiesToRemove.Add(pair.Key);
			foreach (Entity entity in entitiesToRemove)
			{
			    poiByEntity.Remove(entity);
			    changed = true;
			}
			if (PointsOfInterest.length != count || changed) PointsOfInterestVersion++;
			PointsOfInterest.length = count;
		}

		public override void Dispose()
		{
		}
	}
}
