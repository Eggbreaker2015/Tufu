using Codebase.Utilities;
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

        // Unity enables fresh and pooled instances before ViewContainer injects
        // and calls Setup. Render only after the current binding is complete.
        private bool hasCompletedSetup;

		public void Setup(string key)
		{
            // Original APK Setup ELF0x165a31c, with local lifecycle readiness.
            hasCompletedSetup = false;
            this.key = key;
            Initialize();
            hasCompletedSetup = true;
		}

		private void OnEnable()
		{
            // Original OnEnable ELF0x165a340 rendered unconditionally. In this
            // recovered container Instantiate/SetActive precedes InjectInto/Setup.
            if (!hasCompletedSetup || eventsRepository == null || eventStyle == null ||
                string.IsNullOrEmpty(key) || !IsInitialized) return;
            Render();
		}

		protected override void Render()
		{
			// Original APK ARM64 ELF VA 0x165a34c.
			var model = eventsRepository.CurrentEvent;
			if (model == null) return;
			currencyName.text = LocalizationStrings.Get("EVENT_CURRENCY_NAME_" + key);
			amount.text = model.GetCurrency(key).Format();
			var style = eventStyle.GetCurrencyStyle(key);
			icon.sprite = style.Sprite;
			value.text = string.Concat("<sprite name=", '"', style.PrefixSprite.name, '"', "> ", Format(EventBalance.GetCurrencyValue(key)));
		}

        public override void Cleanup()
        {
            // Clear the old binding before pool Despawn can trigger OnDisable,
            // so the next SetActive cannot render the previous currency key.
            hasCompletedSetup = false;
            key = null;
            base.Cleanup();
        }

		private string Format(double value)
		{
			// Original APK ARM64 ELF VA 0x165a544.
			return (value * 100).Format() + "%";
		}
	}
}
