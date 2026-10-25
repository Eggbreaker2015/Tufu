using System;

namespace Codebase.Core.UserData
{
	[Serializable]
	internal class LegacyEventSlotLock
	{
		public string EventKey;

		public int SlotStartUnixSeconds;
	}
}
