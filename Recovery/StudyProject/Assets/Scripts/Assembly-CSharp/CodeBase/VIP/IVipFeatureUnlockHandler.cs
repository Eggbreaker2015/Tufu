namespace Codebase.VIP
{
	public interface IVipFeatureUnlockHandler
	{
		void OnFeatureUnlocked(IVipFeature feature, int level);
	}
}
