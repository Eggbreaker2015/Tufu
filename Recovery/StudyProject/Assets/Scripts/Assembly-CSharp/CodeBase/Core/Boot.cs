using System;
using System.Collections.Generic;
using Codebase.Core.Commands;
using Codebase.Core.Flows;
using Codebase.Core.UserData;
using Codebase.Player.Models;
using Codebase.UI.Inventory;
using Cysharp.Threading.Tasks;
using Scellecs.Morpeh;
using UniMob;
using UnityEngine;

namespace Codebase.Core
{
    [DefaultExecutionOrder(-1000)]
    public class Boot : LifetimeMonoBehaviour
    {
        [SerializeField] private ConfigsInitializer configsInitializer;
        [SerializeField] private InventoryView inventoryView;
        private List<ISystem> systems;
        private List<Model> models;
        private readonly LifetimeController lifetimeController = new LifetimeController();

        protected override void Start()
        {
            base.Start();
            UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = 200;
            App.Startup(lifetimeController);
            GameContext.Set(new global::Codebase.Enemy.EnemiesModel());
            GameContext.Set(new global::Codebase.ABTests.ABTestService());
            GameContext.Set(new global::Codebase.Integrations.Ads.GemsHudOfferModel());
            GameContext.Set<global::Codebase.Services.Time.ITimeService>(new global::Codebase.Services.Time.TimeService(Lifetime));
            GameContext.Set<global::Codebase.Notifications.INotificationService>(new global::Codebase.Notifications.LocalNotificationService());
            configsInitializer.Initialize();
            GameContext.Set(inventoryView);
            models = new List<Model>();
            var playerConfig = GameContext.Get<global::Codebase.Player.PlayerConfig>();
            RegisterModel(new global::Codebase.Economy.CurrencyModel(Lifetime));
            RegisterModel(new global::Codebase.Player.Models.PlayerModel(Lifetime, playerConfig));
            RegisterModel(new global::Codebase.Pet.PetsModel(Lifetime));
            RegisterModel(new global::Codebase.Rpg.HeroRosterModel(Lifetime));
            GameContext.Set(new global::Codebase.Rpg.HeroDeathService());
            RegisterModel(new global::Codebase.Rpg.RpgProgressModel(Lifetime));
            RegisterModel(new global::Codebase.Analytics.UserModel(Lifetime));
            RegisterModel(new global::Codebase.Gates.GatesModel(Lifetime));
            RegisterModel(new global::Codebase.Services.Cheats.CheatsModel(Lifetime));
            RegisterModel(new global::Codebase.Cashback.CashbackModel(Lifetime));
            RegisterModel(new global::Codebase.Events.Analytics.EventAnalyticsModel(Lifetime));
            RegisterModel(new global::Codebase.VIP.VipModel(Lifetime));
            RegisterModel(new global::Codebase.Shop.ChestQueueModel(Lifetime));
            RegisterModel(new global::Codebase.Shop.ChestsModel(Lifetime));
            RegisterModel(new global::Codebase.LevelPass.LevelPassModel(Lifetime));
            RegisterModel(new global::Codebase.Amulet.AmuletModel(Lifetime));
            RegisterModel(new global::Codebase.Amulet.Upgrades.AmuletUpgradesModel(Lifetime));
            RegisterModel(new global::Codebase.BossTower.BossTowerModel(Lifetime));
            RegisterModel(new global::Codebase.Meta.MetaStatsModel(Lifetime));
            RegisterModel(new global::Codebase.Crystals.CrystalModel(Lifetime));
            RegisterModel(new global::Codebase.Location.IslandMasteryModel(Lifetime));
            RegisterModel(new global::Codebase.Rage.RageModel(Lifetime));
            RegisterModel(new global::Codebase.UI.UiModel(Lifetime));
            RegisterModel(new global::Codebase.Player.Distance.DistanceTraveledModel(Lifetime));
            RegisterModel(new global::Codebase.Player.DailyProcs.DailyProcsModel(Lifetime));
            RegisterModel(new global::Codebase.SecretChest.SecretChestModel(Lifetime));
            RegisterModel(new global::Codebase.Quests.QuestModel(Lifetime));
            RegisterModel(new global::Codebase.Mines.MinesModel(Lifetime));
            RegisterModel(new global::Codebase.Notifications.NotificationsModel(Lifetime));
            RegisterModel(new global::Codebase.PromoCodes.PromoCodesModel(Lifetime));
            var disposer = Multicast.Disposer.WhileAppRunning;
            var rewardsInbox = new Multicast.Backend.BackendRewardInboxService(disposer);
            GameContext.Set<Multicast.RewardsInbox.IRewardsInboxService>(rewardsInbox);
            // Learning adaptation: all purchase screens use the same local sandbox payment service.
            var payments = new global::Codebase.Services.Purchases.StudyPaymentsService(GameContext.Get<global::Codebase.In_apps.UnityIAPManager>());
            payments.Initialize();
            GameContext.Set<Multicast.Payments.IPaymentsService>(payments);
            var iapManager = new global::Codebase.In_apps.IAPManager(GameContext.Get<global::Codebase.In_apps.UnityIAPManager>(), payments);
            GameContext.Set(iapManager);
            var rewardIconResolver = new global::Codebase.Rewards.RewardIconResolver();
            Injector.InjectInto(rewardIconResolver);
            GameContext.Set(rewardIconResolver);
            GameContext.Set(new FlowFactory());
            GameContext.Set(new CommandFactory());
            GameContext.Set(new global::Codebase.Abilities.Vfx.AbilityVfxService());
            GameContext.Set(new global::Codebase.Vfx.WorldVfxService());
            GameContext.Set(new global::Codebase.Skills.SkillService());
            GameContext.Set(new global::Codebase.Unit.StatModifierPool());
            var shop = new global::Codebase.Shop.ShopLotRepository(Lifetime);
            RegisterModel(shop);
            var events = new global::Codebase.Events.Models.EventsRepository(Lifetime);
            RegisterModel(events);
            var mails = new global::Codebase.Mail.MailsRepository(Lifetime);
            RegisterModel(mails);
            var purchaseProcessing = new global::Codebase.Services.Purchases.PurchaseProcessingService(GameContext.Get<global::Codebase.UI.Shop.ShopSectionsConfig>(), GameContext.Get<global::Codebase.Events.EventsProvider>(), GameContext.Get<global::Codebase.LevelPass.LevelPassConfig>(), iapManager, GameContext.Get<global::Codebase.Player.IconStyleConfig>(), payments);
            GameContext.Set(purchaseProcessing);
            GameContext.Set(new global::Codebase.Services.Purchases.PurchasePopupController(purchaseProcessing));
            InitializeSystems();
            var bossSpawn = new global::Codebase.BossTower.BossSpawnService();
            GameContext.Set(bossSpawn);
            var bossMinion = new global::Codebase.BossTower.BossMinionService();
            GameContext.Set(bossMinion);
            var questMobs = new global::Codebase.Quests.QuestMobSpawnerService();
            GameContext.Set(questMobs);
            App.ExecuteCommand(new global::Codebase.Player.Commands.MigrateItemsCommand());
            InjectWorld();
            InjectModels();
            Injector.InjectInto(GameContext.Get<global::Codebase.ABTests.ABTestService>());
            Injector.InjectScene();
            shop.Populate();
            shop.PopulateFrom(GameContext.Get<global::Codebase.UI.Shop.AmuletShopSectionsConfig>());
            shop.PopulateFrom(GameContext.Get<global::Codebase.UI.Shop.ChestShopSectionsConfig>());
            shop.PopulateFrom(GameContext.Get<global::Codebase.UI.Shop.SoulGraphShopSectionsConfig>());
            events.Populate();
            mails.Populate();
            GameContext.Get<global::Codebase.BossTower.BossTowerModel>().Populate();
            GameContext.Get<global::Codebase.Meta.MetaStatsModel>().Populate();
            GameContext.Get<global::Codebase.Amulet.Upgrades.AmuletUpgradesModel>().Populate();
            GameContext.Get<global::Codebase.Crystals.CrystalModel>().Populate();
            GameContext.Get<global::Codebase.Shop.ChestsModel>().Populate();
            Injector.InjectInto(bossSpawn);
            Injector.InjectInto(bossMinion);
            Injector.InjectInto(questMobs);
            GameContext.Get<global::Codebase.Notifications.NotificationsModel>().InitializeNotifications();
            App.ExecuteCommand(new global::Codebase.BossTower.Commands.RebakeBossTowerRewardsCommand());
            GameContext.Get<global::Codebase.Analytics.BootTimeReporter>().FlushFinalTick();
            GameContext.Get<global::Codebase.Integrations.AppMetrica.AppMetricaAnalyticsWrapper>().Send(new global::Codebase.Analytics.Events.GameLoadedEvent());
            PlayerPrefs.SetInt("game_loaded_once", 1);
            App.RequestAppUpdateFlow();
            App.InitiateFlow(new AppStartupFlow()).Forget();
        }

        private void InitializeSystems()
        {
            var world = World.Default;
            var group = world.CreateSystemsGroup();
            systems = new SystemsInstaller(group).Install();
            world.AddSystemsGroup(0, group);
            world.UpdateByUnity = true;
            GameContext.Set(world);
        }

        private void RegisterModel<T>(T model) where T : Model
        {
            GameContext.Set(model);
            models.Add(model);
        }

        private void InjectWorld()
        {
            foreach (var system in systems) Injector.InjectInto(system);
        }

        private void InjectModels()
        {
            foreach (var model in models)
            {
                Injector.InjectInto(model);
                model.Initialize();
            }
            GameContext.Get<GameDataService>().OnBeforeSave += GameContext.Get<PlayerModel>().SaveToUserData;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Application.isEditor) lifetimeController.Dispose();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            try
            {
                if (pauseStatus) GameContext.Get<global::Codebase.Notifications.NotificationsModel>().RescheduleAll();
                else GameContext.Get<global::Codebase.Notifications.NotificationsModel>().CancelAll();
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (pauseStatus) GameContext.Get<GameDataService>().ForceSave();
            }
        }
    }
}
