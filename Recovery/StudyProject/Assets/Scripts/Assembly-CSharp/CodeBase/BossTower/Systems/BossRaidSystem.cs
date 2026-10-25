using Codebase.Unit;
using System.Linq;
using System;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.BossTower.Components;
using Codebase.Buildings;
using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.UI;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Codebase.Unit.Systems;
using Codebase.Vfx;
using Scellecs.Morpeh;
using UnityEngine;
using Scellecs.Morpeh.Providers;
using Codebase.Enemy;
using Codebase.BossTower.Helpers;
using Codebase.BossTower.Commands;
using Codebase.UI.BossTower;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Codebase.Enemy.Systems;

namespace Codebase.BossTower.Systems
{
	public class BossRaidSystem : SystemBase
	{
		[Serializable]
		[StructLayout((LayoutKind)0, Size = 1)]
		private struct RaidUiStateMarker : ISystemStateComponent, IComponent
		{
		}

		private const float VICTORY_END_DELAY = 2.5f;
		private Filter activeRaidFilter;
		private Filter fightingRaidFilter;
		private Filter arenaUnitsFilter;
		private Filter bossVisualFilter;
		private Filter spiritFilter;
		private Event<WeaponHitEvent> hitEvent;
		private Event<PlayerDeathEvent> playerDeathEvent;
		private SystemStateProcessor<RaidUiStateMarker> raidUiProcessor;
		[Inject]
		private Stash<BossRaidComponent> raidStash;
		[Inject]
		private Stash<BossFightState> fightStateStash;
		[Inject]
		private Stash<BossTowerBossTag> bossTagStash;
		[Inject]
		private Stash<UnitWaitingAttackMarker> unitWaitAttack;
		[Inject]
		private Stash<UnitComponent> unitStash;
		[Inject]
		private Stash<BossVisualComponent> visualStash;
		[Inject]
		private Stash<VampireBossComponent> vampireBossStash;
		[Inject]
		private Stash<VampireBossFightVisualComponent> vampireBossFightVisualStash;
		[Inject]
		private Stash<VikingsBossComponent> vikingsStash;
		[Inject]
		private Stash<VikingsPendingSpawnComponent> vikingsPendingStash;
		[Inject]
		private Stash<HydraBossComponent> hydraStash;
		[Inject]
		private Stash<HydraPendingSpawnComponent> hydraPendingStash;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private BossTowerModel bossTowerModel;
		[Inject]
		private BossTowerConfig bossTowerConfig;
		[Inject]
		private WorldVfxService worldVfxService;
		[Inject]
		private BossArenaSystem bossArenaSystem;
		[Inject]
		private FirecampSystem firecampSystem;
		[Inject]
		private UnitDamageSystem unitDamageSystem;
		[Inject]
		private CheatsModel cheatsModel;
		[Inject]
		private UiModel uiModel;
		[Inject]
		private IAudioPlayer audioPlayer;
		private Filter bossesFilter;
		private Filter pendingSpawnFilter;
		private Filter vikingsAliveFilter;
		private Filter vikingsPendingFilter;
		private Filter hydraAliveFilter;
		private Filter hydraPendingFilter;
		private (double current, double max)? cachedCurrentBossHealth;
		public bool IsDisposing { get; set; }

		public override void OnAwake()
		{
			activeRaidFilter = Filter.With<BossRaidComponent>().With<PlayerTag>().Build();
			fightingRaidFilter = Filter.With<BossRaidComponent>().With<BossFightState>().With<PlayerTag>().Build();
			arenaUnitsFilter = Filter.With<BossArenaUnitTag>().Without<UnitDeadMarker>().Build();
			bossVisualFilter = Filter.With<BossVisualComponent>().Build();
			bossesFilter = Filter.With<BossTowerBossTag>().Without<UnitDeadMarker>().Build();
			raidUiProcessor = Filter.With<BossRaidComponent>().With<PlayerTag>().ToSystemStateProcessor<RaidUiStateMarker>(OnRaidStarted, OnRaidEnded);
			spiritFilter = Filter.With<VampireSpiritTag>().Without<UnitDeadMarker>().Build();
			pendingSpawnFilter = Filter.With<BossPendingSpawnMarker>().Build();
			vikingsAliveFilter = Filter.With<VikingsBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			vikingsPendingFilter = Filter.With<VikingsPendingSpawnComponent>().Build();
			hydraAliveFilter = Filter.With<HydraBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
			hydraPendingFilter = Filter.With<HydraPendingSpawnComponent>().Build();
			hitEvent = World.GetEvent<WeaponHitEvent>();
			playerDeathEvent = World.GetEvent<PlayerDeathEvent>();
		}

