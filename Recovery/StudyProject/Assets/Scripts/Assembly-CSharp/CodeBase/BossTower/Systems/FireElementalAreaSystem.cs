using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Systems
{
	public class FireElementalAreaSystem : SystemBase
	{
		private Filter bossFilter;

		private Filter areaFilter;

		private Filter meteorFilter;

		[Inject]
		private Stash<FireElementalBossComponent> fireElementalStash;

		[Inject]
		private Stash<DamageAreaComponent> areaStash;

		[Inject]
		private Stash<MeteorComponent> meteorStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private PlayerModel playerModel;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void UpdateBosses(float deltaTime)
		{
		}

		private void SpawnArea(Vector3 position, ref FireElementalBossComponent boss)
		{
		}

		private void SpawnMeteor(ref FireElementalBossComponent boss)
		{
		}

		private void UpdateAreas(float deltaTime)
		{
		}

		private void UpdateMeteors(float deltaTime)
		{
		}
	}
}
