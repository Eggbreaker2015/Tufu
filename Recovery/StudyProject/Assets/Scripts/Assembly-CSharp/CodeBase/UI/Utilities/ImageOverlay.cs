using DG.Tweening;
using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Utilities
{
	[ExecuteAlways]
	public class ImageOverlay : MonoBehaviour
	{
		private readonly struct MaterialKey
		{
			private readonly ImageBlendMode blendMode;

			private readonly Color blendColor;

			private readonly float intensity;

			public MaterialKey(ImageBlendMode blendMode, Color blendColor, float intensity)
			{
// Original ELF VA 0x160bef4.
                this.blendMode = blendMode;
                this.blendColor = blendColor;
                this.intensity = intensity;
		}

			public override bool Equals(object obj)
			{
// Original ELF VA 0x160c590 uses Unity's approximate Color == and Single.Equals.
                return obj is MaterialKey other && blendMode == other.blendMode && blendColor == other.blendColor && intensity.Equals(other.intensity);
		}

			public override int GetHashCode()
			{
// Original ELF VA 0x160c660: signed shifts and unchecked 397 multipliers.
                unchecked
                {
                    int colorHash = blendColor.r.GetHashCode() ^ (blendColor.g.GetHashCode() << 2) ^ (blendColor.b.GetHashCode() >> 2) ^ (blendColor.a.GetHashCode() >> 1);
                    return (((int)blendMode * 397) ^ colorHash) * 397 ^ intensity.GetHashCode();
                }
		}
		}

		private readonly struct ImageMaterialPair
		{
			public readonly Image Image;

			public readonly Material OriginalMaterial;

			public ImageMaterialPair(Image image, Material originalMaterial)
			{
// Original ELF VA 0x160c048.
                Image = image;
                OriginalMaterial = originalMaterial;
		}
		}

		private static readonly int BlendColorId = Shader.PropertyToID("_BlendColor");

		private static readonly int BlendIntensityId = Shader.PropertyToID("_BlendIntensity");

		private static readonly int BlendModeId = Shader.PropertyToID("_BlendMode");

		private static readonly Dictionary<MaterialKey, Material> BlendMaterials = new Dictionary<MaterialKey, Material>();

		[SerializeField]
		private ImageBlendMode _blendMode = ImageBlendMode.Overlay;

		[SerializeField]
		private Color _blendColor = Color.white;

		[SerializeField]
		[Range(0f, 1f)]
		private float _intensity = 1f;

		[SerializeField]
		private bool _applyToChildren;

		private readonly List<ImageMaterialPair> _targets = new List<ImageMaterialPair>();

		private Material _blendMaterial;

		private bool _ownsBlendMaterial;

		// Original ELF VA 0x160b0a8..0x160b35c.
        public ImageBlendMode BlendMode
        {
            get => _blendMode;
            set { _blendMode = value; ApplyProperties(); }
        }
        public Color BlendColor
        {
            get => _blendColor;
            set { _blendColor = value; ApplyProperties(); }
        }
        public float Intensity
        {
            get => _intensity;
            set { _intensity = Mathf.Clamp01(value); ApplyProperties(); }
        }
        public bool ApplyToChildren
        {
            get => _applyToChildren;
            set
            {
                if (_applyToChildren == value) return;
                RestoreOriginalMaterials();
                _applyToChildren = value;
                CollectTargets();
                ApplyBlendMaterial();
            }
        }

        public void AnimateIntensity(float duration)
		{
// Original ELF VA 0x160b1ec. Original tween is not given a target or link.
            float targetIntensity = _intensity;
            enabled = true;
            _intensity = 0f;
            ApplyProperties();
            DOTween.To(() => _intensity,value => Intensity = value,targetIntensity,duration);
		}

		private void OnEnable()
		{
// Original ELF VA 0x160ba78.
            CollectTargets();
            _blendMaterial = GetSharedBlendMaterial(_blendMode,_blendColor,_intensity);
            ApplyBlendMaterial();
		}

		private void OnDisable()
		{
// Original ELF VA 0x160bd58.
            DOTween.Kill(this);
            RestoreOriginalMaterials();
            ReleaseBlendMaterial();
		}

		private void OnDestroy()
		{
// Original ELF VA 0x160bedc.
            RestoreOriginalMaterials();
            ReleaseBlendMaterial();
		}

		private static Material GetSharedBlendMaterial(ImageBlendMode blendMode, Color blendColor, float intensity)
		{
// Original ELF VA 0x160bb28. Shader/key/error strings resolved from original native metadata.
            var key = new MaterialKey(blendMode,blendColor,intensity);
            if (BlendMaterials.TryGetValue(key,out Material material)) return material;
            Shader shader = Shader.Find("UI/ColorBlend");
            if (shader == null)
            {
                Debug.LogError("Shader 'UI/ColorBlend' not found");
                return null;
            }
            material = new Material(shader);
            SetMaterialProperties(material,blendMode,blendColor,intensity);
            BlendMaterials[key] = material;
            return material;
		}

		private void CollectTargets()
		{
// Original ELF VA 0x160b5dc includes inactive child Images when requested.
            _targets.Clear();
            if (_applyToChildren)
            {
                foreach (Image image in GetComponentsInChildren<Image>(true)) _targets.Add(new ImageMaterialPair(image,image.material));
            }
            else
            {
                Image image = GetComponent<Image>();
                if (image != null) _targets.Add(new ImageMaterialPair(image,image.material));
            }
		}

		private void ApplyBlendMaterial()
		{
// Original ELF VA 0x160b83c.
            if (_blendMaterial == null) return;
            foreach (ImageMaterialPair pair in _targets) if (pair.Image != null) pair.Image.material = _blendMaterial;
		}

		private void RestoreOriginalMaterials()
		{
// Original ELF VA 0x160b364.
            foreach (ImageMaterialPair pair in _targets) if (pair.Image != null) pair.Image.material = pair.OriginalMaterial;
            _targets.Clear();
		}

		private void ApplyProperties()
		{
// Original ELF VA 0x160b0b8 clones shared material on the first mutation only.
            if (_blendMaterial == null) return;
            CreateOwnedBlendMaterial();
            SetMaterialProperties(_blendMaterial,_blendMode,_blendColor,_intensity);
		}

		private void CreateOwnedBlendMaterial()
		{
// Original ELF VA 0x160c078.
            if (_ownsBlendMaterial) return;
            _blendMaterial = new Material(_blendMaterial);
            _ownsBlendMaterial = true;
            ApplyBlendMaterial();
		}

		private void ReleaseBlendMaterial()
		{
// Original ELF VA 0x160bdc4. Shared entries remain owned by the static cache.
            if (_blendMaterial != null && _ownsBlendMaterial)
            {
                if (Application.isPlaying) Destroy(_blendMaterial);
                else DestroyImmediate(_blendMaterial);
                _blendMaterial = null;
                _ownsBlendMaterial = false;
                return;
            }
            _blendMaterial = null;
		}

		private static void SetMaterialProperties(Material material, ImageBlendMode blendMode, Color blendColor, float intensity)
		{
// Original ELF VA 0x160bf08.
            if (material == null) return;
            material.SetFloat(BlendModeId,(float)blendMode);
            material.SetColor(BlendColorId,blendColor);
            material.SetFloat(BlendIntensityId,intensity);
		}

		[StaticReset]
		private static void ResetStaticState()
		{
// Original ELF VA 0x160c114.
            foreach (Material material in BlendMaterials.Values) if (material != null) Destroy(material);
            BlendMaterials.Clear();
		}
	}
}
