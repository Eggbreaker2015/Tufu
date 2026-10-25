using Codebase.Analytics;
using Codebase.Core;
using Codebase.Services.PostProcessing;
using JetBrains.Annotations;
using Scellecs.Morpeh;

namespace Codebase.Player.Systems
{
	public class QualitySystem : SystemBase
	{
		[Inject]
		private UserModel userModel;

		[Inject]
		private PostProcessingProvider postProcessingProvider;

		private bool highQuality;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void UpdateQuality()
		{
		}

		[UsedImplicitly]
		private int GetTargetMaxFrameRate()
		{
			return unchecked((int)60L);
		}
	}
}
