using UnityEngine;

namespace Codebase.UI
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public sealed class SafeAreaHeightFitter : MonoBehaviour
	{
		private RectTransform rectTransform;

		private Canvas rootCanvas;

		private float initialHeight;

		private Rect appliedSafeArea;

		private Vector2Int appliedScreenSize;

		private void Awake()
		{
		}

		private void Update()
		{
		}

		private void Apply()
		{
		}
	}
}
