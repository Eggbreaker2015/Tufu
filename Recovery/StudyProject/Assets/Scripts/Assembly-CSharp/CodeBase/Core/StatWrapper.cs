using System;
using System.Collections.Generic;
using Codebase.Unit;

namespace Codebase.Core
{
	[Serializable]
	public class StatWrapper
	{
		public double BaseValue;

		public StatType StatType;

		public List<StatModifier> Modifiers;

		public StatWrapper(Stat stat)
		{
		}

		public StatWrapper()
		{
		}
	}
}
