using UnityEngine;
using UnityEngine.Rendering;

namespace StylizedWaterShader
{
	[RequireComponent(typeof(Light))]
	[ExecuteInEditMode]
	public class StylizedWaterShadowCaster : MonoBehaviour
	{
		private CommandBuffer cmd;

		public Light dirLight;

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}
	}
}
