using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Services.Time;
using Scellecs.Morpeh;

namespace Codebase.BossTower.Systems
{
	public class BossTowerTicketsRestoreSystem : SystemBase
	{
		[Inject]
		private BossTowerModel bossTowerModel;
		[Inject]
		private ITimeService timeService;
		[Inject]
		private MetaStatsModel metaStatsModel;
		[Inject]
		private PlayerModel playerModel;
		private DateTime lastCheckedDate;
		private double lastCheckTowerWeakness;
		private MetaStat weaknessStat;
		private MetaStat rewardsStat;
		private MetaStat rewardsLegendaryStat;
		private MetaStat rewardsPercentStat;
		private MetaStat rewardsLegendaryFlatStat;
		private double rewardStatLevel;
		private double rewardStatLegendaryLevel;
		private double rewardStatLegendaryFlatLevel;
		private double rewardStatPercentLevel;
		private readonly Dictionary<string, double> rewardLegendaryFlatByBoss;
		private readonly Dictionary<string, double> rewardLegendaryMultByBoss;
		private double ticketsAmount;
		private bool bossTowerPortalWasUnlocked;
		public override void OnAwake()
		{
			lastCheckedDate = timeService.Now.ToDateTime().ToUniversalTime().Date;
			CheckAndRestoreTickets();
			weaknessStat = metaStatsModel.Get("tower_boss_weakness");
			rewardsStat = metaStatsModel.Get("boss_tower_items_drop");
			rewardsPercentStat = metaStatsModel.Get("tower_drop_bonus_percent");
			rewardsLegendaryStat = metaStatsModel.Get("boss_tower_legendary_drop");
			rewardsLegendaryFlatStat = metaStatsModel.Get("tower_drop_legend_per_floor");
			bossTowerPortalWasUnlocked = playerModel.IsPortalUnlocked;
			UpdateStatValues();
			lastCheckTowerWeakness = weaknessStat.Value;
		}

		private void UpdateStatValues()
		{
			rewardStatLevel = rewardsStat.Value;
			rewardStatLegendaryLevel = rewardsLegendaryStat.Value;
			rewardStatLegendaryFlatLevel = rewardsLegendaryFlatStat.Value;
			rewardStatPercentLevel = rewardsPercentStat.Value;
			rewardLegendaryFlatByBoss.Clear();
			rewardLegendaryMultByBoss.Clear();
			foreach (BossDef boss in bossTowerModel.AllBosses)
			{
				rewardLegendaryFlatByBoss[boss.Key] = rewardsLegendaryFlatStat.GetValue(boss.Key);
				rewardLegendaryMultByBoss[boss.Key] = rewardsLegendaryStat.GetValue(boss.Key);
			}
		}

		private bool IsDropStatChanged()
		{
			if (rewardsStat.Value != rewardStatLevel || rewardsLegendaryFlatStat.Value != rewardStatLegendaryFlatLevel || rewardsPercentStat.Value != rewardStatPercentLevel || rewardsLegendaryStat.Value != rewardStatLegendaryLevel)
				return true;
			foreach (BossDef boss in bossTowerModel.AllBosses)
			{
				if (rewardsLegendaryFlatStat.GetValue(boss.Key) != rewardLegendaryFlatByBoss.GetValueOrDefault(boss.Key) || rewardsLegendaryStat.GetValue(boss.Key) != rewardLegendaryMultByBoss.GetValueOrDefault(boss.Key))
					return true;
			}

			return false;
		}

		public override void OnUpdate(float deltaTime)
		{
			DateTime date = timeService.Now.ToDateTime().ToUniversalTime().Date;
			if (IsDropStatChanged())
			{
				UpdateStatValues();
				App.ExecuteCommand(new Codebase.BossTower.Commands.RebakeBossTowerRewardsCommand());
			}

			if (bossTowerPortalWasUnlocked != playerModel.IsPortalUnlocked || ticketsAmount != bossTowerModel.Tickets)
			{
				ticketsAmount = bossTowerModel.Tickets;
				bossTowerPortalWasUnlocked = playerModel.IsPortalUnlocked;
				bossTowerModel.PortalVersion++;
			}

			if (date > lastCheckedDate)
			{
				CheckAndRestoreTickets();
				lastCheckedDate = date;
			}

			if (Math.Abs(weaknessStat.Value - lastCheckTowerWeakness) > 0.01)
			{
				if (lastCheckTowerWeakness <= 0.0)
					bossTowerModel.SelectRandomWeakenedBoss();
				lastCheckTowerWeakness = weaknessStat.Value;
			}
		}

		private void CheckAndRestoreTickets()
		{
			DateTime now = timeService.Now.ToDateTime().ToUniversalTime();
			int lastTimestamp = bossTowerModel.Data.LastTicketsRestoreTimestamp.Value;
			DateTime previous = DateTimeOffset.FromUnixTimeSeconds(lastTimestamp).UtcDateTime;
			if (!(now.Date > previous.Date) && lastTimestamp != 0)
				return;
			bossTowerModel.RestoreTicketsToMax();
			bossTowerModel.Data.LastTicketsRestoreTimestamp.Value = (int)new DateTimeOffset(now.Date).ToUnixTimeSeconds();
		}

		public BossTowerTicketsRestoreSystem()
		{
			rewardLegendaryFlatByBoss = new Dictionary<string, double>();
			rewardLegendaryMultByBoss = new Dictionary<string, double>();
		}
	}
}
