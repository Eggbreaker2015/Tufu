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

        // P5's asymptotic heuristic; fixed inputs for Regenerate, not fitted by this unit.
        public double[] WeaponHeuristicTypeMultipliers = { 1.0, 0.8, 0.573 };
        public double[] WeaponHeuristicRarityMultipliers = { 1.0, 1.25, 1.5, 1.875, 2.5 };
        public double ArmorHeuristicMultiplier = 0.8;

        internal string RebuildValidateInventory()
        {
            if (WeaponSlotUnlockLocations == null || WeaponSlotUnlockLocations.Length != 2
                || WeaponSlotUnlockLocations[0] < 1 || WeaponSlotUnlockLocations[1] < WeaponSlotUnlockLocations[0])
                return "params.WeaponSlotUnlockLocations must contain two ordered positive locations";
            if (WeaponHeuristicTypeMultipliers == null || WeaponHeuristicTypeMultipliers.Length != 3
                || WeaponHeuristicRarityMultipliers == null || WeaponHeuristicRarityMultipliers.Length != 5)
                return "params inventory heuristic arrays require three types and five rarities";
            foreach (double value in WeaponHeuristicTypeMultipliers)
                if (!(value > 0) || double.IsInfinity(value)) return "params weapon type heuristics must be finite and positive";
            foreach (double value in WeaponHeuristicRarityMultipliers)
                if (!(value > 0) || double.IsInfinity(value)) return "params weapon rarity heuristics must be finite and positive";
            if (!(ArmorHeuristicMultiplier > 0) || double.IsInfinity(ArmorHeuristicMultiplier)) return "params armor heuristic must be finite and positive";
            return null;
        }
    }
}
