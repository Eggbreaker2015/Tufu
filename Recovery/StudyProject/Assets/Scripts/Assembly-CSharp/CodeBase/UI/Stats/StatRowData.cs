using Codebase.Meta;
using Codebase.Unit;

namespace Codebase.UI.Stats
{
	public class StatRowData
	{
		public Stat Stat { get; }

		public MetaStat MetaStat { get; }

		public MetaStatDef MetaDef { get; }

		public bool IsSpecial { get; }

		public bool ShowSeparatorAbove { get; }

		public bool IsMeta => false;

		public StatRowData(Stat stat, bool isSpecial, bool showSeparatorAbove)
		{
		}

		public StatRowData(MetaStat metaStat, MetaStatDef metaDef, bool showSeparatorAbove)
		{
		}
	}
}
