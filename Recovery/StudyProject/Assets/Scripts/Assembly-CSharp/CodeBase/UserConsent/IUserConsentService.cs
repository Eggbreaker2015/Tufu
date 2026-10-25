namespace Codebase.UserConsent
{
	public interface IUserConsentService
	{
		bool HasUserConsent();

		bool IsDoNotSell();

		bool IsAppTrackingAllowed();
	}
}
