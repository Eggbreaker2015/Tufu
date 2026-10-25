using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Utilities
{
	[AddComponentMenu("Layout/Animated Grid Layout Group", 152)]
	public class AnimatedGridLayoutGroup : LayoutGroup
	{
		public enum Corner
		{
			UpperLeft = 0,
			UpperRight = 1,
			LowerLeft = 2,
			LowerRight = 3
		}

		public enum Axis
		{
			Horizontal = 0,
			Vertical = 1
		}

		public enum Constraint
		{
			Flexible = 0,
			FixedColumnCount = 1,
			FixedRowCount = 2
		}

		[SerializeField]
		private Corner startCorner;

		[SerializeField]
		private Axis startAxis;

		[SerializeField]
		private Vector2 cellSize;

		[SerializeField]
		private Vector2 spacing;

		[SerializeField]
		private Constraint constraint;

		[SerializeField]
		private int constraintCount;

		[Header("Animation")]
		[SerializeField]
		private float animationDuration;

		[SerializeField]
		private Ease animationEase;

		[SerializeField]
		private bool ignoreTimeScale;

		private readonly Dictionary<RectTransform, Vector2> previousPositions;

		private readonly Dictionary<RectTransform, Tween> activeTweens;

		private readonly HashSet<RectTransform> knownChildren;

		private readonly List<RectTransform> cleanupBuffer;

		public Corner StartCorner
		{
			get
			{
				return this.startCorner;
			}
			set
			{
			}
		}

		public Axis StartAxis
		{
			get
			{
				return this.startAxis;
			}
			set
			{
			}
		}

		public Vector2 CellSize
		{
			get
			{
				return default;
			}
			set
			{
			}
		}

		public Vector2 Spacing
		{
			get
			{
				return default;
			}
			set
			{
			}
		}

		public Constraint ConstraintMode
		{
			get
			{
				return this.constraint;
			}
			set
			{
			}
		}

		public int ConstraintCount
		{
			get
			{
				return this.constraintCount;
			}
			set
			{
			}
		}

		public float AnimationDuration
		{
			get
			{
				return this.animationDuration;
			}
			set
			{
			}
		}

		public Ease AnimationEase
		{
			get
			{
				return this.animationEase;
			}
			set
			{
				this.animationEase = value;
			}
		}

		protected AnimatedGridLayoutGroup()
		{
		}

		protected override void OnDisable()
		{
		}

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

		private void CapturePreviousPositions()
		{
		}

		private void AnimateToTargetPositions()
		{
		}

		private void CleanupKnownChildren()
		{
		}

		private void KillAllTweens()
		{
		}

		private void SetCellsAlongAxis(int axis)
		{
		}
	}
}
