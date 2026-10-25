using Codebase.BossTower;
using Codebase.Core;
using Codebase.Enemy.Components;
using Codebase.Gates.Components;
using Codebase.Map.Components;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Scellecs.Morpeh.Collections;
using UnityEngine;

namespace Codebase.Map.Systems
{
	public class StaticMiniMapPoiSystem : SystemBase
	{
		[Inject]
		private Stash<PointOfInterestComponent> poiStash;

		[Inject]
		private Stash<PositionComponent> positionStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<PlayerTag> playerStash;

		[Inject]
		private Stash<EnemySpawnerComponent> enemySpawnerStash;

		[Inject]
		private Stash<EventSpawnerInactiveTag> eventSpawnerInactiveStash;

		[Inject]
		private Stash<GateComponent> gateStash;

		[Inject]
		private Stash<BossTowerPortalComponent> portalStash;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		public FastList<PointOfInterest> PointsOfInterest;

		public int PointsOfInterestVersion;

		private Filter filter;

		private int lastEntityCount;

		private Vector3 lastPlayerPosition;

		private bool includeOnlyNearPlayer = true;

		private bool rebuildRequired;

		private Filter playerFilter;

		private const int InitialPoiCapacity = 128;

		private const float StateRefreshIntervalSeconds = 1f;

		private readonly Color white = Color.white;

		private readonly Color invisible = new Color(1f, 1f, 1f, 0f);

		private float stateRefreshTimer;

		public bool IncludeOnlyNearPlayer
		{
			get
			{
				return this.includeOnlyNearPlayer;
			}
			set
			{
				if (includeOnlyNearPlayer == value) return;
				includeOnlyNearPlayer = value;
				rebuildRequired = true;
			}
		}

		public override void OnAwake()
		{
			filter = World.Filter.With<PointOfInterestComponent>().With<PositionComponent>().Build();
			playerFilter = World.Filter.With<UnitComponent>().With<PositionComponent>().With<PlayerTag>().Build();
			PointsOfInterest = new FastList<PointOfInterest>(128);
			lastEntityCount = 0;
		}

