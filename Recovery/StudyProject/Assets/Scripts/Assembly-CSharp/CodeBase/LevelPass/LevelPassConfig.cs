using System.Collections.Generic;
using UnityEngine;

namespace Codebase.LevelPass
{
	[CreateAssetMenu(menuName = "Codebase/LevelPass/Level Pass Config")]
	public class LevelPassConfig : ScriptableObject
	{
		public List<LevelPassDef> Passes;
	}
}
