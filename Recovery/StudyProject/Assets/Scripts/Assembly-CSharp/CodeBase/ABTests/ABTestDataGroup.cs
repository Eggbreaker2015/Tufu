using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
namespace Codebase.ABTests
{
public class ABTestDataGroup<T> : IABTestDataGroup
{
    private T defaultValue;
    private bool hasDefaultValue;
    private T cachedValue;
    private int cachedFrame = -1;
    public string TestName { get; }
    public Dictionary<string,T> Values { get; private set; }
    public int Version { get; private set; }
    public ABTestDataGroup(string testName) { TestName=testName; Values=new Dictionary<string,T>(); }
    public T GetCurrentValue()
    {
        int frame=Time.frameCount;
        if (cachedFrame != frame) { cachedValue=ResolveCurrentValue(); cachedFrame=frame; }
        return cachedValue;
    }
    private T ResolveCurrentValue()
    {
        string variant=ABTestData.GetAssignedVariantOrNull(TestName);
        return !string.IsNullOrEmpty(variant) && Values.TryGetValue(variant,out T value) ? value : GetOrCreateDefaultValue();
    }
    private T GetOrCreateDefaultValue()
    {
        if (!hasDefaultValue) { defaultValue=Activator.CreateInstance<T>(); hasDefaultValue=true; }
        return defaultValue;
    }
    public void SetValues(Dictionary<string,T> values) { Values=values; InvalidateCache(); }
    public void ApplyRemote(string json) => SetValues(JsonConvert.DeserializeObject<Dictionary<string,T>>(json));
    public void ValidateCohorts(string cohortsJson) { JsonConvert.DeserializeObject<Dictionary<string,T>>(cohortsJson); }
    public void SetValuesUntyped(object dictionary) => SetValues((Dictionary<string,T>)dictionary);
    private void InvalidateCache() { cachedFrame=-1; Version++; }
    public void Reset() { Values.Clear(); defaultValue=default; hasDefaultValue=false; cachedValue=default; cachedFrame=-1; Version=0; }
}
}
