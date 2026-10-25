using Codebase.Core;
using Scellecs.Morpeh;

namespace Codebase.Analytics.Systems
{
	public class AfkPlaytimeSystem : SystemBase
	{
		[Inject]
		private UserModel userModel;

		[Inject]
		private IAnalyticsWrapper analyticsWrapper;

		private const float AFK_THRESHOLD = 60f;

		private float timeSinceLastInput;

		private float afkElapsed;

		private int afkSeconds;

		public bool IsAfk => false;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private bool CheckForInput()
		{
			return false;
		}

		private void CountAfkTime(float deltaTime)
		{
		}
	}
}
