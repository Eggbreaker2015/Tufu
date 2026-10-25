using Codebase.Analytics;

namespace Codebase.Integrations.AppMetrica
{
	public class AppMetricaAnalyticsWrapper : IAnalyticsWrapper
	{
		public void Send<TEvent>(TEvent eventData) where TEvent : BaseAnalyticsEvent
		{
		}
	}
}
