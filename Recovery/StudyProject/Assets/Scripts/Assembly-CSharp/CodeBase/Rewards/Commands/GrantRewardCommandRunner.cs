using System;
using System.Collections.Generic;
using CodeBase.Audio;
using Codebase.Amulet;
using Codebase.Analytics;
using Codebase.Cashback;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Economy;
using Codebase.Events.Models;
using Codebase.LevelPass;
using Codebase.Mines;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.Services.Time;
using Codebase.Shop;
using Codebase.Simulation.Systems;
using Codebase.UI;
using Codebase.Unit;
using System.Linq;
using Codebase.Mail.Commands;
using Codebase.SoulPass.Commands;
using Codebase.Utilities;

namespace Codebase.Rewards.Commands
{
	public struct GrantRewardCommandRunner : ICommandHandler<GrantRewardCommand>
	{
		[Inject]
		private EventsRepository eventsRepository;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private IAnalyticsWrapper analytics;
		[Inject]
		private CheatsModel cheatsModel;
		[Inject]
		private SimulationSystem simulationSystem;
		[Inject]
		private ChestsConfig chestsConfig;
		[Inject]
		private ItemsProvider itemsProvider;
		[Inject]
		private StatRewardContainer statRewardContainer;
		[Inject]
		private IconStyleConfig iconStyle;
		[Inject]
		private CashbackModel cashbackModel;
		[Inject]
		private ChestQueueModel chestQueue;
		[Inject]
		private UserModel userModel;
		[Inject]
		private LevelPassModel levelPass;
		[Inject]
		private AmuletModel amuletModel;
		[Inject]
		private AmuletConfig amuletConfig;
		[Inject]
		private CurrencyModel currencyModel;
		[Inject]
		private ITimeService timeService;
		[Inject]
		private IAudioPlayer audioPlayer;
		[Inject]
		private MinesModel minesModel;
		private static readonly List<StatType> randomStats = new List<StatType>
		{
			(StatType)1,
			(StatType)2,
			(StatType)8,
			(StatType)6,
			(StatType)12,
			(StatType)16,
			(StatType)14,
			(StatType)20
		};
		private static readonly Dictionary<Type, Action<GrantRewardCommandRunner, RewardBase>> rewardHandlers = new Dictionary<Type, Action<GrantRewardCommandRunner, RewardBase>>
		{
			[typeof(EventCurrencyReward)] = (runner, reward) => runner.HandleEventCurrencyReward((EventCurrencyReward)reward),
			[typeof(CurrencyReward)] = (runner, reward) => runner.HandleCurrencyReward((CurrencyReward)reward),
			[typeof(StatReward)] = (runner, reward) => runner.HandleStatReward((StatReward)reward),
			[typeof(ChestReward)] = (runner, reward) => runner.HandleChestReward((ChestReward)reward),
			[typeof(ItemReward)] = (runner, reward) => runner.HandleItemReward((ItemReward)reward),
			[typeof(EventReward)] = (runner, reward) => runner.HandleEventReward((EventReward)reward),
			[typeof(GemsReward)] = (runner, reward) => runner.HandleGemsReward((GemsReward)reward),
			[typeof(BundleReward)] = (runner, reward) => runner.HandleBundleReward((BundleReward)reward),
			[typeof(VipPointsReward)] = (runner, reward) => runner.HandleVipPointsReward((VipPointsReward)reward),
			[typeof(NoAdsReward)] = (runner, reward) => runner.HandleNoAdsReward((NoAdsReward)reward),
			[typeof(SoulPassReward)] = (runner, reward) => runner.HandleSoulPassReward((SoulPassReward)reward),
			[typeof(CrystalPassReward)] = (runner, reward) => runner.HandleCrystalPassReward((CrystalPassReward)reward),
			[typeof(FreeBoosterTokenReward)] = (runner, reward) => runner.HandleFreeBoosterToken((FreeBoosterTokenReward)reward),
			[typeof(LevelPassReward)] = (runner, reward) => runner.HandleLevelPassReward((LevelPassReward)reward),
			[typeof(SoulReward)] = (runner, reward) => runner.HandleSoulReward((SoulReward)reward),
			[typeof(ItemSlotReward)] = (runner, reward) => runner.HandleItemSlotReward((ItemSlotReward)reward),
			[typeof(MineUnlockReward)] = (runner, reward) => runner.HandleMineUnlockReward((MineUnlockReward)reward),
			[typeof(ChaoticSoulOfferReward)] = (runner, reward) => runner.HandleChaoticSoulOfferReward((ChaoticSoulOfferReward)reward)
		};
		private string sourcePath;
		private ModifierSource? sourceOverride;
		public void Execute(GrantRewardCommand command)
		{
			sourcePath = command.SourcePath;
			sourceOverride = command.SourceOverride;
			HandleReward(command.Reward);
		}

