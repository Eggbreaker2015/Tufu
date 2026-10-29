using Codebase.Core;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Skills;
using Codebase.Unit;
using UnityEngine;
using Scellecs.Morpeh;

namespace Codebase.Rpg.Systems
{
	public class RpgHeroSpawnSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private HeroRosterModel roster;

		[Inject]
		private PetsModel petsModel;

		[Inject]
		private ItemsProvider itemsProvider;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			if (!RpgModeConfig.Enabled) return;
			PlayerUnit player = playerModel.Unit;
			if (player == null) return;
			// The protagonist body can be rebuilt; keep its hero record pointing at the live unit.
			HeroModel existing = roster.Heroes.Find(hero => hero.IsProtagonist);
			if (existing != null)
			{
				if (existing.Unit != player)
				{
					existing.Unit = player;
					if (roster.Controlled == existing) player.IsControlled = true;
				}
				return;
			}
			HeroDefinition definition = RpgModeConfig.Instance.Heroes.Find(hero => hero != null && hero.IsProtagonist);
			HeroModel protagonist = new HeroModel { Definition = definition, Unit = player, SlotOrder = 0, IsProtagonist = true };
			roster.Heroes.Add(protagonist);
			roster.ForceControl(protagonist);
			int slotOrder = 1;
			foreach (string key in RpgModeConfig.Instance.StartingHeroKeys)
			{
				HeroDefinition companion = RpgModeConfig.Instance.Heroes.Find(hero => hero != null && hero.Key == key);
				if (companion == null || companion.IsProtagonist)
					continue;
				SpawnCompanion(companion, slotOrder++, player);
			}
		}
		private void SpawnCompanion(HeroDefinition definition, int slotOrder, PlayerUnit player)
		{
			ItemModel item = new ItemModel(petsModel.Lifetime, itemsProvider.Get(definition.AttackItemKey));
			item.AbilityDamageMultiplier = 1f;
			item.AbilityAttackSpeedMultiplier = 1f;
			item.AbilityCritMultiplier = 1f;
			item.AbilityStates.Clear();
			PetDefinition petDefinition = PetConfig.Instance.Find(itemsProvider.Get(definition.PetItemKey));
			Transform owner = player.transform;
			PetsModel.TrySamplePosition(PetsModel.GetFollowPosition(owner.position, owner.eulerAngles.y, slotOrder), owner.position, out Vector3 position);
			float speed = (float)player.GetStatSpecificValue(StatType.MovementSpeed) * PetConfig.Instance.FollowSpeedMultiplier;
			PetUnit unit = PetUnitFactory.Create(item, petDefinition, position, owner.rotation, speed);
			unit.Stats = HeroStats.Build(definition);
			unit.BuildStatLookup();
			unit.CurrentHealth = unit.GetStatValue(StatType.Health);
			PetModel pet = new PetModel(null, item, petDefinition, slotOrder);
			UnitSkillLoadout loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Pet, item.Key);
			int skillLevel = PetStats.GetSkillLevel(item);
			if (loadout != null && GameContext.Has<SkillService>())
				GameContext.Get<SkillService>().Install(unit, unit.EntityProvider.Entity, loadout, levelOverride: skillLevel);
			pet.SkillLevel = skillLevel;
			pet.Unit = unit;
			petsModel.Pets.Add(pet);
			roster.Heroes.Add(new HeroModel { Definition = definition, Unit = unit, Pet = pet, SlotOrder = slotOrder });
		}

	}
}
