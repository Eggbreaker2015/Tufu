using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	[DisallowMultipleComponent]
	public class ShopItemGemsFx : MonoBehaviour
	{
		[Header("Optional overrides")]
		[SerializeField]
		private float scatterRadius;

		[SerializeField]
		private float flyDuration;

		private CurrencyFlyAnimator animator;

		[SerializeField]
		private Image currencyView;

		[SerializeField]
		private CurrencyFlyAnimator animatorPrefab;

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}

		private void HandleGemsReceived(GemsReceivedSignal signal)
		{
		}
	}
}
