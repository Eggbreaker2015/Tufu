using Codebase.BossTower.Components;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;
using Scellecs.Morpeh.Providers;
using Codebase.Unit;

namespace Codebase.BossTower.Commands
{
	public class EnterBossArenaCommandHandler : ICommandHandler<EnterBossArenaCommand>
	{
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private BossArenaSystem bossArenaSystem;
		[Inject]
		private BossSpawnService bossSpawnService;
		[Inject]
		private BossTowerModel bossTowerModel;
		private World world;
		private Stash<BossRaidComponent> raidStash;
		private Stash<PlayerTag> playerTagStash;
		public void Execute(EnterBossArenaCommand command)
		{
			if (playerModel.InBossTowerArena && !command.IsRetry)
				return;
			if (!bossTowerModel.TryGetBoss(command.BossKey, out BossModel boss))
			{
				Debug.LogError("Boss with key '" + command.BossKey + "' not found");
				return;
			}

			if (!bossTowerModel.IsBossUnlocked(command.BossKey))
				return;
			string arenaKey = boss.Def.ArenaKey;
			if (!bossArenaSystem.TryGet(arenaKey, out BossArenaComponent arena))
			{
				Debug.LogError("Arena with key '" + arenaKey + "' not found for boss '" + command.BossKey + "'");
				return;
			}

			ResetItemCooldowns();
			var unit = playerModel.Unit;
			var entity = unit.GetComponent<EntityProvider>().Entity;
			Vector3 returnPosition = unit.transform.position;
			Vector3 playerPosition = arena.PlayerSpawnPoint.position;
			Vector3 bossPosition = arena.BossSpawnPoint.position;
			if (!command.IsCalledFromArena)
				playerModel.SavePositionOverride = returnPosition;
			unit.CurrentHealth = unit.GetStatValue(StatType.Health, true);
			unit.Agent.Warp(playerPosition);
			var bossUnit = bossSpawnService.SpawnBoss(boss, bossPosition);
			if (bossUnit == null)
			{
				Debug.LogError("Failed to spawn boss '" + command.BossKey + "'");
				return;
			}

			playerModel.InBossTowerArena = true;
			bossTowerModel.SetArenaMusic();
			raidStash.Set(entity, new BossRaidComponent { BossKey = command.BossKey, ArenaKey = arenaKey, Floor = boss.Floor, BossUnit = bossUnit, ReturnPosition = returnPosition, RemainingTime = boss.IsSuperBoss ? 120f : 60f, DelayToMakeSureBossIsDead = 3f });
		}

		private void ResetItemCooldowns()
		{
			foreach (var item in playerModel.Items)
			{
				item.Cooldown = 0f;
				item.AttackInProgress = false;
				item.ResetCooldown = false;
				foreach (var state in item.AbilityStates)
					state.Cooldown = 0f;
			}
		}

		public EnterBossArenaCommandHandler()
		{
			world = World.Default;
			raidStash = world.GetStash<BossRaidComponent>();
			playerTagStash = world.GetStash<PlayerTag>();
		}
	}
}