		private void HandleReward(RewardBase reward)
		{
			System.Type type = reward.GetType();
			if (rewardHandlers.TryGetValue(type, out var handler))
				handler(this, reward);
			else
				UnityEngine.Debug.LogError(string.Format("No handler for reward type: {0}", type));
		}

		private void HandleNoAdsReward(NoAdsReward reward)
		{
			userModel.NoAdsPurchased.Value = true;
		}

		private void HandleSoulPassReward(SoulPassReward reward)
		{
			userModel.SoulPassPurchased.Value = true;
			userModel.SoulPassPurchaseTime.Value = new DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds();
			audioPlayer.PlaySoundOnce("pass_claim");
            // Learning adaptation: the original purchase mail is delivered directly because mailbox restoration is excluded.
            HandleSoulReward(new SoulReward { SoulKey = "divine", Amount = amuletConfig.PassOneTimeDivineSouls });
            App.ExecuteCommand(new SendSoulPassDailyMailCommand());
		}

		private void HandleCrystalPassReward(CrystalPassReward reward)
		{
			userModel.CrystalPassPurchased.Value = true;
			userModel.CrystalPassPurchaseTime.Value = new DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds();
			audioPlayer.PlaySoundOnce("pass_claim");
		}

		private void HandleFreeBoosterToken(FreeBoosterTokenReward reward)
		{
			userModel.AddFreeBoosterToken(reward.productId);
		}

		private void HandleLevelPassReward(LevelPassReward reward)
		{
			levelPass.MarkPassPurchased(reward.productId);
			audioPlayer.PlaySoundOnce("pass_claim");
		}

		private void HandleSoulReward(SoulReward reward)
		{
			amuletModel.AddSouls(reward.SoulKey, reward.Amount, false);
		}

		private void HandleItemSlotReward(ItemSlotReward reward)
		{
			var slot = playerModel.ItemSlots.FirstOrDefault(s => s.Key == reward.SlotKey);
			if (slot != null && !slot.IsUnlocked)
				slot.IsUnlocked = true;
		}

		private void HandleMineUnlockReward(MineUnlockReward reward)
		{
			if (!minesModel.IsUnlocked)
				minesModel.Unlock();
		}

		private void HandleChaoticSoulOfferReward(ChaoticSoulOfferReward reward)
		{
			amuletModel.AddSouls("chaotic", 1.0, false);
			userModel.ChaoticSoulOfferPurchased.Value = true;
			userModel.ChaoticSoulOfferPurchaseTime.Value = new DateTimeOffset(timeService.Now.ToDateTime()).ToUnixTimeSeconds();
			audioPlayer.PlaySoundOnce("graph_unregular_buy");
		}

		private void HandleEventCurrencyReward(EventCurrencyReward reward)
		{
			var current = eventsRepository.CurrentEvent;
			if (current != null)
				current.AddCurrency(reward.Key, reward.Amount);
		}

		private void HandleCurrencyReward(CurrencyReward reward)
		{
			currencyModel.Add(reward.Key, reward.Amount);
		}

		private void HandleStatReward(StatReward reward)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return;
			StatType type = reward.Type;
			float value = reward.Value;
			var completed = eventsRepository.CompletedEvent;
			bool resolveDynamic = false;
			if (completed != null && !completed.IsProgressComplete)
			{
				if (reward.BakedData == null && (reward.IsRandom || reward.UseGpm) && !string.IsNullOrEmpty(reward.SaveKey) && BakedRewardStorage.Has(reward.SaveKey))
					reward.BakedData = BakedRewardStorage.Load(reward.SaveKey);
				if (reward.BakedData != null)
				{
					type = reward.BakedData.Type;
					value = reward.BakedData.Value;
				}
				else
					resolveDynamic = true;
			}
			else if ((reward.IsRandom || reward.UseGpm) && !string.IsNullOrEmpty(reward.SaveKey))
			{
				if (BakedRewardStorage.Has(reward.SaveKey))
				{
					var baked = BakedRewardStorage.Load(reward.SaveKey);
					if (baked != null)
					{
						type = baked.Type;
						value = baked.Value;
					}
				}
			}
			else
				resolveDynamic = true;
			if (resolveDynamic)
			{
				if (reward.IsRandom)
					type = randomStats[UnityEngine.Random.Range(0, randomStats.Count)];
				if (reward.UseGpm)
					value *= (float)simulationSystem.RPMCalculatedValue[type];
			}

