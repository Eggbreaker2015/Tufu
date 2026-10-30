using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.ABTests;
using Codebase.Abilities.Systems;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Crystals;
using Codebase.Crystals.Objective;
using Codebase.Events.Models;
using Codebase.Meta;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Player.Systems;
using Codebase.Services.Time;
using Codebase.Simulation;
using Codebase.Simulation.Systems;
using Codebase.UI;
using Codebase.Unit;
using Codebase.Utilities.ObjectPool;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UniMob;
using UnityEngine;
using System.Linq;
using Codebase.Core.UserData;
using UnityEngine.AI;
using Scellecs.Morpeh;
using Codebase.Unit.Components;
using Codebase.Enemy.Components;
using Codebase.Events;
using Codebase.Events.Systems;
using Codebase.DebugTools.Dummy;
using Codebase.Map.Components;
using Codebase.Enemy.Commands;
using Codebase.Crystals.Commands;
using Codebase.Abilities;
using Codebase.Abilities.Events;
using Codebase.UI.Flows;

namespace Codebase.Enemy
{
	public class EnemySpawner : LifetimeMonoBehaviour
	{
		private enum SpawnMode
		{
			Restore = 0,
			Normal = 1,
			Forced = 2
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003COpenUnlockPopupDelayed_003Ed__200 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			private UniTask.Awaiter _003C_003Eu__1;
			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		private const float SpawnVfxNearPlayerDistanceSq = 3600f;
		private const float MinAbCooldownSeconds = 10f;
		private const int DormantProcessTime = 2147483647;
		private static bool rpmCalculatedOnLoad;
		[SerializeField]
		private EnemySpawnerConfig config;
		[SerializeField]
		private SpawnerView view;
		[SerializeField]
		private ParticleSystem spawnVfx;
		[Inject]
		private AbilityExecuteEventSystem abilityExecuteEventSystem;
		[Inject]
		private PlayerRewardSystem playerRewardSystem;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private UserModel userModel;
		[Inject]
		private ABTestService abTestService;
		[Inject]
		private EventsRepository eventsRepository;
		[Inject]
		private ITimeService timeService;
		[Inject]
		private IAudioPlayer audioPlayer;
		[Inject]
		private SimulationSystem simulationSystem;
		[Inject]
		private SimulationConfig simulationConfig;
		[Inject]
		private IconStyleConfig iconStyleConfig;
		[Inject]
		private ItemsProvider itemsProvider;
		[Inject]
		private MetaStatsModel metaStatsModel;
		[Inject]
		private CrystalModel crystalModel;
		private bool hasBeenClearedOnceCache;
		private bool wasAnyoneAlivePreviousFrame;
		private List<EnemyUnit> units = new List<EnemyUnit>();
		private ObjectPool<EnemyUnit> unitPool = new ObjectPool<EnemyUnit>();
		private ObjectPool<ParticleSystem> vfxPool = new ObjectPool<ParticleSystem>();
		private SpawnersUd spawnersUd;
		private string spawnerKey;
		private SpawnerDataUd spawnerData;
		private bool spawnerDataPersisted;
		public static Action<EnemyUnit> OnDied;
		private bool madeFirstSpawn;
		private bool isInitialized;
		private bool restoredEventSavedUnits;
		private bool awaitingMiniBossDevour;
		private int nextProcessTime;
		private int cachedEffectiveCharges;
		private int cachedEffectiveChargesFrame = -1;
		private int chargeUpgradeSourceVersion = -1;
		private bool usesSpawnerCooldownsAb;
		private MetaStat itemChargesStat;
		public string GetFullName
		{
			get
			{
				return string.Format("{0}[{1}]", name, config.LocationNumber);
			}
		}

		public float Cooldown { get; set; }

		public bool AnyoneAlive
		{
			get
			{
				return units.Count > 0;
			}
		}

		public bool IsFull
		{
			get
			{
				return units.Count >= config.Count;
			}
		}

		public bool HasBeenClearedOnce => this.hasBeenClearedOnceCache;

		public float BaseCooldown
		{
			get
			{
				return GetBaseCooldown();
			}
		}

		public ItemConfig ItemReward { get; private set; }
		public StatModifier StatReward { get; private set; }
		public Sprite MinimapSprite { get; private set; }
		public Color MinimapColor { get; private set; }

		public EnemySpawnerConfig Config
		{
			get
			{
				return this.config;
			}

			set
			{
				config = value;
			}
		}

		public ParticleSystem SpawnVfx => this.spawnVfx;
		public bool HasItemReward { get; private set; }
		public bool HasStatReward { get; private set; }
		public bool IsEnabled { get; private set; } = true;
		public bool IsBootstrapComplete => this.isInitialized;

		public IReadOnlyList<EnemyUnit> Units
		{
			get
			{
				return units;
			}
		}

		public int MinimapVersion { get; set; }

		private int LastSpawnTime
		{
			get
			{
				return spawnerData.LastSpawnTime.Value;
			}

			set
			{
				spawnerData.LastSpawnTime.Value = value;
			}
		}

		private bool ClearedOnce
		{
			get
			{
				return spawnerData != null && spawnerData.ClearedOnce.Value > 0;
			}

			set
			{
				if (value)
					EnsureSpawnerDataPersisted();
				spawnerData.ClearedOnce.Value = value ? 1 : 0;
			}
		}

		private int ChargesSyncPeriod
		{
			get
			{
				return spawnerData.ChargesSyncPeriod.Value;
			}

			set
			{
				spawnerData.ChargesSyncPeriod.Value = value;
			}
		}

		private int KnownMaxCharges
		{
			get
			{
				return spawnerData.KnownMaxCharges.Value;
			}

			set
			{
				spawnerData.KnownMaxCharges.Value = value;
			}
		}

