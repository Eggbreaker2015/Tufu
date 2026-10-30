using System.Collections.Generic;
using Scellecs.Morpeh;

namespace Codebase.Core
{
	public class SystemsInstaller
	{
		private readonly SystemsGroup systemsGroup;

		private readonly List<ISystem> systems;

		public SystemsInstaller(SystemsGroup systemsGroup)
		{
			this.systemsGroup = systemsGroup;
			systems = new List<ISystem>();
		}

		public List<ISystem> Install()
		{
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemyVisibilitySystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemyUnitTickSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemyDeathSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemyRegenSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemySpawnerCooldownSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemySpawnerCooldownBarSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemyHealthBarSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemySoulSystem());
			RegisterSystem(new global::Codebase.Crystals.Objective.Systems.CrystalObjectiveBannerSystem());
			RegisterSystem(new global::Codebase.Crystals.Objective.Systems.CrystalObjectivePoiHighlightSystem());
			RegisterSystem(new global::Codebase.Crystals.Objective.Systems.CrystalUnitSystem());
			RegisterSystem(new global::Codebase.Crystals.Objective.Systems.CrystalUnitHitVfxSystem());
			RegisterSystem(new global::Codebase.Events.Systems.EventBossGateSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EventBossProgressBarSystem());
			RegisterSystem(new global::Codebase.Events.Systems.EventMiniBossSpawnSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EventMobDespawnSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemySpawnerSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EventSpawnerControlSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemySpawnerBootstrapSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemySpawnSystem());
			RegisterSystem(new global::Codebase.Location.Systems.IslandMasterySystem());
			RegisterSystem(new global::Codebase.Location.Systems.IslandMasteryPopupSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.BossArenaSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.HydraDeathSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.HydraPendingSpawnSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.AssassinIllusionSystem());
			RegisterSystem(new global::Codebase.Skills.Systems.SkillAutoCastSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.EnemyCastSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.LavaCrabExplosionSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.FireElementalAreaSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.BoneEffectSlotsSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.VampireSpiritSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.MinionHealthBarSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.BossHealthBarSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.VikingsDeathSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.VikingsBuffSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.VikingsRageDebuffSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.VikingsGhostSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.LichSkeletonSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.LichSkeletonDeathSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.MageBossSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.BerserkerBossSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.UnitHealthSyncSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.BossTowerTicketsRestoreSystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.BossRaidSystem());
			RegisterSystem(new global::Codebase.Player.Systems.UnstackSystem());
			RegisterSystem(new global::Codebase.Analytics.Systems.AppPauseTimeSkipSystem());
			RegisterSystem(new global::Codebase.Skills.Systems.SkillBuffSystem());
			RegisterSystem(new global::Codebase.Unit.Systems.UnitBuffSystem());
			RegisterSystem(new global::Codebase.Unit.Systems.UnitDamageSystem());
			RegisterSystem(new global::Codebase.Enemy.Systems.CreeperExplosionSystem());
			RegisterSystem(new global::Codebase.Unit.Systems.UnitSpawnAnimSystem());
			RegisterSystem(new global::Codebase.Unit.Systems.UnitDeathAnimSystem());
			RegisterSystem(new global::Codebase.Abilities.Systems.AbilityExecuteEventSystem());
			RegisterSystem(new global::Codebase.Pet.Systems.PetCollectionBonusSystem());
			RegisterSystem(new global::Codebase.Rpg.Systems.RpgHeroSpawnSystem());
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) RegisterSystem(new global::Codebase.Rpg.Systems.RpgLevelSystem());
			RegisterSystem(new global::Codebase.Pet.Systems.PetSpawnSystem());
			RegisterSystem(new global::Codebase.Pet.Systems.PetStatsSyncSystem());
			RegisterSystem(new global::Codebase.Pet.Systems.PetMovementSystem());
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) RegisterSystem(new global::Codebase.Rpg.Systems.ProtagonistAiSystem());
			RegisterSystem(new global::Codebase.Pet.Systems.PetAttackSystem());
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) RegisterSystem(new global::Codebase.Rpg.Systems.HeroControlSystem());
			RegisterSystem(new global::Codebase.Pet.Systems.PetReviveSystem());
			RegisterSystem(new global::Codebase.Pet.Systems.PetRegenSystem());
			RegisterSystem(new global::Codebase.Pet.Systems.PetHealthBarSystem());
			RegisterSystem(new global::Codebase.Abilities.Vfx.Systems.AbilityVfxSystem());
			RegisterSystem(new global::Codebase.Vfx.WorldVfxSystem());
			RegisterSystem(new global::Codebase.Abilities.Vfx.Systems.ShockVfxSystem());
			RegisterSystem(new global::Codebase.Abilities.Vfx.Systems.StunVfxSystem());
			RegisterSystem(new global::Codebase.Abilities.Systems.PiercingWeaponMarkSystem());
			RegisterSystem(new global::Codebase.Abilities.Systems.MarkDecaySystem());
			RegisterSystem(new global::Codebase.BossTower.Systems.HydraMarkSyncSystem());
			RegisterSystem(new global::Codebase.Abilities.Systems.ConsumedShockDecaySystem());
			RegisterSystem(new global::Codebase.Player.Systems.RageExtraHealthSystem());
			RegisterSystem(new global::Codebase.Player.Systems.RageExtraHitSystem());
			RegisterSystem(new global::Codebase.Player.Systems.HammerRegenSystem());
			RegisterSystem(new global::Codebase.Player.Systems.PlayerRegenSystem());
			RegisterSystem(new global::Codebase.Player.Systems.PlayerHitsTakenDecaySystem());
			RegisterSystem(new global::Codebase.Particles.Systems.ParticlesVisibilitySystem());
			RegisterSystem(new global::Codebase.ProximityView.Systems.ProximityViewDistanceSystem());
			RegisterSystem(new global::Codebase.ProximityView.Systems.ProximityViewDisplaySystem());
			RegisterSystem(new global::Codebase.SecretChest.Systems.SecretChestSpawnSystem());
			RegisterSystem(new global::Codebase.SecretChest.Systems.SecretChestProximitySystem());
			RegisterSystem(new global::Codebase.SecretChest.Systems.SecretChestPromptSystem());
			RegisterSystem(new global::Codebase.Quests.Systems.QuestStateRestoreSystem());
			RegisterSystem(new global::Codebase.Quests.Systems.QuestPrologueWorldEnsureSystem());
			RegisterSystem(new global::Codebase.Quests.Systems.QuestRoadVisibilitySystem());
			RegisterSystem(new global::Codebase.Quests.Systems.QuestNpcMarkerSystem());
			RegisterSystem(new global::Codebase.Quests.Systems.QuestNpcProximitySystem());
			RegisterSystem(new global::Codebase.Quests.Systems.QuestPopupSystem());
			RegisterSystem(new global::Codebase.Quests.Systems.QuestSpawnerControlSystem());
			RegisterSystem(new global::Codebase.Quests.Systems.QuestMobCameraSystem());
			RegisterSystem(new global::Codebase.Quests.Systems.QuestMiniMapPoiSystem());
			RegisterSystem(new global::Codebase.Player.Systems.PlayerRewardSystem());
			RegisterSystem(new global::Codebase.Player.Systems.PlayerPathIndicatorSystem());
			RegisterSystem(new global::Codebase.Player.Distance.PlayerDistanceTrackerSystem());
			RegisterSystem(new global::Codebase.Map.Systems.StaticMiniMapPoiSystem());
			RegisterSystem(new global::Codebase.Map.Systems.DynamicMinimapPoiSystem());
			RegisterSystem(new global::Codebase.Unit.Systems.NavMeshSyncSystem());
			RegisterSystem(new global::Codebase.Player.Systems.PlayerNavMeshFallbackSystem());
			RegisterSystem(new global::Codebase.Vibration.Systems.HapticSystem());
			RegisterSystem(new global::Codebase.Player.Systems.QualitySystem());
			RegisterSystem(new global::Codebase.UI.RateUs.RateUsSystem());
			RegisterSystem(new global::Codebase.Analytics.Systems.AfkPlaytimeSystem());
			RegisterSystem(new global::Codebase.Shop.Systems.ShopRepositorySystem());
			RegisterSystem(new global::Codebase.Shop.Systems.ShopLotRpmCalculationSystem());
			RegisterSystem(new global::Codebase.Shop.Systems.ShopOfferTimerSystem());
			RegisterSystem(new global::Codebase.VIP.Systems.VipLevelUpSystem());
			RegisterSystem(new global::Codebase.VIP.Systems.VipLocationNotificationSystem());
			RegisterSystem(new global::Codebase.VIP.Systems.VipProgressPopupSystem());
			RegisterSystem(new global::Codebase.Simulation.Systems.SimulationSystem());
			RegisterSystem(new global::Codebase.Cashback.Systems.CashbackSystem());
			RegisterSystem(new global::Codebase.Analytics.Systems.SnapshotStatsSystem());
			RegisterSystem(new global::Codebase.Events.Systems.EventAnalyticsHeartbeatSystem());
			RegisterSystem(new global::Codebase.Events.Systems.EventAnalyticsPlaytimeSystem());
			RegisterSystem(new global::Codebase.Events.Systems.EventTimerSystem());
			RegisterSystem(new global::Codebase.ABTests.Systems.EnemyHpDamageMultSystem());
			RegisterSystem(new global::Codebase.Crystals.Systems.CrystalPassBonusSystem());
			RegisterSystem(new global::Codebase.Crystals.Systems.GeodeTimerSystem());
			RegisterSystem(new global::Codebase.Crystals.Systems.CrystalEnchantExtraAttemptVipSystem());
			RegisterSystem(new global::Codebase.Meta.Systems.UpgradeMaxCapacityBonusSystem());
			RegisterSystem(new global::Codebase.SoulPass.Systems.SoulPassDailyMailSystem());
			RegisterSystem(new global::Codebase.Amulet.Upgrades.UpgradeStatApplySystem());
			RegisterSystem(new global::Codebase.Amulet.Systems.AmuletLevelUpSystem());
			RegisterSystem(new global::Codebase.Amulet.Systems.AmuletProgressPopupSystem());
			RegisterSystem(new global::Codebase.DebugTools.FpsCounterSystem());
			RegisterSystem(new global::Codebase.DebugTools.Dummy.DummyDamageTrackingSystem());
			RegisterSystem(new global::Codebase.Player.Systems.PlayerReviveSystem());
			return systems;
		}

		private void RegisterSystem<T>(T system) where T : class, ISystem
		{
			systemsGroup.AddSystem(system, true);
			GameContext.Set(system);
			systems.Add(system);
		}
	}
}
