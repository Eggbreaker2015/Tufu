using UnityEngine;

namespace Codebase.Core.UI
{
	public class TooltipView : MonoBehaviour
	{
		[SerializeField]
		private float hideDelay;

		private bool isVisible;

		private float hideTimer;

		public bool IsVisible => this.isVisible;

		private void Awake()
		{
		}

		public void Show()
		{
		}

		public void Hide()
		{
		}

		private void Update()
		{
		}

		private void ProcessHideInput()
		{
		}

		private bool IsPointerOverTooltip()
		{
			return false;
		}
	}
}
