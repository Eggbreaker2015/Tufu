using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player.Inventory;
using Codebase.Skills;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Scellecs.Morpeh.Providers;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Pet
{
	public static class PetUnitFactory
	{
		private const float AgentRadius = 0.5f;

		private const float AgentHeight = 2f;

		private const float AgentAngularSpeed = 720f;

		private const float AgentAcceleration = 20f;

		public static PetUnit Create(ItemModel item, PetDefinition definition, Vector3 pos, Quaternion rot, float agentSpeed)
		{
			Stash<PetTag> petTagStash = World.Default.GetStash<PetTag>();
			// Order matters: UnitCore.Awake reads the EntityProvider and Start needs the agent.
			GameObject root = new GameObject("Pet " + item.Key);
			root.transform.SetPositionAndRotation(pos, rot);
			root.AddComponent<EntityProvider>();
			NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
			agent.radius = AgentRadius;
			agent.height = AgentHeight;
			agent.angularSpeed = AgentAngularSpeed;
			agent.acceleration = AgentAcceleration;
			agent.speed = agentSpeed;
			PetUnit unit = root.AddComponent<PetUnit>();
			Transform healthBarPoint = new GameObject("HealthBarPoint").transform;
			healthBarPoint.SetParent(root.transform, false);
			Transform visualSlot = new GameObject("VisualSlot").transform;
			visualSlot.SetParent(root.transform, false);
			unit.Setup(item, definition, agent, healthBarPoint, visualSlot);
			unit.AttachVisual(definition);
			unit.Stats = new List<Stat>();
			unit.BuildStatLookup();
			petTagStash.Set(unit.EntityProvider.Entity);
			Injector.InjectInto(unit);
			return unit;
		}
		public static void Destroy(PetUnit unit)
		{
			if (unit == null)
				return;
			if (unit.HealthBar != null)
			{
				unit.Dispose();
				unit.HealthBar = null;
			}
			// The EntityProvider does not remove its entity on destroy; without this NavMeshSyncSystem
			// keeps reading the destroyed NavMeshAgent (MissingReferenceException every frame).
			Entity entity = unit.EntityProvider.Entity;
			if (!World.Default.IsDisposed(entity))
			{
				if (GameContext.Has<SkillService>())
				{
					GameContext.Get<SkillService>().ClearBuffs(entity);
					GameContext.Get<SkillService>().CancelCast(entity);
				}
				World.Default.RemoveEntity(entity);
			}
			Object.Destroy(unit.gameObject);
		}

	}
}
