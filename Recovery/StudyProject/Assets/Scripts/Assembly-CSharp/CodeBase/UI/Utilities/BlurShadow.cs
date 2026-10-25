using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Utilities
{
	[AddComponentMenu("UI/Effects/Blur Shadow")]
	public class BlurShadow : BaseMeshEffect
	{
		[SerializeField]
		private Color _effectColor;

		[SerializeField]
		private Vector2 _scale;

		[SerializeField]
		[Range(1f, 32f)]
		private int _iterations;

		[SerializeField]
		[Range(0.5f, 6f)]
		private float _falloff;

		[SerializeField]
		private bool _useGraphicAlpha;

		private readonly List<UIVertex> _output;

		private readonly List<UIVertex> _result;

		public Color EffectColor
		{
			get
			{
				return default;
			}
			set
			{
			}
		}

		public Vector2 Scale
		{
			get
			{
				return default;
			}
			set
			{
			}
		}

		public int Iterations
		{
			get
			{
				return this._iterations;
			}
			set
			{
			}
		}

		public float Falloff
		{
			get
			{
				return this._falloff;
			}
			set
			{
			}
		}

		public override void ModifyMesh(VertexHelper vertexHelper)
		{
		}

		private void AppendScaledCopy(int count, Vector2 center, Vector2 layerScale, float layerAlpha)
		{
		}

		private void MarkDirty()
		{
		}
	}
}
