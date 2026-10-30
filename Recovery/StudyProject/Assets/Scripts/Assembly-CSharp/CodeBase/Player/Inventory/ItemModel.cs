using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Abilities;
using Codebase.Abilities.Core;
using Codebase.Balance;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Player.Models;
using UniMob;
using UniMob.Core;
using UnityEngine;

namespace Codebase.Player.Inventory
{
	[Serializable]
	public class ItemModel : Model, IAbilityOwner
	{
		private int level;

		private int copies;

		private bool evolved;

		[CompilerGenerated]
		private int __SlotIndex__BackingField;

		[CompilerGenerated]
		private float __AbilityDamageMultiplier__BackingField;

		[CompilerGenerated]
		private float __AbilityDamageFlat__BackingField;

		[CompilerGenerated]
		private float __AbilityAttackSpeedMultiplier__BackingField;

		[CompilerGenerated]
		private float __AbilityCritMultiplier__BackingField;

		[CompilerGenerated]
		private float __AbilityCritChanceBonus__BackingField;

		[CompilerGenerated]
		private float __AbilitySuperCritChanceBonus__BackingField;

		[CompilerGenerated]
		private float __AbilitySuperCritDamageBonus__BackingField;

		[CompilerGenerated]
		private bool __ShowNotification__BackingField;

		public ItemConfig Config;

		private readonly List<AbilityDef> activeAbilityDefs;

		private ComputedAtom<int> __EffectiveLevel;

		private ComputedAtom<int> __DisplayLevel;

		private ComputedAtom<int> __NextEffectiveLevel;

		private ComputedAtom<bool> __CanEvolve;

		private ComputedAtom<int> __Level;

		private ComputedAtom<int> __Copies;

		private ComputedAtom<bool> __Evolved;

		private ComputedAtom<int> __SlotIndex;

		private ComputedAtom<float> __AbilityDamageMultiplier;

		private ComputedAtom<float> __AbilityDamageFlat;

		private ComputedAtom<float> __AbilityAttackSpeedMultiplier;

		private ComputedAtom<float> __AbilityCritMultiplier;

		private ComputedAtom<float> __AbilityCritChanceBonus;

		private ComputedAtom<float> __AbilitySuperCritChanceBonus;

		private ComputedAtom<float> __AbilitySuperCritDamageBonus;

		private ComputedAtom<bool> __ShowNotification;

		private ComputedAtom<double> __DPS;

		private ComputedAtom<double> __Damage;

		private ComputedAtom<double> __Armor;

		private ComputedAtom<double> __NextLevelDamage;

		private ComputedAtom<double> __NextLevelArmor;

		private ComputedAtom<double> __StatPerCopy;

		string IAbilityOwner.Key => Config.Key; // Native 0x1463364.

		List<AbilityState> IAbilityOwner.AbilityStates => AbilityStates; // Native 0x146337c.

		bool IAbilityOwner.HasAbility => HasAbility; // Native 0x1463384 tail branch.

		public string Key => Config.Key;

		public bool IsDivine => Evolved && Config.Rarity != Rarity.Mythical;

		public bool HasAbility => activeAbilityDefs.Count > 0; // Native 0x1463388.

		public bool HasUnlockableAbility // Native 0x1463410 counts all definitions; pet-only ones are skipped while pet mode is off.
		{
			get
			{
				foreach (AbilityDef definition in Config.AbilityDefs)
					if (!global::Codebase.Abilities.Conditions.PetModeCondition.IsHidden(definition)) return true;
				return false;
			}
		}

		public bool HasStars
        {
            get
            {
                // Native 0x1463468: only active definitions contribute stars.
                foreach (AbilityDef definition in activeAbilityDefs)
                    if (definition.LevelScaling.Count > 0) return true;
                return false;
            }
        }

		// Native ELF VA 0x1463524.
		public Sprite CurrentIcon => IsDivine && Config.DivineIcon != null ? Config.DivineIcon : Config.Icon;

