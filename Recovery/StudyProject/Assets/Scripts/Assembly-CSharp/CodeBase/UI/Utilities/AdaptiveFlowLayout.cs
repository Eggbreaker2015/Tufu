using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Utilities
{
	public class AdaptiveFlowLayout : LayoutGroup
	{
		private enum SizeMode
		{
			Min = 0,
			Preferred = 1,
			Lerp = 2
		}

		private class RowData
		{
			public readonly List<ChildData> Children;

			public float TotalWidth { get; private set; }

			public float MaxHeight { get; private set; }

			public void AddChild(RectTransform child, Vector2 size, float spacingX)
			{
			}
		}

		private readonly struct ChildData
		{
			public readonly RectTransform RectTransform;

			public readonly Vector2 Size;

			public ChildData(RectTransform rectTransform, Vector2 size)
			{
				RectTransform = null;
				Size = default;
			}
		}

		[SerializeField]
		private Vector2 spacing;

		[SerializeField]
		private Vector2 defaultCellSize;

		[SerializeField]
		private bool shrinkToFit;

		private readonly List<RowData> rows;

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

		private void BuildLayout()
		{
		}

		private void BuildRowsWithSize(float containerWidth, SizeMode mode, float lerpT = 0f)
		{
		}

		private float CalculateContentHeight()
		{
			return 0f;
		}

		private float CalculateTotalHeight()
		{
			return 0f;
		}

		private void LayoutChildren()
		{
		}

		private Vector2 GetChildMinSize(RectTransform child)
		{
			return default;
		}

		private Vector2 GetChildPreferredSize(RectTransform child)
		{
			return default;
		}
	}
}
