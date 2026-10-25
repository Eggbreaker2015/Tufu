using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Analytics.Events;
using Codebase.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Codebase.ABTests
{
public static class ABTestData
{
    private static ABTestModel model;
    private static JObject currentAbData;
    public static readonly ABTestDataGroup<RewardedDelayFromStartData> RewardedDelayFromStart = new ABTestDataGroup<RewardedDelayFromStartData>(ABKeys.RewardedDelayFromStart);
    public static readonly ABTestDataGroup<EnemyHpDamageMultData> EnemyHpDamageMult = new ABTestDataGroup<EnemyHpDamageMultData>(ABKeys.EnemyHpDamageMult);
    public static readonly ABTestDataGroup<NoAdsData> NoAds = new ABTestDataGroup<NoAdsData>(ABKeys.NoAds);
    public static readonly ABTestDataGroup<LevelPassData> LevelPass = new ABTestDataGroup<LevelPassData>(ABKeys.LevelPass);
    public static readonly ABTestDataGroup<RpmOldAlgorithmData> RpmOldAlgorithm = new ABTestDataGroup<RpmOldAlgorithmData>(ABKeys.RpmOldAlgorithm);
    public static readonly ABTestDataGroup<FastRPMParamsData> FastRPMParams = new ABTestDataGroup<FastRPMParamsData>(ABKeys.FastRPMParams);
    public static readonly ABTestDataGroup<SoulPassAbData> SoulPass = new ABTestDataGroup<SoulPassAbData>(ABKeys.SoulPass);
    public static readonly ABTestDataGroup<CrystalPassAbData> CrystalPass = new ABTestDataGroup<CrystalPassAbData>(ABKeys.CrystalPass);
    public static readonly ABTestDataGroup<OfferwallData> Offerwall = new ABTestDataGroup<OfferwallData>(ABKeys.Offerwall);
    public static readonly ABTestDataGroup<PatchCompensationGemsData> PatchCompensationGems = new ABTestDataGroup<PatchCompensationGemsData>(ABKeys.PatchCompensationGems);
    public static readonly ABTestDataGroup<CommonRewardMultipliersData> CommonRewardMultipliers = new ABTestDataGroup<CommonRewardMultipliersData>(ABKeys.CommonRewardMultipliers);
    public static readonly ABTestDataGroup<SpawnerCooldownsData> SpawnerCooldowns = new ABTestDataGroup<SpawnerCooldownsData>(ABKeys.SpawnerCooldowns);
    public static readonly ABTestDataGroup<EnchantFailureDustCashbackData> EnchantFailureDustCashback = new ABTestDataGroup<EnchantFailureDustCashbackData>(ABKeys.EnchantFailureDustCashback);
    private static readonly Dictionary<string,IABTestDataGroup> groupsByKey=new Dictionary<string,IABTestDataGroup>
    {
        { ABKeys.RewardedDelayFromStart, RewardedDelayFromStart },
        { ABKeys.EnemyHpDamageMult, EnemyHpDamageMult },
        { ABKeys.NoAds, NoAds },
        { ABKeys.LevelPass, LevelPass },
        { ABKeys.RpmOldAlgorithm, RpmOldAlgorithm },
        { ABKeys.FastRPMParams, FastRPMParams },
        { ABKeys.SoulPass, SoulPass },
        { ABKeys.CrystalPass, CrystalPass },
        { ABKeys.Offerwall, Offerwall },
        { ABKeys.PatchCompensationGems, PatchCompensationGems },
        { ABKeys.CommonRewardMultipliers, CommonRewardMultipliers },
        { ABKeys.SpawnerCooldowns, SpawnerCooldowns },
        { ABKeys.EnchantFailureDustCashback, EnchantFailureDustCashback },
    };
    public static void Initialize(ABTestModel abTestModel,ABTestDataConfig dataConfig)
    {
        model=abTestModel;
        var sanitized=new JObject();
        string saved=model.Data.AbData.Value;
        if (!string.IsNullOrEmpty(saved))
        {
            try { foreach (var property in JObject.Parse(saved).Properties()) if (groupsByKey.ContainsKey(property.Name)) sanitized[property.Name]=property.Value; }
            catch { }
        }
        currentAbData=sanitized;
        ApplyRemoteData(currentAbData.ToString(Formatting.None),false);
        model.Data.AbData.Value=currentAbData.ToString(Formatting.None);
    }
    internal static string GetAssignedVariantOrNull(string testName) => model.GetVariant(testName);
    public static void ApplyRemote(string testName,string json)
    { if (groupsByKey.TryGetValue(testName,out var group)) group.ApplyRemote(json); }
    public static void ApplyRemoteAll(string json) => ApplyRemoteAll(json,null,false);
    public static void ApplyRemoteAll(string dataJson,string rulesJson) => ApplyRemoteAll(dataJson,rulesJson,false);
    public static void ApplyRemoteAll(string dataJson,string rulesJson,bool isRemote)
    {
        if (!string.IsNullOrEmpty(dataJson)) ApplyRemoteData(dataJson,isRemote);
        if (!string.IsNullOrEmpty(rulesJson)) ApplyRemoteRules(rulesJson,isRemote);
    }
    private static void ApplyRemoteData(string json,bool isRemote)
    {
        try
        {
            if (currentAbData == null) currentAbData=new JObject();
            string previous=model.Data.AbData.Value;
            JObject incoming=JObject.Parse(json);
            foreach (var property in incoming.Properties())
                if (!TryValidateCohorts(property.Name,property.Value?.ToString(),isRemote)) return;
            foreach (var property in incoming.Properties())
            {
                if (!groupsByKey.ContainsKey(property.Name)) continue;
                currentAbData[property.Name]=property.Value;
                ApplyRemote(property.Name,property.Value.ToString());
            }
            string updated=currentAbData.ToString(Formatting.None);
            if (isRemote && previous != updated)
            {
                int revision=++model.Data.AbDataRevision.Value;
                GameContext.Get<IAnalyticsWrapper>().Send(new ABTestDataEvent(revision,updated));
            }
            model.Data.AbData.Value=updated;
        }
        catch (System.Exception error)
        {
            string message="[ABTest] ABData JSON is invalid and will be ignored. Error: " + error.Message;
            Debug.LogWarning(message);
            if (isRemote) GameContext.Get<IAnalyticsWrapper>().Send(new ABTestDataEvent(message));
        }
    }
    private static bool TryValidateCohorts(string testName,string cohortsJson,bool isRemote)
    {
        if (!groupsByKey.TryGetValue(testName,out var group)) return true;
        try { group.ValidateCohorts(cohortsJson); return true; }
        catch (System.Exception error)
        {
            string message="[ABTest] Cohorts JSON is invalid for test '"+testName+"' and will be ignored. Error: "+error.Message;
            Debug.LogWarning(message);
            if (isRemote) GameContext.Get<IAnalyticsWrapper>().Send(new ABTestDataEvent(message));
            return false;
        }
    }
    private static void ApplyRemoteRules(string json,bool isRemote)
    {
        try
        {
            var tests=new Dictionary<string,string>();
            foreach (var property in JObject.Parse(json).Properties())
            { string value=property.Value?.ToString(); if (groupsByKey.ContainsKey(property.Name) && !string.IsNullOrEmpty(value)) tests[property.Name]=value; }
            if (tests.Count > 0) model.Apply(tests,isRemote);
        }
        catch (System.Exception error)
        {
            string message="[ABTest] ABRules JSON is invalid and will be ignored. Error: "+error.Message;
            Debug.LogWarning(message);
            if (isRemote) GameContext.Get<IAnalyticsWrapper>().Send(new ABTestDataEvent(message));
        }
    }
    [StaticReset]
    private static void ResetStaticState()
    { model=null; currentAbData=null; foreach (var group in groupsByKey.Values) group.Reset(); }
}
}
