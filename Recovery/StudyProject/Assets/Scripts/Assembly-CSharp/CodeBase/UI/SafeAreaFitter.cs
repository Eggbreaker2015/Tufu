using UnityEngine;

namespace Codebase.UI
{
	[RequireComponent(typeof(RectTransform))]
	public class SafeAreaFitter : MonoBehaviour
	{
		[SerializeField]
		private bool applyBottomInset;

		[SerializeField]
		private bool _ignoreTopInset;

		private RectTransform _rectTransform;

		private Rect _lastSafeArea;

		private void Awake()
		{
		}

		private void ApplySafeArea()
		{
		}
	}
}
