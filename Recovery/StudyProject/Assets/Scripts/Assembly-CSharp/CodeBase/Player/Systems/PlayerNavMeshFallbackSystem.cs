using Codebase.Buildings;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Player.Models;
using Scellecs.Morpeh;
using UnityEngine.AI;

namespace Codebase.Player.Systems
{
	public class PlayerNavMeshFallbackSystem : SystemBase
	{
		private const float OffNavMeshTimeout = 3f;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private FirecampSystem firecampSystem;

		[Inject]
		private PetsModel petsModel;

		private float offNavMeshTime;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			// Native ELF VA 0x1454344. Reuse the original firecamp after 3 seconds off mesh.
			NavMeshAgent agent = playerModel.Unit.Agent;
			if (agent != null && agent.enabled && !agent.isOnNavMesh)
			{
				offNavMeshTime += deltaTime;
				if (offNavMeshTime < OffNavMeshTimeout) return;
				agent.Warp(firecampSystem.transform.position);
				// Pet mode: pets follow the player to the fountain.
				if (PetConfig.Enabled) petsModel.WarpAll(firecampSystem.transform.position);
			}
			offNavMeshTime = 0f;
		}
	}
}
