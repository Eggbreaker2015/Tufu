using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Location
{
	[CreateAssetMenu(menuName = "Devour/IslandMedalStyleConfig")]
	public class IslandMedalStyleConfig : ScriptableObject
	{
		public List<IslandMedalStyle> Styles;

		public IslandMedalStyle GetStyle(int level)
		{
			foreach (IslandMedalStyle style in Styles)
			    if (style.Level == level) return style;
			return null;
		}

		public Sprite GetSprite(int level)
		{
			return GetStyle(level).Sprite;
		}
	}
}
