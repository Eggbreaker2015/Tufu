using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	[RequireComponent(typeof(Image))]
	public class SpriteDeformer : BaseMeshEffect
	{
		public enum DeformDirection
		{
			Horizontal = 0,
			Vertical = 1
		}

		public enum MaskChannel
		{
			Red = 0,
			Green = 1,
			Blue = 2,
			Alpha = 3,
			Grayscale = 4
		}

		[SerializeField]
		private int subdivisions;

		[SerializeField]
		private AnimationCurve deformCurve;

		[SerializeField]
		private float deformStrength;

		[SerializeField]
		private DeformDirection direction;

		[SerializeField]
		[Range(0f, 1f)]
		private float progress;

		[SerializeField]
		private bool animateOnEnable;

		[SerializeField]
		private float animationDuration;

		[Header("Deform Mask")]
		[SerializeField]
		private bool useMask;

		[SerializeField]
		private Texture2D deformMask;

		[SerializeField]
		private MaskChannel maskChannel;

		[SerializeField]
		private Vector2 maskOffset;

		[SerializeField]
		private Vector2 maskScale;

		[SerializeField]
		private bool animateMaskOffset;

		[SerializeField]
		private Vector2 maskScrollSpeed;

		private float _currentProgress;

		private float _animationTime;

		private bool _isAnimating;

		private Vector2 _currentMaskOffset;

		protected override void OnEnable()
		{
		}

		private void Update()
		{
		}

		public void StartAnimation()
		{
		}

		public void SetProgress(float value)
		{
		}

		public void SetMaskOffset(Vector2 offset)
		{
		}

		public void SetMask(Texture2D mask)
		{
		}

		public override void ModifyMesh(VertexHelper vh)
		{
		}

		private List<UIVertex> SubdivideMesh(List<UIVertex> originalVerts)
		{
			return null;
		}

		private UIVertex CreateVertex(List<UIVertex> originalVerts, Rect rect, float u, float v)
		{
			return default;
		}

		private List<UIVertex> ApplyDeformation(List<UIVertex> verts)
		{
			return null;
		}

		private float SampleMask(Vector2 uv)
		{
			return 0f;
		}
	}
}
