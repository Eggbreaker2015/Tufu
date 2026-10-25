using System;
using Codebase.Events;
using Codebase.Utilities;

namespace Codebase.Enemy
{
	[Serializable]
	public class EventSpawnerVariant
	{
		public EnemyUnit Prefab;

		[KeysDropdown(typeof(EventCurrencies))]
		public string CurrencyKey;

		public float CurrencyAmount;
	}
}
