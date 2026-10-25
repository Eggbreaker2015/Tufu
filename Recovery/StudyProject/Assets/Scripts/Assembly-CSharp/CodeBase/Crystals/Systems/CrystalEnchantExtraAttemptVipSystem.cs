using Codebase.Core;
using Codebase.Meta;
using Codebase.VIP;
using Scellecs.Morpeh;

namespace Codebase.Crystals.Systems
{
	public class CrystalEnchantExtraAttemptVipSystem : SystemBase
	{
		private const string ModifierName = "vip_crystal_enchant_extra_attempts";
		[Inject]
		private VipModel vipModel;
		[Inject]
		private MetaStatsModel metaStatsModel;
		private int prevExtraAttempts;
		public override void OnAwake()
		{
			Apply(GetVipExtraAttempts());
		}

		public override void OnUpdate(float deltaTime)
		{
			int extra = GetVipExtraAttempts();
			if (extra != prevExtraAttempts)
				Apply(extra);
		}

		private int GetVipExtraAttempts()
		{
			var feature = vipModel.GetFeature<Codebase.VIP.Features.VipCrystalEnchantExtraAttemptFeature>("crystal_enchant_extra_attempt");
			return feature != null ? feature.ExtraAttempts : 0;
		}

		private void Apply(int extraAttempts)
		{
			prevExtraAttempts = extraAttempts;
			var stat = metaStatsModel.Get("crystal_enchant_extra_attempts");
			if (stat == null)
				return;
			RemoveByName(stat, "vip_crystal_enchant_extra_attempts");
			if (extraAttempts > 0)
				stat.AddModifier(new Codebase.Unit.StatModifier { Name = "vip_crystal_enchant_extra_attempts", ModifierType = (Codebase.Unit.ModifierType)0, Source = (Codebase.Unit.ModifierSource)15, Value = (float)extraAttempts });
		}

		private static void RemoveByName(MetaStat stat, string name)
		{
			for (int index = stat.Modifiers.Count - 1; index >= 0; index--)
				if (stat.Modifiers[index].Name == name)
					stat.RemoveModifier(stat.Modifiers[index]);
		}

		public CrystalEnchantExtraAttemptVipSystem()
		{
			prevExtraAttempts = -1;
		}
	}
}
