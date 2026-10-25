namespace Codebase.VIP
{
	public interface IVipFeature
	{
		string Key { get; }

		int UnlockLevel { get; }

		bool IsUnlocked(int vipLevel);

		void OnUnlocked(int vipLevel);

		void OnLevelUp(int fromLevel, int toLevel);
	}
}