		private int SavedCount
		{
			get
			{
				return spawnerData.SavedCount.Value;
			}

			set
			{
				spawnerData.SavedCount.Value = value;
			}
		}

		private int RemainingCharges
		{
			get
			{
				return spawnerData.RemainingCharges.Value;
			}

			set
			{
				if (IsChargesEnabled)
					spawnerData.RemainingCharges.Value = value;
			}
		}

		private int NextBigSpawnTime
		{
			get
			{
				return spawnerData.NextBigSpawnTime.Value;
			}

			set
			{
				spawnerData.NextBigSpawnTime.Value = value;
			}
		}

		private bool IsChargesEnabled
		{
			get
			{
				return !config.IsEvent && GetEffectiveChargesBeforeBigRespawn() > 0;
			}
		}

		public int ChargesLeft
		{
			get
			{
				return IsChargesEnabled ? RemainingCharges : 0;
			}
		}

		public int ChargesTotal
		{
			get
			{
				return IsChargesEnabled ? GetEffectiveChargesBeforeBigRespawn() : 0;
			}
		}

		public int ChargesBase
		{
			get
			{
				return IsChargesEnabled ? GetBaseChargesBeforeBigRespawn() : 0;
			}
		}

		private int EffectiveChargesBeforeBigRespawn
		{
			get
			{
				return GetEffectiveChargesBeforeBigRespawn();
			}
		}

		public bool IsBigCooldownActive
		{
			get
			{
				return IsChargesEnabled && NextBigSpawnTime > 0;
			}
		}

		public float BigCooldownSeconds
		{
			get
			{
				return config.BigCooldown > 0f ? config.BigCooldown : config.Cooldown;
			}
		}

		public EnemyUnit FirstUnit
		{
			get
			{
				return units.First();
			}
		}

		public int CurrentDevourRewardMultiplier { get; private set; } = 1;

		private bool IsEventMiniBossSpawner
		{
			get
			{
				return config.IsEvent && config.EventDef != null && config.EventDef.UnitType == EventUnitType.MiniBoss;
			}
		}

		private bool IsRosterMiniBossSpawner
		{
			get
			{
				return IsEventMiniBossSpawner && config.EventDef.Key == "crystal_event_roster";
			}
		}

		public int CurrentCount
		{
			get
			{
				return units.Count;
			}
		}

		public int MaxCount
		{
			get
			{
				return config.Count;
			}
		}

		public bool CanSpawn
		{
			get
			{
				return CurrentCount < config.Count;
			}
		}

		private int NowUnixSeconds
		{
			get
			{
				return (int)new DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds();
			}
		}

		public void SetEnabled(bool value)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled && (config.IsEvent || config.Prefab is CrystalUnit)) { IsEnabled = false; return; }
			IsEnabled = value;
		}

		public void ConsumeAllCharges()
		{
		}

		public void DefeatAllUnits()
		{
		}

		public void ResetEventSpawnData()
		{
            LastSpawnTime = 0;
            SavedCount = 0;
            madeFirstSpawn = false;
            restoredEventSavedUnits = false;
            awaitingMiniBossDevour = false;
            nextProcessTime = 0;
            Cooldown = 0f;
        }

		public void ResetForRespawn()
		{
			ClearedOnce = false;
			hasBeenClearedOnceCache = false;
			madeFirstSpawn = false;
			SavedCount = 0;
			LastSpawnTime = 0;
			nextProcessTime = 0;
			Cooldown = 0f;
			NextBigSpawnTime = 0;
			enabled = true;
			IsEnabled = true;
		}

		public bool RespawnCrystalObjective()
		{
			if (AnyoneAlive)
				return false;
			ClearedOnce = false;
			hasBeenClearedOnceCache = false;
			enabled = true;
			IsEnabled = true;
			Cooldown = 0f;
			nextProcessTime = 0;
			if (!TrySpawnIgnoringMaxCount())
				return false;
			GameContext.Get<CrystalUnitScene>().ResetAliveState();
			return true;
		}

		private void Awake()
		{
			spawnersUd = GameContext.Get<GameDataService>().Data.Spawners;
			spawnerKey = SpawnerKey.ToUdKey(config.Key);
			spawnersUd.Spawners.TryGetValue(spawnerKey, out spawnerData);
			if (config.IsEvent)
				hasBeenClearedOnceCache = false;
			else
			{
				hasBeenClearedOnceCache = ClearedOnce;
				if (hasBeenClearedOnceCache && config.DisableAfterFirstClear && !ShouldKeepCrystalObjectiveSpawnerEnabled())
				{
					enabled = false;
					IsEnabled = false;
				}
			}

			ItemReward = config.ItemReward;
			StatReward = config.StatRewards.FirstOrDefault();
			HasItemReward = ItemReward != null;
			HasStatReward = StatReward != null;
		}

		private void CacheMinimapSprite()
		{
			if (ItemReward != null)
			{
				var styles = ItemReward.SlotType == 0 ? iconStyleConfig.WeaponTypeMinimizedStyles : iconStyleConfig.ArmorTypeMinimizedStyles;
				MinimapSprite = styles.First(style => style.WeaponType == ItemReward.DamageType).Sprite;
				MinimapColor = itemsProvider.GetRarityColor(ItemReward.Rarity);
			}
			else if (StatReward != null)
			{
				MinimapSprite = iconStyleConfig.MinimapStyle.Enemy;
				MinimapColor = Color.white;
			}
		}

