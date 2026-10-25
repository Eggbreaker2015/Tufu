using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Codebase.Services.PostProcessing
{
	public class PostProcessingProvider : MonoBehaviour
	{
		[SerializeField]
		private PostProcessVolume postProcessVolume;

		public PostProcessVolume PostProcessVolume => this.postProcessVolume;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<PostProcessingProvider>(this);
        }
	}
}
