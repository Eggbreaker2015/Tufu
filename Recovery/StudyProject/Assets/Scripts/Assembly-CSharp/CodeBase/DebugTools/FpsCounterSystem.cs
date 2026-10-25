using Scellecs.Morpeh;

namespace Codebase.DebugTools
{
	public class FpsCounterSystem : SystemBase
	{
		private const float UpdateInterval = 0.5f;

		private float elapsed;

		private int frameCount;

		public float CurrentFps { get; private set; }

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
