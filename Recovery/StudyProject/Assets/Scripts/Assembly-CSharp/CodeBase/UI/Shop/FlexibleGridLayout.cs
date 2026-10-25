using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
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

		public int Columns
		{
			get
			{
				return this.columns;
			}
			set
            {
                // Native 0x1624440.
                if (columns == value) return;
                columns = value; SetDirty();
            }
		}

		public override void CalculateLayoutInputHorizontal()
		{
            // Native 0x1624460: actual child positions are assigned in calculation, not SetLayoutHorizontal.
            base.CalculateLayoutInputHorizontal();
            float width = (((rectTransform.rect.width - padding.left) - padding.right) - spacing.x * (columns - 1)) / columns;
            for (int i = 0; i < rectChildren.Count; i++) {
                var child = rectChildren[i];
                child.GetComponent<LayoutElement>();
                float size = width > 0f ? width : cellSize.x;
                SetChildAlongAxis(child, 0, (size + spacing.x) * (i % columns) + padding.left, size);
            }
		}

		public override void CalculateLayoutInputVertical()
		{
            // Native 0x1624604; original FRINTP/FCVTPS confirms ceiling for a partial final row.
            int rows = Mathf.CeilToInt((float)rectChildren.Count / columns);
            float total = padding.top + padding.bottom;
            float y = padding.top;
            for (int row = 0; row < rows; row++) {
                float height = cellSize.y;
                if (flexibleHeight) for (int column = 0; column < columns; column++) {
                    int index = column + row * columns;
                    if (index >= rectChildren.Count) break;
                    var child = rectChildren[index];
                    var element = child.GetComponent<LayoutElement>();
                    float preferred = element != null && element.preferredHeight >= 0f ? element.preferredHeight : LayoutUtility.GetPreferredHeight(child);
                    height = Mathf.Max(height, preferred);
                }
                for (int column = 0; column < columns; column++) {
                    int index = column + row * columns;
                    if (index >= rectChildren.Count) break;
                    SetChildAlongAxis(rectChildren[index], 1, y, height);
                }
                y = y + height + spacing.y;
                total = total + height + (row < rows - 1 ? spacing.y : 0f);
            }
            // Unity 6.6 adds totalMax between original min and preferred inputs; preserve no maximum.
            SetLayoutInputForAxis(total, -1f, total, -1f, 1);
		}

		public override void SetLayoutHorizontal()
		{
            // Native 0x16248f0 is genuinely empty; calculation already positions children.
		}

		public override void SetLayoutVertical()
		{
            // Native 0x16248f4 is genuinely empty.
		}
	}
}
