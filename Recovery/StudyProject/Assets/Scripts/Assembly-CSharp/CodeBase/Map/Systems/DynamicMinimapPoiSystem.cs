using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Crystals.Objective;
using Codebase.Enemy;
using Codebase.Events;
using Codebase.Map.Components;
using Codebase.Player;
using Codebase.Quests.Components;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Map.Systems
{
	public class DynamicMinimapPoiSystem : SystemBase
	{
		[Serializable]
		private struct DynamicPoiStateComponent : ISystemStateComponent, IComponent
		{
			public Entity Entity;
		}

		[Inject]
		private Stash<PositionComponent> positionStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<CrystalUnitTag> crystalUnitStash;

		[Inject]
		private Stash<QuestMobTag> questMobStash;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private EventStyleConfig eventStyleConfig;

		private const float CrystalIconSize = 2f;

		private SystemStateProcessor<DynamicPoiStateComponent> processor;

		private readonly Color white = Color.white;

		private const int InitialCapacity = 64;

		public Dictionary<Entity, DynamicPoiData> ActivePois { get; private set; }

		public int Version { get; private set; }

		public override void OnAwake()
		{
			ActivePois = new Dictionary<Entity, DynamicPoiData>(64);
			Version = 0;
			processor = World.Filter.With<DynamicPoiTag>().With<PositionComponent>().With<EnemyTag>().With<UnitComponent>().Without<UnitDeadMarker>().ToSystemStateProcessor<DynamicPoiStateComponent>(OnEntityAdded, OnEntityRemoved);
		}

		public override void OnUpdate(float deltaTime)
		{
			processor.Process();
			foreach (Entity entity in processor.Entities)
			{
			    if (!ActivePois.TryGetValue(entity, out DynamicPoiData data)) continue;
			    data.WorldPosition = positionStash.Get(entity).Position;
			    ActivePois[entity] = data;
			}
		}

		private DynamicPoiStateComponent OnEntityAdded(Entity entity)
		{
			Vector3 position = positionStash.Get(entity).Position;
			bool crystal = crystalUnitStash.Has(entity);
			bool quest = questMobStash.Has(entity);
			EventUnitType type = GetEventUnitType(entity);
			EnemyUnit unit = unitStash.Get(entity).Core as EnemyUnit;
			ActivePois[entity] = new DynamicPoiData
			{
			    Entity = entity,
			    WorldPosition = position,
			    Sprite = quest ? iconStyleConfig.MinimapStyle.QuestMob : crystal ? iconStyleConfig.MinimapStyle.Crystal : GetSpriteForUnitType(unit, type),
			    Color = crystal ? white : GetColorForUnitType(unit, type),
			    Size = quest ? 1.25f : crystal ? 2f : GetSizeForUnitType(type),
			    IsNew = true
			};
			Version++;
			return new DynamicPoiStateComponent { Entity = entity };
		}

		private void OnEntityRemoved(ref DynamicPoiStateComponent state)
		{
			ActivePois.Remove(state.Entity);
			Version++;
		}

		private EventUnitType GetEventUnitType(Entity entity)
		{
			if (unitStash.Has(entity) && unitStash.Get(entity).Core is EnemyUnit enemy && enemy.Config != null && enemy.Config.IsEvent && enemy.Config.EventDef != null)
			    return enemy.Config.EventDef.UnitType;
			return EventUnitType.Mob;
		}

		private Color GetColorForUnitType(EnemyUnit unit, EventUnitType unitType)
		{
			if (unit.EventUnitReward == null || !(unit.EventUnitReward.CurrencyAmount > 0)) return white;
			return eventStyleConfig.GetCurrencyStyle(unit.EventUnitReward.CurrencyKey).Color;
		}

		private Sprite GetSpriteForUnitType(EnemyUnit unit, EventUnitType unitType)
		{
			if (unitType == EventUnitType.Boss) return iconStyleConfig.MinimapStyle.EventBoss;
			if (unitType == EventUnitType.MiniBoss) return iconStyleConfig.MinimapStyle.EventMiniBoss;
			if (unitType == EventUnitType.Mob && unit.EventUnitReward != null && unit.EventUnitReward.CurrencyAmount > 0)
			    return eventStyleConfig.GetCurrencyStyle(unit.EventUnitReward.CurrencyKey).MinimapSprite;
			return iconStyleConfig.MinimapStyle.EventMob;
		}

		private float GetSizeForUnitType(EventUnitType unitType)
		{
			switch (unitType)
			{
			    case EventUnitType.Mob: return 1.25f;
			    case EventUnitType.MiniBoss: return 1.25f;
			    case EventUnitType.Boss: return 1.5f;
			    default: return .5f;
			}
		}

		public override void Dispose()
		{
			processor.Dispose();
			ActivePois?.Clear();
		}
	}
}
