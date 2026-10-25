using Codebase.BossTower;
using Codebase.Core;
using Codebase.Crystals;
using Codebase.Shop;
using Scellecs.Morpeh;

namespace Codebase.Meta.Systems
{
	public class UpgradeMaxCapacityBonusSystem : SystemBase
	{
		private const string FreeChestLotKey = "chest_1";

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private MetaStatsModel metaStatsModel;

		[Inject]
		private ShopLotRepository shopLotRepository;

		private int prevMaxEnchantmentStones;

		private int prevMaxGeodes;

		private int prevMaxTickets;

		private int prevFreeChestCooldownDays;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private int GetFreeChestCooldownDays()
		{
			return 0;
		}
	}
}
