using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Events
{
	[CreateAssetMenu(menuName = "Codebase/Events/Events Provider")]
	public class EventsProvider : ScriptableObject
	{
		public List<EventConfig> Events;

		public EventConfig Get(string key)
		{
			// Original APK ARM64 ELF VA 0x14daa08.
			return Events.FirstOrDefault(item => item.Key == key);
		}

		public IEnumerable<EventConfig> GetAll()
		{
			// Original APK ARM64 ELF VA 0x14dab04.
			return Events;
		}
	}
}
