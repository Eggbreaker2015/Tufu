using System.Collections.Generic;
using Codebase.Core;
using UniMob;

namespace Codebase.Meta
{
	public class MetaStatsModel : Model<MetaStatsUd>
	{
		[Inject]
		private MetaStatsConfig config;

		private readonly Dictionary<string, MetaStat> stats;

		public IReadOnlyDictionary<string, MetaStat> All => stats;

		public MetaStatsModel(Lifetime lifetime)
			: base(lifetime)
		{
			// Native ELF VA 0x1487dc4.
			stats = new Dictionary<string, MetaStat>();
		}

		public MetaStat Get(string key)
		{
			// Native ELF VA 0x1485794: CollectionExtensions.GetValueOrDefault.
			return stats.TryGetValue(key, out MetaStat value) ? value : null;
		}

		public void Populate()
		{
			// Native ELF VA 0x1487e78: last saved key wins; configured duplicates are skipped.
			var savedByKey = new Dictionary<string, MetaStatDataUd>();
			foreach (MetaStatDataUd saved in Data.Stats)
			    if (!string.IsNullOrEmpty(saved.Key.Value)) savedByKey[saved.Key.Value] = saved;
			foreach (MetaStatDef definition in config.Stats)
			{
			    if (string.IsNullOrEmpty(definition.Key) || stats.ContainsKey(definition.Key)) continue;
			    var stat = new MetaStat(Lifetime, definition.BaseValue, definition.Key);
			    if (!savedByKey.TryGetValue(definition.Key, out MetaStatDataUd saved))
			    {
			        saved = Data.Stats.Create();
			        saved.Key.Value = definition.Key;
			        saved.BaseValue.Value = definition.BaseValue;
			    }
			    stat.BindToUserData(saved);
			    stats[definition.Key] = stat;
			}
		}
	}
}
