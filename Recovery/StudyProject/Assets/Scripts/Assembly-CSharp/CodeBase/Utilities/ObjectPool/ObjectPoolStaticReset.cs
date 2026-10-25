using System;
using System.Collections.Generic;
using Codebase.Core;

namespace Codebase.Utilities.ObjectPool
{
	internal static class ObjectPoolStaticReset
	{
		private static readonly List<Action> Resetters = new List<Action>();

		public static void Register(Action resetter)
		{
			Resetters.Add(resetter);
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			foreach (Action resetter in Resetters) resetter();
		}
	}
}
