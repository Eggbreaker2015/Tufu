using Codebase.Core;

namespace Codebase.VIP
{
	public class VipData : ISaveable
	{
		public DataVar<int> DailyChestCooldownTimestamp;

		public DataVar<int> AutoFarmCooldownTimestamp;

		public void Initialize()
		{
		}
	}
}
