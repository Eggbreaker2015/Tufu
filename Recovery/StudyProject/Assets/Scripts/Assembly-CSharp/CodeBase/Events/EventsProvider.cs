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
			return null;
		}

		public IEnumerable<EventConfig> GetAll()
		{
			return null;
		}
	}
}
