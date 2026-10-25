using Codebase.Utilities;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Shop;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Shop
{
	public class ShopTimerView : View
	{
		[SerializeField]
		private TMP_Text timerText;

		[Inject]
		private ShopLotRepository shopLotRepository;

		protected override void Render()
		{
            // Native 0x162a0b4 truncates seconds before formatting.
            timerText.text = TimeLocalizer.GetFormatTime((int)shopLotRepository.TimeToNextFreeClaim.TotalSeconds);
		}
	}
}
