using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Utilities
{
	public class FlexibleGridLayout : LayoutGroup
	{
		[SerializeField]
		private int columns;

		[SerializeField]
		private Vector2 spacing;

		[SerializeField]
		private Vector2 cellSize;

		[SerializeField]
		private new bool flexibleHeight;

		public override void CalculateLayoutInputHorizontal()
		{
		}

		public override void CalculateLayoutInputVertical()
		{
		}

		public override void SetLayoutHorizontal()
		{
		}

		public override void SetLayoutVertical()
		{
		}
	}
}