		protected override void Start()
		{
			base.Start();
			CacheMinimapSprite();
			InitializeChargeUpgradeSource();
			GameContext.Get<EnemiesModel>().Spawners.Add(this);
			if (spawnerData == null)
				spawnerData = spawnersUd.Spawners.CreateDetached(spawnerKey);
			else
				spawnerDataPersisted = true;
			if (global::Codebase.Rpg.RpgModeConfig.Enabled && (config.IsEvent || config.Prefab is CrystalUnit)) { enabled = false; IsEnabled = false; isInitialized = true; return; }
			InitializePersistentState(NowUnixSeconds);
			if (config.Prefab is CrystalUnit)
			{
				MigrateCrystalObjectiveWaitingState();
				ApplyCrystalObjectiveSpawnerState();
				RestoreRespawnedCrystalIfNeeded();
				crystalModel.TryCompleteRespawn();
				GameContext.Get<CrystalUnitScene>().SyncObjectiveVisualState();
			}

			if (!config.IsEvent)
			{
				if (enabled)
					GameContext.Get<EnemiesModel>().PendingBootstrap.Add(this);
				else
					isInitialized = true;
			}
			else if (config.EventDef != null && config.EventDef.UnitType == 0 && enabled)
				GameContext.Get<EnemiesModel>().PendingEventMobBootstrap.Add(this);
			else
				isInitialized = true;
		}

		internal void CompleteBootstrap(int now)
		{
			if (isInitialized)
				return;
			InitializeSpawnerNow(now);
		}

		private void EnsureSpawnerDataPersisted()
		{
			if (spawnerDataPersisted)
				return;
			spawnersUd.Spawners.Attach(spawnerData);
			spawnerDataPersisted = true;
		}

