using System;
using Codebase.Unit;

namespace Codebase.Cashback
{
	[Serializable]
	public struct CashbackStatEntry
	{
		public StatType Type;

		public double Value;
	}
}
