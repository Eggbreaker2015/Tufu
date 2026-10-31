using System;
using Codebase.Abilities.Core;
using Codebase.Abilities.Vfx;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	// Pet mode: heals living pets by the ability value as a fraction of their max health.
	[Serializable]
	public class PetHealEffect : AbilityEffectBase
	{
		public PetHealTarget Target;

		public override void Execute(AbilityContext context)
		{
			float fraction = context.State.GetValue();
			if (fraction <= 0f)
				return;
			switch (Target)
			{
				case PetHealTarget.Self:
					Heal(GetOwnPet(context), fraction);
					break;
				case PetHealTarget.AllPets:
					foreach (PetModel pet in GameContext.Get<PetsModel>().AlivePets)
						Heal(pet.Unit, fraction);
					break;
				case PetHealTarget.LowestPet:
					Heal(FindLowestPet(), fraction);
					break;
			}
		}

		private static PetUnit GetOwnPet(AbilityContext context)
		{
			PetUnit attacker = context.Attacker as PetUnit;
			if (attacker != null)
				return attacker;
			return AbilityTargetResolver.GetOwnerPetUnit(context.Owner);
		}

		private static PetUnit FindLowestPet()
		{
			PetUnit best = null;
			double bestRatio = double.MaxValue;
			foreach (PetModel pet in GameContext.Get<PetsModel>().AlivePets)
			{
				PetUnit unit = pet.Unit;
				if (unit.IsDead)
					continue;
				double maximum = unit.GetStatValue(StatType.Health);
				if (maximum <= 0.0)
					continue;
				double ratio = unit.CurrentHealth / maximum;
				if (ratio < bestRatio)
				{
					bestRatio = ratio;
					best = unit;
				}
			}
			return best;
		}

		private static void Heal(PetUnit unit, float fraction)
		{
			if (unit == null || unit.IsDead)
				return;
			double maximum = unit.GetStatValue(StatType.Health);
			if (maximum <= 0.0 || unit.CurrentHealth >= maximum)
				return;
			unit.CurrentHealth = Math.Min(maximum, unit.CurrentHealth + maximum * fraction);
			GameContext.Get<AbilityVfxService>().Spawn(AbilityVfxConstants.HealSmall, unit);
		}
	}
}