		private void InitializePersistentState(int now)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled && NextBigSpawnTime > 0) NextBigSpawnTime = 0;
			if (IsChargesEnabled && spawnerData.RemainingCharges.Value < 0)
				RemainingCharges = GetEffectiveChargesBeforeBigRespawn() - SavedCount;
			if (IsChargesEnabled && KnownMaxCharges < 0)
				KnownMaxCharges = GetEffectiveChargesBeforeBigRespawn();
		}

		private void ApplyChargeUpgradeIfIncreased(int sourceVersion)
		{
			chargeUpgradeSourceVersion = sourceVersion;
			cachedEffectiveChargesFrame = -1;
			int total = GetEffectiveChargesBeforeBigRespawn();
			if (total <= 0)
				return;
			int known = KnownMaxCharges;
			if (total > known)
			{
				RemainingCharges = Math.Min(total, RemainingCharges + total - known);
				nextProcessTime = 0;
			}

			if (total != known)
				KnownMaxCharges = total;
		}

		private void InitializeChargeUpgradeSource()
		{
			usesSpawnerCooldownsAb = CanUseSpawnerCooldownsAb();
			if (config.ItemReward != null)
				itemChargesStat = metaStatsModel.Get("items_rarity_charges");
		}

		private void InitializeSpawnerNow(int now)
		{
			if (!config.IsEvent)
				RestoreSavedUnits();
			if (config.Prefab is CrystalUnit)
				GameContext.Get<CrystalUnitScene>().SyncObjectiveVisualState();
			ProcessSpawnTick(now);
			isInitialized = true;
		}

		public SpawnerView SetupView()
		{
			// Original ARM64 ELF VA 0x1503b50.
			if (!config.ShowCooldown) return null;
			var canvas = GameContext.Get<Codebase.UI.World.DynamicWorldCanvas>();
			var instance = canvas.CreateNew(view);
			canvas.AddTarget(transform, instance.transform as RectTransform);
			instance.Initialize(this);
			return instance;
		}

		public void MarkAllUnitsForEventDespawn()
		{
            foreach (EnemyUnit unit in new List<EnemyUnit>(units))
            {
                unit.CurrentHealth = 0;
                var entity = unit.EntityProvider.Entity;
                World.Default.GetStash<Codebase.Unit.Components.UnitDeadMarker>().Set(entity);
                World.Default.GetStash<Codebase.Enemy.Components.EventDespawnRequest>().Set(entity,
                    new Codebase.Enemy.Components.EventDespawnRequest { Spawner = this });
            }
        }

		public void ProcessEventDespawn(EnemyUnit unit)
		{
            unit.OnDied -= HandleDeath;
            unit.OnDevoured -= HandleDevour;
            units.Remove(unit);
            nextProcessTime = 0;
            GameContext.Get<EnemiesModel>().RemoveEnemy(unit);
            unitPool.Despawn(unit);
            if (config.IsEvent && config.EventDef != null && config.EventDef.UnitType == EventUnitType.MiniBoss && units.Count == 0)
                awaitingMiniBossDevour = false;
            SyncMinimapAliveState();
        }

		public bool TrySpawn()
		{
			if (!CanSpawn || !TrySpawnUnit(SpawnMode.Forced))
				return false;
			LastSpawnTime = NowUnixSeconds;
			return true;
		}

		public bool TrySpawnIgnoringMaxCount()
		{
			bool spawned = TrySpawnUnit(SpawnMode.Normal);
			if (spawned)
				LastSpawnTime = NowUnixSeconds;
			return spawned;
		}

		public void ProcessSpawn(int nowUnixSeconds, int spawnerCooldownsVersion, int itemChargesVersion)
		{
			if (!isInitialized)
				return;
			if (!config.IsEvent)
			{
				int version = usesSpawnerCooldownsAb ? spawnerCooldownsVersion : itemChargesStat != null ? itemChargesVersion : 0;
				if (version != chargeUpgradeSourceVersion)
					ApplyChargeUpgradeIfIncreased(version);
			}

			if (nextProcessTime == DormantProcessTime)
				return;
			if (nextProcessTime > nowUnixSeconds)
			{
				float remaining = nextProcessTime - nowUnixSeconds;
				if (Cooldown == remaining)
					return;
				Cooldown = remaining;
				SyncMinimapCooldown();
				return;
			}

			ProcessSpawnTick(nowUnixSeconds);
		}

		public void ApplyTimeSkip(float seconds)
		{
		}

		private int GetSmallCooldownSeconds()
		{
			return (int)Math.Ceiling(GetBaseCooldown());
		}

		private float GetBaseCooldown()
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return global::Codebase.Rpg.RpgSpawnerRules.GetCooldownSeconds(config);
			float seconds = CanUseSpawnerCooldownsAb() ? abTestService.GetSpawnerCooldownSeconds(config.IsEliteBoss, config.LocationNumber) : config.Cooldown;
			return SpawnerTimerCalculator.Apply(seconds, config, metaStatsModel);
		}

		private int GetEffectiveChargesBeforeBigRespawn()
		{
			int frame = Time.frameCount;
			if (frame == cachedEffectiveChargesFrame)
				return cachedEffectiveCharges;
			cachedEffectiveCharges = ComputeEffectiveChargesBeforeBigRespawn();
			cachedEffectiveChargesFrame = frame;
			return cachedEffectiveCharges;
		}

		private int ComputeEffectiveChargesBeforeBigRespawn()
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return 0;
			return CanUseSpawnerCooldownsAb() ? abTestService.GetSpawnerCharges(config.IsEliteBoss, config.LocationNumber, config.Count) : SpawnerChargesCalculator.GetTotalCharges(config, metaStatsModel);
		}

		private int GetBaseChargesBeforeBigRespawn()
		{
			return CanUseSpawnerCooldownsAb() ? abTestService.GetSpawnerCharges(config.IsEliteBoss, config.LocationNumber, config.Count) : config.ChargesBeforeBigRespawn;
		}

		private bool CanUseSpawnerCooldownsAb()
		{
			return !config.IsEvent && !config.IsVip && config.ItemReward == null && config.Cooldown >= 10f;
		}

		public int ConsumeZeroCooldownStatRewardMultiplier(EnemyUnit unit)
		{
			if (GetSmallCooldownSeconds() == 0 && IsChargesEnabled && unit.ItemReward == null)
			{
				int remaining = RemainingCharges;
				RemainingCharges = 0;
				return Math.Max(1, remaining + 1);
			}

			return 1;
		}

		private void RestoreSavedUnits()
		{
			if (config.Prefab is CrystalUnit && crystalModel.IsUnlocked && crystalModel.IsWaitingForRespawn)
				return;
			int missing = SavedCount - units.Count;
			for (int i = 0; i < missing; i++)
				if (!TrySpawnUnit(SpawnMode.Restore))
					return;
		}

		private void ProcessSpawnTick(int now)
		{
			if (config.IsEvent && !TryInitializeEventSpawner(now))
			{
				Cooldown = 0f;
				return;
			}

			if ((IsRosterMiniBossSpawner && units.Count == 0 && eventsRepository.CurrentEvent != null && eventsRepository.CurrentEvent.Progress > 0) || (IsEventMiniBossSpawner && awaitingMiniBossDevour))
			{
				Cooldown = 0f;
				nextProcessTime = 0;
				return;
			}

			SyncMinimapAliveState();
			if (config.Prefab is CrystalUnit && crystalModel.IsUnlocked)
			{
				if (crystalModel.IsObjectiveRespawnReady)
					crystalModel.TryCompleteRespawn();
				Cooldown = 0f;
				nextProcessTime = 0;
				return;
			}

			if (!config.IsEvent && !(config.Prefab is CrystalUnit) && AnyoneAlive && IsFull && CanSkipChargeProcessingForFullSpawner())
			{
				ScheduleFullSpawnerProcessing(now);
				return;
			}

			if (!AnyoneAlive && madeFirstSpawn && config.DisableAfterFirstClear && !(config.Prefab is CrystalUnit))
			{
				Cooldown = 0f;
				LastSpawnTime = 0;
				NextBigSpawnTime = 0;
				enabled = false;
				IsEnabled = false;
				return;
			}

			if (TryProcessChargesManager(now))
				return;
			if (units.Count < config.Count)
			{
				int small = GetSmallCooldownSeconds();
				int missing = config.Count - units.Count;
				int charges = IsChargesEnabled ? RemainingCharges : int.MaxValue;
				if (small <= 0)
				{
					int count = Math.Min(missing, charges), spawned = 0;
					for (int i = 0; i < count; i++)
					{
						if (!TrySpawnUnit(SpawnMode.Normal))
							break;
						spawned++;
					}

					if (spawned > 0)
						LastSpawnTime = now;
					Cooldown = 0f;
					return;
				}

				int spawnCount = Math.Min((now - LastSpawnTime) / small, Math.Min(missing, charges));
				if (spawnCount <= 0)
					Cooldown = small - (now - LastSpawnTime);
				else
				{
					int spawned = 0;
					for (int i = 0; i < spawnCount; i++)
					{
						if (!TrySpawnUnit(SpawnMode.Normal))
							break;
						spawned++;
					}

					if (spawned > 0)
						LastSpawnTime = now;
					if (units.Count >= config.Count)
					{
						if (config.IsEvent || config.Prefab is CrystalUnit)
						{
							Cooldown = 0f;
							nextProcessTime = 0;
						}
						else
							ScheduleFullSpawnerProcessing(now);
						return;
					}

					if (IsChargesEnabled && RemainingCharges <= 0)
					{
						Cooldown = 0f;
						if (!config.IsEvent && !(config.Prefab is CrystalUnit) && AnyoneAlive)
							ScheduleChargesDepletedProcessing(now);
						return;
					}

					Cooldown = small - (now - LastSpawnTime);
				}

				UpdateNextProcessTime(now);
				SyncMinimapCooldown();
				return;
			}

			if (config.IsEvent || config.Prefab is CrystalUnit)
			{
				Cooldown = 0f;
				nextProcessTime = 0;
			}
			else
				ScheduleFullSpawnerProcessing(now);
		}

		private bool TryInitializeEventSpawner(int now)
		{
			return false;
		}

		private bool TryProcessChargesManager(int now)
		{
			if (!IsChargesEnabled)
				return false;
			RestoreChargesIfReady(now);
			if (IsInBigCooldown(now))
			{
				nextProcessTime = NextBigSpawnTime;
				return true;
			}

			if (RemainingCharges > 0)
				return false;
			TryStartBigCooldown(now);
			if (!config.IsEvent && !(config.Prefab is CrystalUnit) && AnyoneAlive)
				ScheduleChargesDepletedProcessing(now);
			return true;
		}

		private bool CanSkipChargeProcessingForFullSpawner()
		{
			return !IsChargesEnabled || (!config.SyncBigRespawnToUtc && NextBigSpawnTime < 1 && RemainingCharges > 0);
		}

		private void ScheduleFullSpawnerProcessing(int now)
		{
			Cooldown = 0f;
			if (config.SyncBigRespawnToUtc)
			{
				int period = (int)Math.Ceiling(BigCooldownSeconds);
				if (period > 0)
				{
					nextProcessTime = GetNextUtcAlignedTime(now, period);
					return;
				}
			}

			nextProcessTime = DormantProcessTime;
		}

		private void ScheduleChargesDepletedProcessing(int now)
		{
			if (config.SyncBigRespawnToUtc)
			{
				int period = (int)Math.Ceiling(BigCooldownSeconds);
				if (period > 0)
				{
					nextProcessTime = GetNextUtcAlignedTime(now, period);
					return;
				}
			}

			nextProcessTime = DormantProcessTime;
		}

		private bool TrySpawnUnit(SpawnMode mode)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled && (config.IsEvent || config.Prefab is CrystalUnit)) return false;
			if (config.IsDummy)
				return false;
			if (config.IsVip && playerModel.LastUnlockedLocation.Number < 2 && !global::Codebase.Rpg.RpgModeConfig.Enabled)
				return false;
			if (mode != SpawnMode.Restore && IsChargesEnabled)
			{
				int now = NowUnixSeconds;
				RestoreChargesIfReady(now);
				if (IsInBigCooldown(now) || RemainingCharges <= 0)
					return false;
			}

			if (config.IsEvent && mode != SpawnMode.Restore && config.EventDef != null)
			{
				if (!eventsRepository.HasEventPlan)
					return false;
				var activeEvent = eventsRepository.CurrentEvent;
				if (activeEvent == null || activeEvent.Key != config.EventDef.Key)
					return false;
				if (config.EventDef.UnitType == EventUnitType.MiniBoss)
				{
					int offset = activeEvent.Key == "crystal_event_roster" ? 0 : 1;
					if (activeEvent.Progress >= activeEvent.ProgressMax - offset)
						return false;
					if (mode == SpawnMode.Normal && GameContext.Get<EventBossGateSystem>().IsMiniBossFlowRunning)
						return false;
				}
			}

			Vector3 position = GetSpawnPosition();
			EnemyUnit prefab = config.Prefab;
			EventSpawnerVariant variant = null;
			if (config.IsEvent && config.EventDef != null)
			{
				variant = config.EventDef.GetRandomVariant();
				if (variant != null && variant.Prefab != null)
					prefab = variant.Prefab;
			}

			if (config.IsEvent && config.EventDef != null)
				ApplyEventPlanToConfig();
