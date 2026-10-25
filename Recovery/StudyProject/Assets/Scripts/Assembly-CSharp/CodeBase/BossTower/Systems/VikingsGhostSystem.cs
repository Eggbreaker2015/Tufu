using System.Collections.Generic;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Enemy;
using Scellecs.Morpeh;
using Codebase.Unit.Components;
using CodeBase.Audio;

namespace Codebase.BossTower.Systems
{
	public class VikingsGhostSystem : SystemBase
	{
		private const float GHOST_SPAWN_DELAY = 1f;
		private const float REVIVAL_SFX_DELAY = 0.8f;
		private Filter aliveVikingsFilter;
		private Filter pendingFilter;
		private readonly List<Entity> toFinish = new List<Entity>();
		[Inject]
		private BossTowerModel bossTowerModel;
		[Inject]
		private BossSpawnService bossSpawnService;
		[Inject]
		private VikingsBuffSystem vikingsBuffSystem;
		[Inject]
		private Stash<VikingsBossComponent> vikingsStash;
		[Inject]
		private Stash<BossPendingSpawnMarker> pendingMarkerStash;
		[Inject]
		private Stash<VikingsPendingSpawnComponent> pendingSpawnStash;
		public override void OnAwake()
		{
			aliveVikingsFilter = Filter.With<VikingsBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			pendingFilter = Filter.With<BossPendingSpawnMarker>().With<VikingsPendingSpawnComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			toFinish.Clear();
			foreach (Entity entity in pendingFilter)
			{
				ref VikingsPendingSpawnComponent pending = ref pendingSpawnStash.Get(entity);
				pending.Timer -= deltaTime;
				if (pending.Timer <= 0f)
					toFinish.Add(entity);
			}

			foreach (Entity entity in toFinish)
			{
				VikingsPendingSpawnComponent pending = pendingSpawnStash.Get(entity);
				EnemyUnit ghost = bossSpawnService.SpawnGhostViking(pending.DeadViking, pending.VikingData);
				vikingsBuffSystem.ApplyBuffToViking(ghost, ghost.EntityProvider.Entity, pending.VikingData, pending.DeadAllies, true);
				pendingMarkerStash.Remove(entity);
				pendingSpawnStash.Remove(entity);
			}
		}

		public void OnVikingDied(EnemyUnit unit, VikingsBossComponent vikingData, int deadAlliesCount)
		{
			if (vikingData.IsGhost)
				return;
			if (!bossTowerModel.TryGetBoss(vikingData.BossKey, out BossModel boss))
				return;
			if (!boss.IsSuperBoss && !IsLastNonGhostViking(vikingData.GroupId))
				return;
			GameContext.Get<IAudioPlayer>().PlayAtPosition("vikings_revival", unit.transform.position, .8f);
			Entity entity = World.CreateEntity();
			pendingMarkerStash.Set(entity);
			pendingSpawnStash.Set(entity, new VikingsPendingSpawnComponent { DeadViking = unit, VikingData = vikingData, Timer = 1f, DeadAllies = deadAlliesCount });
		}

		public void OnRaidEnded()
		{
			var pending = new List<Entity>();
			foreach (Entity entity in pendingFilter)
				pending.Add(entity);
			foreach (Entity entity in pending)
			{
				pendingMarkerStash.Remove(entity);
				pendingSpawnStash.Remove(entity);
			}
		}

		private bool IsLastNonGhostViking(int groupId)
		{
			foreach (Entity entity in aliveVikingsFilter)
			{
				VikingsBossComponent data = vikingsStash.Get(entity);
				if (data.GroupId == groupId && !data.IsGhost)
					return false;
			}

			return true;
		}
	}
}
