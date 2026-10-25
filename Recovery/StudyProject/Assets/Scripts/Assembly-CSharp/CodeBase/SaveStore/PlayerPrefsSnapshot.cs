using System;
using System.Collections.Generic;

namespace Codebase.SaveStore
{
	[Serializable]
	public class PlayerPrefsSnapshot : IDataSnapshot
	{
		public string Name;

		public string CreatedAtIso;

		public List<RegistryEntry> Entries;

		string IDataSnapshot.Name => this.Name;

		string IDataSnapshot.CreatedAtIso => this.CreatedAtIso;

		string IDataSnapshot.Details => null;
	}
}
