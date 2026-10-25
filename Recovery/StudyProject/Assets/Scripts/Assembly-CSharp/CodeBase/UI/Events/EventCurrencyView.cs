using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class EventCurrencyView : View
	{
		[SerializeField]
		private TMP_Text amount;

		[SerializeField]
		private TMP_Text value;

		[SerializeField]
		private TMP_Text currencyName;

		[SerializeField]
		private Image icon;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventStyleConfig eventStyle;

		private string key;

		public void Setup(string key)
		{
		}

		private void OnEnable()
		{
		}

		protected override void Render()
		{
		}

		private string Format(double value)
		{
			return null;
		}
	}
}
