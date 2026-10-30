using System.Collections.Generic;
using Codebase.Rewards;

namespace Codebase.Rpg
{
 public static class RpgRewardFilter
 {
  public static List<RewardBase> Filter(IEnumerable<RewardBase> rewards)
  {
   var result = new List<RewardBase>();
   if (rewards == null) return result;
   foreach (var reward in rewards)
   {
    if (reward is ItemReward || reward is ItemSlotReward || reward is StatReward) continue;
    if (reward is BundleReward bundle)
    {
     var nested = Filter(bundle.Rewards);
     if (nested.Count > 0) result.Add(new BundleReward { Rewards = nested.ToArray() });
    }
    else result.Add(reward);
   }
   return result;
  }
 }
}
