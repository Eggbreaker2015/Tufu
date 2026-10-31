using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Player.Flows;
using Cysharp.Threading.Tasks;

namespace Codebase.VIP.Features
{
	public class ExtraWeaponSlotFeature : VipFeatureBase
	{
		public override string Key
		{
			get
			{
				return "extra_weapon_slot";
			}
		}

		public ExtraWeaponSlotFeature(int unlockLevel) : base(unlockLevel)
		{
		}

		public override void OnUnlocked(int vipLevel)
		{
			return;
		}
	}
}
