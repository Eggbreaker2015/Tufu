using System.Collections.Generic;
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
		[Inject] private PlayerModel playerModel;
		[Inject] private HeroRosterModel roster;
		[Inject] private PetsModel petsModel;
		private readonly List<HeroModel> removed = new List<HeroModel>();
		private bool unlocked;

		public override void OnAwake() { }

		public override void OnUpdate(float deltaTime)
		{
			if (!RpgModeConfig.Enabled) return;
			PlayerUnit player = playerModel.Unit;
			if (player == null) return;
			if (!unlocked)
			{
				foreach (ItemSlotModel slot in playerModel.ItemSlots)
					if (slot.Key == "weapon_1" || slot.Key == "weapon_2" || slot.Key == "weapon_3" || slot.Key == "weapon_4") slot.IsUnlocked = true;
				unlocked = true;
			}
			HeroModel protagonist = roster.Heroes.Find(hero => hero.IsProtagonist);
			if (protagonist == null)
			{
				HeroDefinition definition = RpgModeConfig.Instance.Heroes.Find(hero => hero != null && hero.IsProtagonist);
				protagonist = new HeroModel { Definition = definition, Unit = player, SlotOrder = 0, IsProtagonist = true };
				roster.Heroes.Add(protagonist);
				roster.ForceControl(protagonist);
			}
			else if (protagonist.Unit != player)
			{
				protagonist.Unit = player;
				if (roster.Controlled == protagonist) player.IsControlled = true;
			}
			removed.Clear();
			foreach (HeroModel hero in roster.Heroes)
				if (!hero.IsProtagonist && (hero.Pet == null || hero.Pet.Unit == null || hero.Pet.Slot == null || !hero.Pet.Slot.IsEquipped || hero.Pet.Slot.ItemModel != hero.Pet.Item)) removed.Add(hero);
			foreach (HeroModel hero in removed) DespawnCompanion(hero);
			int order = 1;
			foreach (ItemSlotModel slot in playerModel.ItemSlots)
			{
				if (slot.SlotType != ItemSlotType.Weapon || slot.IsVip) continue;
				int slotOrder = order++;
				if (slot.IsEquipped && petsModel.GetBySlot(slot) == null) SpawnCompanion(slot, slotOrder, player);
			}
			foreach (HeroModel hero in roster.Heroes)
			{
				if (hero.IsProtagonist || hero.Unit == null || hero.Pet == null) continue;
				int star = RpgStars.FromCopies(hero.Pet.Item.Copies);
				if (star != hero.AppliedStar) { HeroStats.ApplyStar(hero.Unit, star); hero.AppliedStar = star; }
				if (star != hero.Pet.SkillLevel)
				{
					if (GameContext.Has<SkillService>()) GameContext.Get<SkillService>().SetSkillLevel(hero.Unit, star);
					hero.Pet.SkillLevel = star;
				}
			}
		}

		private void SpawnCompanion(ItemSlotModel slot, int order, PlayerUnit player)
		{
			ItemModel item = slot.ItemModel;
			item.AbilityDamageMultiplier = 1f;
			item.AbilityAttackSpeedMultiplier = 1f;
			item.AbilityCritMultiplier = 1f;
			PetDefinition petDefinition = PetConfig.Instance.Find(item.Config);
			if (petDefinition == null) return;
			HeroDefinition definition = RpgModeConfig.Instance.ResolveHero(item.Config);
			PetModel pet = new PetModel(slot, item, petDefinition, order);
			Transform owner = player.transform;
			PetsModel.TrySamplePosition(PetsModel.GetFollowPosition(owner.position, owner.eulerAngles.y, order), owner.position, out Vector3 position);
			float speed = (float)player.GetStatSpecificValue(StatType.MovementSpeed) * PetConfig.Instance.FollowSpeedMultiplier;
			PetUnit unit = PetUnitFactory.Create(item, petDefinition, position, owner.rotation, speed);
			unit.Stats = HeroStats.Build(definition);
			unit.BuildStatLookup();
			HeroStats.ApplyBase(unit, definition);
			HeroStats.ApplyLevel(unit, GameContext.Get<RpgProgressModel>().Level);
			int star = RpgStars.FromCopies(item.Copies);
			HeroStats.ApplyStar(unit, star);
			unit.CurrentHealth = unit.GetStatValue(StatType.Health);
			UnitSkillLoadout loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Pet, item.Key);
			if (loadout != null && GameContext.Has<SkillService>()) GameContext.Get<SkillService>().Install(unit, unit.EntityProvider.Entity, loadout, levelOverride: star);
			pet.SkillLevel = star;
			pet.Unit = unit;
			petsModel.Pets.Add(pet);
			if (petsModel.RestoreDeath(pet)) unit.CurrentHealth = 0;
			roster.Heroes.Add(new HeroModel { Definition = definition, Unit = unit, Pet = pet, SlotOrder = order, AppliedStar = star });
		}

		private void DespawnCompanion(HeroModel hero)
		{
			roster.Remove(hero);
			PetModel pet = hero.Pet;
			if (pet == null) return;
			petsModel.RememberRevive(pet);
			petsModel.Pets.Remove(pet);
			PetUnit unit = pet.Unit;
			pet.Unit = null;
			PetUnitFactory.Destroy(unit);
		}
	}
}
