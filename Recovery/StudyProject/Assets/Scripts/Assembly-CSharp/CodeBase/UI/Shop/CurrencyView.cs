using Codebase.Core;
using Codebase.Core.UI;
using System.Globalization;
using Codebase.Player.Models;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Shop
{
	public class CurrencyView : View
	{
		[Inject]
		private PlayerModel playerModel;

		[SerializeField]
		private TMP_Text text;

		private void OnEnable()
		{
			// Original ELF 0x1623a40.
			Initialize();
			Render();
		}

		protected override void Render()
		{
			// Original ELF 0x1623a68: raw invariant double, intentionally not abbreviated.
			text.text = GameContext.Get<PlayerModel>().Gems.ToString(CultureInfo.InvariantCulture);
		}
	}
}
