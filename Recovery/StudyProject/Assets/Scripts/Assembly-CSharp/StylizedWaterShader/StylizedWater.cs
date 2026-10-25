using System;
using System.Collections.Generic;
using UnityEngine;

namespace StylizedWaterShader
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(MeshRenderer))]
	[HelpURL("http://staggart.xyz/unity/stylized-water-shader/documentation/")]
	public class StylizedWater : MonoBehaviour
	{
		public string[] shaderNames;

		public int shaderIndex;

		public Shader shader;

		private Shader DesktopShader;

		private Shader MobileAdvancedShader;

		[Range(2000f, 4000f)]
		public int renderQueue;

		public bool enableVertexColors;

		public bool enableDepthTex;

		public bool isUnlit;

		public bool enableGradient;

		public Gradient colorGradient;

		[ColorUsage(true, true)]
		public Color waterShallowColor;

		[Range(0f, 100f)]
		public float depth;

		[ColorUsage(true, true)]
		public Color waterColor;

		[ColorUsage(true, true)]
		public Color fresnelColor;

		public float fresnel;

		[ColorUsage(true, true)]
		public Color rimColor;

		[Range(-0.5f, 0.5f)]
		public float waveTint;

		[Range(0f, 1f)]
		public float transparency;

		[Range(0.01f, 1f)]
		public float glossiness;

		[Range(0f, 1f)]
		public float metallicness;

		[Range(0f, 3f)]
		public float edgeFade;

		public string[] tilingMethodNames;

		public float worldSpaceTiling;

		[Range(0f, 0.2f)]
		public float refractionAmount;

		public bool enableNormalMap;

		[Range(0f, 1f)]
		public float normalStrength;

		public bool enableMacroNormals;

		[Range(250f, 3000f)]
		public float macroNormalsDistance;

		[Range(0f, 50f)]
		public float normalTiling;

		public int intersectionSolver;

		public string[] intersectionSolverNames;

		[Range(0f, 30f)]
		public float rimSize;

		[Range(0.1f, 30f)]
		public float rimFalloff;

		public float rimTiling;

		[Range(0f, 1f)]
		public float rimDistortion;

		public bool enableVCIntersection;

		public int foamSolver;

		public string[] foamSolverNames;

		[Range(-1f, 1f)]
		public float foamOpacity;

		public float foamTiling;

		[Range(0f, 1f)]
		public float foamSize;

		[Range(0f, 3f)]
		public float foamDistortion;

		[Range(0f, 1f)]
		public float foamSpeed;

		[Range(0f, 1f)]
		public float waveFoam;

		[Range(0f, 1f)]
		public float reflectionStrength;

		[Range(0.01f, 10f)]
		public float reflectionFresnel;

		public bool showReflection;

		[Range(0f, 0.2f)]
		public float reflectionRefraction;

		[Range(0.01f, 10f)]
		public float waveSpeed;

		[Range(0f, 1f)]
		public float waveStrength;

		[Range(-1f, 1f)]
		public Vector4 waveDirectionXZ;

		public bool enableSecondaryWaves;

		public Texture2D customIntersection;

		public Texture2D customNormal;

		public Texture2D customHeightmap;

		public string[] intersectionStyleNames;

		public int intersectionStyle;

		public string[] waveStyleNames;

		public int waveStyle;

		public string[] waveHeightmapNames;

		public int waveHeightmapStyle;

		public float waveSize;

		public bool useCustomIntersection;

		public bool useCustomNormals;

		public bool useCustomHeightmap;

		public Texture2D normals;

		public Texture2D shadermap;

		public Texture2D colorGradientTex;

		public bool useCompression;

		public static bool EnableReflections;

		private Camera reflectionCamera;

		private Camera refractCamera;

		public bool useReflection;

		public bool useRefractionCam;

		public bool enableReflectionBlur;

		[Range(1f, 15f)]
		public float reflectionBlurLength;

		[Range(1f, 4f)]
		public int reflectionBlurPasses;

		private StylizedWaterBlur reflectionBlurRenderer;

		public string[] refractionSolverNames;

		public int refractionSolver;

		public string[] resolutionNames;

		public int reflectionRes;

		public int refractRes;

		public int reflectionTextureSize;

		public int refractTextureSize;

		[Range(0f, 10f)]
		public float clipPlaneOffset;

		public LayerMask reflectLayers;

		public LayerMask refractLayers;

		private Dictionary<Camera, Camera> m_ReflectionCameras;

		private Dictionary<Camera, StylizedWaterBlur> m_BlurRenderers;

		private Dictionary<Camera, Camera> m_RefractCameras;

		private RenderTexture m_ReflectionTexture;

		private RenderTexture m_RefractTexture;

		private int m_OldReflectionTextureSize;

		private int m_OldRefractTextureSize;

		private bool s_InsideRendering;

		public int lightingMethod;

		public string[] lightingMethodNames;

		public bool enableShadows;

		private StylizedWaterShadowCaster shadowRenderer;

		public Light shadowCaster;

		[NonSerialized]
		private MeshRenderer meshRenderer;

		public Material material;

		public bool isMobileAdvanced;

		public bool isMobilePlatform;

		public string shaderName;

		public bool isWaterLayer;

		public bool hasShaderParams;

		public bool hasMaterial;

		public bool usingSinglePassRendering;

		public bool hideMeshRenderer;

		public void OnEnable()
		{
		}

		public void Init()
		{
		}

		public void GetProperties()
		{
		}

		private void GetShaderProperties()
		{
		}

		private void GetShaderType()
		{
		}

		private void SetShaderType()
		{
		}

		public void SetProperties()
		{
		}

		private void SetShaderProperties()
		{
		}

		public void DisableReflectionCam()
		{
		}

		public void DisableRefractionCam()
		{
		}

		private void EnableShadowRendering()
		{
		}

		private void DisableShadowRendering()
		{
		}

		public void SetVegetationStudioWaterLevel()
		{
		}

		public void OnWillRenderObject()
		{
		}

		public void CreateReflectionTexture()
		{
		}

		public void CreateRefractionTexture()
		{
		}

		private void CreateWaterObjects(Camera currentCamera, out Camera reflectionCamera, out Camera refractCamera)
		{
			reflectionCamera = null;
			refractCamera = null;
		}

		private void OnDisable()
		{
		}

		private void OnDestroy()
		{
		}
	}
}
