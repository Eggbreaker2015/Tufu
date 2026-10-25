using System;
using System.Collections;
using System.Collections.Generic;
namespace Codebase.ABTests
{
[Serializable]
public abstract class AbTestConfigBase
{
    public string rules;
    protected Dictionary<string,TData> BuildVariantsDictionary<TData>(IList variants)
    {
        var result = new Dictionary<string,TData>(variants?.Count ?? 0);
        if (variants == null) return result;
        foreach (object entry in variants)
        {
            if (entry == null) continue;
            Type type = entry.GetType();
            string key = (string)type.GetField("variant")?.GetValue(entry);
            TData data = (TData)type.GetField("data")?.GetValue(entry);
            result[key] = data;
        }
        return result;
    }
}
}
