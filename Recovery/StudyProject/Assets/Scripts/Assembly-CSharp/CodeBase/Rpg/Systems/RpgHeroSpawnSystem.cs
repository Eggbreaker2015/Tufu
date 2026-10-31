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
		[Inject] private ItemsProvider itemsProvider;
		private readonly List<HeroModel> removed = new List<HeroModel>();
		private bool unlocked;

		public override void OnAwake() { }

		public override void OnUpdate(float deltaTime)
		{
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
				protagonist = new HeroModel { Definition = definition, Unit = player, SlotOrder = 0, IsProtagonist = true, AppliedStar = 1 };
				InitializeHero(protagonist, 1);
				roster.Heroes.Add(protagonist);
				roster.ForceControl(protagonist);
			}
			else if (protagonist.Unit != player)
			{
				protagonist.Unit = player;
				InitializeHero(protagonist, 1);
				if (roster.Controlled == protagonist) player.IsControlled = true;
			}
			removed.Clear();
			foreach (HeroModel hero in roster.Heroes)
				if (!hero.IsProtagonist && (hero.Unit == null || hero.Slot == null || !hero.Slot.IsEquipped || hero.Slot.ItemModel != hero.Item)) removed.Add(hero);
			foreach (HeroModel hero in removed) DespawnCompanion(hero);
			int order = 1;
			foreach (ItemSlotModel slot in playerModel.ItemSlots)
			{
				if (slot.SlotType != ItemSlotType.Weapon || slot.IsVip) continue;
				int slotOrder = order++;
				if (slot.IsEquipped && roster.Heroes.Find(hero => hero.Slot == slot) == null) SpawnCompanion(slot, slotOrder, player);
			}
			foreach (HeroModel hero in roster.Heroes)
			{
				if (hero.IsProtagonist || hero.Unit == null || hero.Pet == null) continue;
				int star = RpgStars.FromCopies(hero.Item.Copies);
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
			HeroModel hero = new HeroModel { Definition = definition, Unit = unit, Pet = pet, Item = item, Slot = slot, SlotOrder = order };
			int star = RpgStars.FromCopies(item.Copies);
			InitializeHero(hero, star);
			pet.SkillLevel = star;
			pet.Unit = unit;
			if (petsModel.RestoreDeath(pet)) { unit.CurrentHealth = 0; hero.IsDown = true; hero.ReviveAt = pet.ReviveAt; }
			roster.Heroes.Add(hero);
		}

		private void InitializeHero(HeroModel hero, int star)
		{
			HeroUnit unit = hero.Unit;
			HeroDefinition definition = hero.Definition;
			ItemConfig attackConfig = itemsProvider.Items.Find(item => item != null && item.Key == definition.AttackItemKey);
			if (attackConfig == null && hero.IsProtagonist && playerModel.PlayerAttackItem != null) attackConfig = playerModel.PlayerAttackItem.Config;
			if (attackConfig == null && hero.Item != null) attackConfig = hero.Item.Config;
			if (attackConfig != null) hero.AttackItem = new ItemModel(roster.Lifetime, attackConfig, false);
			unit.Stats = HeroStats.Build(definition);
			unit.BuildStatLookup();
			HeroStats.ApplyBase(unit, definition);
			HeroStats.ApplyLevel(unit, GameContext.Get<RpgProgressModel>().Level);
			HeroStats.ApplyStar(unit, star);
			hero.AppliedStar = star;
			unit.CurrentHealth = unit.GetStatValue(StatType.Health);
			UnitSkillLoadout loadout = hero.IsProtagonist ? SkillLibrary.FindLoadout(SkillOwnerKind.Hero, "hero") : SkillLibrary.FindLoadout(SkillOwnerKind.Pet, hero.Item.Key);
			if (definition.ExtraSkillKeys != null && definition.ExtraSkillKeys.Count > 0)
			{
				var combined = new UnitSkillLoadout();
				if (loadout != null && loadout.Skills != null) combined.Skills.AddRange(loadout.Skills);
				foreach (string key in definition.ExtraSkillKeys) combined.Skills.Add(new SkillSlotDef { SkillKey = key });
				loadout = combined;
			}
			if (loadout != null && GameContext.Has<SkillService>()) GameContext.Get<SkillService>().Install(unit, unit.EntityProvider.Entity, loadout, levelOverride: star);

		}

		private void DespawnCompanion(HeroModel hero)
		{
			roster.Remove(hero);
			PetModel pet = hero.Pet;
			if (pet == null) return;
			petsModel.RememberRevive(pet);
			PetUnit unit = pet.Unit;
			pet.Unit = null;
			PetUnitFactory.Destroy(unit);
		}
	}
}
