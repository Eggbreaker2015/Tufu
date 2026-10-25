using System.Linq;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Meta;
using Codebase.Meta.Upgrades;
using Codebase.Unit;
using UniMob;
using UniMob.Core;

namespace Codebase.Amulet.Upgrades
{
	public class AmuletUpgradesModel : Model<AmuletUd>
	{
		private readonly struct CachedSoulDropUnlock
		{
			public readonly MetaStat Stat;

			public readonly int ModifierVersion;

			public readonly bool IsUnlocked;

			public CachedSoulDropUnlock(MetaStat stat, bool isUnlocked)
			{
				Stat = stat;
				ModifierVersion = stat.ModifierVersion;
				IsUnlocked = isUnlocked;
			}
		}

		[Inject]
		private AmuletUpgradeConfig config;

		[Inject]
		private AmuletConfig amuletConfig;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private MetaStatsModel metaStatsModel;

		private readonly Dictionary<EnemySpawnerConfig, CachedSoulDropUnlock> soulDropUnlocks;

		private ComputedAtom<int> __Version;

		private ComputedAtom<int> __PurchasableNodeCount;

		public UpgradeModel Upgrades { get; private set; }

		public int Version
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__Version == null) __Version = CodeGenAtom.CreatePooled(this,null,()=>Version,false);
                return __Version.DirectEvaluate() ? (Upgrades?.Version ?? 0) : __Version.Value;
            }
        }

		public int PurchasableNodeCount
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__PurchasableNodeCount == null) __PurchasableNodeCount = CodeGenAtom.CreatePooled(this,null,()=>PurchasableNodeCount,false);
                if (!__PurchasableNodeCount.DirectEvaluate()) return __PurchasableNodeCount.Value;
                _ = Version;
                _ = amuletModel.Level;
                foreach (string key in amuletModel.Souls) _ = amuletModel.GetSouls(key);
                return CountPurchasableNodes();
            }
        }

		public AmuletUpgradesModel(Lifetime lifetime)
			: base(lifetime)
		{
soulDropUnlocks = new Dictionary<EnemySpawnerConfig,CachedSoulDropUnlock>();
		}

		public void Populate()
		{
// Native ELF VA 0x15b9bd0: use the original definition builder and saved level dictionary.
            List<UpgradeDef> definitions = AmuletUpgradeDefsBuilder.Build();
            foreach (UpgradeDef definition in definitions)
            {
                if (amuletConfig.UpgradeCostMultipliers.TryGetValue(definition.Key,out float cost)) definition.WithCostMultiplier(cost);
                if (amuletConfig.StatModifierMultipliers.TryGetValue(definition.Key,out float stat)) definition.WithStatModifierMultiplier(stat);
            }
            Upgrades = new UpgradeModel(Lifetime,ModifierSource.GraphUpgrade);
            Injector.InjectInto(Upgrades);
            Upgrades.Setup(definitions,Data.UpgradeLevels,metaStatsModel.All);
            PruneSeenUpgradeKeys();
		}

		private void PruneSeenUpgradeKeys()
		{
// Native ELF VA 0x15b9f00 prunes seen keys already purchased, not arbitrary unknown keys.
            foreach (string key in new List<string>(Data.SeenUpgradeKeys.AsList()))
                if ((Upgrades?.GetLevel(key) ?? 0) > 0) Data.SeenUpgradeKeys.Remove(key);
		}

		public UpgradeDef GetDef(string upgradeKey)
		{
return Upgrades?.GetDef(upgradeKey);
		}

		public int GetLevel(string upgradeKey)
		{
return Upgrades?.GetLevel(upgradeKey) ?? 0;
		}

		public int GetMaxLevel(string upgradeKey)
		{
return GetDef(upgradeKey)?.MaxLevel ?? 0;
		}

		public bool IsMaxLevel(string upgradeKey)
		{
return Upgrades?.IsMaxLevel(upgradeKey) ?? true;
		}

		public bool IsUnlocked(string upgradeKey)
		{
return GetLevel(upgradeKey) > 0;
		}

		public string FindUpgradeKeyByModifier(string statKey, int param)
		{
if (Upgrades == null) return null;
            foreach (UpgradeDef definition in Upgrades.Defs)
                foreach (UpgradeModifierDef modifier in definition.Modifiers)
                    if (modifier.StatKey == statKey && modifier.Param == param) return definition.Key;
            return null;
		}

		public bool IsSoulsDropUnlocked(EnemySpawner spawner)
		{
return IsSoulsDropUnlocked(spawner.Config);
		}

		public bool IsSoulsDropUnlocked(EnemySpawnerConfig config)
		{
// Original ELF VA 0x15ba79c: cache is invalidated by the actual MetaStat modifier version.
            if (config.SoulAlwaysAvailable) return true;
            if (soulDropUnlocks.TryGetValue(config,out var cached) && cached.ModifierVersion == cached.Stat.ModifierVersion) return cached.IsUnlocked;
            MetaStat stat = metaStatsModel.Get(config.IsStatic ? "feature_unlock" : "souls_drop_unlock");
            bool unlocked = stat.GetValue(config.IsStatic ? "static_souls_drop" : config.SoulReward) > 0.0;
            soulDropUnlocks[config] = new CachedSoulDropUnlock(stat,unlocked);
            return unlocked;
		}

		public bool IsPurchasable(string upgradeKey)
		{
return IsAvailable(upgradeKey) && CanAfford(upgradeKey) && (GetLevel(upgradeKey) > 0 || !IsUnknown(upgradeKey));
		}

		private bool IsUnknown(string upgradeKey)
		{
AmuletUpgradeDef definition = config.GetUpgrade(upgradeKey);
            if (definition == null) return true;
            if (definition.RequiredUpgradeKeys == null || definition.RequiredUpgradeKeys.Count == 0) return false;
            foreach (string parent in definition.RequiredUpgradeKeys) if (GetLevel(parent) > 0) return false;
            return true;
		}

		private int CountPurchasableNodes()
		{
int count = 0;
            foreach (AmuletUpgradeDef definition in config.Upgrades) if (IsPurchasable(definition.Key)) count++;
            return count;
		}

		public bool IsAvailable(string upgradeKey)
		{
AmuletUpgradeDef definition = config.GetUpgrade(upgradeKey);
            if (definition == null || Upgrades == null || Upgrades.IsMaxLevel(upgradeKey) || amuletModel.Level < definition.RequiredAmuletLevel) return false;
            if (definition.RequiredUpgradeKeys != null)
                foreach (string parent in definition.RequiredUpgradeKeys) if (Upgrades.GetLevel(parent) < 1) return false;
            return true;
		}

		public bool IsAmuletLevelLocked(string upgradeKey)
		{
AmuletUpgradeDef definition = config.GetUpgrade(upgradeKey);
            return definition != null && amuletModel.Level < definition.RequiredAmuletLevel;
		}

		public bool CanAfford(string upgradeKey)
		{
return Upgrades?.CanAfford(upgradeKey) ?? false;
		}

		public bool TryUpgrade(string upgradeKey, bool forFree = false)
		{
// Native ELF VA 0x15bae84: only paid soul cost grants actual upgrade XP.
            if (!IsAvailable(upgradeKey)) return false;
            UpgradeCostInstance? soulCost = forFree ? null : GetNextSoulCost(upgradeKey);
            if (Upgrades == null || !Upgrades.TryUpgrade(upgradeKey,forFree)) return false;
            Data.SeenUpgradeKeys.Remove(upgradeKey);
            if (soulCost.HasValue) amuletModel.AddUpgradeExperience(soulCost.Value.CurrencyKey.Substring("soul.".Length),soulCost.Value.Amount);
            return true;
		}

		public List<UpgradeCostInstance> GetNextCost(string upgradeKey)
		{
return Upgrades?.GetNextCost(upgradeKey);
		}

		public UpgradeCostInstance? GetNextSoulCost(string upgradeKey)
		{
List<UpgradeCostInstance> costs = GetNextCost(upgradeKey);
            if (costs == null) return null;
            foreach (UpgradeCostInstance cost in costs)
                if (!string.IsNullOrEmpty(cost.CurrencyKey) && cost.Amount > 0.0 && cost.CurrencyKey.StartsWith("soul.")) return cost;
            return null;
		}

		public double GetNextUpgradeExperience(string upgradeKey)
		{
UpgradeCostInstance? cost = GetNextSoulCost(upgradeKey);
            return cost.HasValue ? amuletConfig.GetBaseExperience(cost.Value.CurrencyKey.Substring("soul.".Length)) * cost.Value.Amount : 0.0;
		}

		public string GetNextSoulKey(string upgradeKey)
		{
UpgradeCostInstance? cost = GetNextSoulCost(upgradeKey);
            return cost.HasValue ? cost.Value.CurrencyKey.Substring("soul.".Length) : null;
		}

		public double GetCurrentModifierValue(string upgradeKey, int modifierIndex)
		{
return Upgrades?.GetCurrentModifierValue(upgradeKey,modifierIndex) ?? 0.0;
		}

		public double GetNextModifierValue(string upgradeKey, int modifierIndex)
		{
return Upgrades?.GetNextModifierValue(upgradeKey,modifierIndex) ?? 0.0;
		}

		public double GetTotalModifierValue(string upgradeKey, int modifierIndex)
		{
return Upgrades?.GetTotalModifierValue(upgradeKey,modifierIndex) ?? 0.0;
		}

		public double GetNextTotalModifierValue(string upgradeKey, int modifierIndex)
		{
return Upgrades?.GetNextTotalModifierValue(upgradeKey,modifierIndex) ?? 0.0;
		}

		public double GetTotalModifierValueAtLevel(string upgradeKey, int modifierIndex, int targetLevel)
		{
return Upgrades?.GetTotalModifierValueAtLevel(upgradeKey,modifierIndex,targetLevel) ?? 0.0;
		}

		public double GetStatModifierValueAtLevel(string upgradeKey, int modifierIndex, int targetLevel)
		{
return Upgrades?.GetStatModifierValueAtLevel(upgradeKey,modifierIndex,targetLevel) ?? 0.0;
		}

		public double GetModifierValueAtLevel(string upgradeKey, int modifierIndex, int targetLevel)
		{
return Upgrades?.GetModifierValueAtLevel(upgradeKey,modifierIndex,targetLevel) ?? 0.0;
		}

		public List<StatModifier> CollectStatModifiers()
		{
return Upgrades?.CollectStatModifiers() ?? new List<StatModifier>();
		}

		public bool IsNewNode(string upgradeKey)
		{
return IsAvailable(upgradeKey) && GetLevel(upgradeKey) <= 0 && !Data.SeenUpgradeKeys.Contains(upgradeKey);
		}

		public void MarkNodeSeen(string upgradeKey)
		{
if (GetLevel(upgradeKey) > 0 || !IsAvailable(upgradeKey) || Data.SeenUpgradeKeys.Contains(upgradeKey)) return;
            Data.SeenUpgradeKeys.Add(upgradeKey);
		}
	}
}
