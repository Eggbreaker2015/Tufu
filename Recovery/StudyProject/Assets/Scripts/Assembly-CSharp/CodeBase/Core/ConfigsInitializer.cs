using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Core
{
	public class ConfigsInitializer : MonoBehaviour
	{
		[SerializeField]
		private List<ScriptableObject> configs;

		public void Initialize()
		{
			foreach (ScriptableObject config in configs)
			{
				GameContext.Set(config, true);
			}
		}
	}
}
