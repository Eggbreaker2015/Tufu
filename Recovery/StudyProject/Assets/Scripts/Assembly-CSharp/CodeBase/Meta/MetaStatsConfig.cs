using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Meta
{
	[CreateAssetMenu(menuName = "Devour/MetaStatsConfig")]
	public class MetaStatsConfig : ScriptableObject
	{
		public List<MetaStatDef> Stats;

		private void Fill()
		{
		}
	}
}
