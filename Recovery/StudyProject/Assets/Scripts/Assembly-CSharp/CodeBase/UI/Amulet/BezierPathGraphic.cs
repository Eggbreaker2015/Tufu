using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class BezierPathGraphic : MaskableGraphic
	{
		[SerializeField]
		private float thickness = 6f;

		[SerializeField]
		private float tileScale = .05f;

		[SerializeField]
		private int bezierSegments = 20;

		[SerializeField]
		private Texture2D pathTexture;

		private readonly List<Vector2> points = new List<Vector2>();

		private readonly List<Vector2> sampledPoints = new List<Vector2>();

		// Original ELF VA 0x13d91f0 uses Graphic.s_WhiteTexture fallback.
        public override Texture mainTexture => pathTexture != null ? pathTexture : s_WhiteTexture;

		// Native ELF VA 0x13d92a0 calls virtual defaultMaterial; setter 0x13d92b0 is RET.
        public override Material material
        {
            get => defaultMaterial;
            set { }
        }

        public override Material defaultMaterial => Canvas.GetDefaultCanvasMaterial();

        public void SetPath(Vector2 start, Vector2 end, int seed)
		{
// Original ELF VA 0x13d92bc. Same seed produces the same single perpendicular control-point offset.
            points.Clear();
            points.Add(start);
            var random = new System.Random(seed);
            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y,direction.x);
            float offset = (float)(random.NextDouble() * 2.0 - 1.0);
            Vector2 middle = (start + end) * .5f + perpendicular * offset * Vector2.Distance(start,end) * .3f;
            points.Add(middle);
            points.Add(end);
            SampleBezierCurve();
            SetVerticesDirty();
		}

		public void SetPath(List<Vector2> controlPoints)
		{
// Original ELF VA 0x13d97cc.
            points.Clear();
            points.AddRange(controlPoints);
            SampleBezierCurve();
            SetVerticesDirty();
		}

		public void SetThickness(float value)
		{
// Original ELF VA 0x13d98a8, no clamp.
            thickness = value;
            SetVerticesDirty();
		}

		public void SetTileScale(float value)
		{
// Original ELF VA 0x13d98bc, no clamp.
            tileScale = value;
            SetVerticesDirty();
		}

		private void SampleBezierCurve()
		{
// Original ELF VA 0x13d9594: two points are a line; larger lists use the first three only.
            sampledPoints.Clear();
            if (points.Count < 2) return;
            if (points.Count == 2)
            {
                sampledPoints.Add(points[0]);
                sampledPoints.Add(points[1]);
                return;
            }
            for (int index = 0; index <= bezierSegments; index++)
                sampledPoints.Add(EvaluateQuadraticBezier(points[0],points[1],points[2],(float)index / bezierSegments));
		}

		private Vector2 EvaluateQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
		{
// Original ELF VA 0x13d98d0; no time clamp and native addition ordering retained.
            float inverse = 1f - t;
            float startWeight = inverse * inverse;
            float middleWeight = (inverse + inverse) * t;
            float endWeight = t * t;
            return p0 * startWeight + p1 * middleWeight + p2 * endWeight;
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
// Original ELF VA 0x13d9914: one independent textured quad per sampled segment.
            vh.Clear();
            if (sampledPoints.Count < 2) return;
            float distance = 0f;
            for (int index = 0; index < sampledPoints.Count - 1; index++)
            {
                Vector2 start = sampledPoints[index];
                Vector2 end = sampledPoints[index + 1];
                Vector2 direction = (end - start).normalized;
                float u0 = distance * tileScale;
                distance += Vector2.Distance(start,end);
                float u1 = distance * tileScale;
                Vector2 perpendicular = new Vector2(-direction.y,direction.x) * thickness * .5f;
                int first = vh.currentVertCount;
                vh.AddVert(start - perpendicular,color,new Vector2(u0,0f));
                vh.AddVert(start + perpendicular,color,new Vector2(u0,1f));
                vh.AddVert(end + perpendicular,color,new Vector2(u1,1f));
                vh.AddVert(end - perpendicular,color,new Vector2(u1,0f));
                vh.AddTriangle(first,first + 1,first + 2);
                vh.AddTriangle(first,first + 2,first + 3);
            }
		}
	}
}
