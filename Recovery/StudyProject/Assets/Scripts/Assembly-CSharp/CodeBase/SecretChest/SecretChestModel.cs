using System.Runtime.CompilerServices;
using Codebase.Core;
using UniMob;
using UniMob.Core;

namespace Codebase.SecretChest
{
	public class SecretChestModel : Model<SecretChestsUd>
	{
		[CompilerGenerated]
		private int __TotalCount__BackingField;

		private ComputedAtom<int> __TotalCount;

		public int TotalCount
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		public int OpenedCount => 0;

		public SecretChestModel(Lifetime lifetime)
			: base(default(Lifetime))
		{
		}

		public bool IsOpened(string chestKey)
		{
			return false;
		}

		public void MarkOpened(string chestKey)
		{
		}

		public void SetTotalCount(int count)
		{
		}
	}
}
