using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Skills.Components;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Unit.Events;
using Codebase.Vfx;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Skills
{
	// Skill layer entry point (GameContext): loadout install, instant casts, buffs, damage, heals, missiles, vfx.
	public class SkillService
	{
		public const float ThinkInterval = 0.2f;

		private const float MissileHeight = 1.2f;

		// Stat.CalculateSpecificValue(MovementSpeed) is 4.6 * v^0.115 below 10000: the hero's felt speed.
		private const double HeroSpeedExponent = 0.115;

		// Tracked missiles hit on arrival or at the latest after travel time * this + MissileFlightMargin (a running hero can outrun the visual).
		private const float MissileFlightSlack = 1.5f;

		private const float MissileFlightMargin = 0.5f;

		private class PendingMissile
		{
			public SkillCastContext Context;

			public UnitCore Target;

			public List<ISkillEffect> OnImpact;

			public float Remaining;
		}

		private readonly List<PendingMissile> pendingMissiles = new List<PendingMissile>();

		private int modifierCounter;

		private World world;

		private Stash<SkillCasterComponent> casterStash;

		private Stash<SkillCastingComponent> castingStash;

		private Stash<UnitBuffsComponent> buffsStash;

		private Stash<BuffStunComponent> stunStash;

		private Event<WeaponHitEvent> hitEvent;

		private World CurrentWorld
		{
			get
			{
				World current = World.Default;
				if (world != current)
				{
					world = current;
					casterStash = current.GetStash<SkillCasterComponent>();
					castingStash = current.GetStash<SkillCastingComponent>();
					buffsStash = current.GetStash<UnitBuffsComponent>();
					stunStash = current.GetStash<BuffStunComponent>();
					hitEvent = current.GetEvent<WeaponHitEvent>();
				}
				return world;
			}
		}

		// EnemyUnit.Initialize (every spawn path, pool reuse included): drop the previous life's cast and buffs, then install by kind.
		public void InstallForEnemy(EnemyUnit enemy, EnemySpawnerConfig config)
		{
			if (enemy == null || enemy.EntityProvider == null)
				return;
			World current = CurrentWorld;
			Entity entity = enemy.EntityProvider.Entity;
			if (current.IsDisposed(entity))
				return;
			if (castingStash.Has(entity))
				CancelCast(entity);
			if (buffsStash.Has(entity))
				ClearBuffs(entity);
			UnitSkillLoadout loadout = SkillLibrary.HasLoadouts ? FindEnemyLoadout(enemy, config) : null;
			if (loadout == null)
			{
				if (casterStash.Has(entity))
					casterStash.Remove(entity);
				return;
			}
			Install(enemy, entity, loadout);
		}

		public void Install(UnitCore unit, Entity entity, UnitSkillLoadout loadout)
		{
			World current = CurrentWorld;
			var slots = new List<SkillSlot>(loadout.Skills != null ? loadout.Skills.Count : 0);
			if (loadout.Skills != null)
				foreach (SkillSlotDef slotDef in loadout.Skills)
				{
					SkillDef def = slotDef != null ? SkillLibrary.FindSkill(slotDef.SkillKey) : null;
					if (def == null)
						continue;
					float auraInterval = GetAuraInterval(def);
					slots.Add(new SkillSlot { Def = def, Level = Mathf.Max(1, slotDef.Level), Cooldown = def.InitialCooldown, AuraTimer = UnityEngine.Random.Range(0f, auraInterval) });
				}
			if (slots.Count == 0)
			{
				if (casterStash.Has(entity))
					casterStash.Remove(entity);
				return;
			}
			casterStash.Set(entity, new SkillCasterComponent { Owner = unit, Slots = slots, NextThinkTime = Time.time + UnityEngine.Random.Range(0f, ThinkInterval), Engaged = false });
		}

		private static UnitSkillLoadout FindEnemyLoadout(EnemyUnit enemy, EnemySpawnerConfig config)
		{
			UnitSkillLoadout loadout = null;
			if (config != null && config.IsBossFightPart)
				loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Boss, config.Key);
			// The spawned prefab (event variants and boss minions differ from config.Prefab); pool clones carry "(Clone)".
			if (loadout == null)
				loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Enemy, StripClone(enemy.gameObject.name));
			if (loadout == null && config != null && config.Prefab != null)
				loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Enemy, config.Prefab.name);
			return loadout;
		}

		private static string StripClone(string name)
		{
			const string suffix = "(Clone)";
			return name != null && name.EndsWith(suffix, StringComparison.Ordinal) ? name.Substring(0, name.Length - suffix.Length).TrimEnd() : name;
		}

		public static float GetAuraInterval(SkillDef def)
		{
			if (def == null || def.TargetType != SkillTargetType.Passive || def.Effects == null)
				return 0f;
			foreach (ISkillEffect effect in def.Effects)
				if (effect is Effects.AuraEffect aura)
					return Mathf.Max(0.1f, aura.Interval);
			return 0.5f;
		}

		public SkillCastContext CreateContext(UnitCore caster, SkillDef def, int level, float externalPower, UnitCore target, Vector3 point, ItemModel sourceItem)
		{
			return new SkillCastContext
			{
				Caster = caster,
				CasterEntity = caster != null && caster.EntityProvider != null ? caster.EntityProvider.Entity : default,
				Instigator = caster,
				PrimaryTarget = target,
				Point = point,
				Skill = def,
				Level = level,
				Power = (def != null ? def.GetLevelScale(level) : 1f) * externalPower,
				SourceItem = sourceItem
			};
		}

		// Item ability adapters: no cast point, telegraph or casting state; effects resolve immediately.
		public void CastInstant(UnitCore caster, SkillDef def, int level, float externalPower, UnitCore target, ItemModel sourceItem)
		{
			if (caster == null || def == null || caster.IsDead)
				return;
			Vector3 point = target != null ? target.transform.position : caster.transform.position;
			SkillCastContext context = CreateContext(caster, def, level, externalPower, target, point, sourceItem);
			Execute(def.Effects, context);
		}

		public static void Execute(List<ISkillEffect> effects, in SkillCastContext context)
		{
			if (effects == null)
				return;
			foreach (ISkillEffect effect in effects)
			{
				if (effect == null)
					continue;
				try
				{
					effect.Apply(context);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}

		public static bool CheckConditions(List<ISkillCastCondition> conditions, in SkillCastContext context)
		{
			if (conditions == null)
				return true;
			foreach (ISkillCastCondition condition in conditions)
				if (condition != null && !condition.Check(context))
					return false;
			return true;
		}

		#region Damage and healing

		// Friendly -> enemy: fixed damage through UnitDamageSystem's Weapon == null branch (no OnHit triggers).
		// Enemy -> hero/pet: the caster's attack x power through the normal incoming path, like LavaCrabExplosionSystem.
		public void DealDamage(in SkillCastContext context, UnitCore target, float power, WeaponType? damageType, bool undodgeable)
		{
			UnitCore attacker = context.Source;
			if (attacker == null || power <= 0f || !SkillTargetQuery.IsAlive(target))
				return;
			World current = CurrentWorld;
			if (target is EnemyUnit enemy)
			{
				if (attacker is EnemyUnit)
					return;
				double attack = attacker.GetStatValue(StatType.Damage);
				ItemModel item = context.SourceItem;
				if (attacker is PetUnit pet)
				{
					if (item == null)
						item = pet.Item;
					if (item != null)
						attack += PetStats.GetOwnAttack(item.Damage, pet.Definition);
				}
				else if (attacker is PlayerUnit && item != null && !item.IsArmor)
					attack += item.Damage;
				hitEvent.NextFrame(new WeaponHitEvent
				{
					Source = attacker, Target = enemy, Weapon = null, DamageType = damageType ?? GetDamageType(attacker, item), Undodgeable = undodgeable,
					HasFixedDamage = true, FixedDamage = attack * power
				});
				return;
			}
			if (!(attacker is EnemyUnit source))
				return;
			hitEvent.NextFrame(new WeaponHitEvent
			{
				Source = source, Target = target, DamageType = damageType ?? source.DamageType, Undodgeable = undodgeable, HasSpecialMultiplier = true, SpecialMultiplier = power
			});
		}

		private static WeaponType GetDamageType(UnitCore attacker, ItemModel item)
		{
			if (item != null && item.Config != null)
				return item.Config.DamageType;
			if (attacker is PetUnit pet && pet.Item != null)
				return pet.Item.Config.DamageType;
			if (attacker is EnemyUnit enemy)
				return enemy.DamageType;
			return WeaponType.Universal;
		}

		public void Heal(UnitCore target, double amount)
		{
			if (amount <= 0.0 || !SkillTargetQuery.IsAlive(target) || !target.HasStat(StatType.Health))
				return;
			double maximum = target.GetStatValue(StatType.Health);
			if (target.CurrentHealth < maximum)
				target.CurrentHealth = Math.Min(target.CurrentHealth + amount, maximum);
		}

		#endregion

		#region Buffs

		public void ApplyBuff(UnitCore target, BuffDef def, UnitCore source, float power = 1f)
		{
			if (def == null || !SkillTargetQuery.IsAlive(target) || target.EntityProvider == null)
				return;
			World current = CurrentWorld;
			Entity entity = target.EntityProvider.Entity;
			if (current.IsDisposed(entity))
				return;
			// Phase 1: hostile buffs on the hero and pets only slow (and tick); no stun, silence or other stat changes.
			bool restricted = source is EnemyUnit && !(target is EnemyUnit);
			if (restricted && !HasRestrictedPayload(def))
				return;
			ref UnitBuffsComponent buffs = ref buffsStash.Get(entity, out bool exists);
			if (!exists)
			{
				buffsStash.Set(entity, new UnitBuffsComponent { Owner = target, Buffs = new List<BuffInstance>(2) });
				buffs = ref buffsStash.Get(entity);
			}
			BuffInstance instance = def.Stack == BuffStackMode.Independent ? null : Find(buffs.Buffs, def);
			if (instance != null)
			{
				instance.Source = source;
				instance.Power = power;
				switch (def.Stack)
				{
					case BuffStackMode.KeepLonger:
						instance.Remaining = Mathf.Max(instance.Remaining, def.Duration);
						break;
					case BuffStackMode.Stack:
						instance.Remaining = def.Duration;
						if (def.MaxStacks <= 0 || instance.Stacks < def.MaxStacks)
						{
							instance.Stacks++;
							RemoveMods(instance);
							ApplyMods(target, instance);
						}
						break;
					default:
						instance.Remaining = def.Duration;
						break;
				}
				ApplyStun(target, entity, instance);
				return;
			}
			instance = new BuffInstance { Def = def, Remaining = def.Duration, Stacks = 1, Power = power, Source = source, Restricted = restricted };
			buffs.Buffs.Add(instance);
			ApplyMods(target, instance);
			ApplyStun(target, entity, instance);
		}

		private static bool HasRestrictedPayload(BuffDef def)
		{
			if (def.TickInterval > 0f && def.OnTick != null && def.OnTick.Count > 0)
				return true;
			if (def.StatMods != null)
				foreach (BuffStatMod mod in def.StatMods)
					if (mod != null && mod.StatType == StatType.MovementSpeed)
						return true;
			return false;
		}

		private static BuffInstance Find(List<BuffInstance> buffs, BuffDef def)
		{
			foreach (BuffInstance instance in buffs)
				if (instance.Def == def)
					return instance;
			return null;
		}

		// Pets: their stats are rebuilt by PetStatsSyncSystem, so phase 1 only reads movement through GetMoveSpeedFactor.
		private void ApplyMods(UnitCore target, BuffInstance instance)
		{
			if (target is PetUnit || instance.Def.StatMods == null)
				return;
			foreach (BuffStatMod mod in instance.Def.StatMods)
			{
				if (mod == null || instance.Restricted && mod.StatType != StatType.MovementSpeed)
					continue;
				if (!target.TryGetStat(mod.StatType, out Stat stat))
					continue;
				var modifier = new StatModifier
				{
					Name = "skill:" + instance.Def.Key + ":" + (++modifierCounter),
					StatType = mod.StatType,
					ModifierType = mod.ModifierType,
					Source = ModifierSource.Skill,
					Value = (float)GetModifierValue(target, mod, instance.Stacks)
				};
				stat.AddModifier(modifier);
				instance.Applied.Add(new AppliedStatMod { Stat = stat, Modifier = modifier });
			}
		}

		private static double GetModifierValue(UnitCore target, BuffStatMod mod, int stacks)
		{
			bool multiplicative = mod.ModifierType == ModifierType.PercentMultiplicative || mod.ModifierType == ModifierType.PostFormulaMultiplicative;
			double value = multiplicative ? Math.Pow(mod.ValuePerStack, stacks) : mod.ValuePerStack * stacks;
			// Felt speed: the hero moves at 4.6 * v^0.115, so 0.7 must become 0.7^(1/0.115) on the raw stat.
			if (mod.StatType == StatType.MovementSpeed && mod.ModifierType == ModifierType.PercentMultiplicative && target is PlayerUnit && value > 0.0)
				value = Math.Pow(value, 1.0 / HeroSpeedExponent);
			return value;
		}

		private static void RemoveMods(BuffInstance instance)
		{
			foreach (AppliedStatMod applied in instance.Applied)
				if (applied.Stat != null && applied.Stat.Modifiers.Contains(applied.Modifier))
					applied.Stat.RemoveModifier(applied.Modifier);
			instance.Applied.Clear();
		}

		private void ApplyStun(UnitCore target, Entity entity, BuffInstance instance)
		{
			if (!instance.Def.Stun || instance.Restricted || !(target is EnemyUnit enemy))
				return;
			ref BuffStunComponent stun = ref stunStash.Get(entity, out bool exists);
			if (!exists || stun.Duration < instance.Remaining)
				stunStash.Set(entity, new BuffStunComponent { Duration = instance.Remaining });
			enemy.ApplyStunAnimation(true);
		}

		public void RemoveBuffAt(List<BuffInstance> buffs, int index)
		{
			RemoveMods(buffs[index]);
			buffs.RemoveAt(index);
		}

		public void ClearBuffs(Entity entity)
		{
			World current = CurrentWorld;
			if (!buffsStash.Has(entity))
				return;
			List<BuffInstance> buffs = buffsStash.Get(entity).Buffs;
			if (buffs != null)
				for (int i = buffs.Count - 1; i >= 0; i--)
					RemoveBuffAt(buffs, i);
			buffsStash.Remove(entity);
		}

		public bool HasBuff(UnitCore unit, string key)
		{
			if (unit == null || unit.EntityProvider == null || string.IsNullOrEmpty(key))
				return false;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			if (current.IsDisposed(entity) || !buffsStash.Has(entity))
				return false;
			foreach (BuffInstance instance in buffsStash.Get(entity).Buffs)
				if (instance.Def.Key == key)
					return true;
			return false;
		}

		public bool IsSilenced(Entity entity)
		{
			World current = CurrentWorld;
			if (current.IsDisposed(entity) || !buffsStash.Has(entity))
				return false;
			foreach (BuffInstance instance in buffsStash.Get(entity).Buffs)
				if (instance.Def.Silence && !instance.Restricted)
					return true;
			return false;
		}

		// Movement multiplier from skill buffs for units that do not read their own MovementSpeed stat (pets).
		public float GetMoveSpeedFactor(UnitCore unit)
		{
			if (unit == null || unit.EntityProvider == null)
				return 1f;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			if (current.IsDisposed(entity) || !buffsStash.Has(entity))
				return 1f;
			float factor = 1f;
			foreach (BuffInstance instance in buffsStash.Get(entity).Buffs)
			{
				if (instance.Def.StatMods == null)
					continue;
				foreach (BuffStatMod mod in instance.Def.StatMods)
					if (mod != null && mod.StatType == StatType.MovementSpeed && mod.ModifierType == ModifierType.PercentMultiplicative)
						factor *= Mathf.Pow(mod.ValuePerStack, instance.Stacks);
			}
			return factor;
		}

		#endregion

		#region Casting

		public void CancelCast(Entity entity)
		{
			World current = CurrentWorld;
			if (!castingStash.Has(entity))
				return;
			SkillTelegraphView telegraph = castingStash.Get(entity).Telegraph;
			if (telegraph != null)
				telegraph.Despawn();
			castingStash.Remove(entity);
		}

		#endregion

		#region Missiles and vfx

		public void LaunchMissile(in SkillCastContext context, UnitCore target, string vfxKey, float speed, Vector3 startOffset, List<ISkillEffect> onImpact)
		{
			if (context.Caster == null || !SkillTargetQuery.IsAlive(target))
				return;
			SkillCastContext impact = context;
			impact.PrimaryTarget = target;
			Vector3 start = context.Caster.transform.position + Vector3.up * MissileHeight + startOffset;
			speed = Mathf.Max(0.1f, speed);
			float travel = Vector3.Distance(start, target.transform.position + Vector3.up * MissileHeight) / speed;
			// No visual: resolve after the travel time.
			var missile = new PendingMissile { Context = impact, Target = target, OnImpact = onImpact, Remaining = travel };
			SkillVfxEntry vfx = string.IsNullOrEmpty(vfxKey) ? null : SkillLibrary.FindVfx(vfxKey);
			if (vfx != null && vfx.Prefab != null && GameContext.Has<WorldVfxService>())
			{
				// The impact is always resolved here: arrival lands it early, the flight cap bounds it (visual despawns with it).
				missile.Remaining = travel * MissileFlightSlack + MissileFlightMargin;
				GameContext.Get<WorldVfxService>().SpawnTracking(vfx.Prefab, start, target.transform, Vector3.up * MissileHeight, speed, () => missile.Remaining = 0f, missile.Remaining);
			}
			pendingMissiles.Add(missile);
		}

		public void TickMissiles(float deltaTime)
		{
			for (int i = pendingMissiles.Count - 1; i >= 0; i--)
			{
				PendingMissile missile = pendingMissiles[i];
				missile.Remaining -= deltaTime;
				if (missile.Remaining > 0f)
					continue;
				pendingMissiles.RemoveAt(i);
				Impact(missile.Context, missile.Target, missile.OnImpact);
			}
		}

		// Pooled units are never destroyed: a target that died or was despawned in flight is skipped, and so is a dead caster's hit.
		private static void Impact(SkillCastContext context, UnitCore target, List<ISkillEffect> onImpact)
		{
			if (!SkillTargetQuery.IsAlive(target) || !SkillTargetQuery.IsAlive(context.Caster))
				return;
			context.PrimaryTarget = target;
			context.Point = target.transform.position;
			Execute(onImpact, context);
		}

		public ParticleSystem SpawnVfx(string key, Vector3 position, Quaternion rotation, float scale)
		{
			SkillVfxEntry vfx = SkillLibrary.FindVfx(key);
			if (vfx == null || vfx.Prefab == null || !GameContext.Has<WorldVfxService>())
				return null;
			// Keep the prefab's authored root rotation (SmashFx lies flat via -90 X); Spawn overwrites it.
			ParticleSystem particle = GameContext.Get<WorldVfxService>().Spawn(vfx.Prefab, position, rotation * vfx.Prefab.transform.rotation, vfx.Lifetime);
			if (particle != null)
				particle.transform.localScale = Vector3.one * (scale * vfx.Scale);
			return particle;
		}

		#endregion
	}
}
