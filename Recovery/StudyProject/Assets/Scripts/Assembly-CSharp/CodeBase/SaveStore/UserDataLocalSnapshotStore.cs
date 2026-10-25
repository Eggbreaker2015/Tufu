using System.Collections.Generic;
using UnityEngine;

namespace Codebase.SaveStore
{
	public class UserDataLocalSnapshotStore : ScriptableObject
	{
		[SerializeField]
		private string editorSnapshotsFolder;

		public List<UserDataLocalSnapshot> snapshots;
	}
}
