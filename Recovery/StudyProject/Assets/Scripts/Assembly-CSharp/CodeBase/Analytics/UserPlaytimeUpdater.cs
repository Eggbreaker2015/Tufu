using Codebase.Analytics.Systems;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Analytics
{
	public class UserPlaytimeUpdater : MonoBehaviour
	{
		[Inject]
		private UserModel userModel;

		[Inject]
		private IAnalyticsWrapper analyticsWrapper;

		[Inject]
		private AfkPlaytimeSystem afkPlaytimeSystem;

		private float elapsed;

		private int seconds;

		private int activeSeconds;

		private void Start()
		{
		}

		private void Update()
		{
		}
	}
}
