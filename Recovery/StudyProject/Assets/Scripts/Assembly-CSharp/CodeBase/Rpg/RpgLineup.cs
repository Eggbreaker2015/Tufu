using System;
using Codebase.Core;

namespace Codebase.Rpg
{
	public static class RpgLineup
	{
		public static event Action<int> SlotOpened;
		public static void Invoke(int size) { SlotOpened?.Invoke(size); }
		[StaticReset]
		private static void Reset() { SlotOpened = null; }
	}
}
