using System.Collections.Generic;
using Codebase.Analytics;
using Codebase.Analytics.Events;
using Codebase.Core;
using UniMob;
using UnityEngine;
namespace Codebase.ABTests
{
public class ABTestModel : Model<ABTestUd>
{
    private const string FirstStart="game_loaded_once";
    private readonly IAnalyticsWrapper analytics;
    private readonly bool isNewUser;
    private readonly Dictionary<string,string> variantLists=new Dictionary<string,string>();
    public ABTestModel(Lifetime lifetime) : base(lifetime)
    { analytics=GameContext.Get<IAnalyticsWrapper>(); isNewUser=PlayerPrefs.GetInt(FirstStart,0)==0; }
    public void Apply(Dictionary<string,string> tests,bool isRemote)
    { foreach (var test in tests) TryApply(test.Key,test.Value,isRemote); }
    public ABTestVariant Get(string testName) => new ABTestVariant(GetVariant(testName));
    public string GetVariant(string testName) => Data.Tests.TryGetValue(testName,out string value) ? value : null;
    public string GetVariantList(string testName) => variantLists.TryGetValue(testName,out string value) ? value : null;
    public void SetVariant(string testName,string variant) { Data.Tests[testName]=variant; }
    private bool TryApply(string testName,string value,bool isRemote)
    {
        if (string.IsNullOrEmpty(testName) || string.IsNullOrEmpty(value)) return false;
        char prefix=value[0];
        if (isRemote && prefix != '*' && prefix != '+' && new DataVar<int>("playtime_seconds",0).Value > 30) return false;
        if (prefix != '*' && Data.Tests.ContainsKey(testName)) return false;
        if (prefix != '*' && prefix != '+' && !isNewUser) return false;
        string variants=prefix == '*' || prefix == '+' ? value.Substring(1) : value;
        string selected=SelectVariant(variants);
        Data.Tests[testName]=selected;
        variantLists[testName]=variants;
        Debug.Log("[ABTest] " + (isRemote ? "Remote" : "Develop") + " '" + testName + " = " + selected + "' (" + value + ")");
        if (isRemote) analytics.Send(new ABTestRemoteEvent(testName,value,selected));
        return true;
    }
    private static string SelectVariant(string variants)
    { return string.IsNullOrEmpty(variants) ? "A" : variants.Length == 1 ? variants : variants[UnityEngine.Random.Range(0,variants.Length)].ToString(); }
}
}
