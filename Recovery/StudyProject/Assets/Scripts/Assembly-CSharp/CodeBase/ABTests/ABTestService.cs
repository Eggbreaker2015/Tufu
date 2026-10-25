using System;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Unit;
namespace Codebase.ABTests
{
public class ABTestService
{
    private const string DefaultRewardedAdUnitId="1a8c44deda6caa58";
    [Inject] private UserModel userModel;
    public int SpawnerCooldownsVersion => ABTestData.SpawnerCooldowns.Version;
    public string GetRewardedAdUnitId() => DefaultRewardedAdUnitId;
    public bool IsDynamicRewardedWeights() => false;
    public int GetRewardedDelayFromStartMinutes() => ABTestData.RewardedDelayFromStart.GetCurrentValue().delayMinutes;
    public float GetRewardedDelayFromStartMinutesPerUsdSpent() => ABTestData.RewardedDelayFromStart.GetCurrentValue().delayMinutesPerUsdSpent;
    public float GetTotalRewardedDelayMinutes()
    { var value=ABTestData.RewardedDelayFromStart.GetCurrentValue(); return value.delayMinutes+value.delayMinutesPerUsdSpent*userModel.InAppsBoughtValueInRewarded.Value; }
    public float GetEnemyHpDamageMultiplier(int locationNumber)
    { var data=ABTestData.EnemyHpDamageMult.GetCurrentValue().damageMultiplier; return data.overridesByLocation != null && data.overridesByLocation.TryGetValue(locationNumber,out float value) ? value : data.defaultValue; }
    public double GetRewardedValueMultiplierForEnemyHpDamageMultTest() => ABTestData.EnemyHpDamageMult.GetCurrentValue().rewardedValueMultiplier;
    public bool IsRewardedOfferEnabled() => false;
    public bool IsGemsHudOfferEnabled() => false;
    public bool IsNoAdsEnabled() => ABTestData.NoAds.GetCurrentValue().enabled;
    public NoAdsData GetNoAdsData() => ABTestData.NoAds.GetCurrentValue();
    public bool IsLevelPassEnabled() => ABTestData.LevelPass.GetCurrentValue().enabled;
    public bool UseOldRpmAlgorithm() => ABTestData.RpmOldAlgorithm.GetCurrentValue().enabled;
    public FastRPMParamsData GetFastRPMParams() => ABTestData.FastRPMParams.GetCurrentValue();
    public bool IsSoulPassEnabled() => ABTestData.SoulPass.GetCurrentValue().enabled;
    public bool IsCrystalPassEnabled() => ABTestData.CrystalPass.GetCurrentValue().enabled;
    public bool IsOfferwall() => ABTestData.Offerwall.GetCurrentValue().enabled;
    public int GetOfferwallLastLocationUnlocked() => ABTestData.Offerwall.GetCurrentValue().lastLocationUnlocked;
    public float GetCrystalPassInAppMagicDustMultiplier() => ABTestData.CrystalPass.GetCurrentValue().inAppMagicDustMultiplier;
    public float GetEnchantFailureDustCashbackRpm() => ABTestData.EnchantFailureDustCashback.GetCurrentValue().rpm;
    public int GetPatchCompensationGems(int maxLocationNum)
    { var value=ABTestData.PatchCompensationGems.GetCurrentValue(); return value.useFixedGems ? value.fixedGems : (int)(Math.Pow(maxLocationNum,1.5)*150.0); }
    public float GetRewardedStatValueMultiplier(int locationNumber) => GetCommonRewardMultiplier(ABTestData.CommonRewardMultipliers.GetCurrentValue().rewardedStatValueMultiplier,locationNumber);
    public float GetInAppStatValueMultiplier(int locationNumber) => GetCommonRewardMultiplier(ABTestData.CommonRewardMultipliers.GetCurrentValue().inAppStatValueMultiplier,locationNumber);
    public float GetSoulValueMultiplier(int locationNumber) => GetCommonRewardMultiplier(ABTestData.CommonRewardMultipliers.GetCurrentValue().soulValueMultiplier,locationNumber);
    public float GetCrystalDustValueMultiplier(int locationNumber) => GetCommonRewardMultiplier(ABTestData.CommonRewardMultipliers.GetCurrentValue().crystalDustValueMultiplier,locationNumber);
    public float GetRpmStatValueMultiplier(StatType statType)
    { var v=ABTestData.CommonRewardMultipliers.GetCurrentValue(); return GetRpmStatValueMultiplier(statType,v.attackStatValueMultiplier,v.armorStatValueMultiplier,v.sustainStatValueMultiplier); }
    public float GetInAppRpmStatValueMultiplier(StatType statType)
    { var v=ABTestData.CommonRewardMultipliers.GetCurrentValue(); return GetRpmStatValueMultiplier(statType,v.inAppAttackStatValueMultiplier,v.inAppArmorStatValueMultiplier,v.inAppSustainStatValueMultiplier); }
    public float GetSpawnerCooldownSeconds(bool isElite,int locationNumber) => GetValue(GetSpawnerCooldownsGroup(isElite).cooldownMinutes,locationNumber)*60f;
    public int GetSpawnerCharges(bool isElite,int locationNumber,int unitCount) => GetValue(GetSpawnerCooldownsGroup(isElite).charges,locationNumber)*unitCount;
    private static SpawnerCooldownsGroup GetSpawnerCooldownsGroup(bool isElite)
    { var value=ABTestData.SpawnerCooldowns.GetCurrentValue(); return isElite ? value.elite : value.nonElite; }
    private static float GetCommonRewardMultiplier(ValueByLocation multiplier,int locationNumber) => GetValue(multiplier,locationNumber);
    private static float GetRpmStatValueMultiplier(StatType statType,float attackMultiplier,float armorMultiplier,float sustainMultiplier)
    {
        switch ((int)statType)
        {
            case 1: case 2: case 22: return sustainMultiplier;
            case 6: case 8: case 12: return attackMultiplier;
            case 14: case 16: case 20: return armorMultiplier;
            default: return 1f;
        }
    }
    private static float GetValue(ValueByLocation valueByLocation,int locationNumber) => valueByLocation.overridesByLocation.TryGetValue(locationNumber,out float value) ? value : valueByLocation.defaultValue;
    private static int GetValue(IntValueByLocation valueByLocation,int locationNumber) => valueByLocation.overridesByLocation.TryGetValue(locationNumber,out int value) ? value : valueByLocation.defaultValue;
}
}
