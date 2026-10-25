namespace Codebase.Analytics
{
	public interface IAnalyticsWrapper
	{
		void Send<TEvent>(TEvent eventData) where TEvent : BaseAnalyticsEvent;
	}
}
