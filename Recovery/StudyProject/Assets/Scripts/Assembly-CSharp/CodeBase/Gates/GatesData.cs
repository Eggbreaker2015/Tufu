using System.Collections.Generic;
using Codebase.Core;

namespace Codebase.Gates
{
	public class GatesData : ISaveable
	{
		public DataVar<List<GateData>> Gates { get; private set; }

		public void Initialize()
		{
		}
	}
}
