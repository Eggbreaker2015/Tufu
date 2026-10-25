using UnityEngine;

namespace Codebase.BossTower
{
	public class MeshBlinkEffect : MonoBehaviour
	{
		private int BaseColorId;
		[SerializeField]
		private Renderer targetRenderer;
		[SerializeField]
		private Color targetColor = Color.white;
		[SerializeField]
		private Color offColor = Color.black;
		[SerializeField]
		private float frequency = 1.5f;
		[SerializeField]
		private string propertyName = "_BaseColor";
		private MaterialPropertyBlock propertyBlock;
		private bool isActive;
		private void Awake()
		{
			BaseColorId = Shader.PropertyToID(propertyName);
			propertyBlock = new MaterialPropertyBlock();
		}

		public void Enable()
		{
			isActive = true;
		}

		public void Disable()
		{
			isActive = false;
			ApplyColor(offColor);
		}

		private void Update()
		{
			if (!isActive || targetRenderer == null)
				return;
			float phase = Time.time * frequency * Mathf.PI;
			float amount = (Mathf.Sin(phase + phase - 1.5707963705062866f) + 1f) * 0.5f;
			ApplyColor(Color.Lerp(offColor, targetColor, amount));
		}

		private void ApplyColor(Color color)
		{ // Reconstruction compatibility: the original provider may invoke Disable before this Awake in the current Mono host.
			// Keep the original initialization and property-block sequence; preserve unrelated renderer properties.
			if (propertyBlock == null)
				Awake();
			targetRenderer.GetPropertyBlock(propertyBlock);
			propertyBlock.SetColor(BaseColorId, color);
			targetRenderer.SetPropertyBlock(propertyBlock);
		}
	}
}
