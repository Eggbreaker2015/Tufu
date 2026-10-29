using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Skills
{
	// Runtime ground circle: the outer disc shows the full radius at once, the inner disc grows to it over the cast point.
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

		private bool active;

		public static SkillTelegraphView Spawn(Vector3 center, float radius, Transform follow = null)
		{
			if (radius <= 0f || activeCount >= MaxActive || !EnsureShared())
				return null;
			SkillTelegraphView view = null;
			while (Pool.Count > 0 && view == null)
				view = Pool.Pop();
			if (view == null)
				view = Create();
			view.active = true;
			activeCount++;
			view.radius = radius;
			view.follow = follow;
			view.gameObject.SetActive(true);
			view.transform.position = center + Vector3.up * GroundOffset;
			view.outer.localScale = Vector3.one * (radius * 2f);
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
			inner.localScale = Vector3.one * (radius * 2f * Mathf.Clamp01(progress));
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

		private void SetColors(Color color)
		{
			SetBlock(outerRenderer, new Color(color.r, color.g, color.b, 0.22f));
			SetBlock(innerRenderer, new Color(color.r, color.g, color.b, 0.45f));
		}

		// Sprites/Default reads _MainTex per renderer, so the texture goes into the block too.
		private static void SetBlock(MeshRenderer meshRenderer, Color color)
		{
			block.Clear();
			block.SetColor(ColorId, color);
			Texture texture = sharedMaterial.mainTexture;
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

		private void OnDestroy()
		{
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
			sharedMaterial = null;
			container = null;
			block = null;
		}
	}
}
