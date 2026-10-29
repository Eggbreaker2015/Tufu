namespace Codebase.Simulation.Rebuild
{
    public enum EmissionModel { Chain = 0, E1 = 1 }
    public enum PreferredMapping { Swapped = 0, Direct = 1 }
    public enum EvolutionTrigger { Timed = 0, Greedy = 1, TimedLag = 2, TimedForce = 3 }
    public enum DayChangeInventoryOrder { StatsThenItems = 0, ItemsThenStats = 1 }
    public enum SlotRpmOrder { SlotBeforeRpm = 0, SlotAfterRpm = 1 }

    public partial class SimulationRebuildParams
    {
        public EmissionModel EmissionModel = EmissionModel.Chain;
        public PreferredMapping PreferredMapping = PreferredMapping.Swapped;
        public EvolutionTrigger EvolutionTrigger = EvolutionTrigger.Timed;
        public int[] WeaponSlotUnlockLocations = { 4, 8 };
        public DayChangeInventoryOrder DayChangeInventoryOrder = DayChangeInventoryOrder.StatsThenItems;
        public SlotRpmOrder SlotRpmOrder = SlotRpmOrder.SlotBeforeRpm;

        internal string RebuildValidateInventory()
        {
            if (WeaponSlotUnlockLocations == null || WeaponSlotUnlockLocations.Length != 2
                || WeaponSlotUnlockLocations[0] < 1 || WeaponSlotUnlockLocations[1] < WeaponSlotUnlockLocations[0])
                return "params.WeaponSlotUnlockLocations must contain two ordered positive locations";
            return null;
        }
    }
}
