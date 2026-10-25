using UnityEngine;

namespace Codebase.BossTower
{
	public class LavaCrabHitBlinkView : MonoBehaviour
	{
		[SerializeField]
		private Renderer targetRenderer;

		[SerializeField]
		private string propertyName = "_EmissionColor";

		[SerializeField]
		private Color targetColor = Color.white;

		[SerializeField]
		private Color offColor = new Color(0f, 0f, 0f, 1f);

		[SerializeField]
		private float minFrequency = 0.2f;

		[SerializeField]
		private float maxFrequency = 1.5f;

		[SerializeField]
		private int startHits = 3;

		private int currentHits;

		private int maxHits;

		private bool isActive;

		private bool interactsWithRenderer = true;

		private Color originalColor;

		private void Awake()
		{
			// Original ELF VA 0x155d228; shared material access is intentional.
			originalColor = targetRenderer.sharedMaterial.GetColor(propertyName);
		}

		private void OnEnable()
		{
			Reset();
		}

		public void SetHits(int current, int max)
		{
			currentHits = current;
			maxHits = max;
			isActive = current >= startHits;
			if (!isActive && interactsWithRenderer) targetRenderer.sharedMaterial.SetColor(propertyName, offColor);
		}

		public void StopRendererInteraction()
		{
			interactsWithRenderer = false;
			targetRenderer.sharedMaterial.SetColor(propertyName, offColor);
		}

		public void Reset()
		{
			isActive = false;
			interactsWithRenderer = true;
			currentHits = 0;
			targetRenderer.sharedMaterial.SetColor(propertyName, originalColor);
		}

		private void Update()
		{
			// Original ELF VA 0x155d348: cubic hit progress controls frequency,
			// and the sine wave starts at offColor rather than its midpoint.
			if (!isActive || !interactsWithRenderer) return;
			int span = maxHits - startHits;
			float progress = span < 1 ? 0f : Mathf.Clamp01((float)(currentHits - startHits) / span);
			float frequency = Mathf.Lerp(minFrequency, maxFrequency, progress * progress * progress);
			float phase = Time.time * frequency * Mathf.PI;
			float amount = (Mathf.Sin(phase + phase - 1.5707963705062866f) + 1f) * 0.5f;
			targetRenderer.sharedMaterial.SetColor(propertyName, Color.Lerp(offColor, targetColor, amount));
		}
	}
}
