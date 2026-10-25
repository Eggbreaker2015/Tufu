using Codebase.Player.Inventory;

namespace Codebase.Events.Services
{
	public class CrystalRewardCandidate
	{
		public int VariantIndex;

		public ItemSlotType SlotType;

		public Rarity Rarity;

		public int Count;

		public int TotalPoints;

		public double SelectWeight;
	}
}
