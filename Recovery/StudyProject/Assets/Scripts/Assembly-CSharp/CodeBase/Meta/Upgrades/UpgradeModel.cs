using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Unit;
using Multicast.UserData;
using UniMob;
using UniMob.Core;

namespace Codebase.Meta.Upgrades
{
    // Original ARM64 methods 0x148a848..0x148d2d4. Native index Ghidra-AmuletModelUpgrades.
    public class UpgradeModel : Model
    {
        [Inject] private CurrencyModel currencyModel;
        private readonly ModifierSource modifierSource;
        private readonly List<(MetaStat stat,StatModifier modifier)> appliedModifiers;
        private List<UpgradeDef> defs;
        private UdDictValue<int> levels;
        private IReadOnlyDictionary<string,MetaStat> metaStats;
        [CompilerGenerated] private int __Version__BackingField;
        private ComputedAtom<int> __Version;
        public int Version
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if(__Version==null)__Version=CodeGenAtom.CreatePooled(this,null,()=>Version,false);
                return __Version.DirectEvaluate()?__Version__BackingField:__Version.Value;
            }
            private set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if(__Version!=null&&!__Version.CompareAndInvalidate(value))return;
                __Version__BackingField=value;
            }
        }
        public IReadOnlyList<UpgradeDef> Defs=>defs;
        public UpgradeModel(Lifetime lifetime,ModifierSource modifierSource=ModifierSource.CrystalUpgrade):base(lifetime)
        {
            appliedModifiers=new List<(MetaStat,StatModifier)>();this.modifierSource=modifierSource;
        }
        public void Setup(List<UpgradeDef> defs,UdDictValue<int> levels,IReadOnlyDictionary<string,MetaStat> metaStats)
        {
            this.defs=defs;this.levels=levels;this.metaStats=metaStats;ApplyStartingLevels();ApplyAll();
        }
        private void ApplyStartingLevels()
        {
            foreach(UpgradeDef def in defs)
                if(def.StartingLevel>0&&(!levels.TryGetValue(def.Key,out int level)||level<def.StartingLevel))levels[def.Key]=def.StartingLevel;
        }
        public UpgradeDef GetDef(string upgradeKey)
        {
            foreach(UpgradeDef def in defs)if(def.Key==upgradeKey)return def;
            return null;
        }
        public int GetLevel(string upgradeKey)=>levels.TryGetValue(upgradeKey,out int level)?level:0;
        public bool IsMaxLevel(string upgradeKey)
        {
            UpgradeDef def=GetDef(upgradeKey);return def==null||GetLevel(upgradeKey)>=def.MaxLevel;
        }
        public List<UpgradeCostInstance> GetNextCost(string upgradeKey)
        {
            UpgradeDef def=GetDef(upgradeKey);if(def==null)return null;
            int next=GetLevel(upgradeKey)+1;return next>def.MaxLevel?null:def.EvaluateCosts(next);
        }
        public bool CanAfford(string upgradeKey)
        {
            List<UpgradeCostInstance> costs=GetNextCost(upgradeKey);if(costs==null)return true;
            foreach(UpgradeCostInstance cost in costs)if(!string.IsNullOrEmpty(cost.CurrencyKey)&&!currencyModel.HasEnough(cost.CurrencyKey,cost.Amount))return false;
            return true;
        }
        public bool TryUpgrade(string upgradeKey,bool forFree=false)
        {
            if(IsMaxLevel(upgradeKey)||(!CanAfford(upgradeKey)&&!forFree))return false;
            List<UpgradeCostInstance> costs=GetNextCost(upgradeKey);
            if(costs!=null&&!forFree)foreach(UpgradeCostInstance cost in costs)if(!string.IsNullOrEmpty(cost.CurrencyKey)&&cost.Amount>0.0)currencyModel.TrySpend(cost.CurrencyKey,cost.Amount);
            levels[upgradeKey]=GetLevel(upgradeKey)+1;ApplyAll();Version++;return true;
        }
        public double GetCurrentModifierValue(string upgradeKey,int modifierIndex)
        {
            UpgradeDef def=GetDef(upgradeKey);if(def==null||modifierIndex<0||modifierIndex>=def.Modifiers.Count)return 0.0;
            int level=GetLevel(upgradeKey);return level>0?def.Modifiers[modifierIndex].Evaluate(level):0.0;
        }
        public double GetNextModifierValue(string upgradeKey,int modifierIndex)
        {
            UpgradeDef def=GetDef(upgradeKey);if(def==null||modifierIndex<0||modifierIndex>=def.Modifiers.Count)return 0.0;
            int next=GetLevel(upgradeKey)+1;return next<=def.MaxLevel?def.Modifiers[modifierIndex].Evaluate(next):0.0;
        }
        public double GetTotalModifierValue(string upgradeKey,int modifierIndex)
        {
            UpgradeDef def=GetDef(upgradeKey);if(def==null||modifierIndex<0||modifierIndex>=def.Modifiers.Count)return 0.0;
            UpgradeModifierDef mod=def.Modifiers[modifierIndex];if(!metaStats.TryGetValue(mod.StatKey,out MetaStat stat))return 0.0;
            double total=0.0;
            foreach(StatModifier applied in stat.Modifiers)
                if(applied.Source==modifierSource&&applied.ModifierType==mod.ModifierType&&(!def.DisplayParam.HasValue||MatchesDisplayParam(applied,def.DisplayParam.Value)))total+=applied.Value;
            return total;
        }
        public double GetNextTotalModifierValue(string upgradeKey,int modifierIndex)
        {
            UpgradeDef def=GetDef(upgradeKey);if(def==null||modifierIndex<0||modifierIndex>=def.Modifiers.Count)return 0.0;
            int next=GetLevel(upgradeKey)+1;
            return next>def.MaxLevel?GetTotalModifierValue(upgradeKey,modifierIndex):GetTotalModifierValueAtLevel(upgradeKey,modifierIndex,next);
        }
        public double GetTotalModifierValueAtLevel(string upgradeKey,int modifierIndex,int targetLevel)
        {
            UpgradeDef def=GetDef(upgradeKey);if(def==null||modifierIndex<0||modifierIndex>=def.Modifiers.Count)return 0.0;
            UpgradeModifierDef mod=def.Modifiers[modifierIndex];int current=GetLevel(upgradeKey);
            return GetTotalModifierValue(upgradeKey,modifierIndex)-(current>0?mod.Evaluate(current):0.0)+(targetLevel>0?mod.Evaluate(targetLevel):0.0);
        }
        public double GetStatModifierValueAtLevel(string upgradeKey,int modifierIndex,int targetLevel)
        {
            UpgradeDef def=GetDef(upgradeKey);if(def==null||modifierIndex<0||modifierIndex>=def.StatModifiers.Count||targetLevel<1)return 0.0;
            return def.StatModifiers[modifierIndex].Evaluate(targetLevel);
        }
        public double GetModifierValueAtLevel(string upgradeKey,int modifierIndex,int targetLevel)
        {
            UpgradeDef def=GetDef(upgradeKey);if(def==null||modifierIndex<0||modifierIndex>=def.Modifiers.Count||targetLevel<1)return 0.0;
            return def.Modifiers[modifierIndex].Evaluate(targetLevel);
        }
        public void ApplyAll()
        {
            foreach(var applied in appliedModifiers)applied.stat.RemoveModifier(applied.modifier);
            appliedModifiers.Clear();
            foreach(UpgradeDef def in defs)
            {
                int level=GetLevel(def.Key);if(level<1)continue;
                foreach(UpgradeModifierDef mod in def.Modifiers)
                {
                    if(!metaStats.TryGetValue(mod.StatKey,out MetaStat stat))continue;
                    var modifier=new StatModifier{Name=GetModifierName(def.Key,mod),ModifierType=mod.ModifierType,Source=modifierSource,Value=(float)mod.Evaluate(level),Param=mod.Param,StringParam=mod.StringParam};
                    stat.AddModifier(modifier);appliedModifiers.Add((stat,modifier));
                }
            }
        }
        public List<StatModifier> CollectStatModifiers()
        {
            var result=new List<StatModifier>();
            foreach(UpgradeDef def in defs)
            {
                int level=GetLevel(def.Key);if(level<1)continue;
                foreach(UpgradeStatModifierDef mod in def.StatModifiers)
                    result.Add(new StatModifier{Name=GetModifierName(def.Key,mod),StatType=mod.StatType,ModifierType=mod.ModifierType,Source=modifierSource,Value=(float)mod.Evaluate(level),Param=mod.Param,StringParam=mod.StringParam});
            }
            return result;
        }
        private static bool MatchesDisplayParam(StatModifier modifier,int displayParam)=>string.IsNullOrEmpty(modifier.StringParam)&&(modifier.Param==0||modifier.Param==displayParam);
        private static string GetModifierName(string upgradeKey,UpgradeModifierDef modDef)
        {
            if(!string.IsNullOrEmpty(modDef.StringParam))return "upgrade_"+upgradeKey+"_"+modDef.StringParam;
            return modDef.Param!=0?string.Format("upgrade_{0}_{1}",upgradeKey,modDef.Param):"upgrade_"+upgradeKey;
        }
        private static string GetModifierName(string upgradeKey,UpgradeStatModifierDef statDef)
        {
            if(!string.IsNullOrEmpty(statDef.StringParam))return "upgrade_"+upgradeKey+"_"+statDef.StringParam;
            return statDef.Param!=0?string.Format("upgrade_{0}_{1}",upgradeKey,statDef.Param):"upgrade_"+upgradeKey;
        }
    }
}