			value *= cheatsModel.DropMultiplier;
			if (value <= 0f)
				return;
			var modifier = new StatModifier
			{
				StatType = type,
				Value = value,
				Param = 0,
				Name = reward.CustomModifier ? reward.Name : sourcePath,
				ModifierType = reward.CustomModifier ? reward.ModifierType : ModifierType.Flat,
				Source = reward.CustomModifier ? reward.Source : (sourceOverride ?? ModifierSource.Reward)
			};
			Stat stat = playerModel.Unit.Stats.First(s => s.Type == modifier.StatType);
			stat.AddModifier(modifier);
			cashbackModel.AddEarnedStat(modifier);
			bool percent = modifier.ModifierType == ModifierType.PercentAdditive || modifier.ModifierType == ModifierType.PercentMultiplicative || modifier.ModifierType == ModifierType.PreAdditivePostMultiplicative || modifier.ModifierType == ModifierType.PostFormulaMultiplicative;
			bool eventPercent = modifier.ModifierType == ModifierType.EventPercentModifier;
			float display = percent ? modifier.Value * 100f : modifier.Value;
			string formatted = ((double)display).Format();
			if (!percent && !eventPercent && stat.Type == StatType.Health)
				playerModel.Unit.CurrentHealth += modifier.Value;
			StatIconStyle style = iconStyle.Styles.First(s => s.StatType == modifier.StatType);
			statRewardContainer.Create(string.Concat(eventPercent ? "<color=yellow>" : "<color=green>", "+", formatted, percent || eventPercent ? "%" : string.Empty, "<sprite name=", style.Sprite.name, ">"), false);
		}

		private void HandleChestReward(ChestReward reward)
		{
			var chest = chestsConfig.Get(reward.Key);
			if (chest == null)
			{
				UnityEngine.Debug.LogError("Chest with key " + reward.Key + " not found");
				return;
			}

			chestQueue.EnqueueChest(chest, reward.Amount);
			App.RequestAppUpdateFlow();
		}

		private void HandleItemReward(ItemReward reward)
		{
			ItemConfig def;
			if (!reward.IsRandom)
				def = reward.Def;
			else
			{
				if (reward.BakedData != null && !string.IsNullOrEmpty(reward.BakedData.ItemKey))
					def = itemsProvider.Get(reward.BakedData.ItemKey);
				else if (!string.IsNullOrEmpty(reward.SaveKey) && BakedRewardStorage.HasItem(reward.SaveKey))
				{
					reward.BakedData = BakedRewardStorage.LoadItem(reward.SaveKey);
					def = itemsProvider.Get(reward.BakedData.ItemKey);
				}
				else
				{
					var candidates = itemsProvider.Items.Where(item => ItemsProvider.IsAvailable(item) && item.SlotType == reward.SlotType && item.Rarity == reward.Rarity).ToList();
					if (candidates.Count == 0)
						return;
					def = candidates[UnityEngine.Random.Range(0, candidates.Count)];
				}
			}

			if (def != null)
				playerModel.CreateItem(def, reward.Level);
		}

		private void HandleEventReward(EventReward reward)
		{
			var current = eventsRepository.CurrentEvent;
			if (current != null)
				current.ExtraDuration += reward.ExtraDurationMinutes;
		}

		private void HandleGemsReward(GemsReward reward)
		{
			playerModel.Gems += reward.Gems;
		}

		private void HandleVipPointsReward(VipPointsReward reward)
		{
			userModel.InAppsBoughtValueInRewarded.Value += reward.Amount;
		}

		private void HandleBundleReward(BundleReward reward)
		{
			for (int index = 0; index < reward.Rewards.Length; index++)
				HandleReward(reward.Rewards[index]);
		}
	}
}
