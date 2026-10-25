using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Codebase.Meta.Upgrades
{
	[CreateAssetMenu(menuName = "Devour/UpgradeStyleConfig")]
	public class UpgradeStyleConfig : ScriptableObject
	{
		public List<UpgradeStyle> Styles = new List<UpgradeStyle>(); // Original ctor ELF 0x148daf8.

		public UpgradeStyle GetStyle(string upgradeKey)
		{
			return Styles.FirstOrDefault(style => style.Key == upgradeKey); // Original ELF 0x148d2d4 / predicate 0x148dd50.
		}

		public Sprite GetIcon(string upgradeKey)
		{
			return GetStyle(upgradeKey)?.Icon; // Original ELF 0x148d3d0. Editor Fill remains outside this runtime slice.
		}

		private void Fill()
		{
		}
	}
}
