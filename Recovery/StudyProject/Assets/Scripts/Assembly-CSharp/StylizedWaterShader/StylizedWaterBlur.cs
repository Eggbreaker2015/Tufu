using UnityEngine;
using UnityEngine.Rendering;

namespace StylizedWaterShader
{
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	public class StylizedWaterBlur : MonoBehaviour
	{
		public Camera cam;

		public float length;

		public int passes;

		private static Shader m_BlurRenderShader;

		private static Material m_BlurRenderMat;

		private CommandBuffer cmd;

		private static Shader BlurRenderShader => null;

		private static Material BlurRenderMat => null;

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}

		public void Render()
		{
		}
	}
}
