using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player.Inventory;
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
	}
}
