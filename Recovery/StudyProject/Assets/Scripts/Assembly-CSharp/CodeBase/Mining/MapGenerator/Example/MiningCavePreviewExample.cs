using Codebase.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.Mining.MapGenerator.Example
{
	public sealed class MiningCavePreviewExample : MonoBehaviour
	{
		[Header("Target")]
		[SerializeField]
		private Renderer targetRenderer;

		[SerializeField]
		private RawImage targetRawImage;

		[Header("Auto Preview Object")]
		[SerializeField]
		private bool createQuadIfNoTarget;

		[SerializeField]
		private Vector2 quadWorldSize;

		[SerializeField]
		private Vector3 quadLocalOffset;

		[SerializeField]
		private Shader previewShader;

		[Header("Generation")]
		[SerializeField]
		private MapGenConfig config;

		[SerializeField]
		private bool generateOnStart;

		[SerializeField]
		private bool useGenerateCaveWithRetries;

		[Header("Colors")]
		[SerializeField]
		private Color32 wallColor;

		[SerializeField]
		private Color32 floorColor;

		[SerializeField]
		private Color32 roadColor;

		[SerializeField]
		private Color32 corridorColor;

		[SerializeField]
		private Color32 baseColor;

		[SerializeField]
		private Color32 portalUpColor;

		[SerializeField]
		private Color32 portalDownColor;

		[SerializeField]
		private Color32 spawnerColor;

		[SerializeField]
		private Color32 guardSpawnerColor;

		[SerializeField]
		private Color32 buildingColor;

		[SerializeField]
		private Color32 digWallColor;

		[SerializeField]
		private Color32 treasureColor;

		private Texture2D generatedTexture;

		private static Mesh doubleSidedQuadMesh;

		[StaticReset]
		private static void ResetStaticState()
		{
		}

		private void Reset()
		{
		}

		[ContextMenu("Set Example Config Defaults")]
		private void SetExampleConfigDefaults()
		{
		}

		private void Start()
		{
		}

		[ContextMenu("Generate Preview")]
		public void GenerateAndApply()
		{
		}

		private Texture2D BuildTexture(MapGenResult map)
		{
			return null;
		}

		private void ApplyTexture(Texture2D tex)
		{
		}

		private void EnsureTarget()
		{
		}

		private static Mesh BuildDoubleSidedQuadMesh()
		{
			return null;
		}
	}
}
