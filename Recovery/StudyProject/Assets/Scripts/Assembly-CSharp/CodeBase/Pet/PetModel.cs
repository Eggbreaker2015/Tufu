using Codebase.Player.Inventory;
using Codebase.Unit;

namespace Codebase.Pet
{
	public class PetModel
	{
		public ItemSlotModel Slot;

		public ItemModel Item;

		public PetDefinition Definition;

		public PetUnit Unit;

		// Species skill level last applied to the installed slots; 0 = none.
		public int SkillLevel;

		public PetState State;

		public float ReviveAt;

		// Position on the follow ring: index of the slot among the weapon slots.
		public int SlotOrder;

		public float PathFailTimer;

		// Keeps a target that just stepped out of range for a moment, like the player's 0.2 s retain.
		public float TargetOutOfRangeTimer;

		// Stat sync bookkeeping, indexed like PlayerConfig.Stats.
		public Stat[] SyncedSources;

		public int[] SyncedVersions;

		// Ratios the pet's Health/Damage stats were last built with; changes with the item's level, copies and evolution.
		public PetStatRatios SyncedRatios;

		public int SyncedSynergyKey = -1;

		public bool IsAlive => State == PetState.Alive && Unit != null;

		public PetModel(ItemSlotModel slot, ItemModel item, PetDefinition definition, int slotOrder)
		{
			Slot = slot;
			Item = item;
			Definition = definition;
			SlotOrder = slotOrder;
			State = PetState.Alive;
		}
	}
}