		public override void Dispose()
		{
			IsDisposing = true;
			raidUiProcessor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{
			raidUiProcessor.Process();
			if (!playerModel.InBossTowerArena)
				return;
			ProcessPlayerAttacks();
			UpdateRaidTimers(deltaTime);
			CheckRaidConditions();
			CheckPlayerDeath();
			EnforceArenaBounds();
		}

		private RaidUiStateMarker OnRaidStarted(Entity entity)
		{
			uiModel.IsMinimapVisible = false;
			return default;
		}

		private void OnRaidEnded(ref RaidUiStateMarker state)
		{
			if (!IsDisposing)
				uiModel.IsMinimapVisible = true;
		}

		private void ProcessPlayerAttacks()
		{
			foreach (var pair in hitEvent.IterateHitsOn(bossTagStash))
			{
				foreach (Entity player in activeRaidFilter)
				{
					if (fightStateStash.Has(player))
						continue;
					ref var raid = ref raidStash.Get(player);
					if (bossTagStash.Get(pair.Item2).BossFightKey == raid.BossKey)
						StartBossBattle(player);
					foreach (Entity unit in arenaUnitsFilter)
						unitWaitAttack.Remove(unit);
				}
			}
		}

		private void StartBossBattle(Entity playerEntity)
		{
			audioPlayer.PlaySoundOnce("chains_break");
			foreach (Entity entity in bossVisualFilter)
			{
				GameObject visual = visualStash.Get(entity).Visual;
				if (visual != null && visual.activeInHierarchy)
					visual.SetActive(false);
				if (vampireBossStash.Has(entity))
					foreach (Entity vampire in World.Filter.With<VampireBossFightVisualComponent>().Build())
					{
						if (!vampireBossFightVisualStash.Has(vampire))
							continue;
						GameObject center = vampireBossFightVisualStash.Get(vampire).CenterVisual;
						center.SetActive(true);
						center.transform.localScale = Vector3.zero;
						center.transform.DOScale(Vector3.one, 1f).SetEase((Ease)7);
					}
			}

			foreach (Entity entity in arenaUnitsFilter)
			{
				var unit = unitStash.Get(entity).Core;
				if (unit == null)
					continue;
				Transform transform = unit.transform;
				var vfx = worldVfxService.Spawn(bossTowerConfig.BossSpawnVfxPrefab, transform.position + Vector3.up * 0.25f, Quaternion.identity, 2f);
				if (vfx != null)
					vfx.transform.localScale = transform.lossyScale;
			}

			fightStateStash.Set(playerEntity);
			bossTowerModel.IsFighting = true;
			bossTowerModel.SetFightMusic();
			BossHudView hud = GameContext.Get<BossHudView>();
			if (hud != null)
				hud.ForceToShow();
		}

		private void UpdateRaidTimers(float deltaTime)
		{
			foreach (Entity entity in fightingRaidFilter)
			{
				ref var raid = ref raidStash.Get(entity);
				if (raid.FinishPending || arenaUnitsFilter.IsEmpty())
					continue;
				if (!cheatsModel.StopBossTowerTimer)
					raid.RemainingTime -= deltaTime;
			}
		}

		private void CheckRaidConditions()
		{
			float delta = Time.deltaTime;
			foreach (Entity entity in activeRaidFilter)
			{
				ref var raid = ref raidStash.Get(entity);
				if (playerModel.Unit == null)
					continue;
				if (raid.FinishPending)
				{
					raid.FinishDelay -= delta;
					if (raid.FinishDelay <= 0f && !raid.FinishFlowStarted)
						FinishBossFight(ref raid);
					continue;
				}

				bool noUnits = arenaUnitsFilter.IsEmpty() && pendingSpawnFilter.IsEmpty();
				bool fighting = fightStateStash.Has(entity);
				if (!noUnits)
				{
					raid.DelayToMakeSureBossIsDead = 0.1f;
					if (fighting && raid.RemainingTime <= 0f)
						KillPlayerByTime();
					continue;
				}

				raid.FinishPending = true;
				raid.FinishDelay = 2.5f;
				raid.FinishFightResult = true;
				raid.DelayToMakeSureBossIsDead -= delta;
				uiModel.BossHudVisible = false;
			}
		}

		public void ForcePlayerLeaveArena()
		{
			if (!playerModel.InBossTowerArena || playerModel.Unit == null || playerModel.Unit.IsDead)
				return;
			Entity entity = playerModel.Unit.GetComponent<EntityProvider>().Entity;
			if (!raidStash.Has(entity) || raidStash.Get(entity).FinishPending)
				return;
			KillPlayerByTime();
		}

		private void KillPlayerByTime()
		{
			Entity entity = playerModel.Unit.GetComponent<EntityProvider>().Entity;
			if (!raidStash.Has(entity) || playerModel.Unit.IsDead || raidStash.Get(entity).FinishPending)
				return;
			foreach (Entity boss in bossesFilter)
			{
				var unit = unitStash.Get(boss).Core as EnemyUnit;
				if (unit == null)
					continue;
				playerModel.Unit.CurrentHealth = 0;
				unitDamageSystem.ProcessPlayerDeath(unit.name, playerModel.Unit);
				return;
			}
		}

		private static void FinishBossFight(ref BossRaidComponent raid)
		{
			App.InitiateFlow(new EndBossFightFlow { BossKey = raid.BossKey, Victory = raid.FinishFightResult }).Forget();
			raid.FinishFlowStarted = true;
		}

		private void CheckPlayerDeath()
		{
			foreach (var death in playerDeathEvent.BatchedChanges)
				foreach (Entity entity in activeRaidFilter)
				{
					if (!playerModel.InBossTowerArena)
						continue;
					ref var raid = ref raidStash.Get(entity);
					if (raid.FinishPending)
						continue;
					raid.FinishPending = true;
					raid.FinishDelay = 2.5f;
					raid.FinishFightResult = false;
					CacheCurrentBossHealth();
				}
		}

		public void TeleportPlayerToTowerEntrance()
		{
			var unit = playerModel.Unit;
			var portal = GameContext.Get<Codebase.Gates.Portal>();
			unit.transform.position = portal.playerSpawnPosition.position;
			if (unit.Agent != null)
			{
				unit.Agent.Warp(portal.playerSpawnPosition.position);
				unit.Agent.ResetPath();
			}
		}

		public void EndRaid(bool victory)
		{
			if (playerModel.Unit != null)
			{
				var entity = playerModel.Unit.GetComponent<Scellecs.Morpeh.Providers.EntityProvider>().Entity;
				if (raidStash.Has(entity))
				{
					ref var raid = ref raidStash.Get(entity);
					GameContext.Get<BossSpawnService>().DespawnArena();
					fightStateStash.Remove(entity);
					raidStash.Remove(entity);
				}
			}

			bossTowerModel.ClearBossMusic();
			GameContext.Get<VikingsDeathSystem>().OnRaidEnded();
			GameContext.Get<VikingsGhostSystem>().OnRaidEnded();
			GameContext.Get<HydraDeathSystem>().OnRaidEnded();
			GameContext.Get<HydraPendingSpawnSystem>().OnRaidEnded();
			GameContext.Get<LichSkeletonSystem>().OnRaidEnded();
			GameContext.Get<VampireSpiritSystem>().OnRaidEnded();
			GameContext.Get<LavaCrabExplosionSystem>().OnRaidEnded();
			GameContext.Get<BerserkerBossSystem>().OnRaidEnded();
			GameContext.Get<EnemyCastSystem>().OnRaidEnded();
		}

		public (double, double) GetCurrentBossHealth()
		{
			return cachedCurrentBossHealth ?? (0.0, 0.0);
		}

		public void CacheCurrentBossHealth()
		{
			cachedCurrentBossHealth = CalculateCurrentBossHealth();
		}

		public (double, double) CalculateCurrentBossHealth()
		{
			if (!vikingsAliveFilter.IsEmpty() || !vikingsPendingFilter.IsEmpty())
				return CalculateVikingsHealth();
			if (!hydraAliveFilter.IsEmpty() || !hydraPendingFilter.IsEmpty())
				return CalculateHydraHealth();
			double current = 0.0, maximum = 0.0;
			foreach (Entity entity in bossesFilter)
			{
				var unit = unitStash.Get(entity).Core as EnemyUnit;
				if (unit == null)
					continue;
				double hp = unit.GetStatValue(Codebase.Unit.StatType.Health, true);
				if (current < unit.CurrentHealth)
				{
					current = unit.CurrentHealth;
					maximum = hp;
				}
			}

			return (current, maximum);
		}

		private (double, double) CalculateVikingsHealth()
		{
			double current = 0.0, baseHealth = 0.0;
			int livingNormals = 0, pending = 0;
			bool anyGhost = false;
			string bossKey = null;
			foreach (Entity entity in vikingsAliveFilter)
			{
				EnemyUnit unit = unitStash.Get(entity).Core as EnemyUnit;
				VikingsBossComponent data = vikingsStash.Get(entity);
				if (unit == null)
					continue;
				bossKey = data.BossKey;
				current += unit.CurrentHealth;
				if (!data.IsGhost)
					livingNormals++;
				anyGhost |= data.IsGhost;
				if (baseHealth == 0.0)
					baseHealth = unit.GetStatValue(Codebase.Unit.StatType.Health, true);
			}

			foreach (Entity entity in vikingsPendingFilter)
			{
				VikingsPendingSpawnComponent data = vikingsPendingStash.Get(entity);
				double health = data.DeadViking.GetStatValue(Codebase.Unit.StatType.Health, true);
				current += health;
				pending++;
				if (baseHealth == 0.0)
					baseHealth = health;
			}

			if (bossKey == null || baseHealth == 0.0)
				return (0.0, 0.0);
			BossModel model = bossTowerModel.GetBoss(bossKey);
			bool ordinary = model == null || !model.IsSuperBoss;
			int count = 1;
			BossDef definition = bossTowerConfig.Get(bossKey);
			if (definition != null)
				foreach (BossBehaviorConfig behavior in definition.Behaviors)
					if (behavior is VikingsBossConfig vikings)
					{
						count = vikings.AdditionalVikings.Count + 1;
						break;
					}

			double future = ordinary ? (!anyGhost && pending < 1 ? 1.0 : 0.0) : livingNormals;
			double maximum = baseHealth * (count + (ordinary ? 1 : count));
			return (Math.Min(current + baseHealth * future, maximum), maximum);
		}

		private (double, double) CalculateHydraHealth()
		{
			// Native ELF VA 0x1565964; Health predicates 0x156615c/178. Future Hydra health uses original parent base HP.
			double current = 0.0, baseHealth = 0.0;
			int future = 0, spawnCount = 0, maximumSplits = 0;
			foreach (Entity entity in hydraAliveFilter)
			{
				var unit = unitStash.Get(entity).Core as EnemyUnit;
				if (unit == null)
					continue;
				HydraBossComponent hydra = hydraStash.Get(entity);
				if (baseHealth == 0.0)
				{
					Stat health = hydra.ParentConfig?.Stats?.FirstOrDefault(stat => stat.Type == Codebase.Unit.StatType.Health);
					if (health != null)
						baseHealth = health.BaseValue;
					spawnCount = hydra.SpawnCount;
					maximumSplits = hydra.MaxSplits;
				}

				current += unit.CurrentHealth;
				future = unchecked(future + CountFutureHydras(hydra.RemainingSpawns, hydra.SpawnCount));
			}

			foreach (Entity entity in hydraPendingFilter)
			{
				HydraPendingSpawnComponent pending = hydraPendingStash.Get(entity);
				HydraBossComponent hydra = pending.HydraData;
				if (baseHealth == 0.0)
				{
					Stat health = hydra.ParentConfig?.Stats?.FirstOrDefault(stat => stat.Type == Codebase.Unit.StatType.Health);
					if (health != null)
						baseHealth = health.BaseValue;
					spawnCount = hydra.SpawnCount;
					maximumSplits = hydra.MaxSplits;
				}

				current += baseHealth;
				future = unchecked(future + CountFutureHydras(hydra.RemainingSpawns, hydra.SpawnCount));
			}

			if (baseHealth == 0.0)
				return (0.0, 0.0);
			double maximum = baseHealth * unchecked(CountFutureHydras(maximumSplits, spawnCount) + 1);
			return (Math.Min(current + baseHealth * future, maximum), maximum);
		}

		private static int CountFutureHydras(int remainingSpawns, int spawnCount)
		{
			return remainingSpawns > 0 ? unchecked(spawnCount + spawnCount * CountFutureHydras(remainingSpawns - 1, spawnCount)) : 0;
		}

		public BossModel GetCurrentBossModel()
		{
			if (playerModel.Unit == null)
				return null;
			Entity entity = playerModel.Unit.GetComponent<EntityProvider>().Entity;
			if (!raidStash.Has(entity))
				return null;
			return bossTowerModel.GetBoss(raidStash.Get(entity).BossKey);
		}

		private void EnforceArenaBounds()
		{
			if (activeRaidFilter.IsEmpty() || !bossArenaSystem.TryGetActive(out BossArenaComponent arena))
				return;
			var unit = playerModel.Unit;
			if (unit == null)
				return;
			if (!arena.ArenaBounds.Contains(unit.transform.position))
			{
				bool hasAgent = unit.Agent != null;
			}
		}
	}
}
