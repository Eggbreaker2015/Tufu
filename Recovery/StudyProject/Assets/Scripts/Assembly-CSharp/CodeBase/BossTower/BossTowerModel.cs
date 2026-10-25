using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Meta;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Services.Time;
using UniMob;
using UniMob.Core;
using UnityEngine;
using UnityEngine.Pool;
using System.Linq;

namespace Codebase.BossTower
{
	public class BossTowerModel : ModelRepository<BossModel, BossTowerUd>
	{
		[Inject]
		private BossTowerConfig config;
		[Inject]
		private CurrencyModel currencyModel;
		[Inject]
		private ITimeService timeService;
		[Inject]
		private MetaStatsModel metaStatsModel;
		[CompilerGenerated]
		private bool __IsFighting__BackingField;
		[CompilerGenerated]
		private AudioClip __CurrentBossMusic__BackingField;
		private Dictionary<Rarity, BossDropItemData> weaponsDrops;
		private Dictionary<Rarity, BossDropItemData> armorDrops;
		[CompilerGenerated]
		private bool __IsShowingRaidResults__BackingField;
		private ObjectPool<ItemReward> itemRewardPool;
		private ComputedAtom<bool> __IsFighting;
		private ComputedAtom<AudioClip> __CurrentBossMusic;
		private ComputedAtom<TimeSpan> __TimeToReset;
		private ComputedAtom<List<BossModel>> __Bosses;
		private ComputedAtom<List<BossDef>> __UndefeatedBosses;
		private ComputedAtom<int> __DefeatedCount;
		private ComputedAtom<int> __Tickets;
		private ComputedAtom<int> __MaxTickets;
		private ComputedAtom<bool> __IsShowingRaidResults;
		public bool IsFighting
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsFighting == null)
					__IsFighting = CodeGenAtom.CreatePooled(this, null, () => IsFighting, false);
				return __IsFighting.DirectEvaluate() ? __IsFighting__BackingField : __IsFighting.Value;
			}

			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsFighting != null && !__IsFighting.CompareAndInvalidate(value))
					return;
				__IsFighting__BackingField = value;
			}
		}

		public AudioClip CurrentBossMusic
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CurrentBossMusic == null)
					__CurrentBossMusic = CodeGenAtom.CreatePooled(this, null, () => CurrentBossMusic, false);
				return __CurrentBossMusic.DirectEvaluate() ? __CurrentBossMusic__BackingField : __CurrentBossMusic.Value;
			}

			[CompilerGenerated]
			private set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CurrentBossMusic != null && !__CurrentBossMusic.CompareAndInvalidate(value))
					return;
				__CurrentBossMusic__BackingField = value;
			}
		}

		public bool BossMusicInstant { get; private set; }

		public TimeSpan TimeToReset
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__TimeToReset == null)
					__TimeToReset = CodeGenAtom.CreatePooled(this, null, () => TimeToReset, false);
				return __TimeToReset.DirectEvaluate() ? (timeService.Now.ToDateTime().ToUniversalTime().Date.AddDays(1) - timeService.Now.ToDateTime().ToUniversalTime()) : __TimeToReset.Value;
			}
		}

		public List<BossModel> Bosses
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Bosses == null)
					__Bosses = CodeGenAtom.CreatePooled(this, null, () => Bosses, false);
				return __Bosses.DirectEvaluate() ? values.ToList() : __Bosses.Value;
			}
		}

		public IReadOnlyList<BossDef> AllBosses
		{
			get
			{
				return config.All;
			}
		}

		public List<BossDef> UndefeatedBosses
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__UndefeatedBosses == null)
					__UndefeatedBosses = CodeGenAtom.CreatePooled(this, null, () => UndefeatedBosses, false);
				return __UndefeatedBosses.DirectEvaluate() ? config.All.Where(def => !IsBossDefeated(def.Key)).ToList() : __UndefeatedBosses.Value;
			}
		}

		public int DefeatedCount
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__DefeatedCount == null)
					__DefeatedCount = CodeGenAtom.CreatePooled(this, null, () => DefeatedCount, false);
				return __DefeatedCount.DirectEvaluate() ? Data.DefeatedBosses.Count : __DefeatedCount.Value;
			}
		}

		public int Tickets
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Tickets == null)
					__Tickets = CodeGenAtom.CreatePooled(this, null, () => Tickets, false);
				return __Tickets.DirectEvaluate() ? (int)currencyModel.Get(CurrencyKeys.BossTowerTickets) : __Tickets.Value;
			}
		}

		public int MaxTickets
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MaxTickets == null)
					__MaxTickets = CodeGenAtom.CreatePooled(this, null, () => MaxTickets, false);
				return __MaxTickets.DirectEvaluate() ? config.BaseTickets + (int)metaStatsModel.Get("boss_tower_tickets").Value : __MaxTickets.Value;
			}
		}

		public bool IsShowingRaidResults
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsShowingRaidResults == null)
					__IsShowingRaidResults = CodeGenAtom.CreatePooled(this, null, () => IsShowingRaidResults, false);
				return __IsShowingRaidResults.DirectEvaluate() ? __IsShowingRaidResults__BackingField : __IsShowingRaidResults.Value;
			}

			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsShowingRaidResults != null && !__IsShowingRaidResults.CompareAndInvalidate(value))
					return;
				__IsShowingRaidResults__BackingField = value;
			}
		}

		public int PortalVersion { get; set; }

		public void SetArenaMusic()
		{
			BossMusicInstant = true;
			CurrentBossMusic = config.ArenaMusic;
		}

		public void SetFightMusic()
		{
			BossMusicInstant = true;
			CurrentBossMusic = config.FightMusic;
		}

		public void ClearBossMusic()
		{
			BossMusicInstant = false;
			CurrentBossMusic = null;
		}

		public BossTowerModel(Lifetime lifetime) : base(lifetime)
		{
			weaponsDrops = new Dictionary<Rarity, BossDropItemData>();
			armorDrops = new Dictionary<Rarity, BossDropItemData>();
			itemRewardPool = new ObjectPool<ItemReward>(() => new ItemReward(), null, null, null, true, 10, 10000);
		}

		public void Populate()
		{
			foreach (BossDropItemData drop in config.bossItemArmorDrops)
				armorDrops.Add(drop.rarity, drop);
			foreach (BossDropItemData drop in config.bossItemWeaponDrops)
				weaponsDrops.Add(drop.rarity, drop);
			foreach (BossDef def in config.All)
			{
				BossUd data = Data.Bosses.GetOrCreate(def.Key, out bool created);
				var boss = new BossModel(Lifetime, def, data);
				Injector.InjectInto(boss);
				boss.Initialize();
				Add(boss);
			}
		}

		public BossModel GetBoss(string key)
		{
			return Bosses.FirstOrDefault(boss => boss.Def.Key == key);
		}

		public bool TryGetBoss(string key, out BossModel boss)
		{
			boss = GetBoss(key);
			return boss != null;
		}

		public bool IsBossDefeated(string key)
		{
			return Data.DefeatedBosses.Contains(key);
		}

		public bool IsBossUnlocked(string bossKey)
		{
			return GetBoss(bossKey).Def.isUnlockedByDefault || metaStatsModel.Get("boss_tower_unlock").GetValue(bossKey) > 0.0;
		}

		public void MarkBossDefeated(string key)
		{
			if (!IsBossDefeated(key))
				Data.DefeatedBosses.Add(key);
		}

		public void AddTickets(int amount)
		{
			if (amount > 0)
				currencyModel.Add(CurrencyKeys.BossTowerTickets, amount);
		}

		public bool TrySpendTickets(int amount)
		{
			return currencyModel.TrySpend(CurrencyKeys.BossTowerTickets, amount);
		}

		public bool HasEnoughTickets(int amount)
		{
			return currencyModel.HasEnough(CurrencyKeys.BossTowerTickets, amount);
		}

		public void RestoreTicketsToMax()
		{
			currencyModel.Set(CurrencyKeys.BossTowerTickets, MaxTickets);
			foreach (BossModel boss in Bosses)
			{
				boss.DailyClaims = 0;
				boss.Data.IsWeakened.Value = false;
			}

			PortalVersion++;
			SelectRandomWeakenedBoss();
		}

		public void SelectRandomWeakenedBoss()
		{
			if (metaStatsModel.Get("tower_boss_weakness").Value <= 0.0)
				return;
			var candidates = CollectionPool<List<BossModel>, BossModel>.Get();
			foreach (BossModel boss in Bosses)
				if (IsBossUnlocked(boss.Def.Key))
					candidates.Add(boss);
			candidates[UnityEngine.Random.Range(0, candidates.Count)].Data.IsWeakened.Value = true;
			CollectionPool<List<BossModel>, BossModel>.Release(candidates);
		}

		public bool CanGrantBossClaimReward(string bossKey)
		{
			BossModel boss = GetBoss(bossKey);
			return boss != null && IsBossDefeated(bossKey) && boss.HasRemainingDailyClaims;
		}

		public void IncrementDailyClaims(string bossKey)
		{
			BossModel boss = GetBoss(bossKey);
			if (boss == null)
				return;
			PortalVersion++;
			boss.DailyClaims++;
		}

		public int GetBossFloor(string bossKey)
		{
			BossModel boss = GetBoss(bossKey);
			return boss != null ? boss.Floor : 1;
		}

		public void IncrementBossFloor(string bossKey)
		{
			BossModel boss = GetBoss(bossKey);
			boss.Floor++;
			RebakeRewards(boss);
		}

		public void RebakeRewards(BossModel bossModel)
		{
			int floor = bossModel.Floor;
			foreach (RewardBase reward in bossModel.RewardsBaked)
				if (reward is ItemReward item)
					itemRewardPool.Release(item);
			bossModel.RewardsBaked.Clear();
			foreach (RewardBase reward in bossModel.PreviousFloorRewardsBaked)
				if (reward is ItemReward item)
					itemRewardPool.Release(item);
			bossModel.PreviousFloorRewardsBaked.Clear();
			CalculateRewards(bossModel.Def, floor, bossModel.RewardsBaked);
			CalculateRewards(bossModel.Def, Math.Max(1, floor - 1), bossModel.PreviousFloorRewardsBaked);
			bossModel.RewardsVersion.Invalidate();
		}

		private void CalculateRewards(BossDef bossDef, int floor, List<RewardBase> bossModelRewardsBaked)
		{
			double bonusPercent = metaStatsModel.Get("tower_drop_bonus_percent").Value;
			double itemsDrop = metaStatsModel.Get("boss_tower_items_drop").Value;
			float legendaryFlat = (float)metaStatsModel.Get("tower_drop_legend_per_floor").GetValue(bossDef.Key);
			double legendaryMultiplier = metaStatsModel.Get("boss_tower_legendary_drop").GetValue(bossDef.Key);
			bossModelRewardsBaked.Add(new SoulReward { SoulKey = floor > 0 && floor % 10 == 0 ? "divine" : "great", Amount = 1 });
			foreach (RewardBase reward in bossDef.Rewards)
			{
				if (!(reward is ItemReward item))
					continue;
				Rarity rarity = item.Def.Rarity;
				BossDropItemData drop = item.Def.SlotType == ItemSlotType.Weapon ? weaponsDrops[rarity] : armorDrops[rarity];
				if (floor < drop.floorStart)
					continue;
				float baseAmount, finalAmount;
				if (rarity == Rarity.Legendary)
				{
					baseAmount = (float)Math.Round(legendaryFlat);
					finalAmount = (float)Math.Round(baseAmount * (float)(legendaryMultiplier + 1.0));
				}
				else
				{
					baseAmount = (int)Math.Round((drop.startItemDrop + drop.addItemDrop * (floor - 1)) * Math.Pow(drop.modItemDrop, floor - 1));
					finalAmount = baseAmount * (float)(bonusPercent + itemsDrop + 1.0);
				}

				if (baseAmount <= 0f)
					continue;
				ItemReward baked = itemRewardPool.Get();
				baked.Def = item.Def;
				baked.IsRandom = false;
				baked.SlotType = item.Def.SlotType;
				baked.Level = (int)finalAmount;
				bossModelRewardsBaked.Add(baked);
			}
		}
	}
}
