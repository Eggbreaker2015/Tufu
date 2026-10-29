using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Skills
{
	// Runtime ground shapes: the inner shape grows to the outer boundary over the cast point.
	// Pooled; at most MaxActive on screen, extra requests draw nothing.
	public class SkillTelegraphView : MonoBehaviour
	{
		public const int MaxActive = 6;

		private const float GroundOffset = 0.06f;

		private const string ShaderName = "Sprites/Default";

		private static readonly Color DefaultColor = new Color(1f, 0.15f, 0.1f, 1f);

		private static readonly Stack<SkillTelegraphView> Pool = new Stack<SkillTelegraphView>();

		private static int activeCount;

		private static Mesh quadMesh;

		private static Mesh lineMesh;

		private static Material sharedMaterial;

		private static Transform container;

		private static MaterialPropertyBlock block;

		private static readonly int ColorId = Shader.PropertyToID("_Color");

		private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

		private Transform outer;

		private Transform inner;

		private MeshRenderer outerRenderer;

		private MeshRenderer innerRenderer;

		private Transform follow;

		private float radius;

		private TelegraphShape shape;

		private float width;

		private float angle;

		private Vector3 direction;

		private Mesh fanMesh;

		private float fanAngle;

		private bool active;

		public static SkillTelegraphView Spawn(Vector3 center, float radius, Transform follow = null)
		{
			return Spawn(TelegraphShape.Circle, center, Vector3.forward, radius, 90f, 1f, follow);
		}

		public static SkillTelegraphView Spawn(TelegraphShape shape, Vector3 origin, Vector3 direction, float radius, float angle, float width, Transform follow = null)
		{
			if (shape == TelegraphShape.None || radius <= 0f || activeCount >= MaxActive || !EnsureShared())
				return null;
			SkillTelegraphView view = null;
			while (Pool.Count > 0 && view == null)
				view = Pool.Pop();
			if (view == null)
				view = Create();
			view.active = true;
			activeCount++;
			view.radius = radius;
			view.shape = shape;
			view.width = width > 0f ? width : 1f;
			view.angle = Mathf.Clamp(angle > 0f ? angle : 90f, 0.01f, 360f);
			direction.y = 0f;
			view.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
			view.follow = follow;
			view.gameObject.SetActive(true);
			view.transform.position = origin + Vector3.up * GroundOffset;
			view.transform.rotation = shape == TelegraphShape.Circle ? Quaternion.identity : Quaternion.LookRotation(view.direction);
			Mesh mesh = shape == TelegraphShape.Circle ? quadMesh : shape == TelegraphShape.Line ? lineMesh : view.GetFan();
			Quaternion rotation = shape == TelegraphShape.Circle ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
			view.outer.GetComponent<MeshFilter>().sharedMesh = mesh;
			view.inner.GetComponent<MeshFilter>().sharedMesh = mesh;
			view.outer.localRotation = rotation;
			view.inner.localRotation = rotation;
			view.outer.localScale = view.GetScale(1f);
			view.SetColors(DefaultColor);
			view.SetProgress(0f);
			return view;
		}

		public void SetProgress(float progress)
		{
			if (!active)
				return;
			if (follow != null)
			{
				Vector3 position = follow.position;
				transform.position = position + Vector3.up * GroundOffset;
			}
			inner.localScale = GetScale(Mathf.Clamp01(progress));
		}

		public void LockPosition()
		{
			// Channels keep the warning where the wind-up finished, even if pushed.
			follow = null;
		}

		public void Despawn()
		{
			if (!active)
				return;
			active = false;
			activeCount = Mathf.Max(0, activeCount - 1);
			follow = null;
			if (this == null)
				return;
			gameObject.SetActive(false);
			Pool.Push(this);
		}

		private Vector3 GetScale(float progress)
		{
			if (shape == TelegraphShape.Line)
				return new Vector3(width, 1f, radius * progress);
			if (shape == TelegraphShape.Cone)
				return new Vector3(radius * progress, 1f, radius * progress);
			return Vector3.one * (radius * 2f * progress);
		}

		private Mesh GetFan()
		{
			if (fanMesh != null && Mathf.Abs(fanAngle - angle) <= 0.01f)
				return fanMesh;
			if (fanMesh == null)
				fanMesh = new Mesh { name = "Skill Telegraph Fan" };
			fanMesh.Clear();
			fanAngle = angle;
			int segments = Mathf.Clamp(Mathf.CeilToInt(angle / 6f), 4, 64);
			var vertices = new Vector3[segments + 2];
			var normals = new Vector3[vertices.Length];
			var uv = new Vector2[vertices.Length];
			var triangles = new int[segments * 3];
			normals[0] = Vector3.up;
			uv[0] = new Vector2(0.5f, 0.5f);
			for (int i = 0; i <= segments; i++)
			{
				float a = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, (float)i / segments) * Mathf.Deg2Rad;
				vertices[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
				normals[i + 1] = Vector3.up;
				uv[i + 1] = new Vector2(vertices[i + 1].x * 0.5f + 0.5f, vertices[i + 1].z * 0.5f + 0.5f);
				if (i == segments)
					continue;
				triangles[i * 3] = 0;
				triangles[i * 3 + 1] = i + 1;
				triangles[i * 3 + 2] = i + 2;
			}
			fanMesh.vertices = vertices;
			fanMesh.normals = normals;
			fanMesh.uv = uv;
			fanMesh.triangles = triangles;
			fanMesh.RecalculateBounds();
			return fanMesh;
		}

		private void SetColors(Color color)
		{
			Texture texture = shape == TelegraphShape.Circle ? sharedMaterial.mainTexture : Texture2D.whiteTexture;
			SetBlock(outerRenderer, new Color(color.r, color.g, color.b, 0.22f), texture);
			SetBlock(innerRenderer, new Color(color.r, color.g, color.b, 0.45f), texture);
		}

		// Sprites/Default reads _MainTex per renderer, so the texture goes into the block too.
		private static void SetBlock(MeshRenderer meshRenderer, Color color, Texture texture)
		{
			block.Clear();
			block.SetColor(ColorId, color);
			if (texture != null)
				block.SetTexture(MainTexId, texture);
			meshRenderer.SetPropertyBlock(block);
		}

		private static SkillTelegraphView Create()
		{
			if (container == null)
				container = new GameObject("Skill Telegraphs").transform;
			var root = new GameObject("Skill Telegraph");
			root.transform.SetParent(container, false);
			SkillTelegraphView view = root.AddComponent<SkillTelegraphView>();
			view.outer = CreateDisc(root.transform, "Outer", 0, out view.outerRenderer);
			view.inner = CreateDisc(root.transform, "Inner", 1, out view.innerRenderer);
			return view;
		}

		private static Transform CreateDisc(Transform parent, string name, int order, out MeshRenderer meshRenderer)
		{
			var disc = new GameObject(name);
			disc.transform.SetParent(parent, false);
			disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
			disc.transform.localPosition = Vector3.up * (order * 0.01f);
			disc.AddComponent<MeshFilter>().sharedMesh = quadMesh;
			meshRenderer = disc.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = sharedMaterial;
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			meshRenderer.sortingOrder = order;
			return disc.transform;
		}

		private static bool EnsureShared()
		{
			if (sharedMaterial == null)
			{
				Shader shader = Shader.Find(ShaderName);
				if (shader == null)
				{
					SkillLibrary.WarnOnce("telegraph-shader", "[Skills] Shader '" + ShaderName + "' not found, telegraphs disabled");
					return false;
				}
				sharedMaterial = new Material(shader) { name = "Skill Telegraph", renderQueue = 3100 };
				Texture2D texture = SkillLibrary.SharedTelegraphTexture;
				if (texture != null)
					sharedMaterial.mainTexture = texture;
			}
			if (quadMesh == null)
				quadMesh = CreateQuad();
			if (lineMesh == null)
				lineMesh = CreateLine();
			if (block == null)
				block = new MaterialPropertyBlock();
			return true;
		}

		private static Mesh CreateQuad()
		{
			var mesh = new Mesh { name = "Skill Telegraph Quad" };
			mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
			mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
			// Facing up after the disc's 90 degree X rotation.
			mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		private static Mesh CreateLine()
		{
			var mesh = new Mesh { name = "Skill Telegraph Line" };
			mesh.vertices = new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 0f, 1f), new Vector3(0.5f, 0f, 1f) };
			mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
			mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		private void OnDestroy()
		{
			if (fanMesh != null)
				Destroy(fanMesh);
			if (active)
			{
				active = false;
				activeCount = Mathf.Max(0, activeCount - 1);
			}
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			Pool.Clear();
			activeCount = 0;
			quadMesh = null;
			lineMesh = null;
			sharedMaterial = null;
			container = null;
			block = null;
		}
	}
}
