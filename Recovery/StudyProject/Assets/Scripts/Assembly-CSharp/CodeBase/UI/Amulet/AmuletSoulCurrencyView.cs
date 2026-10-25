using Codebase.Amulet;
using Codebase.Core;
using Codebase.Core.UI;
using TMPro;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.UI.Amulet
{
	public class AmuletSoulCurrencyView : View
	{
		[SerializeField]
		private TMP_Text amountText;

		[SerializeField]
		private SoulView soulView;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		private string soulKey;

		public void Setup(string soulKey)
		{
			// Native ELF VA 0x13cad94.
			this.soulKey = soulKey;
			soulView.Setup(soulStyleConfig.GetStyle(soulKey));
			Initialize();
		}

		protected override void Render()
		{
			// Native ELF VA 0x13cadfc.
			if (!string.IsNullOrEmpty(soulKey)) amountText.text = amuletModel.GetSouls(soulKey).Format();
		}
	}
}
