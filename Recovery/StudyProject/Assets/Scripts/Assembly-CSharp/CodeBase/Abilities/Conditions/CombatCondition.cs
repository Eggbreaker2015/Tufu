using System;
using Codebase.Abilities.Core;
using Codebase.Core;
using Codebase.Pet;

namespace Codebase.Abilities.Conditions
{
	[Serializable]
	public class CombatCondition : AbilityConditionBase
	{
		public CombatMode Mode;

		public float Timeout = 5f; // Original constructor 0x15f44a4.

		public override bool Evaluate(AbilityContext context)
        {
            // Native ELF VA 0x15f43e8.
            var unit = context.PlayerModel.Unit;
            var last = Math.Max(unit.LastDamageGet, unit.LastDamageDealt);
            // Pet mode: an alive pet's hits also count as combat.
            foreach (PetModel pet in GameContext.Get<PetsModel>().AlivePets)
                    last = Math.Max(last, pet.Unit.LastDamageDealt);
            bool inCombat = (double)UnityEngine.Time.time - last < Timeout;
            return inCombat != ((int)Mode != 0);
        }
	}
}
