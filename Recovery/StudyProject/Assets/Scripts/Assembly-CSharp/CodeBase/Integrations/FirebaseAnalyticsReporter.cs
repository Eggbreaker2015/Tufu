using UnityEngine;

namespace Codebase.Integrations
{
	public class FirebaseAnalyticsReporter : MonoBehaviour
	{
		[SerializeField]
		private FirebaseInitializer initializer;

		private static int[] ImpressionValues => null;

		private static float[] RevenueValues => null;

		private static int[] ECPMValues => null;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<FirebaseAnalyticsReporter>(this);
        }

		public void ReportRevenue(string productId, decimal commandPrice, string commandCurrency)
		{
		}

		public void ReportAdRevenue(double revenue, string currency, string networkName, string adUnitIdentifier, string placement, string creativeId, string adFormat)
		{
		}

		private void UpdateAdAchievements()
		{
		}

		private void SendAchievement(string evtName)
		{
		}
	}
}
