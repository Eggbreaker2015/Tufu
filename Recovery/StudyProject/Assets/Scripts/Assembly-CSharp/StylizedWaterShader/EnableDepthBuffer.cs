using UnityEngine;

namespace StylizedWaterShader
{
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	public class EnableDepthBuffer : MonoBehaviour
	{
		private Camera cam;

		private void OnValidate()
		{
		}

		private void OnEnable()
		{
		}

		private void OnDestroy()
		{
		}
	}
}
