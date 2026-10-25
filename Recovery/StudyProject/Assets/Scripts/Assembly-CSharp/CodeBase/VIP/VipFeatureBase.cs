namespace Codebase.VIP
{
	public abstract class VipFeatureBase : IVipFeature
	{
		protected int unlockLevel;
		public abstract string Key { get; }
		public int UnlockLevel => this.unlockLevel;

		protected VipFeatureBase(int unlockLevel)
		{
			this.unlockLevel = unlockLevel;
		}

		public bool IsUnlocked(int vipLevel)
		{
			return vipLevel >= unlockLevel;
		}

		public virtual void OnUnlocked(int vipLevel)
		{
		}

		public virtual void OnLevelUp(int fromLevel, int toLevel)
		{
		}
	}
}
