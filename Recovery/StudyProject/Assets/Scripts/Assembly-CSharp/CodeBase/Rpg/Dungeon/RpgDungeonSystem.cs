using System;
using System.Collections.Generic;
using Codebase.Abilities;
using Codebase.Abilities.Events;
using Codebase.Abilities.Systems;
using Codebase.BossTower.Components;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Core.UserData;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Rpg.UI;
using Codebase.Services.Time;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Utilities.ObjectPool;
using Scellecs.Morpeh;
using UniMob;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Rpg.Dungeon
{
	// Instanced dungeons (Design.md section 20): a portal warps the party into an idle Boss Tower arena where
	// enemies never respawn and fallen heroes stay down; the boss kill adds a clear bonus, shows a summary and starts a 24 h lockout.
	public class RpgDungeonSystem : SystemBase
	{
		private class Run
		{
			public RpgDungeonDef Def;
			public float StartTime;
			public int StartLevel;
			public int Kills;
			public double Xp;
			public int Gold;
			public EnemyUnit Boss;
			public Vector3 ReturnPosition;
			public float ReturnYaw;
			public float ResultTimer = -1f;
			public string ResultBody;
		}

		[Inject] private PlayerModel playerModel;
		[Inject] private HeroRosterModel roster;
		[Inject] private PetsModel petsModel;
		[Inject] private BossArenaSystem arenaSystem;
		[Inject] private AbilityExecuteEventSystem abilityExecuteEventSystem;

		public static RpgDungeonSystem Instance { get; private set; }

		private readonly ObjectPool<EnemyUnit> pool = new ObjectPool<EnemyUnit>();
		private readonly List<EnemyUnit> alive = new List<EnemyUnit>();
		private readonly Dictionary<string, RpgDungeonPortal> portals = new Dictionary<string, RpgDungeonPortal>();
		private Dictionary<string, EnemySpawnerConfig> sources;
		private Run run;
		private RpgDungeonPortal exitPortal;
		private RpgDungeonDialogState dialog;
		private string insidePortal;
		private bool insideExit;
		private float labelTimer;

		private enum RpgDungeonDialogState { None, Leave, Result, Cooldown }

		public bool InDungeon => run != null;
		public static bool BlocksRevive => Instance != null && Instance.InDungeon;
		public string ActiveKey => run?.Def.Key;
		public int RunKills => run?.Kills ?? 0;
		public double RunXp => run?.Xp ?? 0;
		public int RunGold => run?.Gold ?? 0;
		public IReadOnlyList<EnemyUnit> AliveEnemies => alive;
		public EnemyUnit BossUnit => run?.Boss;
		public string LastResult { get; private set; }

		public override void OnAwake()
		{
			Instance = this;
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!RpgModeConfig.Enabled || playerModel.Unit == null) return;
			EnsurePortals();
			labelTimer -= deltaTime;
			if (labelTimer <= 0f) { labelTimer = 0.25f; UpdateLabels(); }
			if (run == null) { CheckEntry(); return; }
			PlayerUnit pudge = playerModel.Unit;
			if (run.ResultTimer < 0f && (pudge.IsDying || pudge.IsDead)) { Wipe(); return; }
			if (run.ResultTimer >= 0f)
			{
				if (dialog != RpgDungeonDialogState.Result && (run.ResultTimer -= deltaTime) <= 0f) ShowResult();
				return;
			}
			CheckExit();
		}

		public override void Dispose()
		{
			if (Instance == this) Instance = null;
		}

		[StaticReset]
		private static void ResetStaticState() { Instance = null; }

		// ---- Cooldown ----

		private static RpgUd Data => GameContext.Has<GameDataService>() ? GameContext.Get<GameDataService>().Data.Rpg : null;

		private static DateTime UtcNow => GameContext.Has<ITimeService>() ? GameContext.Get<ITimeService>().Now.ToDateTime() : DateTime.UtcNow;

		public static TimeSpan CooldownRemaining(string key)
		{
			RpgUd data = Data;
			Dungeon.RpgDungeonCooldownUd entry = data?.DungeonCooldowns.Find(item => item.Key.Value == key);
			if (entry == null) return TimeSpan.Zero;
			TimeSpan left = new DateTime(entry.EndUtcTicks.Value, DateTimeKind.Utc) - UtcNow;
			return left > TimeSpan.Zero ? left : TimeSpan.Zero;
		}

		private static void SetCooldown(string key, DateTime endUtc)
		{
			RpgUd data = Data;
			if (data == null) return;
			Dungeon.RpgDungeonCooldownUd entry = data.DungeonCooldowns.Find(item => item.Key.Value == key);
			if (entry == null) { entry = data.DungeonCooldowns.Create(); entry.Key.Value = key; }
			entry.EndUtcTicks.Value = endUtc.Ticks;
		}

		public static void ResetCooldowns()
		{
			RpgUd data = Data;
			if (data == null) return;
			foreach (RpgDungeonDef def in RpgModeConfig.Instance.Dungeons)
			{
				Dungeon.RpgDungeonCooldownUd entry = data.DungeonCooldowns.Find(item => item.Key.Value == def.Key);
				if (entry != null) entry.EndUtcTicks.Value = 0;
			}
		}

		// ---- Portals ----

		private void EnsurePortals()
		{
			foreach (RpgDungeonDef def in RpgModeConfig.Instance.Dungeons)
				if (def != null && !string.IsNullOrEmpty(def.Key) && (!portals.TryGetValue(def.Key, out RpgDungeonPortal portal) || portal == null))
					portals[def.Key] = RpgDungeonPortal.Create("RpgDungeonPortal_" + def.Key, def.PortalPosition, def.PortalRotationY);
		}

		private void UpdateLabels()
		{
			foreach (RpgDungeonDef def in RpgModeConfig.Instance.Dungeons)
			{
				if (def == null || !portals.TryGetValue(def.Key, out RpgDungeonPortal portal) || portal == null) continue;
				TimeSpan left = CooldownRemaining(def.Key);
				if (left > TimeSpan.Zero)
					portal.SetLabel(def.DisplayName + "\n冷却中 " + FormatHours(left), new Color(0.75f, 0.75f, 0.8f));
				else
					portal.SetLabel(def.DisplayName + "\n副本", new Color(0.55f, 0.85f, 1f));
			}
			if (exitPortal != null) exitPortal.SetLabel("离开副本", new Color(1f, 0.8f, 0.45f));
		}

		private Vector3 LeaderPosition => roster.Controlled?.Unit != null ? roster.Controlled.Unit.transform.position : playerModel.Unit.transform.position;

		private static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude <= RpgModeConfig.Instance.DungeonPortalRadius * RpgModeConfig.Instance.DungeonPortalRadius;

		private void CheckEntry()
		{
			Vector3 leader = LeaderPosition;
			string inside = null;
			foreach (RpgDungeonDef def in RpgModeConfig.Instance.Dungeons)
				if (def != null && Near(leader, def.PortalPosition)) { inside = def.Key; break; }
			// Entering takes a fresh step into the trigger, so standing on it after a refusal or exit does nothing.
			if (inside != null && inside != insidePortal && dialog == RpgDungeonDialogState.None)
			{
				if (CooldownRemaining(inside) > TimeSpan.Zero) ShowCooldown(inside);
				else TryEnter(inside);
			}
			insidePortal = inside;
		}

		// Development builds (and the editor) get a reset button so the lockout does not block testing.
		private void ShowCooldown(string key)
		{
			RpgDungeonDef def = RpgModeConfig.Instance.Dungeons.Find(item => item != null && item.Key == key);
			if (def == null) return;
			string body = "通关后需要等待冷却才能再次挑战。\n剩余 " + FormatHours(CooldownRemaining(key));
			void Closed() => dialog = RpgDungeonDialogState.None;
			dialog = RpgDungeonDialogState.Cooldown;
			RpgDialogView view = Debug.isDebugBuild
				? RpgDialogView.Show(def.DisplayName + " 冷却中", body, "知道了", Closed, "重置冷却(调试)", () => { Closed(); ResetCooldowns(); TryEnter(key); })
				: RpgDialogView.Show(def.DisplayName + " 冷却中", body, "知道了", Closed);
			if (view == null) Closed();
		}

		private void CheckExit()
		{
			bool inside = exitPortal != null && Near(LeaderPosition, exitPortal.transform.position);
			if (inside && !insideExit && dialog == RpgDungeonDialogState.None)
			{
				dialog = RpgDungeonDialogState.Leave;
				RpgDialogView.Show("离开副本？", "副本将重置，下次进入时怪物全部刷新。", "离开", () => { dialog = RpgDungeonDialogState.None; Leave(); }, "取消", () => dialog = RpgDungeonDialogState.None);
			}
			insideExit = inside;
		}

		// ---- Flows ----

		public bool TryEnter(string key)
		{
			RpgDungeonDef def = RpgModeConfig.Instance.Dungeons.Find(item => item != null && item.Key == key);
			PlayerUnit pudge = playerModel.Unit;
			if (def == null || run != null || pudge == null || pudge.IsDying || pudge.IsDead || pudge.IsReviving) return false;
			if (CooldownRemaining(key) > TimeSpan.Zero) return false;
			if (arenaSystem == null || !arenaSystem.TryGet(def.ArenaKey, out BossArenaComponent arena) || arena.PlayerSpawnPoint == null || arena.BossSpawnPoint == null)
			{
				Debug.LogError("[RPG] Dungeon '" + key + "' arena '" + def.ArenaKey + "' not found");
				return false;
			}

			Vector3 start = arena.PlayerSpawnPoint.position;
			Vector3 forward = arena.BossSpawnPoint.position - start;
			forward.y = 0f;
			forward = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
			Vector3 right = new Vector3(forward.z, 0f, -forward.x);
			float yaw = Quaternion.LookRotation(forward).eulerAngles.y;

			Vector3 portalForward = Quaternion.Euler(0f, def.PortalRotationY, 0f) * Vector3.forward;
			Vector3 returnPosition = Sample(def.PortalPosition + portalForward * (RpgModeConfig.Instance.DungeonPortalRadius + 1.5f));
			run = new Run { Def = def, StartTime = Time.time, StartLevel = GameContext.Get<RpgProgressModel>().Level, ReturnPosition = returnPosition, ReturnYaw = def.PortalRotationY };
			playerModel.SavePositionOverride = returnPosition;
			WarpParty(start, yaw, true);

			foreach (RpgDungeonPack pack in def.Packs)
				SpawnPack(pack, Sample(start + forward * pack.Along + right * pack.Side), yaw + 180f);
			List<EnemyUnit> boss = SpawnPack(def.Boss, Sample(arena.BossSpawnPoint.position), yaw + 180f);
			run.Boss = boss.Count > 0 ? boss[0] : null;
			if (run.Boss == null) Debug.LogError("[RPG] Dungeon '" + key + "' boss failed to spawn");

			exitPortal = RpgDungeonPortal.Create("RpgDungeonExit_" + key, Sample(start + forward * def.ExitAlong), yaw + 180f);
			insideExit = true;
			UpdateLabels();
			return true;
		}

		public void Leave()
		{
			if (run == null) return;
			Vector3 position = run.ReturnPosition;
			float yaw = run.ReturnYaw;
			EndRun();
			WarpParty(position, yaw, false);
			playerModel.SavePositionOverride = null;
		}

		private void Wipe()
		{
			// Pudge died: the original death sequence takes the party to the fountain; the run is simply lost.
			EndRun();
		}

		private void EndRun()
		{
			for (int i = alive.Count - 1; i >= 0; i--)
				Despawn(alive[i]);
			alive.Clear();
			if (exitPortal != null) UnityEngine.Object.Destroy(exitPortal.gameObject);
			exitPortal = null;
			run = null;
			dialog = RpgDungeonDialogState.None;
			insidePortal = null;
		}

		private void Clear()
		{
			RpgDungeonDef def = run.Def;
			for (int i = alive.Count - 1; i >= 0; i--)
				Despawn(alive[i]);
			alive.Clear();
			SetCooldown(def.Key, UtcNow.AddHours(def.CooldownHours));
			// The clear bonus is paid right away so a late death cannot lose it; the summary follows after a short beat.
			var progress = GameContext.Get<RpgProgressModel>();
			double bonusXp = run.Xp * def.ClearBonusFraction;
			int bonusGold = (int)Math.Round(run.Gold * def.ClearBonusFraction, MidpointRounding.AwayFromZero);
			int levelBefore = run.StartLevel;
			RpgRewardService.Grant(playerModel.Unit.transform.position, bonusXp, bonusGold);
			int levelAfter = progress.Level;
			int seconds = Mathf.Max(0, Mathf.RoundToInt(Time.time - run.StartTime));
			run.ResultBody = def.DisplayName + "\n" +
				"用时 " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00") + "　击杀 " + run.Kills + "\n" +
				"<color=#80CFFF>经验 +" + Format(run.Xp + bonusXp) + "</color>（击杀 " + Format(run.Xp) + " + 通关奖励 " + Format(bonusXp) + "）\n" +
				"<color=#FFCD38>金币 +" + (run.Gold + bonusGold) + "</color>（击杀 " + run.Gold + " + 通关奖励 " + bonusGold + "）\n" +
				(levelAfter != levelBefore ? "<color=#9CFF8A>等级 " + levelBefore + " → " + levelAfter + "</color>\n" : "") +
				"<size=80%><color=#B0B4BE>下次可挑战：" + Mathf.RoundToInt(def.CooldownHours) + " 小时后</color></size>";
			LastResult = run.ResultBody;
			run.ResultTimer = 1.5f;
		}

		private void ShowResult()
		{
			dialog = RpgDungeonDialogState.Result;
			if (RpgDialogView.Show("副本通关", run.ResultBody, "领取", Collect) == null) Collect();
		}

		private void Collect()
		{
			PlayerUnit pudge = playerModel.Unit;
			if (pudge != null && (pudge.IsDying || pudge.IsDead)) EndRun();
			else Leave();
		}

		private void WarpParty(Vector3 position, float yaw, bool restoreHealth)
		{
			PlayerUnit pudge = playerModel.Unit;
			if (restoreHealth) pudge.CurrentHealth = pudge.GetStatValue(StatType.Health);
			pudge.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
			if (pudge.Agent != null && pudge.Agent.enabled) { pudge.Agent.Warp(position); pudge.Agent.ResetPath(); }
			else pudge.transform.position = position;
			if (PetConfig.Enabled) petsModel.ReviveAll(position, yaw, restoreHealth);
		}

		// ---- Enemies ----

		private EnemySpawnerConfig Source(string key)
		{
			if (sources == null)
			{
				sources = new Dictionary<string, EnemySpawnerConfig>();
				foreach (EnemySpawner spawner in UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include))
					if (spawner.Config != null && !string.IsNullOrEmpty(spawner.Config.Key)) sources[spawner.Config.Key] = spawner.Config;
			}
			return key != null && sources.TryGetValue(key, out EnemySpawnerConfig config) ? config : null;
		}

		private List<EnemyUnit> SpawnPack(RpgDungeonPack pack, Vector3 center, float yaw)
		{
			var spawned = new List<EnemyUnit>();
			EnemySpawnerConfig config = pack != null ? Source(pack.SpawnerKey) : null;
			if (config == null || config.Prefab == null)
			{
				Debug.LogError("[RPG] Dungeon enemy source '" + pack?.SpawnerKey + "' not found");
				return spawned;
			}
			int count = Mathf.Max(1, pack.Count);
			for (int i = 0; i < count; i++)
			{
				Vector3 offset = count == 1 ? Vector3.zero : Quaternion.Euler(0f, 360f * i / count, 0f) * Vector3.forward * 2f;
				spawned.Add(SpawnUnit(config, Sample(center + offset), yaw, pack.Level));
			}
			return spawned;
		}

		private EnemyUnit SpawnUnit(EnemySpawnerConfig config, Vector3 position, float yaw, int level)
		{
			Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
#if UNITY_EDITOR
			EnemyUnit unit = pool.Spawn(config.Prefab, position, rotation, null, RecoveryCompatibility.EditorEnemySpawnTemplates.Resolve);
#else
			EnemyUnit unit = pool.Spawn(config.Prefab, position, rotation, null);
#endif
			Entity entity = unit.EntityProvider.Entity;
			Stash<UnitDeadMarker> dead = World.Default.GetStash<UnitDeadMarker>();
			if (dead.Has(entity)) dead.Remove(entity);
			unit.OnDied -= HandleDeath;
			unit.OnDevoured -= HandleDevour;
			unit.transform.localScale = config.Prefab.transform.localScale;
			unit.SetDeathState(false);
			unit.Initialize(Lifetime.Eternal, config, position);
			RpgEnemyScaling.Apply(unit, Mathf.Clamp(level, 1, RpgModeConfig.Instance.LevelCap), Lifetime.Eternal);
			if (!config.IsStatic && !unit.IsFlying)
			{
				unit.Agent.enabled = true;
				if (unit.Agent.isOnNavMesh) unit.Agent.Warp(position);
				World.Default.GetStash<NavMeshAgentActiveMarker>().Set(entity);
				unit.LastDestination = unit.CurrentSpawnerPatrolPoint;
				if (unit.Agent.isOnNavMesh) unit.Agent.SetDestination(unit.CurrentSpawnerPatrolPoint);
			}
			unit.AggroTimer = 0f;
			GameContext.Get<EnemiesModel>().AddEnemy(unit);
			unit.OnDied += HandleDeath;
			unit.OnDevoured += HandleDevour;
			alive.Add(unit);
			return unit;
		}

		private void HandleDeath(UnitCore core)
		{
			core.OnDied -= HandleDeath;
			EnemyUnit enemy = (EnemyUnit)core;
			if (!alive.Remove(enemy)) return;
			GameContext.Get<EnemiesModel>().RemoveEnemy(enemy);
			if (run != null)
			{
				run.Kills++;
				RpgRewardService.Compute(enemy, enemy.Config, out double xp, out int gold);
				RpgRewardService.Grant(enemy.transform.position, xp, gold);
				run.Xp += xp;
				run.Gold += gold;
			}
			abilityExecuteEventSystem?.FireEvent(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnKill, Target = enemy });
			// Drives the devour hook animation, which ends in HandleDevour.
			EnemySpawner.OnDied?.Invoke(enemy);
			if (!enemy.IsFlying && !enemy.DisableAgent && !enemy.IsStatic && enemy.Agent.enabled && enemy.Agent.isOnNavMesh)
				enemy.Agent.isStopped = true;
			if (run != null && enemy == run.Boss && run.ResultTimer < 0f) Clear();
		}

		private void HandleDevour(UnitCore core)
		{
			EnemyUnit enemy = (EnemyUnit)core;
			enemy.OnDevoured -= HandleDevour;
			abilityExecuteEventSystem?.ProcessEventImmediate(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnDevour, Target = enemy });
			pool.Despawn(enemy);
		}

		private void Despawn(EnemyUnit enemy)
		{
			if (enemy == null) return;
			enemy.OnDied -= HandleDeath;
			enemy.OnDevoured -= HandleDevour;
			GameContext.Get<EnemiesModel>().RemoveEnemy(enemy);
			pool.Despawn(enemy);
		}

		private static Vector3 Sample(Vector3 position) => NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas) ? hit.position : position;

		private static string Format(double value) => value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

		private static string FormatHours(TimeSpan span) => ((int)span.TotalHours).ToString("00") + ":" + span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00");
	}
}
