using System.Collections.Generic;
using Codebase.Core.Flows;

namespace Codebase.Gates
{
	public struct SequentialGateOpeningFlow : IFlow
	{
		public List<Gate> Gates;
	}
}
