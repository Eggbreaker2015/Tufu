using Codebase.Core;
using UniMob;
using UniMob.Core;

namespace Codebase.Crystals
{
	public class CrystalSlotModel : Model
	{
		private CrystalSlotDataUd data;
		private bool isUnlocked;
		private ComputedAtom<bool> __IsUnlocked;
		public string Key { get; }
		public CrystalSlotUnlockSource UnlockSource { get; }
		public string UnlockUpgradeKey { get; }

		public bool IsUnlocked
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUnlocked == null)
					__IsUnlocked = CodeGenAtom.CreatePooled(this, null, () => IsUnlocked, false);
				return __IsUnlocked.DirectEvaluate() ? isUnlocked : __IsUnlocked.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUnlocked != null && !__IsUnlocked.CompareAndInvalidate(value))
					return;
				isUnlocked = value;
				if (data != null)
					data.IsUnlocked.Value = value;
			}
		}

		public CrystalSlotModel(Lifetime lifetime, string key, bool isUnlocked, CrystalSlotUnlockSource unlockSource, string unlockUpgradeKey) : base(lifetime)
		{
			Key = key;
			this.isUnlocked = isUnlocked;
			UnlockSource = unlockSource;
			UnlockUpgradeKey = unlockUpgradeKey;
		}

		public void BindToUserData(CrystalSlotDataUd ud)
		{
			data = ud;
			isUnlocked = ud.IsUnlocked.Value;
		}
	}
}
