using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Codebase.ABTests
{
	[CreateAssetMenu(menuName = "Balance/ABTest Data Config")]
	public class ABTestDataConfig : ScriptableObject
	{
		private const string RemoteRulesCrossPlatformKey = "ABRulesCrossPlatform";

		private const string DevelopRulesKey = "Develop";

		private const string IosRulesKey = "iOS";

		private const string AndroidRulesKey = "Android";

		private const string DataKey = "data";

		private const string RulesKey = "rules";

		public AbDevelopCohortsMode developCohortsMode;

		public bool omitDefaultFieldsInGeneratedDataJson;

		public bool beautifyGeneratedJson;

		public RewardedDelayFromStartConfig rewardedDelayFromStart = new RewardedDelayFromStartConfig();

		public EnemyHpDamageMultConfig enemyHpDamageMult = new EnemyHpDamageMultConfig();

		public NoAdsConfig noAds = new NoAdsConfig();

		public AbLevelPassConfig levelPass = new AbLevelPassConfig();

		public RpmOldAlgorithmConfig rpmOldAlgorithm = new RpmOldAlgorithmConfig();

		public FastRPMParamsConfig fastRPMParams = new FastRPMParamsConfig();

		public SoulPassAbConfig soulPass = new SoulPassAbConfig();

		public CrystalPassAbConfig crystalPass = new CrystalPassAbConfig();

		public OfferwallConfig offerwall = new OfferwallConfig();

		public PatchCompensationGemsConfig patchCompensationGems = new PatchCompensationGemsConfig();

		public CommonRewardMultipliersConfig commonRewardMultipliers = new CommonRewardMultipliersConfig();

		public SpawnerCooldownsConfig spawnerCooldowns = new SpawnerCooldownsConfig();

		public EnchantFailureDustCashbackConfig enchantFailureDustCashback = new EnchantFailureDustCashbackConfig();

		public void GenerateDataJSON()
		{
		}

		public void GenerateRulesJSON()
		{
		}

		public string BuildDataJson()
        {
            var values=new Dictionary<string,object>();
            foreach (var field in GetType().GetFields(BindingFlags.Instance|BindingFlags.Public))
            {
                if (field.FieldType == typeof(string) || field.FieldType.IsValueType) continue;
                string key=TryGetAbKeyForConfigField(field.Name);
                object config=field.GetValue(this);
                if (key == null || config == null) continue;
                var method=config.GetType().GetMethod("ToDictionary",BindingFlags.Instance|BindingFlags.Public);
                if (method != null) values[key]=method.Invoke(config,null);
            }
            var formatting=beautifyGeneratedJson ? Formatting.Indented : Formatting.None;
            if (!omitDefaultFieldsInGeneratedDataJson) return JsonConvert.SerializeObject(values,formatting);
            var result=new JObject();
            foreach (var entry in values)
            {
                var variants=(IDictionary)entry.Value;
                var group=new JObject();
                foreach (DictionaryEntry variant in variants)
                {
                    JToken token=JToken.FromObject(variant.Value);
                    JToken defaults=JToken.FromObject(Activator.CreateInstance(variant.Value.GetType()));
                    StripDefaultsRecursive(token,defaults);
                    group[(string)variant.Key]=token;
                }
                result[entry.Key]=group;
            }
            return result.ToString(formatting);
        }

		private static void StripDefaultsRecursive(JToken current,JToken defaults)
        {
            if (!(current is JObject obj) || !(defaults is JObject defaultObj)) return;
            foreach (var property in obj.Properties().ToList())
            {
                JToken defaultValue=defaultObj[property.Name];
                if (defaultValue == null) continue;
                if (JToken.DeepEquals(property.Value,defaultValue)) property.Remove();
                else StripDefaultsRecursive(property.Value,defaultValue);
            }
        }

		public string BuildRulesJson()
        {
            var values=new Dictionary<string,object>();
            foreach (var field in GetType().GetFields(BindingFlags.Instance|BindingFlags.Public))
            {
                if (field.FieldType == typeof(string) || field.FieldType.IsValueType) continue;
                string key=TryGetAbKeyForConfigField(field.Name);
                object config=field.GetValue(this);
                if (key != null && config != null) values[key]=config.GetType().GetField("rules")?.GetValue(config);
            }
            return JsonConvert.SerializeObject(values,beautifyGeneratedJson ? Formatting.Indented : Formatting.None);
        }

		internal static string TryGetAbKeyForConfigField(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return null;
            string name=char.ToUpperInvariant(fieldName[0])+fieldName.Substring(1);
            return (string)typeof(ABKeys).GetField(name,BindingFlags.Static|BindingFlags.Public)?.GetValue(null);
        }
	}
}
