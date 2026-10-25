using UnityEngine;

namespace Codebase.Abilities.Vfx
{
	public class ShockVfxBinder : MonoBehaviour
	{
		[SerializeField]
		private ParticleSystem particleSystem;

		private ParticleSystem.MinMaxCurve originalStartSize;

		private bool originalCaptured;

		private void Awake()
		{
		}

		public void Bind(SkinnedMeshRenderer renderer)
		{
		}

		public void Unbind()
		{
		}

		private void CaptureOriginal()
		{
		}

		private static ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve source, float multiplier)
		{
			return default;
		}
	}
}
