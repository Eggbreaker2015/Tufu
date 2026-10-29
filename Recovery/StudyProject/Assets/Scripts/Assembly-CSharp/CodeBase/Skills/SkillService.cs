using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Enemy.Systems;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Skills.Components;
using Codebase.Skills.Effects;
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

		internal const float MissileHeight = 1.2f;

		// Stat.CalculateSpecificValue(MovementSpeed) is 4.6 * v^0.115 below 10000: the hero's felt speed.
		private const double HeroSpeedExponent = 0.115;

		// Tracked missiles hit on arrival or at the latest after travel time * this + MissileFlightMargin (a running hero can outrun the visual).
		private const float MissileFlightSlack = 1.5f;

		private const float MissileFlightMargin = 0.5f;

		// Ranged basic attacks start this far in front of the shooter.
		private const float RangedLaunchForward = 0.5f;

		// A ranged hit whose visual never arrived (flight cap ran out) still lands only if the visual ended this close to the target.
		private const float RangedCapHitRadius = 1.5f;

		private class PendingMissile
		{
			public SkillCastContext Context;

			public UnitCore Target;

			public List<ISkillEffect> OnImpact;

			public float Remaining;

			// Ranged basic attack: resolved as a plain enemy hit instead of OnImpact.
			public RangedAttackDef Ranged;

			// Tracking visual (null = timer-only missile) and whether it reached the target before the flight cap.
			public ParticleSystem Visual;

			public bool Arrived;
		}

		private readonly List<PendingMissile> pendingMissiles = new List<PendingMissile>();

		private readonly SkillLinearMissiles linearMissiles = new SkillLinearMissiles();

		private readonly SkillForcedMovement forcedMovement = new SkillForcedMovement();

		private readonly SkillSummons summons = new SkillSummons();

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
			if (enemy == null)
				return;
			summons.DespawnOwnedBy(enemy);
			enemy.ResetRangedState(null);
			if (enemy.EntityProvider == null)
				return;
			World current = CurrentWorld;
			Entity entity = enemy.EntityProvider.Entity;
			if (current.IsDisposed(entity))
				return;
			forcedMovement.Stop(entity);
			if (castingStash.Has(entity))
				CancelCast(entity);
			if (buffsStash.Has(entity))
				ClearBuffs(entity);
			UnitSkillLoadout loadout = SkillLibrary.HasLoadouts ? FindEnemyLoadout(enemy, config) : null;
			if (loadout != null && loadout.RangedAttack != null && loadout.RangedAttack.Enabled)
				enemy.ResetRangedState(loadout.RangedAttack);
			if (loadout == null)
			{
				if (casterStash.Has(entity))
					casterStash.Remove(entity);
				return;
			}
			Install(enemy, entity, loadout);
		}

		public void Summon(in SkillCastContext context, SummonEffect def)
		{
			if (def == null || context.Caster == null)
				return;
			summons.Summon(this, context, def);
		}

		public bool IsSummon(UnitCore unit)
		{
			return summons.IsSummon(unit);
		}

		public int CountSummons(UnitCore owner, string key)
		{
			return summons.CountAlive(owner, key);
		}

		public void TickSummons()
		{
			summons.Tick(this);
		}

		public void RemoveSummonSkills(UnitCore unit)
		{
			if (unit == null || unit.EntityProvider == null)
				return;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			if (current.IsDisposed(entity) || !casterStash.Has(entity))
				return;
			List<SkillSlot> slots = casterStash.Get(entity).Slots;
			for (int i = slots.Count - 1; i >= 0; i--)
			{
				List<ISkillEffect> effects = slots[i].Def.Effects;
				if (effects == null)
					continue;
				foreach (ISkillEffect effect in effects)
					if (effect is SummonEffect)
					{
						slots.RemoveAt(i);
						break;
					}
			}
			if (slots.Count == 0)
				casterStash.Remove(entity);
		}

		public void Install(UnitCore unit, Entity entity, UnitSkillLoadout loadout, int levelOverride = 0)
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
					slots.Add(new SkillSlot { Def = def, Level = levelOverride > 0 ? levelOverride : Mathf.Max(1, slotDef.Level), Cooldown = def.InitialCooldown, AuraTimer = UnityEngine.Random.Range(0f, auraInterval) });
				}
			if (slots.Count == 0)
			{
				if (casterStash.Has(entity))
					casterStash.Remove(entity);
				return;
			}
			casterStash.Set(entity, new SkillCasterComponent { Owner = unit, Slots = slots, NextThinkTime = Time.time + UnityEngine.Random.Range(0f, ThinkInterval), Engaged = false });
		}

		public void SetSkillLevel(UnitCore unit, int level)
		{
			if (unit == null || unit.EntityProvider == null)
				return;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			if (current.IsDisposed(entity) || !casterStash.Has(entity))
				return;
			List<SkillSlot> slots = casterStash.Get(entity).Slots;
			for (int i = 0; i < slots.Count; i++)
				slots[i].Level = Mathf.Max(1, level);
		}

		private static UnitSkillLoadout FindEnemyLoadout(EnemyUnit enemy, EnemySpawnerConfig config)
		{
			UnitSkillLoadout loadout = null;
			if (config != null && config.IsBossFightPart)
				loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Boss, config.Key);
			// The spawned prefab (event variants and boss minions differ from config.Prefab); pool clones carry "(Clone)".
			if (loadout == null)
				loadout = SkillLibrary.FindLoadout(SkillOwnerKind.Enemy, StripClone(enemy.gameObject.name));
			// Boss fight units match by boss key or their own prefab only (the lich's *_Summoned skeletons share a Skeleton_Easy config).
			if (loadout == null && config != null && config.Prefab != null && !config.IsBossFightPart)
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
			context.Direction = SkillTargetQuery.GetFlatDirection(caster, target);
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
		// Enemy -> hero/pet: normal incoming damage; sleeping, dying or frozen heroes are skipped (pets still take damage).
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
			if (target is PlayerUnit playerUnit)
			{
				PlayerModel player = GameContext.Has<PlayerModel>() ? GameContext.Get<PlayerModel>() : null;
				if (playerUnit.IsSleeping || playerUnit.IsDying || player != null && !player.CanMove)
					return;
			}
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

		// One enemy stun on the hero lasts at most this long.
		private const float HeroStunCap = 1.5f;

		private const float HeroDisplacementCap = 3f;

		// Shared by control buffs and displacement; friendly sources cannot control friendly units.
		private static bool IsHostileSourceOnFriendlyTarget(UnitCore source, UnitCore target)
		{
			return source is EnemyUnit && !(target is EnemyUnit);
		}

		public bool IsDisplaced(UnitCore unit)
		{
			return forcedMovement.Has(unit);
		}

		public bool IsDashing(UnitCore unit)
		{
			return forcedMovement.Has(unit, true);
		}

		public void ClearForcedMovement(UnitCore unit)
		{
			if (unit != null && unit.EntityProvider != null) forcedMovement.Stop(unit.EntityProvider.Entity);
		}

		public void TickForcedMovement(float deltaTime)
		{
			forcedMovement.Tick(this, deltaTime);
		}

		public void Displace(in SkillCastContext context, UnitCore target, float distance, float duration, bool pull, bool fromPoint)
		{
			if (!SkillTargetQuery.IsAlive(target) || target is EnemyUnit enemy && enemy.ImmuneToDisplacement)
				return;
			if ((target is PlayerUnit || target is PetUnit) && !IsHostileSourceOnFriendlyTarget(context.Source, target))
			{
				string key = context.Skill != null ? context.Skill.Key : context.Buff != null ? context.Buff.Key : "none";
				SkillLibrary.WarnOnce("friendly-stat:" + key + ":Displacement:none", "[Skills] '" + key + "': displacement is ignored on the hero and pets for this source");
				return;
			}
			if (context.Caster == null) return;
			distance = Mathf.Max(0f, distance);
			if (target is PlayerUnit) distance = Mathf.Min(distance, HeroDisplacementCap);
			Vector3 origin = fromPoint ? context.Point : context.Caster.transform.position;
			Vector3 direction = target.transform.position - origin;
			direction.y = 0f;
			if (pull)
			{
				float contact = SkillTargetQuery.GetUnitRadius(context.Caster) + SkillTargetQuery.GetUnitRadius(target);
				distance = Mathf.Min(distance, Mathf.Max(0f, direction.magnitude - contact));
				direction = -direction;
			}
			if (direction.sqrMagnitude < 0.0001f) direction = context.Direction;
			direction.y = 0f;
			if (direction.sqrMagnitude < 0.0001f) direction = SkillTargetQuery.GetFlatDirection(context.Caster, null);
			forcedMovement.Start(context, target, pull ? SkillForcedMovement.Kind.Pull : SkillForcedMovement.Kind.Knockback, direction, distance, distance / Mathf.Max(0.001f, duration));
		}

		public void Dash(in SkillCastContext context, DashEffect def)
		{
			if (def == null || context.Caster == null) return;
			Vector3 direction = context.Direction;
			direction.y = 0f;
			if (direction.sqrMagnitude < 0.0001f) direction = SkillTargetQuery.GetFlatDirection(context.Caster, context.PrimaryTarget);
			forcedMovement.Start(context, context.Caster, SkillForcedMovement.Kind.Dash, direction, Mathf.Max(0f, def.Distance), Mathf.Max(0f, def.Speed), def);
		}

		public void Blink(in SkillCastContext context, BlinkEffect def)
		{
			if (def != null) forcedMovement.Blink(context, def);
		}

		public void ApplyBuff(UnitCore target, BuffDef def, UnitCore source, float power = 1f)
		{
			if (def == null || !SkillTargetQuery.IsAlive(target) || target.EntityProvider == null)
				return;
			World current = CurrentWorld;
			Entity entity = target.EntityProvider.Entity;
			if (current.IsDisposed(entity))
				return;
			bool friendlyTarget = target is PlayerUnit || target is PetUnit;
			bool restricted = IsHostileSourceOnFriendlyTarget(source, target);
			if (friendlyTarget || restricted)
			{
				if (def.StatMods != null)
					foreach (BuffStatMod mod in def.StatMods)
						if (mod != null && !IsAllowedFriendlyMod(mod, restricted))
							WarnIgnoredPayload(def, mod.StatType.ToString(), mod.ModifierType.ToString());
				if (def.Stun && !restricted)
					WarnIgnoredPayload(def, "Stun", "none");
				if (def.Silence)
					WarnIgnoredPayload(def, "Silence", "none");
				if (!HasFriendlyPayload(def, restricted))
					return;
			}
			ref UnitBuffsComponent buffs = ref buffsStash.Get(entity, out bool exists);
			if (!exists)
			{
				buffsStash.Set(entity, new UnitBuffsComponent { Owner = target, Buffs = new List<BuffInstance>(2) });
				buffs = ref buffsStash.Get(entity);
			}
			BuffInstance instance = def.Stack == BuffStackMode.Independent ? null : Find(buffs.Buffs, def, restricted, friendlyTarget);
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
			instance = new BuffInstance { Def = def, Remaining = def.Duration, Stacks = 1, Power = power, Source = source, Restricted = restricted, FriendlyTarget = friendlyTarget };
			buffs.Buffs.Add(instance);
			ApplyMods(target, instance);
			ApplyStun(target, entity, instance);
		}

		private static void WarnIgnoredPayload(BuffDef def, string payload, string modifier)
		{
			SkillLibrary.WarnOnce("friendly-stat:" + def.Key + ":" + payload + ":" + modifier, "[Skills] Buff '" + def.Key + "': " + payload + " x " + modifier + " is ignored on the hero and pets for this source");
		}

		private static bool HasFriendlyPayload(BuffDef def, bool restricted)
		{
			if (def.Stun && restricted || def.TickInterval > 0f && def.OnTick != null && def.OnTick.Count > 0)
				return true;
			if (def.StatMods != null)
				foreach (BuffStatMod mod in def.StatMods)
					if (mod != null && IsAllowedFriendlyMod(mod, restricted))
						return true;
			return false;
		}

		private static bool IsAllowedFriendlyMod(BuffStatMod mod, bool restricted)
		{
			// Hostile MovementSpeed keeps the phase-2 modifier types; friendly payloads only allow multiplicative factors.
			if (restricted && mod.StatType == StatType.MovementSpeed)
				return true;
			return mod.ModifierType == ModifierType.PercentMultiplicative && (mod.StatType == StatType.GlobalDefence || mod.StatType == StatType.AttackCooldown ||
				!restricted && (mod.StatType == StatType.Damage || mod.StatType == StatType.MovementSpeed));
		}

		private static BuffInstance Find(List<BuffInstance> buffs, BuffDef def, bool restricted, bool friendlyTarget)
		{
			foreach (BuffInstance instance in buffs)
				// Different source whitelists must not share stacks or refresh a filtered payload into an allowed one.
				if (instance.Def == def && (!friendlyTarget || instance.Restricted == restricted))
					return instance;
			return null;
		}

		// Friendly stats are read as factors; only the hero's MovementSpeed stays a real modifier.
		private void ApplyMods(UnitCore target, BuffInstance instance)
		{
			if (target is PetUnit || instance.Def.StatMods == null)
				return;
			foreach (BuffStatMod mod in instance.Def.StatMods)
			{
				if (mod == null || (instance.FriendlyTarget || instance.Restricted) && (mod.StatType != StatType.MovementSpeed || !IsAllowedFriendlyMod(mod, instance.Restricted)))
					continue;
				// Enemy defence buffs are read as a damage-taken factor (GetEnemyDefenceFactor), never as a real stat.
				if (target is EnemyUnit && IsEnemyDefenceMod(mod))
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
			if (!instance.Def.Stun || instance.FriendlyTarget && !instance.Restricted)
				return;
			float duration = instance.Remaining;
			if (target is PlayerUnit && instance.Restricted)
				duration = Mathf.Min(duration, HeroStunCap);
			ref BuffStunComponent stun = ref stunStash.Get(entity, out bool exists);
			if (!exists || stun.Duration < duration)
				stunStash.Set(entity, new BuffStunComponent { Duration = duration });
			if (target is EnemyUnit enemy)
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
			forcedMovement.Stop(entity);
			if (current.IsDisposed(entity))
				return;
			UnitCore owner = buffsStash.Has(entity) ? buffsStash.Get(entity).Owner : null;
			// Friendly stuns only: enemy stuns (item StunEffect, Viking rage, skill buffs) keep their UnitBuffSystem countdown.
			if (!(owner is EnemyUnit))
				ClearStun(entity, owner);
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
				if (instance.Def.Silence && !instance.Restricted && !instance.FriendlyTarget)
					return true;
			return false;
		}

		public bool IsStunned(UnitCore unit)
		{
			if (unit == null || unit.EntityProvider == null)
				return false;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			return !current.IsDisposed(entity) && stunStash.Has(entity);
		}

		public void ClearStun(UnitCore unit)
		{
			if (unit == null || unit.EntityProvider == null)
				return;
			ClearStun(unit.EntityProvider.Entity, unit);
		}

		private void ClearStun(Entity entity, UnitCore unit)
		{
			World current = CurrentWorld;
			if (current.IsDisposed(entity) || !stunStash.Has(entity))
				return;
			stunStash.Remove(entity);
			if (unit is EnemyUnit enemy && enemy != null)
				enemy.ApplyStunAnimation(false);
		}

		// Both hostile and friendly skill payloads act at read time on the hero and pets.
		public float GetDefenceFactor(UnitCore unit)
		{
			return Mathf.Clamp(GetFriendlyFactor(unit, StatType.GlobalDefence), 0f, 10f);
		}

		// 1.3 means 30% longer between attacks.
		public float GetAttackIntervalFactor(UnitCore unit)
		{
			return Mathf.Clamp(GetFriendlyFactor(unit, StatType.AttackCooldown), 0.1f, 10f);
		}

		public float GetOutgoingDamageFactor(UnitCore unit)
		{
			return Mathf.Clamp(GetFriendlyFactor(unit, StatType.Damage), 0f, 10f);
		}

		private float GetFriendlyFactor(UnitCore unit, StatType statType)
		{
			if (unit == null || !(unit is PlayerUnit) && !(unit is PetUnit) || unit.EntityProvider == null)
				return 1f;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			if (current.IsDisposed(entity) || !buffsStash.Has(entity))
				return 1f;
			float factor = 1f;
			List<BuffInstance> buffs = buffsStash.Get(entity).Buffs;
			if (buffs == null)
				return factor;
			foreach (BuffInstance instance in buffs)
			{
				if (!instance.FriendlyTarget || instance.Def.StatMods == null)
					continue;
				foreach (BuffStatMod mod in instance.Def.StatMods)
					if (mod != null && mod.StatType == statType && mod.ModifierType == ModifierType.PercentMultiplicative && IsAllowedFriendlyMod(mod, instance.Restricted))
						factor *= Mathf.Pow(mod.ValuePerStack, instance.Stacks);
			}
			return factor;
		}

		// Enemies have no defence stat: GlobalDefence x PercentMultiplicative buffs from any source divide the damage they take.
		// 0.7 (armor_break) means ~1.43x damage taken, 1.5 means ~0.67x. Neutral 1 without such buffs.
		public float GetEnemyDefenceFactor(UnitCore unit)
		{
			if (!(unit is EnemyUnit) || unit.EntityProvider == null)
				return 1f;
			Entity entity = unit.EntityProvider.Entity;
			if (CurrentWorld.IsDisposed(entity) || !buffsStash.Has(entity))
				return 1f;
			List<BuffInstance> buffs = buffsStash.Get(entity).Buffs;
			if (buffs == null)
				return 1f;
			float factor = 1f;
			foreach (BuffInstance instance in buffs)
			{
				if (instance.Def == null || instance.Def.StatMods == null)
					continue;
				foreach (BuffStatMod mod in instance.Def.StatMods)
					if (mod != null && IsEnemyDefenceMod(mod))
						factor *= Mathf.Pow(mod.ValuePerStack, instance.Stacks);
			}
			return Mathf.Clamp(factor, 0.1f, 10f);
		}

		private static bool IsEnemyDefenceMod(BuffStatMod mod)
		{
			return mod.StatType == StatType.GlobalDefence && mod.ModifierType == ModifierType.PercentMultiplicative;
		}

		// Pets read their own factor; their follow speed divides out the hero's real MovementSpeed modifier.
		public float GetMoveSpeedFactor(UnitCore unit)
		{
			return GetFriendlyFactor(unit, StatType.MovementSpeed);
		}

		public void ClearPetSourceBuffs(UnitCore unit)
		{
			if (unit == null || unit.EntityProvider == null)
				return;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			if (current.IsDisposed(entity) || !buffsStash.Has(entity))
				return;
			List<BuffInstance> buffs = buffsStash.Get(entity).Buffs;
			if (buffs == null)
				return;
			for (int i = buffs.Count - 1; i >= 0; i--)
				if (buffs[i].Source is PetUnit)
					RemoveBuffAt(buffs, i);
			if (buffs.Count == 0)
				buffsStash.Remove(entity);
		}

		#endregion

		#region Casting

		public bool IsCasting(UnitCore unit)
		{
			if (unit == null || unit.EntityProvider == null)
				return false;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			return !current.IsDisposed(entity) && (castingStash.Has(entity) || IsDashing(unit));
		}

		public bool IsChanneling(UnitCore unit)
		{
			return IsCasting(unit) && castingStash.Has(unit.EntityProvider.Entity) && castingStash.Get(unit.EntityProvider.Entity).Channeling;
		}

		public bool IsInCastPoint(UnitCore unit)
		{
			return IsCasting(unit) && castingStash.Has(unit.EntityProvider.Entity) && !castingStash.Get(unit.EntityProvider.Entity).Fired;
		}

		public void ResetCaster(UnitCore unit)
		{
			if (unit == null || unit.EntityProvider == null)
				return;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			if (current.IsDisposed(entity))
				return;
			forcedMovement.Stop(entity);
			CancelCast(entity);
			if (!casterStash.Has(entity))
				return;
			ref SkillCasterComponent caster = ref casterStash.Get(entity);
			foreach (SkillSlot slot in caster.Slots)
			{
				slot.Cooldown = slot.Def.InitialCooldown;
				slot.AuraTimer = UnityEngine.Random.Range(0f, GetAuraInterval(slot.Def));
			}
			caster.Engaged = false;
			caster.NextThinkTime = Time.time + UnityEngine.Random.Range(0f, ThinkInterval);
		}

		public void CancelCast(UnitCore unit)
		{
			if (unit == null || unit.EntityProvider == null)
				return;
			World current = CurrentWorld;
			Entity entity = unit.EntityProvider.Entity;
			if (!current.IsDisposed(entity))
				CancelCast(entity);
		}

		public void CancelCast(Entity entity)
		{
			World current = CurrentWorld;
			if (current.IsDisposed(entity)) return;
			forcedMovement.Stop(entity, true);
			if (!castingStash.Has(entity))
				return;
			// Inactive (pooled) enemies keep the old behaviour: EnemyCastSystem times its own state out, no animator calls.
			if (castingStash.Get(entity).Caster is EnemyUnit castingEnemy && castingEnemy != null && castingEnemy.gameObject.activeInHierarchy && GameContext.Has<EnemyCastSystem>())
				GameContext.Get<EnemyCastSystem>().CancelCast(entity);
			SkillTelegraphView telegraph = castingStash.Get(entity).Telegraph;
			if (telegraph != null)
				telegraph.Despawn();
			castingStash.Remove(entity);
		}

		#endregion

		#region Missiles and vfx

		public void LaunchLinearMissiles(in SkillCastContext context, LinearMissileEffect def)
		{
			if (def == null || context.Caster == null)
				return;
			linearMissiles.Launch(this, context, def);
		}

		public void LaunchMissile(in SkillCastContext context, UnitCore target, string vfxKey, float speed, Vector3 startOffset, List<ISkillEffect> onImpact)
		{
			LaunchMissile(context, target, vfxKey, speed, startOffset, onImpact, null);
		}

		// Ranged basic attack (EnemyAttackSystem.Attack): a tracked projectile whose impact is the normal enemy hit.
		public void LaunchRangedAttack(EnemyUnit caster, UnitCore target, RangedAttackDef def)
		{
			if (caster == null || def == null || caster.IsDead || !SkillTargetQuery.IsAlive(target))
				return;
			var context = new SkillCastContext
			{
				Caster = caster,
				CasterEntity = caster.EntityProvider != null ? caster.EntityProvider.Entity : default,
				Instigator = caster,
				PrimaryTarget = target,
				Point = target.transform.position,
				Level = 1,
				Power = 1f
			};
			Vector3 direction = target.transform.position - caster.transform.position;
			direction.y = 0f;
			Vector3 offset = direction.sqrMagnitude > 0.0001f ? direction.normalized * RangedLaunchForward : Vector3.zero;
			LaunchMissile(context, target, def.ProjectileVfxKey, def.ProjectileSpeed, offset, null, def);
		}

		private void LaunchMissile(in SkillCastContext context, UnitCore target, string vfxKey, float speed, Vector3 startOffset, List<ISkillEffect> onImpact, RangedAttackDef ranged)
		{
			if (context.Caster == null || !SkillTargetQuery.IsAlive(target))
				return;
			SkillCastContext impact = context;
			impact.PrimaryTarget = target;
			Vector3 start = context.Caster.transform.position + Vector3.up * MissileHeight + startOffset;
			speed = Mathf.Max(0.1f, speed);
			float travel = Vector3.Distance(start, target.transform.position + Vector3.up * MissileHeight) / speed;
			// No visual: resolve after the travel time.
			var missile = new PendingMissile { Context = impact, Target = target, OnImpact = onImpact, Remaining = travel, Ranged = ranged };
			SkillVfxEntry vfx = string.IsNullOrEmpty(vfxKey) ? null : SkillLibrary.FindVfx(vfxKey);
			// Prefab, or the runtime projectile built from ProjectileTexture.
			ParticleSystem prefab = vfx == null ? null : vfx.Prefab != null ? vfx.Prefab : SkillProjectileView.GetTemplate(vfx);
			if (prefab != null && GameContext.Has<WorldVfxService>())
			{
				// The impact is always resolved here: arrival lands it early, the flight cap bounds it (visual despawns with it).
				missile.Remaining = travel * MissileFlightSlack + MissileFlightMargin;
				ParticleSystem visual = GameContext.Get<WorldVfxService>().SpawnTracking(prefab, start, target.transform, Vector3.up * MissileHeight, speed, () =>
				{
					missile.Arrived = true;
					missile.Remaining = 0f;
				}, missile.Remaining);
				missile.Visual = visual;
				if (visual != null && visual.TryGetComponent(out SkillProjectileView view))
					view.Aim(target.transform.position + Vector3.up * MissileHeight - start);
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
				if (missile.Ranged != null)
					RangedImpact(missile);
				else
					Impact(missile.Context, missile.Target, missile.OnImpact);
			}
			linearMissiles.Tick(this, deltaTime);
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

		// Same WeaponHitEvent as the melee basic attack (EnemyAttackSystem), so dodge, block, defence, thorns and lifesteal all apply.
		private void RangedImpact(PendingMissile missile)
		{
			UnitCore target = missile.Target;
			if (!SkillTargetQuery.IsAlive(target) || !(missile.Context.Caster is EnemyUnit source) || !SkillTargetQuery.IsAlive(source))
				return;
			// Flight cap ran out before the visual arrived (target teleported, switched area...): only a near miss still lands.
			if (missile.Visual != null && !missile.Arrived)
			{
				Vector3 aim = target.transform.position + Vector3.up * MissileHeight;
				if ((missile.Visual.transform.position - aim).sqrMagnitude > RangedCapHitRadius * RangedCapHitRadius)
					return;
			}
			// Same gate as the melee attack: a sleeping, dying or frozen (dialogue, cutscene) hero is not hit.
			if (target is PlayerUnit playerUnit)
			{
				PlayerModel player = GameContext.Has<PlayerModel>() ? GameContext.Get<PlayerModel>() : null;
				if (playerUnit.IsSleeping || playerUnit.IsDying || player != null && !player.CanMove)
					return;
			}
			World current = CurrentWorld;
			RangedAttackDef def = missile.Ranged;
			float multiplier = Mathf.Max(0f, def.DamageMultiplier);
			bool special = Mathf.Abs(multiplier - 1f) > 0.001f;
			hitEvent.NextFrame(new WeaponHitEvent { Source = source, Target = target, DamageType = source.DamageType, HasSpecialMultiplier = special, SpecialMultiplier = multiplier });
			if (!string.IsNullOrEmpty(def.ImpactVfxKey))
				SpawnVfx(def.ImpactVfxKey, target.transform.position + Vector3.up * MissileHeight, Quaternion.identity, def.ImpactVfxScale > 0f ? def.ImpactVfxScale : 1f);
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