#if UNITY_EDITOR
			EnemyUnit unit = unitPool.Spawn(prefab, position, Quaternion.identity, null, RecoveryCompatibility.EditorEnemySpawnTemplates.Resolve);
#else
			EnemyUnit unit = unitPool.Spawn(prefab, position, Quaternion.identity, null);
#endif
			unit.Initialize(Lifetime, config, transform.position);
			float multiplier = abTestService.GetEnemyHpDamageMultiplier(config.LocationNumber);
			if (!config.IsEvent && multiplier != 1f && !global::Codebase.Rpg.RpgModeConfig.Enabled)
			{
				for (int i = 0; i < unit.Stats.Count; i++)
				{
					Stat stat = unit.Stats[i];
					if (stat.Type == StatType.Health || stat.Type == StatType.Damage)
						unit.Stats[i] = new Stat(Lifetime, Math.Round(stat.BaseValue * multiplier), stat.Type);
				}

				unit.BuildStatLookup();
				unit.CurrentHealth = unit.GetStatValue(StatType.Health);
			}

			unit.EventUnitReward = variant;
			var currentEvent = eventsRepository.CurrentEvent;
			if (currentEvent != null && EventKeys.IsCrystal(currentEvent.Key) && EventKeys.IsCrystal(config.EventDef?.Key) && config.EventDef != null && config.EventDef.UnitType == EventUnitType.Mob)
			{
				string currency = variant?.CurrencyKey;
				if (currency == "blue_crystal" || currency == "red_crystal")
				{
					bool blue = currency == "blue_crystal";
					WeaponType weaponType = blue ? currentEvent.Data.WeaponType.Value : currentEvent.Data.ResistType.Value;
					StatType rewardType = blue ? (weaponType == WeaponType.Stabbing ? StatType.StabDefenceFlat : weaponType == (WeaponType)1 ? StatType.CutDefenceFlat : StatType.BuckshotDefenceFlat) : (weaponType == WeaponType.Stabbing ? StatType.StabDamageFlat : weaponType == (WeaponType)1 ? StatType.CutDamageFlat : StatType.BuckshotDamageFlat);
					var instance = SimulationSystem.Instance;
					if (instance != null && instance.RPMCalculatedValue.TryGetValue(rewardType, out double rpm) && rpm > 0.0)
						unit.StatRewards.Add(new StatModifier { Name = "event." + config.EventDef.Key + "." + config.EventDef.UnitType.ToString().ToLowerInvariant(), StatType = rewardType, ModifierType = (ModifierType)0, Source = (ModifierSource)0, Value = (float)Math.Max(1.0, rpm * 0.4), Duration = 0f });
				}
			}

			if (madeFirstSpawn && IsSpawnerNearPlayer())
			{
				ParticleSystem vfx = vfxPool.Spawn(spawnVfx, position, Quaternion.identity, null);
				vfx.Play();
				Destroy(vfx.gameObject, 3f);
			}

			World world = World.Default;
			if (!config.IsStatic)
			{
				if (!unit.Config.IsFlying)
				{
					unit.Agent.enabled = true;
					world.GetStash<NavMeshAgentActiveMarker>().Set(unit.EntityProvider.Entity);
					unit.LastDestination = unit.CurrentSpawnerPatrolPoint;
					unit.Agent.SetDestination(unit.CurrentSpawnerPatrolPoint);
				}

				unit.AggroTimer = 0f;
			}

			units.Add(unit);
			SyncMinimapAliveState();
			GameContext.Get<EnemiesModel>().AddEnemy(unit);
			unit.OnDied += HandleDeath;
			unit.OnDevoured += HandleDevour;
			if (config.IsEvent && config.EventDef != null)
			{
				world.GetStash<DynamicPoiTag>().Set(unit.EntityProvider.Entity);
				if (config.EventDef.UnitType == EventUnitType.MiniBoss)
					world.GetStash<EventMiniBossTag>().Set(unit.EntityProvider.Entity);
			}

			if (config.IsDummy)
				world.GetStash<DummyTargetTag>().Set(unit.EntityProvider.Entity, new DummyTargetTag { Data = new DummyDamageData() });
			if (unit is CrystalUnit)
			{
				world.GetStash<CrystalUnitTag>().Set(unit.EntityProvider.Entity);
				world.GetStash<UnitWithoutRegenMarker>().Set(unit.EntityProvider.Entity);
				world.GetStash<DynamicPoiTag>().Set(unit.EntityProvider.Entity);
				crystalModel.SetupObjectiveUnit((CrystalUnit)unit);
			}

			madeFirstSpawn = true;
			SavedCount = units.Count;
			if (config.IsEvent)
				EnsureSpawnerDataPersisted();
			if (mode == SpawnMode.Normal && IsChargesEnabled)
				RemainingCharges--;
			return true;
		}

		private bool IsInBigCooldown(int now)
		{
			if (!IsChargesEnabled || NextBigSpawnTime <= 0)
				return false;
			Cooldown = NextBigSpawnTime - now;
			SyncMinimapCooldown();
			return Cooldown > 0f;
		}

		private void RestoreChargesIfReady(int now)
		{
			if (config.SyncBigRespawnToUtc)
			{
				int period = (int)Math.Ceiling(BigCooldownSeconds);
				if (period <= 0)
					return;
				int current = now / period;
				int saved = ChargesSyncPeriod;
				if (saved == 0)
					ChargesSyncPeriod = current;
				else if (saved != current)
				{
					ChargesSyncPeriod = current;
					RemainingCharges = GetEffectiveChargesBeforeBigRespawn() - units.Count;
					NextBigSpawnTime = 0;
					Cooldown = 0f;
				}
			}

			if (NextBigSpawnTime > 0 && NextBigSpawnTime <= now)
			{
				NextBigSpawnTime = 0;
				RemainingCharges = GetEffectiveChargesBeforeBigRespawn();
				Cooldown = 0f;
			}
		}

		private void TryStartBigCooldown(int now)
		{
			if (!IsChargesEnabled || RemainingCharges > 0 || units.Count > 0 || NextBigSpawnTime > 0)
				return;
			int period = (int)Math.Ceiling(BigCooldownSeconds);
			if (period <= 0)
				return;
			int next = config.SyncBigRespawnToUtc ? GetNextUtcAlignedTime(now, period) : now + period;
			NextBigSpawnTime = next;
			Cooldown = (float)next - now;
			LastSpawnTime = 0;
			nextProcessTime = next;
		}

		private static int GetNextUtcAlignedTime(int now, int periodSeconds)
		{
			return periodSeconds < 1 ? now : periodSeconds * (now / periodSeconds) + periodSeconds;
		}

		private bool IsSpawnerNearPlayer()
		{
			if (playerModel.Unit != null)
				return (transform.position - playerModel.Unit.transform.position).sqrMagnitude <= SpawnVfxNearPlayerDistanceSq;
			if (playerModel.LastPosition == Vector3.zero)
				return false;
			return (transform.position - playerModel.LastPosition).sqrMagnitude <= SpawnVfxNearPlayerDistanceSq;
		}

		private Vector3 GetSpawnPosition()
		{
			Vector3 origin = transform.position;
			if (!config.IsEvent)
				return origin + GetRandomOffset();
			Vector3 from = playerModel.Unit != null ? playerModel.Unit.transform.position : playerModel.LastPosition;
			for (int i = 0; i < 100; i++)
			{
				Vector3 position = origin + GetRandomOffset();
				if (IsPathReachable(from, position))
					return position;
			}

			return origin;
		}

		private Vector3 GetRandomOffset()
		{
			float x = config.SpawnArea.x * UnityEngine.Random.Range(-0.5f, 0.5f);
			float z = config.SpawnArea.y * UnityEngine.Random.Range(-0.5f, 0.5f);
			return Quaternion.Euler(0f, config.SpawnAreaRotation, 0f) * new Vector3(x, 0f, z);
		}

		private bool IsPathReachable(Vector3 from, Vector3 to)
		{
			var path = new NavMeshPath();
			return NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
		}

		private void ApplyEventPlanToConfig()
		{
            // Original APK ARM64 ELF VA 0x150639c; exact forecast override before EnemyUnit.Initialize.
            var model = eventsRepository.CurrentEvent;
            if (model == null || model.Key != config.EventDef.Key) return;
            var unitType = config.EventDef.UnitType;
            var enemyInfo = eventsRepository.GetEventEnemyInfo(unitType);
            if (!enemyInfo.HasValue) return;
            var enemy = enemyInfo.Value;
            var resistType = model.Data.ResistType.Value;
            config.WeaponType = enemy.AttackType;
            foreach (var armor in config.ArmorConfig.Armors)
                armor.Resist = armor.Type == resistType && armor.Type != WeaponType.Magic ? -0.9f : 0f;
            var health = config.Stats.FirstOrDefault(stat => stat.Type == StatType.Health);
            if (health != null) health.BaseValue = enemy.Health;
            var damage = config.Stats.FirstOrDefault(stat => stat.Type == StatType.Damage);
            if (damage != null) damage.BaseValue = enemy.Damage;
            var rewardInfo = eventsRepository.GetEventRewardInfo(unitType);
            if (!rewardInfo.HasValue || !rewardInfo.Value.HasReward) return;
            var reward = rewardInfo.Value;
            config.StatRewards.Clear();
            config.StatRewards.Add(new StatModifier
            {
                Name = "event." + config.EventDef.Key + "." + unitType.ToString().ToLowerInvariant(),
                Duration = 0f,
                ModifierType = (ModifierType)0,
                Source = (ModifierSource)0,
                StatType = reward.RewardStatType,
                Value = (float)reward.RewardValue,
            });
        }

		private void HandleDeath(UnitCore unit)
		{
			EnsureSpawnerDataPersisted();
			unit.OnDied -= HandleDeath;
			if (unit is CrystalUnit crystal)
			{
				HandleCrystalDestruction(crystal);
				return;
			}

			EnemyUnit enemy = (EnemyUnit)unit;
			int countBefore = units.Count;
			int maxCount = config.Count;
			enemy.SpawnerStatRewardMultiplier = ConsumeZeroCooldownStatRewardMultiplier(enemy);
			if (!global::Codebase.Rpg.RpgModeConfig.Enabled)
			{
			ItemSlotUnlockFlow? unlock = ItemSlotUnlockFlow.For(config.SlotReward);
			App.ExecuteCommand<GrantSpawnerItemSlotRewardCommand, string>(new GrantSpawnerItemSlotRewardCommand { Spawner = this });
			if (unlock.HasValue && unlock.Value.SlotType != ItemSlotType.Helmet)
				App.InitiateFlow(unlock.Value, false).Forget();
			}
			ItemConfig item = enemy.ItemReward ?? config.ItemReward;
			int additionalCharges = 0;
			int copies = 0;
			bool skippedItemGrant = true;
			if (item != null && !global::Codebase.Rpg.RpgModeConfig.Enabled)
			{
				if (IsChargesEnabled)
				{
					copies = ItemDropAmountCalculator.ResolveKillDropCopies(item, metaStatsModel, RemainingCharges, out additionalCharges);
					RemainingCharges -= additionalCharges;
				}
				else
					copies = ItemDropAmountCalculator.CalculatePerKill(item, metaStatsModel);
				if (copies > 0)
				{
					App.ExecuteCommand(new GrantSpawnerItemRewardCommand { Spawner = this, Unit = enemy, Copies = copies });
					skippedItemGrant = false;
				}
			}

			App.ExecuteCommand(new GrantSpawnerGateRewardCommand { Spawner = this });
			App.ExecuteCommand(new GrantSpawnerPortalGemRewardCommand { GemIndex = config.PortalGemReward, HasItemReward = HasItemReward });
			if (!global::Codebase.Rpg.RpgModeConfig.Enabled)
			App.ExecuteCommand(new ReduceGeodeTimerOnKillCommand { KillCount = additionalCharges + enemy.SpawnerStatRewardMultiplier });
			if (!skippedItemGrant && item != null)
				enemy.SpawnerStatRewardMultiplier = copies;
			abilityExecuteEventSystem.FireEvent(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnKill, EnemySpawner = this, Target = enemy });
			units.Remove(enemy);
			GameContext.Get<EnemiesModel>().RemoveEnemy(enemy);
			if (IsEventMiniBossSpawner)
				awaitingMiniBossDevour = true;
			int now = NowUnixSeconds;
			SavedCount = units.Count;
			if ((countBefore >= maxCount || units.Count == 0) && !IsEventMiniBossSpawner)
			{
				LastSpawnTime = now;
				Cooldown = GetSmallCooldownSeconds();
			}

			if (units.Count == 0 && IsChargesEnabled && RemainingCharges <= 0)
				TryStartBigCooldown(now);
			UpdateNextProcessTime(now);
			SyncMinimapAliveState();
			SyncMinimapCooldown();
			OnDied?.Invoke(enemy);
			if (!enemy.IsFlying && !enemy.DisableAgent && !enemy.IsStatic && enemy.Agent.enabled && enemy.Agent.isOnNavMesh)
				enemy.Agent.isStopped = true;
			UserModel user = GameContext.Get<UserModel>();
			if (user.PoolIndex.Value < config.RewardedPoolIndex)
				user.PoolIndex.Value = config.RewardedPoolIndex;
			if (user.OptimumTime.Value < config.OptimumTime && !enemy.IsStatic)
				user.OptimumTime.Value = config.OptimumTime;
			if (enemy.TryGetComponent<CreeperMobProvider>(out _))
			{
				if (global::Codebase.Rpg.RpgModeConfig.Enabled) global::Codebase.Rpg.RpgRewardService.Kill(enemy, config);
				enemy.OnDevoured -= HandleDevour;
				unitPool.Despawn(enemy);
			}
		}

		private void HandleCrystalDestruction(CrystalUnit crystal)
		{
			EnsureSpawnerDataPersisted();
			crystal.OnDevoured -= HandleDevour;
			units.Remove(crystal);
			GameContext.Get<EnemiesModel>().RemoveEnemy(crystal);
			World.Default.GetStash<CrystalUnitTag>().Remove(crystal.EntityProvider.Entity);
			World.Default.GetStash<DynamicPoiTag>().Remove(crystal.EntityProvider.Entity);
			bool wasUnlocked = crystalModel.IsUnlocked;
			if (!wasUnlocked)
				crystalModel.IsUnlocked = true;
			else
				crystalModel.GrantObjectiveKillReward();
			crystalModel.OnObjectiveDestroyed();
			crystal.PlayDestruction();
			audioPlayer.PlaySoundOnce("quest_crystal_shatter");
			ApplyCrystalObjectiveSpawnerState();
			if (!wasUnlocked)
				OpenUnlockPopupDelayed().Forget();
			unitPool.Despawn(crystal);
			SavedCount = units.Count;
			SyncMinimapAliveState();
		}

		private bool ShouldKeepCrystalObjectiveSpawnerEnabled()
		{
			return config.Prefab is CrystalUnit && (!GameContext.Has<CrystalModel>() || GameContext.Get<CrystalModel>().NeedsObjectiveSpawnerActive);
		}

		private void MigrateCrystalObjectiveWaitingState()
		{
			if (SavedCount > 0)
			{
				if (crystalModel.IsObjectiveWaitingForRespawn)
					crystalModel.IsObjectiveWaitingForRespawn = false;
				return;
			}

			if (crystalModel.IsObjectiveWaitingForRespawn || crystalModel.ObjectiveKillCount <= 0)
				return;
			if (crystalModel.ObjectiveGeodesProgress <= 0 && !ClearedOnce)
				return;
			crystalModel.IsObjectiveWaitingForRespawn = true;
		}

		private void RestoreRespawnedCrystalIfNeeded()
		{
			if (ClearedOnce || crystalModel.ObjectiveKillCount <= 0 || crystalModel.ObjectiveGeodesProgress > 0)
				return;
			if (crystalModel.IsObjectiveWaitingForRespawn)
				crystalModel.IsObjectiveWaitingForRespawn = false;
			if (SavedCount <= 0 && !AnyoneAlive)
				RespawnCrystalObjective();
		}

		private void ApplyCrystalObjectiveSpawnerState()
		{
			if (GameContext.Has<CrystalModel>() && hasBeenClearedOnceCache && config.DisableAfterFirstClear)
			{
				bool keep = ShouldKeepCrystalObjectiveSpawnerEnabled();
				enabled = keep;
				IsEnabled = keep;
			}
		}

		private async UniTaskVoid OpenUnlockPopupDelayed()
		{
			await UniTask.Delay(System.TimeSpan.FromSeconds(1.0), false, PlayerLoopTiming.Update);
			GameContext.Get<Codebase.Core.UI.UINavigator>().Open<Codebase.UI.Crystals.CrystalsUnlockView>("Assets/Prefabs/UI/Amulet/Crystals/CrystalsUnlockView.prefab", true);
		}

		private void SyncMinimapAliveState()
		{
			if (wasAnyoneAlivePreviousFrame != AnyoneAlive)
			{
				wasAnyoneAlivePreviousFrame = AnyoneAlive;
				MinimapVersion++;
			}

			if (!AnyoneAlive && madeFirstSpawn && !hasBeenClearedOnceCache)
			{
				hasBeenClearedOnceCache = true;
				ClearedOnce = true;
				MinimapVersion++;
			}
		}

		private void SyncMinimapCooldown()
		{
			if (!AnyoneAlive && (HasItemReward || config.IsEliteBoss) && Cooldown > 0f)
				MinimapVersion++;
		}

		private void UpdateNextProcessTime(int now)
		{
			int next = NextBigSpawnTime;
			if (next <= now)
			{
				if (units.Count < config.Count)
				{
					int small = GetSmallCooldownSeconds();
					next = small > 0 ? Math.Max(now, LastSpawnTime + small) : 0;
				}
				else
					next = 0;
			}

			nextProcessTime = next;
		}

		private void HandleDevour(UnitCore unit)
		{
			EnemyUnit enemy = (EnemyUnit)unit;
			CurrentDevourRewardMultiplier = enemy.SpawnerStatRewardMultiplier;
			if (IsEventMiniBossSpawner)
			{
				awaitingMiniBossDevour = false;
				if (!IsRosterMiniBossSpawner)
				{
					int now = NowUnixSeconds;
					LastSpawnTime = now;
					Cooldown = GetSmallCooldownSeconds();
					UpdateNextProcessTime(now);
				}
			}

			abilityExecuteEventSystem.ProcessEventImmediate(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnDevour, EnemySpawner = this, Target = enemy });
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) global::Codebase.Rpg.RpgRewardService.Kill(enemy, config);
			else
			{
			App.ExecuteCommand(new GrantSpawnerStatRewardCommand { Spawner = this, Unit = enemy });
			App.ExecuteCommand(new GrantSpawnerEventCurrencyRewardCommand { Spawner = this, Unit = enemy });
			App.ExecuteCommand(new GrantSpawnerSoulRewardCommand { Spawner = this });
			App.ExecuteCommand(new GrantSpawnerCurrencyRewardCommand { Spawner = this });
			}
			CurrentDevourRewardMultiplier = 1;
			unitPool.Despawn(enemy);
			enemy.OnDevoured -= HandleDevour;
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			OnDied = null;
			rpmCalculatedOnLoad = false;
		}
	}
}
