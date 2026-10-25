using System;
using UnityEngine;

namespace Codebase.SaveStore
{
	[Serializable]
	public class UserDataLocalSnapshot : IDataSnapshot
	{
		public string CreatedAtIso;

		public TextAsset data;

		public SnapshotInfo info;

		public string Name => null;

		string IDataSnapshot.Name => null;

		string IDataSnapshot.CreatedAtIso => this.CreatedAtIso;

		string IDataSnapshot.Details => null;
	}
}
