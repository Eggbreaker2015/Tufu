using System;
using Codebase.Core;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UniMob;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;
namespace Codebase.ABTests
{
public class ABTestInitializer : LifetimeMonoBehaviour
{
    public struct UserAttributes { }
    public struct AppAttributes { }
    private const string RemoteRulesCrossPlatformKey="ABRulesCrossPlatform";
    private const string DevelopRulesKey="Develop";
    private const string IosRulesKey="iOS";
    private const string AndroidRulesKey="Android";
    private const string DataKey="data";
    private const string RulesKey="rules";
    [SerializeField] private ABTestDataConfig dataConfig;
    private ABTestModel model;
    private void Awake() { GameContext.Set(dataConfig); DontDestroyOnLoad(gameObject); }
    public void Initialize() { InitializeABTests().Forget(); }
    private async UniTaskVoid InitializeABTests()
    {
        model=new ABTestModel(Lifetime);
        model.Initialize();
        GameContext.Set(model);
        ABTestData.Initialize(model,dataConfig);
#if UNITY_EDITOR || UNITY_STANDALONE
        // Editor-only code is stripped from the Android APK. Reconstructed from
        // the retained mode enum, original serialized setting and native develop path.
        // The desktop study build uses the same local cohort setting; remote
        // service SDK bodies were stripped from the APK and cannot initialize here.
        if (dataConfig.developCohortsMode == AbDevelopCohortsMode.Nothing) return;
        if (dataConfig.developCohortsMode == AbDevelopCohortsMode.UseDevelop) { ApplyDevelopTests(); return; }
#endif
        await InitializeUnityServices();
        await FetchRemoteConfig();
    }
    private async UniTask InitializeUnityServices()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Uninitialized) return;
            await UnityServices.InitializeAsync();
            Debug.Log("[ABTest] Unity Services initialized");
        }
        catch (Exception error) { Debug.LogError("[ABTest] Failed to initialize Unity Services: "+error.Message); }
    }
    private async UniTask FetchRemoteConfig()
    {
        try
        {
            await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(),new AppAttributes());
            Debug.Log("[ABTest] Remote Config fetched successfully");
            ApplyRemoteTests();
        }
        catch (Exception error) { Debug.LogError("[ABTest] Failed to fetch Remote Config: "+error.Message); }
    }
    private void ApplyRemoteTests()
    {
        var config=ResolveCrossPlatformConfig(RemoteConfigService.Instance.appConfig.GetJson(RemoteRulesCrossPlatformKey,"{}"));
        string key=config[DataKey].ToString();
        string data=RemoteConfigService.Instance.appConfig.GetJson(key,"{}");
        string rules=config[RulesKey].ToString();
        ABTestData.ApplyRemoteAll(data,rules,true);
        Debug.Log("[ABTest] Remote data key: "+key+" = "+data);
        Debug.Log("[ABTest] Remote rules key: ABRulesCrossPlatform = "+rules);
    }
    private static JObject ResolveCrossPlatformConfig(string json) => (JObject)JObject.Parse(json)[GetRulesPlatformKey()];
    private static string GetRulesPlatformKey()
    {
#if UNITY_IOS
        return IosRulesKey;
#else
        return AndroidRulesKey;
#endif
    }
    private void ApplyDevelopTests() => ABTestData.ApplyRemoteAll(dataConfig.BuildDataJson(),dataConfig.BuildRulesJson(),false);
}
}
