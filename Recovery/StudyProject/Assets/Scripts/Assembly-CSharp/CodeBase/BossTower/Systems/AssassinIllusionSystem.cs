using System.Collections.Generic;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Codebase.Utilities.ObjectPool;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Systems
{
	public class AssassinIllusionSystem : SystemBase
	{
		private Filter bossFilter;

		private Filter illusionFilter;

		private Event<WeaponHitEvent> hitEvent;

		private List<Entity> illusionsToRemove;

		[Inject]
		private Stash<AssassinBossComponent> assassinStash;

		[Inject]
		private Stash<AssassinIllusionTag> illusionTagStash;

		[Inject]
		private Stash<BossHiddenState> hiddenStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<UnitDeadMarker> deadMarkerStash;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private EnemiesModel enemiesModel;

		private ObjectPool<EnemyUnit> illusionPool;

		private Vector3 hiddenPosition;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void UpdateBosses(float deltaTime)
		{
		}

		private void CreateIllusions(Entity realAssassin, ref AssassinBossComponent assassin)
		{
		}

		private void HandleIllusionDeath(UnitCore unit)
		{
		}

		private void HandleIllusionDevour(UnitCore unit)
		{
		}

		private void ProcessHits()
		{
		}

		private void RevealRealAssassin(Entity realAssassin)
		{
		}

		private void DestroyAllIllusions(Entity realAssassin)
		{
		}
	}
}