		// Native ELF VA 0x14635c4.
		public WeaponView CurrentPrefab => IsDivine && Config.DivinePrefab != null ? Config.DivinePrefab : Config.Prefab;

		public Rarity Rarity => IsDivine ? Rarity.Divine : Config.Rarity;

		// Native ELF VA 0x1463664.
		public bool IsPreDivine => !Evolved;

		public int EffectiveLevel
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EffectiveLevel == null) __EffectiveLevel = CodeGenAtom.CreatePooled(this, null, () => EffectiveLevel, false);
				return __EffectiveLevel.DirectEvaluate() ? Evolved ? Level : Copies : __EffectiveLevel.Value;
			}
		}

		public int DisplayLevel
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__DisplayLevel == null) __DisplayLevel = CodeGenAtom.CreatePooled(this, null, () => DisplayLevel, false);
				return __DisplayLevel.DirectEvaluate() ? EffectiveLevel : __DisplayLevel.Value;
			}
		}

		public int NextEffectiveLevel
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__NextEffectiveLevel == null) __NextEffectiveLevel = CodeGenAtom.CreatePooled(this, null, () => NextEffectiveLevel, false);
				return __NextEffectiveLevel.DirectEvaluate() ? EffectiveLevel + 1 : __NextEffectiveLevel.Value;
			}
		}

		public int LevelsToEvolve => CombatBalanceConfig.GetEvolutionShardsRequirement(IsArmor, Config.Rarity, NextEvolutionLevel);

		public bool CanEvolve
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CanEvolve == null) __CanEvolve = CodeGenAtom.CreatePooled(this, null, () => CanEvolve, false);
				return __CanEvolve.DirectEvaluate() ? !IsEvolutionCapped && Copies >= LevelsToEvolve : __CanEvolve.Value;
			}
		}

		// Pet mode: a pet stops evolving at CombatBalanceConfig.MaxEvolutionLevel (higher levels reuse the last balance row).
		public bool IsEvolutionCapped
		{
			get
			{
				if (!PetConfig.Enabled || IsArmor)
					return false;
				int max = CombatBalanceConfig.MaxEvolutionLevel;
				return max > 0 && NextEvolutionLevel > max;
			}
		}

		public bool IsArmor => Config.SlotType == ItemSlotType.Helmet || Config.SlotType == ItemSlotType.Shield;

		public ItemType ItemType => IsArmor ? ItemType.Armor : ItemType.Weapon; // Native 0x1463b38.

		public ItemDataUd Data { get; private set; }

		public int Level
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Level == null) __Level = CodeGenAtom.CreatePooled(this, null, () => Level, false);
				return __Level.DirectEvaluate() ? level : __Level.Value;
			}
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Level != null && !__Level.CompareAndInvalidate(value)) return;
				if (level == value) return;
				level = value;
				if (Data != null) Data.Level.Value = value;
				RebuildAbilityStates();
			}
		}

		public int Copies
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Copies == null) __Copies = CodeGenAtom.CreatePooled(this, null, () => Copies, false);
				return __Copies.DirectEvaluate() ? copies : __Copies.Value;
			}
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Copies != null && !__Copies.CompareAndInvalidate(value)) return;
				if (copies == value) return;
				copies = value;
				if (Data != null) Data.Copies.Value = value;
				if (!Evolved) RebuildAbilityStates();
			}
		}

		public bool Evolved
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Evolved == null) __Evolved = CodeGenAtom.CreatePooled(this, null, () => Evolved, false);
				return __Evolved.DirectEvaluate() ? evolved : __Evolved.Value;
			}
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Evolved != null && !__Evolved.CompareAndInvalidate(value)) return;
				if (evolved == value) return;
				evolved = value;
				if (Data != null) Data.Evolved.Value = value;
				RebuildAbilityStates();
			}
		}

		public int SlotIndex
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__SlotIndex == null) __SlotIndex = CodeGenAtom.CreatePooled(this, null, () => SlotIndex, false);
                return __SlotIndex.DirectEvaluate() ? __SlotIndex__BackingField : __SlotIndex.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__SlotIndex != null && !__SlotIndex.CompareAndInvalidate(value)) return;
                __SlotIndex__BackingField = value;
            }
        }

		public float Cooldown { get; set; }

		public float WearingTime { get; set; }

		public bool AttackInProgress { get; set; }

		public bool JustStartedAttack { get; set; }

		public bool ResetCooldown { get; set; }

		public float AbilityDamageMultiplier
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityDamageMultiplier == null) __AbilityDamageMultiplier = CodeGenAtom.CreatePooled(this, null, () => AbilityDamageMultiplier, false);
                return __AbilityDamageMultiplier.DirectEvaluate() ? __AbilityDamageMultiplier__BackingField : __AbilityDamageMultiplier.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityDamageMultiplier != null && !__AbilityDamageMultiplier.CompareAndInvalidate(value)) return;
                __AbilityDamageMultiplier__BackingField = value;
            }
        }

		public float AbilityDamageFlat
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityDamageFlat == null) __AbilityDamageFlat = CodeGenAtom.CreatePooled(this, null, () => AbilityDamageFlat, false);
                return __AbilityDamageFlat.DirectEvaluate() ? __AbilityDamageFlat__BackingField : __AbilityDamageFlat.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityDamageFlat != null && !__AbilityDamageFlat.CompareAndInvalidate(value)) return;
                __AbilityDamageFlat__BackingField = value;
            }
        }

		public float AbilityAttackSpeedMultiplier
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityAttackSpeedMultiplier == null) __AbilityAttackSpeedMultiplier = CodeGenAtom.CreatePooled(this, null, () => AbilityAttackSpeedMultiplier, false);
                return __AbilityAttackSpeedMultiplier.DirectEvaluate() ? __AbilityAttackSpeedMultiplier__BackingField : __AbilityAttackSpeedMultiplier.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityAttackSpeedMultiplier != null && !__AbilityAttackSpeedMultiplier.CompareAndInvalidate(value)) return;
                __AbilityAttackSpeedMultiplier__BackingField = value;
            }
        }

		public float AbilityCritMultiplier
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityCritMultiplier == null) __AbilityCritMultiplier = CodeGenAtom.CreatePooled(this, null, () => AbilityCritMultiplier, false);
                return __AbilityCritMultiplier.DirectEvaluate() ? __AbilityCritMultiplier__BackingField : __AbilityCritMultiplier.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityCritMultiplier != null && !__AbilityCritMultiplier.CompareAndInvalidate(value)) return;
                __AbilityCritMultiplier__BackingField = value;
            }
        }

		public float AbilityCritChanceBonus
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityCritChanceBonus == null) __AbilityCritChanceBonus = CodeGenAtom.CreatePooled(this, null, () => AbilityCritChanceBonus, false);
                return __AbilityCritChanceBonus.DirectEvaluate() ? __AbilityCritChanceBonus__BackingField : __AbilityCritChanceBonus.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilityCritChanceBonus != null && !__AbilityCritChanceBonus.CompareAndInvalidate(value)) return;
                __AbilityCritChanceBonus__BackingField = value;
            }
        }

		public float AbilitySuperCritChanceBonus
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilitySuperCritChanceBonus == null) __AbilitySuperCritChanceBonus = CodeGenAtom.CreatePooled(this, null, () => AbilitySuperCritChanceBonus, false);
                return __AbilitySuperCritChanceBonus.DirectEvaluate() ? __AbilitySuperCritChanceBonus__BackingField : __AbilitySuperCritChanceBonus.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilitySuperCritChanceBonus != null && !__AbilitySuperCritChanceBonus.CompareAndInvalidate(value)) return;
                __AbilitySuperCritChanceBonus__BackingField = value;
            }
        }

		public float AbilitySuperCritDamageBonus
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilitySuperCritDamageBonus == null) __AbilitySuperCritDamageBonus = CodeGenAtom.CreatePooled(this, null, () => AbilitySuperCritDamageBonus, false);
                return __AbilitySuperCritDamageBonus.DirectEvaluate() ? __AbilitySuperCritDamageBonus__BackingField : __AbilitySuperCritDamageBonus.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__AbilitySuperCritDamageBonus != null && !__AbilitySuperCritDamageBonus.CompareAndInvalidate(value)) return;
                __AbilitySuperCritDamageBonus__BackingField = value;
            }
        }

		public float NextHitEmpower { get; set; }

		public bool ShowNotification
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ShowNotification == null) __ShowNotification = CodeGenAtom.CreatePooled(this, null, () => ShowNotification, false);
                return __ShowNotification.DirectEvaluate() ? __ShowNotification__BackingField : __ShowNotification.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ShowNotification != null && !__ShowNotification.CompareAndInvalidate(value)) return;
                __ShowNotification__BackingField = value;
            }
        }

		private CombatBalanceConfig CombatBalanceConfig => GameContext.Get<CombatBalanceConfig>();

		public double DPS
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__DPS == null) __DPS = CodeGenAtom.CreatePooled(this, null, () => DPS, false);
				return __DPS.DirectEvaluate() ? Damage / Config.AttackCooldown : __DPS.Value;
			}
		}

		public double Damage
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Damage == null) __Damage = CodeGenAtom.CreatePooled(this, null, () => Damage, false);
				return __Damage.DirectEvaluate() ? CalcDamage(EffectiveLevel, Copies) : __Damage.Value;
			}
		}

		public double Armor
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Armor == null) __Armor = CodeGenAtom.CreatePooled(this, null, () => Armor, false);
				return __Armor.DirectEvaluate() ? CalcArmor(EffectiveLevel, Copies) : __Armor.Value;
			}
		}

		public double NextLevelDamage
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__NextLevelDamage == null) __NextLevelDamage = CodeGenAtom.CreatePooled(this, null, () => NextLevelDamage, false);
				return __NextLevelDamage.DirectEvaluate() ? Evolved ? CalcEvolutionDamage(NextEvolutionLevel, CopiesAfterEvolution) : CalcDamage(NextEffectiveLevel, Copies) : __NextLevelDamage.Value;
			}
		}

		public double NextLevelArmor
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__NextLevelArmor == null) __NextLevelArmor = CodeGenAtom.CreatePooled(this, null, () => NextLevelArmor, false);
				return __NextLevelArmor.DirectEvaluate() ? Evolved ? CalcEvolutionArmor(NextEvolutionLevel, CopiesAfterEvolution) : CalcArmor(NextEffectiveLevel, Copies) : __NextLevelArmor.Value;
			}
		}

		private int NextEvolutionLevel => Level + (Evolved ? 1 : 0);

		private int CopiesAfterEvolution => Math.Max(0, Copies - LevelsToEvolve);

		public double CopyStatGrowth => IsArmor ? CombatBalanceConfig.GetEvolutionArmorCopyValue(Config.DamageType, Config.Rarity, Level) : CombatBalanceConfig.GetEvolutionDamageCopyValue(Config.DamageType, Config.Rarity, Level);

		public double StatPerCopy
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__StatPerCopy == null) __StatPerCopy = CodeGenAtom.CreatePooled(this, null, () => StatPerCopy, false);
				return __StatPerCopy.DirectEvaluate() ? Evolved ? CopyStatGrowth : (double)(IsArmor ? CombatBalanceConfig.GetArmorGrowthPerLevel(Config.DamageType, Config.Rarity) : CombatBalanceConfig.GetDamageGrowthPerLevel(Config.DamageType, Config.Rarity)) : __StatPerCopy.Value;
			}
		}

		public List<AbilityDef> ActiveAbilityDefs => this.activeAbilityDefs;

		public List<AbilityDef> AbilityDefs => this.activeAbilityDefs;

		public List<AbilityState> AbilityStates { get; private set; }

		public double CalcDamage(int level)
		{
			return CalcDamage(level, Copies);
		}

		public double CalcDamage(int level, int copies)
		{
			if (Evolved) return CalcEvolutionDamage(level, copies);
			float baseValue = CombatBalanceConfig.GetDamageBase(Config.DamageType, Config.Rarity);
			float growth = CombatBalanceConfig.GetDamageGrowthPerLevel(Config.DamageType, Config.Rarity);
			float scaled = growth * (float)level;
			float result = baseValue + scaled;
			result += Config.FlatDamage;
			return result;
		}

		public double CalcArmor(int level)
		{
			return CalcArmor(level, Copies);
		}

		public double CalcArmor(int level, int copies)
		{
			if (Evolved) return CalcEvolutionArmor(level, copies);
			float baseValue = CombatBalanceConfig.GetArmorBase(Config.DamageType, Config.Rarity);
			float growth = CombatBalanceConfig.GetArmorGrowthPerLevel(Config.DamageType, Config.Rarity);
			float scaled = growth * (float)level;
			float result = baseValue + scaled;
			return result;
		}

		private double CalcEvolutionDamage(int level, int copies)
		{
			float baseValue = CombatBalanceConfig.GetEvolutionDamage(Config.DamageType, Config.Rarity, level);
			float copyValue = CombatBalanceConfig.GetEvolutionDamageCopyValue(Config.DamageType, Config.Rarity, level);
			float scaled = copyValue * (float)copies;
			float result = baseValue + scaled;
			result += Config.FlatDamage;
			return result;
		}

		private double CalcEvolutionArmor(int level, int copies)
		{
			float baseValue = CombatBalanceConfig.GetEvolutionArmor(Config.DamageType, Config.Rarity, level);
			float copyValue = CombatBalanceConfig.GetEvolutionArmorCopyValue(Config.DamageType, Config.Rarity, level);
			float scaled = copyValue * (float)copies;
			float result = baseValue + scaled;
			return result;
		}

		public bool IsAbilityUnlocked(AbilityDef def)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return false;
			if (global::Codebase.Abilities.Conditions.PetModeCondition.IsHidden(def)) return false;
			return Evolved ? def.UnlockLevel <= Level : def.UnlockLevel == 0;
		}

		public ItemModel(Lifetime lifetime, ItemConfig config)
			: base(lifetime)
		{
			activeAbilityDefs = new List<AbilityDef>();
			AbilityStates = new List<AbilityState>();
			Config = config;
			level = 1;
			copies = 1;
			RebuildAbilityStates();
		}

		public bool TryEvolve()
        {
            // Native 0x14658b4: the command owns mutations and follow-up effects.
            bool canEvolve = CanEvolve;
            if (canEvolve) App.ExecuteCommand(new global::Codebase.Player.Commands.EvolveItemCommand { ItemModel = this });
            return canEvolve;
        }

		public void SetInitialCopies(int value)
		{
			// Native ELF VA 0x145f4e4 is the inlined Copies setter.
			Copies = value;
		}

		public void BindToUserData(ItemDataUd dataUd)
		{
			Data = dataUd;
			evolved = false;
			Evolved = dataUd.Evolved.Value;
			level = -1;
			Level = dataUd.Level.Value;
			copies = -1;
			Copies = dataUd.Copies.Value;
		}

		private void RebuildAbilityStates()
		{
			activeAbilityDefs.Clear();
			foreach (var def in Config.AbilityDefs)
				if (IsAbilityUnlocked(def)) activeAbilityDefs.Add(def);
			bool unchanged = AbilityStates.Count == activeAbilityDefs.Count;
			if (unchanged)
				for (int index = 0; index < AbilityStates.Count; index++)
					if (AbilityStates[index].Def != activeAbilityDefs[index]) { unchanged = false; break; }
			if (unchanged) return;
			AbilityStates.Clear();
			foreach (var def in activeAbilityDefs) AbilityStates.Add(new AbilityState(def, this));
		}
	}
}
