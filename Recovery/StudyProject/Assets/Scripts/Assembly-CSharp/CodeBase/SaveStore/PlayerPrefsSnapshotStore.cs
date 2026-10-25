using System.Collections.Generic;
using UnityEngine;

namespace Codebase.SaveStore
{
	public class PlayerPrefsSnapshotStore : ScriptableObject
	{
		public List<PlayerPrefsSnapshot> Snapshots;
	}
}