		public override void OnUpdate(float deltaTime)
		{
			float remaining = stateRefreshTimer - deltaTime;
			stateRefreshTimer = remaining <= 0f ? 1f : remaining;
			bool refresh = remaining <= 0f;
			int unlocked = playerModel.LastUnlockedLocation.Number;
			foreach (Entity entity in filter)
			{
			    ref PointOfInterestComponent poi = ref poiStash.Get(entity);
			    poi.IsDirty = false;
			    poi.WorldPosition = positionStash.Get(entity).Position;
			    if (enemySpawnerStash.Has(entity))
			    {
			        var spawner = enemySpawnerStash.Get(entity).Spawner;
			        int version = spawner.MinimapVersion;
			        if (spawner.Config.IsEvent && spawner.Config.EventDef != null)
			        {
			            poi.Color = invisible;
			            continue;
			        }
			        bool inactive = eventSpawnerInactiveStash.Has(entity);
			        if (refresh || poi.SourceVersion != version)
			        {
			            poi.SourceVersion = version;
			            poi.IsDirty = true;
			            if (inactive || unlocked < spawner.Config.LocationNumber) poi.Color = invisible;
			            else
			            {
			                if (spawner.HasItemReward)
			                {
			                    poi.Priority = 2;
			                    poi.Sprite = spawner.MinimapSprite;
			                    poi.Color = spawner.MinimapColor;
			                    poi.Size = 1f;
			                    if (spawner.AnyoneAlive) poi.RemainingCooldownPercent = 0f;
			                    else
			                    {
			                        float cooldown = spawner.IsBigCooldownActive ? spawner.BigCooldownSeconds : spawner.BaseCooldown;
			                        float percent = 0f;
			                        if (cooldown > 0f)
			                        {
			                            float ratio = spawner.Cooldown / cooldown;
			                            percent = ratio < 0f ? 0f : ratio > 1f ? 1f : ratio;
			                        }
			                        poi.RemainingCooldownPercent = percent;
			                    }
			                }
			                else if (spawner.HasStatReward)
			                {
			                    poi.Priority = 1;
			                    poi.Sprite = spawner.MinimapSprite;
			                    poi.Color = spawner.MinimapColor;
			                    poi.Size = .5f;
			                    poi.Color.a = spawner.AnyoneAlive ? 1f : 0f;
			                }
			                else poi.Color = invisible;
			                if (spawner.Config.IsEliteBoss && (spawner.AnyoneAlive || spawner.Cooldown > 0f))
			                {
			                    poi.Sprite = iconStyleConfig.MinimapStyle.EliteBoss;
			                    poi.Size = 1.25f;
			                    poi.Color = white;
			                    float percent = 0f;
			                    if (!spawner.AnyoneAlive)
			                    {
			                        float cooldown = spawner.IsBigCooldownActive ? spawner.BigCooldownSeconds : spawner.BaseCooldown;
			                        if (cooldown > 0f)
			                        {
			                            float ratio = spawner.Cooldown / cooldown;
			                            percent = ratio < 0f ? 0f : ratio > 1f ? 1f : ratio;
			                        }
			                    }
			                    poi.RemainingCooldownPercent = percent;
			                }
			                if (!spawner.HasBeenClearedOnce && spawner.Config.Gates.Count > 0)
			                {
			                    poi.Sprite = iconStyleConfig.MinimapStyle.Boss;
			                    poi.Size = 1.5f;
			                    poi.Color = white;
			                }
			            }
			        }
			    }
			    if (gateStash.Has(entity))
			    {
			        var gate = gateStash.Get(entity).Gate;
			        if (refresh || poi.SourceVersion != gate.MinimapVersion)
			        {
			            poi.SourceVersion = gate.MinimapVersion;
			            poi.IsDirty = true;
			            if (unlocked < gate.NextLocationNumber - 1) poi.Color = invisible;
			            else
			            {
			                poi.Sprite = gate.IsUnlocked ? iconStyleConfig.MinimapStyle.OpenedGate : iconStyleConfig.MinimapStyle.ClosedGate;
			                poi.Size = 1f;
			                poi.Color = white;
			            }
			        }
			    }
			    if (portalStash.Has(entity))
			    {
			        portalStash.Get(entity);
			        if (refresh || poi.SourceVersion != bossTowerModel.PortalVersion)
			        {
			            poi.SourceVersion = bossTowerModel.PortalVersion;
			            poi.IsDirty = true;
			            poi.Sprite = bossTowerModel.HasEnoughTickets(1) && playerModel.IsPortalUnlocked ? iconStyleConfig.MinimapStyle.PortalWithTickets : iconStyleConfig.MinimapStyle.Portal;
			            poi.Color = white;
			            poi.Size = 1.6f;
			            poi.isHighlighted = playerModel.IsPortalUnlocked && bossTowerModel.HasEnoughTickets(1);
			            poi.highlightColor = iconStyleConfig.MinimapStyle.PoiHighlightColor;
			            poi.HightlightAdditionalSize = .2f;
			        }
			    }
			    if (poi.Reference == null)
			    {
			        poi.Reference = new PointOfInterest(poi);
			        poi.IsDirty = true;
			    }
			    else poi.Reference.Component = poi;
			}
			int count = filter.GetLengthSlow();
			// Reconstruction compatibility: Boot creates the real player asynchronously.
			// Preserve original POI state updates; defer only the player-centered list until that entity exists.
			if (playerFilter.GetLengthSlow() == 0) return;
			Vector3 playerPosition = positionStash.Get(playerFilter.FirstOrDefault()).Position;
			if (count != lastEntityCount)
			{
			    lastEntityCount = count;
			    rebuildRequired = true;
			}
			if (includeOnlyNearPlayer && Vector3.Distance(lastPlayerPosition, playerPosition) > 10f)
			{
			    lastPlayerPosition = playerPosition;
			    rebuildRequired = true;
			}
			if (rebuildRequired)
			{
			    rebuildRequired = false;
			    RebuildPoiList(playerPosition);
			}
		}

		private void RebuildPoiList(Vector3 centerPos)
		{
			int count = 0;
			foreach (Entity entity in filter)
			{
			    ref PointOfInterestComponent poi = ref poiStash.Get(entity);
			    if (poi.Reference == null) continue;
			    if (includeOnlyNearPlayer && (positionStash.Get(entity).Position - centerPos).sqrMagnitude > 8100f) continue;
			    if (count >= PointsOfInterest.data.Length)
			        System.Array.Resize(ref PointsOfInterest.data, PointsOfInterest.data.Length == 0 ? 128 : PointsOfInterest.data.Length * 2);
			    PointsOfInterest.data[count++] = poi.Reference;
			}
			PointsOfInterest.length = count;
			PointsOfInterestVersion++;
		}

		public override void Dispose()
		{
		}
	}
}
