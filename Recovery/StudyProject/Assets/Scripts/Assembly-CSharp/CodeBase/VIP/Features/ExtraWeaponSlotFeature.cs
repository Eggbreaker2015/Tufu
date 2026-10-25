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
			base.OnUnlocked(vipLevel);
			var slot = GameContext.Get<PlayerModel>().ItemSlots.Find(item => item.Key == "weapon_5_vip");
			if (slot == null)
				return;
			bool wasUnlocked = slot.IsUnlocked;
			slot.IsUnlocked = true;
			if (!wasUnlocked)
				App.InitiateFlow(new ShowVipSlotUnlockViewFlow()).Forget();
		}
	}
}
