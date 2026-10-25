using System;
using Codebase.Utilities;

namespace Codebase.Events
{
	[Serializable]
	public class EventStyle
	{
		[KeysDropdown(typeof(EventKeys))]
		public string Key;
	}
}
