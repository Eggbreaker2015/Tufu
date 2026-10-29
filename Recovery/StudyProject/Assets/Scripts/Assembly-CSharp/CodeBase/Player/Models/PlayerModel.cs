using Codebase.UI.World;
using Codebase.Abilities.Effects;
using Codebase.Abilities.Events;
using Codebase.Abilities;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Abilities.Systems;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UserData;
using Codebase.Crystals;
using Codebase.Location;
using Codebase.Player.Inventory;
using Codebase.Rage;
using Codebase.Services.Cheats;
using Codebase.Unit;
using JetBrains.Annotations;
using UniMob;
using UniMob.Core;
using UnityEngine;

namespace Codebase.Player.Models
{
	public class PlayerModel : Model<PlayerUd>
	{
		public const int PORTAL_GEMS_COUNT = 7;
		[CompilerGenerated]
		private List<ItemSlotModel> __ItemSlots__BackingField;
		[CompilerGenerated]
		private List<ItemModel> __Items__BackingField;
		[CompilerGenerated]
		private ItemModel __SelectedItem__BackingField;
		[Inject]
		private AbilityExecuteEventSystem abilityExecuteEventSystem;
		[Inject]
		private LocationsConfig locationsConfig;
		[Inject]
		private IAnalyticsWrapper analytics;
		[Inject]
		private UserModel userModel;
		[Inject]
		private CheatsModel cheats;
		[CompilerGenerated]
		private float __AbilityDamageMultiplier__BackingField;
		[CompilerGenerated]
		private float __AbilityDamageFlat__BackingField;
		[CompilerGenerated]
		private float __AbilityHealthRegenMultiplier__BackingField;
		private PlayerConfig config;
		private readonly MutableAtom<int> version;
		[Inject]
		private ItemsProvider itemsProvider;
		[Inject]
		private GameDataService gameDataService;
		[Inject]
		private CrystalModel crystalModel;
		[Inject]
		private RageModel rageModel;
		private ComputedAtom<List<ItemSlotModel>> __ItemSlots;
		private ComputedAtom<List<ItemModel>> __Items;
		private ComputedAtom<List<ItemSlotModel>> __EmptyItemSlots;
		private ComputedAtom<List<ItemSlotModel>> __EmptyPetSlots;
		private ComputedAtom<List<ItemModel>> __EquippedItems;
		private ComputedAtom<List<ItemModel>> __EquippedWeapons;
		private ComputedAtom<List<ItemModel>> __EquippedArmors;
		private ComputedAtom<int> __EquippedSameWeaponTypeMaxCount;
		private ComputedAtom<int> __EquippedDifferentWeaponTypesCount;
		private ComputedAtom<List<ItemModel>> __UnequippedItems;
		private ComputedAtom<int> __UnlockedWeaponSlots;
		private ComputedAtom<int> __UnlockedArmorSlots;
		private ComputedAtom<ItemModel> __SelectedItem;
		private ComputedAtom<LocationConfig> __LastUnlockedLocation;
		private ComputedAtom<LocationConfig> __BossProgressLocation;
		private ComputedAtom<LocationConfig> __CurrentLocation;
		private ComputedAtom<double> __Gems;
		private ComputedAtom<int> __PortalGemsCollected;
		private ComputedAtom<bool> __VisitedLastUnlockedLocation;
		private ComputedAtom<bool> __InBossTowerArena;
		private ComputedAtom<bool> __DisablePermanentStatsGain;
		private ComputedAtom<bool> __IsUnlockAnimationShown;
		private ComputedAtom<bool> __IsPortalUnlocked;
		private ComputedAtom<long> __UnstackCooldownTimestamp;
		private ComputedAtom<float> __AbilityDamageMultiplier;
		private ComputedAtom<float> __AbilityDamageFlat;
		private ComputedAtom<float> __AbilityHealthRegenMultiplier;
		public PlayerUnit Unit { get; set; }

		public List<ItemSlotModel> ItemSlots
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ItemSlots == null)
					__ItemSlots = CodeGenAtom.CreatePooled(this, null, () => ItemSlots, false);
				return __ItemSlots.DirectEvaluate() ? __ItemSlots__BackingField : __ItemSlots.Value;
			}

			private set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ItemSlots != null && !__ItemSlots.CompareAndInvalidate(value))
					return;
				__ItemSlots__BackingField = value;
			}
		}

		public List<ItemModel> Items
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Items == null)
					__Items = CodeGenAtom.CreatePooled(this, null, () => Items, false);
				return __Items.DirectEvaluate() ? __Items__BackingField : __Items.Value;
			}

			private set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Items != null && !__Items.CompareAndInvalidate(value))
					return;
				__Items__BackingField = value;
			}
		}

		public Vector3 LastPosition { get; set; }

		// Pet mode: the player's own attack item, built like the starting weapon but never stored or saved.
		public ItemModel PlayerAttackItem { get; private set; }
		public Vector3? SavePositionOverride { get; set; }
		public List<Stat> LastStats { get; set; }

		public List<ItemSlotModel> EmptyItemSlots
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EmptyItemSlots == null)
					__EmptyItemSlots = CodeGenAtom.CreatePooled(this, null, () => EmptyItemSlots, false);
				return __EmptyItemSlots.DirectEvaluate() ? ItemSlots.Where(slot => !slot.IsEquipped && slot.IsUnlocked).ToList() : __EmptyItemSlots.Value;
			}
		}

		// Pet mode: unlocked weapon slots without an item. A slot whose pet is dead is not empty.
		public List<ItemSlotModel> EmptyPetSlots
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EmptyPetSlots == null)
					__EmptyPetSlots = CodeGenAtom.CreatePooled(this, null, () => EmptyPetSlots, false);
				return __EmptyPetSlots.DirectEvaluate() ? ItemSlots.Where(slot => !slot.IsEquipped && slot.IsUnlocked && slot.SlotType == ItemSlotType.Weapon).ToList() : __EmptyPetSlots.Value;
			}
		}

		public List<ItemModel> EquippedItems
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EquippedItems == null)
					__EquippedItems = CodeGenAtom.CreatePooled(this, null, () => EquippedItems, false);
				return __EquippedItems.DirectEvaluate() ? ItemSlots.Where(slot => slot.IsEquipped).Select(slot => slot.ItemModel).ToList() : __EquippedItems.Value;
			}
		}

		public List<ItemModel> EquippedWeapons
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EquippedWeapons == null)
					__EquippedWeapons = CodeGenAtom.CreatePooled(this, null, () => EquippedWeapons, false);
				return __EquippedWeapons.DirectEvaluate() ? EquippedItems.Where(item => item.Config.SlotType == ItemSlotType.Weapon).ToList() : __EquippedWeapons.Value;
			}
		}

		public List<ItemModel> EquippedArmors
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EquippedArmors == null)
					__EquippedArmors = CodeGenAtom.CreatePooled(this, null, () => EquippedArmors, false);
				return __EquippedArmors.DirectEvaluate() ? EquippedItems.Where(item => item.Config.SlotType == ItemSlotType.Helmet || item.Config.SlotType == ItemSlotType.Shield).ToList() : __EquippedArmors.Value;
			}
		}

		public bool HasInventoryNotification => Items.Any(item => item.ShowNotification); // Native ELF 0x14588d4 and predicate 0x14612d0.
		public bool CanMove { get; set; }

		public int EquippedSameWeaponTypeMaxCount
		{
			get
			{
				// Native ELF VA 0x14589f0 and selectors 0x1461414/0x1461434.
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EquippedSameWeaponTypeMaxCount == null)
					__EquippedSameWeaponTypeMaxCount = CodeGenAtom.CreatePooled(this, null, () => EquippedSameWeaponTypeMaxCount, false);
				return __EquippedSameWeaponTypeMaxCount.DirectEvaluate() ? EquippedWeapons.GroupBy(item => item.Config.DamageType).Select(group => group.Count()).DefaultIfEmpty(0).Max() : __EquippedSameWeaponTypeMaxCount.Value;
			}
		}

		public int EquippedDifferentWeaponTypesCount
		{
			get
			{
				// Native ELF VA 0x1458cd8 and selector 0x146147c.
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__EquippedDifferentWeaponTypesCount == null)
					__EquippedDifferentWeaponTypesCount = CodeGenAtom.CreatePooled(this, null, () => EquippedDifferentWeaponTypesCount, false);
				return __EquippedDifferentWeaponTypesCount.DirectEvaluate() ? EquippedWeapons.Select(item => item.Config.DamageType).Distinct().Count() : __EquippedDifferentWeaponTypesCount.Value;
			}
		}

		public List<ItemModel> UnequippedItems
		{
			get
			{
				// Native ELF VA 0x1458f14; selectors 0x1460d1c/0x146149c/0x14614e0/0x1461ae4.
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__UnequippedItems == null)
					__UnequippedItems = CodeGenAtom.CreatePooled(this, null, () => UnequippedItems, false);
				return __UnequippedItems.DirectEvaluate() ? Items.Where(item => ItemSlots.All(slot => slot.ItemModel != item)).OrderByDescending(item => item.Rarity).ThenBy(item => item.EffectiveLevel).ToList() : __UnequippedItems.Value;
			}
		}

		public int UnlockedWeaponSlots
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__UnlockedWeaponSlots == null)
					__UnlockedWeaponSlots = CodeGenAtom.CreatePooled(this, null, () => UnlockedWeaponSlots, false);
				return __UnlockedWeaponSlots.DirectEvaluate() ? ItemSlots.Count(slot => slot.IsUnlocked && slot.SlotType == ItemSlotType.Weapon) : __UnlockedWeaponSlots.Value;
			}
		}

		public int UnlockedArmorSlots
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__UnlockedArmorSlots == null)
					__UnlockedArmorSlots = CodeGenAtom.CreatePooled(this, null, () => UnlockedArmorSlots, false);
				return __UnlockedArmorSlots.DirectEvaluate() ? ItemSlots.Count(slot => slot.IsUnlocked && (slot.SlotType == ItemSlotType.Helmet || slot.SlotType == ItemSlotType.Shield)) : __UnlockedArmorSlots.Value;
			}
		}

		[CanBeNull]
		public ItemModel SelectedItem
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__SelectedItem == null)
					__SelectedItem = CodeGenAtom.CreatePooled(this, null, () => SelectedItem, false);
				return __SelectedItem.DirectEvaluate() ? __SelectedItem__BackingField : __SelectedItem.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__SelectedItem != null && !__SelectedItem.CompareAndInvalidate(value))
					return;
				__SelectedItem__BackingField = value;
			}
		}

		// Native ELF 1454f1c 0x1454f1c; predicate reads the original saved location number.
		public LocationConfig LastUnlockedLocation
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__LastUnlockedLocation == null)
					__LastUnlockedLocation = CodeGenAtom.CreatePooled(this, null, () => LastUnlockedLocation, false);
				return __LastUnlockedLocation.DirectEvaluate() ? locationsConfig.Locations.First(location => location.Number == Data.UnlockedLocationNumber.Value) : __LastUnlockedLocation.Value;
			}
		}

		// Native ELF 1459808 0x1459808; predicate reads the original saved location number.
		public LocationConfig BossProgressLocation
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__BossProgressLocation == null)
					__BossProgressLocation = CodeGenAtom.CreatePooled(this, null, () => BossProgressLocation, false);
				return __BossProgressLocation.DirectEvaluate() ? locationsConfig.Locations.First(location => location.Number == Data.BossProgressLocationNumber.Value) : __BossProgressLocation.Value;
			}
		}

		// Native ELF 14599a0 0x14599a0; predicate reads the original saved location number.
		public LocationConfig CurrentLocation
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__CurrentLocation == null)
					__CurrentLocation = CodeGenAtom.CreatePooled(this, null, () => CurrentLocation, false);
				return __CurrentLocation.DirectEvaluate() ? locationsConfig.Locations.First(location => location.Number == Data.CurrentLocationNumber.Value) : __CurrentLocation.Value;
			}
		}

		public double Gems
		{
			// Native generated getter/setter ELF VA 0x1459b38/0x1459c8c.
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Gems == null)
					__Gems = CodeGenAtom.CreatePooled(this, null, () => Gems, false);
				return __Gems.DirectEvaluate() ? Data.Gems.Value : __Gems.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Gems != null && !__Gems.CompareAndInvalidate(value))
					return;
				Data.Gems.Value = value;
			}
		}

		public int PortalGemsCollected
		{
			// Native generated getter/setter ELF VA 0x1459d4c/0x1459ea4.
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__PortalGemsCollected == null)
					__PortalGemsCollected = CodeGenAtom.CreatePooled(this, null, () => PortalGemsCollected, false);
				return __PortalGemsCollected.DirectEvaluate() ? Data.PortalGemsCollected.Value : __PortalGemsCollected.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__PortalGemsCollected != null && !__PortalGemsCollected.CompareAndInvalidate(value))
					return;
				Data.PortalGemsCollected.Value = value;
			}
		}

		public bool VisitedLastUnlockedLocation
		{
			// Native generated getter/setter ELF VA 0x1454dbc/0x1459f5c.
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__VisitedLastUnlockedLocation == null)
					__VisitedLastUnlockedLocation = CodeGenAtom.CreatePooled(this, null, () => VisitedLastUnlockedLocation, false);
				return __VisitedLastUnlockedLocation.DirectEvaluate() ? Data.VisitedLastUnlockedLocation.Value : __VisitedLastUnlockedLocation.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__VisitedLastUnlockedLocation != null && !__VisitedLastUnlockedLocation.CompareAndInvalidate(value))
					return;
				Data.VisitedLastUnlockedLocation.Value = value;
			}
		}

		public bool InBossTowerArena
		{
			// Native generated getter/setter ELF VA 0x1455f14/0x145a01c.
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__InBossTowerArena == null)
					__InBossTowerArena = CodeGenAtom.CreatePooled(this, null, () => InBossTowerArena, false);
				return __InBossTowerArena.DirectEvaluate() ? Data.PlayerInBossArena.Value : __InBossTowerArena.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__InBossTowerArena != null && !__InBossTowerArena.CompareAndInvalidate(value))
					return;
				Data.PlayerInBossArena.Value = value;
			}
		}

		public bool DisablePermanentStatsGain
		{
			get
			{
				// Native ELF VA 0x145a0dc, direct-evaluate tail 0x1455f14 = InBossTowerArena.
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__DisablePermanentStatsGain == null)
					__DisablePermanentStatsGain = CodeGenAtom.CreatePooled(this, null, () => DisablePermanentStatsGain, false);
				return __DisablePermanentStatsGain.DirectEvaluate() ? InBossTowerArena : __DisablePermanentStatsGain.Value;
			}
		}

		public bool IsUnlockAnimationShown
		{
			// Native generated getter/setter ELF VA 0x145a200/0x145a360.
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUnlockAnimationShown == null)
					__IsUnlockAnimationShown = CodeGenAtom.CreatePooled(this, null, () => IsUnlockAnimationShown, false);
				return __IsUnlockAnimationShown.DirectEvaluate() ? Data.IsPortalUnlockAnimationShown.Value : __IsUnlockAnimationShown.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUnlockAnimationShown != null && !__IsUnlockAnimationShown.CompareAndInvalidate(value))
					return;
				Data.IsPortalUnlockAnimationShown.Value = value;
			}
		}

		public bool IsPortalUnlocked
		{
			// Native ELF VA 0x145a420; the original threshold is seven collected portal gems.
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsPortalUnlocked == null)
					__IsPortalUnlocked = CodeGenAtom.CreatePooled(this, null, () => IsPortalUnlocked, false);
				return __IsPortalUnlocked.DirectEvaluate() ? PortalGemsCollected > 6 : __IsPortalUnlocked.Value;
			}
		}

		public long UnstackCooldownTimestamp
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__UnstackCooldownTimestamp == null)
					__UnstackCooldownTimestamp = CodeGenAtom.CreatePooled(this, null, () => UnstackCooldownTimestamp, false);
				return __UnstackCooldownTimestamp.DirectEvaluate() ? Data.UnstackCooldownTimestamp.Value : __UnstackCooldownTimestamp.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__UnstackCooldownTimestamp != null && !__UnstackCooldownTimestamp.CompareAndInvalidate(value))
					return;
				Data.UnstackCooldownTimestamp.Value = value;
			}
		}

		public bool IsPortalUnlockRunning { get; set; }

		public float AbilityDamageMultiplier
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__AbilityDamageMultiplier == null)
					__AbilityDamageMultiplier = CodeGenAtom.CreatePooled(this, null, () => AbilityDamageMultiplier, false);
				return __AbilityDamageMultiplier.DirectEvaluate() ? __AbilityDamageMultiplier__BackingField : __AbilityDamageMultiplier.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__AbilityDamageMultiplier != null && !__AbilityDamageMultiplier.CompareAndInvalidate(value))
					return;
				__AbilityDamageMultiplier__BackingField = value;
			}
		}

		public float AbilityDamageFlat
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__AbilityDamageFlat == null)
					__AbilityDamageFlat = CodeGenAtom.CreatePooled(this, null, () => AbilityDamageFlat, false);
				return __AbilityDamageFlat.DirectEvaluate() ? __AbilityDamageFlat__BackingField : __AbilityDamageFlat.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__AbilityDamageFlat != null && !__AbilityDamageFlat.CompareAndInvalidate(value))
					return;
				__AbilityDamageFlat__BackingField = value;
			}
		}

		public float AbilityHealthRegenMultiplier
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__AbilityHealthRegenMultiplier == null)
					__AbilityHealthRegenMultiplier = CodeGenAtom.CreatePooled(this, null, () => AbilityHealthRegenMultiplier, false);
				return __AbilityHealthRegenMultiplier.DirectEvaluate() ? __AbilityHealthRegenMultiplier__BackingField : __AbilityHealthRegenMultiplier.Value;
			}

			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__AbilityHealthRegenMultiplier != null && !__AbilityHealthRegenMultiplier.CompareAndInvalidate(value))
					return;
				__AbilityHealthRegenMultiplier__BackingField = value;
			}
		}

		public float LethalIgnoreChance { get; set; }
		public bool HasPerTargetLethalIgnore { get; set; }
		public HashSet<int> LethalIgnoreTargetIds { get; }
		public float LethalIgnoreCooldown { get; set; }
		public float ThornsDamage { get; set; }
		public float BonusDamageToStunned { get; set; }
		public bool TreatAsAllSlotsEmpty { get; set; }
		public float LightningDamageBonus { get; set; }
		public float BonusLightningChance { get; set; }
		public float BonusMarkChance { get; set; }
		public int BonusMaxMarks { get; set; }
		public float DamagePerConsumedShockOnTarget { get; set; }
		public float NextHitSuperCritChanceBonus { get; set; }
		public float BlockSuperCritChanceBonus { get; set; }
		public float NextHitSuperCritDamageBonus { get; set; }
		public float NextHitLifestealFlatBonus { get; set; }
		public float NextHitDamageBonus { get; set; }
		public float NextThornsDamageBonus { get; set; }
		public int HitsTakenCount { get; set; }
		public float HitsTakenLastUpdate { get; set; }
		public bool HasLoadout => false;

		public PlayerModel(Lifetime lifetime, PlayerConfig config) : base(lifetime)
		{
			// Native ELF VA 0x145ac5c and lambdas 0x1460fbc/0x1461088.
			__ItemSlots__BackingField = new List<ItemSlotModel>();
			__Items__BackingField = new List<ItemModel>();
			CanMove = true;
			LethalIgnoreTargetIds = new HashSet<int>();
			this.config = config;
			ItemSlots = config.Slots.Select(slot => new ItemSlotModel(Lifetime, slot.SlotType, slot.UnlockedByDefault) { Key = slot.Key, IsVip = slot.IsVip }).ToList();
			foreach (Stat stat in config.Stats)
				stat.Modifiers?.Clear();
			LastStats = config.Stats.Select(stat => new Stat(Lifetime, GetInitialBaseValue(stat), stat.Type, stat.Modifiers)).ToList();
			version = Atom.Value(Lifetime, 0);
		}

		// Pet mode: the Damage stat starts at PetConfig.PlayerBaseAttack. Base values are not loaded from the save
		// (LoadStats keeps PlayerConfig's), so this never persists or stacks.
		private static double GetInitialBaseValue(Stat stat)
		{
			if (stat.Type == StatType.Damage && global::Codebase.Pet.PetConfig.Enabled)
				return stat.BaseValue + global::Codebase.Pet.PetConfig.Instance.PlayerBaseAttack;
			return stat.BaseValue;
		}

		public override void Initialize()
		{
			// Native ELF VA 0x145b128: call base, then the original load order.
			base.Initialize();
			LoadFromUserData();
		}

		private void LoadFromUserData()
		{
			// Native ELF VA 0x145b178.
			LoadItems();
			LoadItemSlots();
			LoadStats();
			LoadPosition();
		}

		private void LoadItems()
		{
			// Native ELF VA 0x145b1a0.
			Items.Clear();
			foreach (ItemDataUd saved in Data.Items)
			{
				ItemConfig itemConfig = itemsProvider.Get(saved.Key.Value);
				if (itemConfig == null)
					continue;
				ItemModel item = new ItemModel(Lifetime, itemConfig);
				item.BindToUserData(saved);
				Items.Add(item);
			}
		}

		private void LoadItemSlots()
		{
			// Native ELF VA 0x145b5e4 and nested lambdas 0x14616b8..0x1461868.
			if (Data.ItemSlots.Count == 0)
			{
				InitializeItemSlotsUserData();
				return;
			}

			foreach (ItemSlotModel slot in ItemSlots)
				slot.Unequip();
			Dictionary<string, ItemModel> itemsByKey = Items.Distinct().ToDictionary(item => item.Config.Key, item => item);
			Dictionary<string, ItemSlotModel> slotsByKey = ItemSlots.Where(slot => !string.IsNullOrWhiteSpace(slot.Key)).ToDictionary(slot => slot.Key, slot => slot);
			Dictionary<ItemSlotType, List<ItemSlotModel>> slotsByType = ItemSlots.GroupBy(slot => slot.SlotType).ToDictionary(group => group.Key, group => group.ToList());
			Dictionary<ItemSlotType, int> nextIndex = new Dictionary<ItemSlotType, int>();
			HashSet<ItemSlotModel> bound = new HashSet<ItemSlotModel>();
			for (int i = 0; i < Data.ItemSlots.Count; i++)
			{
				ItemSlotDataUd saved = Data.ItemSlots[i];
				ItemSlotType type = saved.SlotType.Value;
				string slotKey = saved.SlotKey.Value;
				string itemKey = saved.ItemKey.Value;
				if (!nextIndex.ContainsKey(type))
					nextIndex[type] = 0;
				ItemSlotModel slot;
				if (string.IsNullOrWhiteSpace(slotKey) || !slotsByKey.TryGetValue(slotKey, out slot))
				{
					if (!slotsByType.TryGetValue(type, out List<ItemSlotModel> candidates))
						continue;
					int index = nextIndex[type]++;
					if (index < 0 || index >= candidates.Count)
						continue;
					slot = candidates[index];
				}

				if (slot == null || !bound.Add(slot))
					continue;
				slot.BindToUserData(saved);
				if (string.IsNullOrWhiteSpace(itemKey) || !itemsByKey.TryGetValue(itemKey, out ItemModel item))
					continue;
				ItemSlotModel previous = ItemSlots.FirstOrDefault(candidate => candidate.ItemModel == item);
				if (previous != null)
					previous.Unequip();
				slot.Equip(item);
				item.SlotIndex = ItemSlots.IndexOf(slot);
			}
		}

		private void InitializeItemSlotsUserData()
		{
			// Native ELF VA 0x145cd6c.
			foreach (ItemSlotModel slot in ItemSlots)
			{
				ItemSlotDataUd saved = Data.ItemSlots.Create();
				saved.SlotKey.Value = slot.Key ?? string.Empty;
				saved.SlotType.Value = slot.SlotType;
				saved.IsUnlocked.Value = slot.IsUnlocked;
				saved.IsPinned.Value = slot.IsPinned;
				saved.ItemKey.Value = string.Empty;
				slot.BindToUserData(saved);
			}
		}

		private void LoadStats()
		{
			// Native ELF VA 0x145c268. BaseValue is retained from PlayerConfig.
			if (Data.Stats.Count == 0)
				return;
			foreach (StatDataUd saved in Data.Stats)
			{
				Stat stat = LastStats.FirstOrDefault(candidate => candidate.Type == saved.Stat.Value);
				if (stat == null)
					continue;
				foreach (StatModifierUd modifier in saved.Modifiers)
				{
					if (IsRuntimeOnlySource(modifier.Source.Value))
						continue;
					stat.Modifiers.Add(new StatModifier { Name = modifier.Name.Value, Value = modifier.Value.Value, ModifierType = modifier.ModifierType.Value, Source = modifier.Source.Value });
				}
			}
		}

		private void LoadPosition()
		{
			// Native ELF VA 0x145c9d0.
			Vector3Ud position = Data.LastPosition.Value;
			if (position.X != 0f || position.Y != 0f || position.Z != 0f)
				LastPosition = position;
		}

		public void SaveToUserData()
		{
			// Native ELF VA 0x145d444: SaveStats first, then tail call SavePosition.
			SaveStats();
			SavePosition();
		}

		private void SaveStats()
		{
			// Native ELF VA 0x145d45c; original native source-selection and unsigned exclusion mask 0x50a.
			Data.Stats.Clear();
			foreach (Stat stat in Unit?.Stats ?? LastStats)
			{
				StatDataUd saved = Data.Stats.Create();
				saved.Stat.Value = stat.Type;
				saved.BaseValue.Value = stat.BaseValue;
				foreach (StatModifier modifier in stat.Modifiers)
				{
					uint source = (uint)modifier.Source;
					if (source <= 10u && ((1u << (int)source) & 0x50au) != 0u)
						continue;
					if (IsRuntimeOnlySource(modifier.Source))
						continue;
					StatModifierUd target = saved.Modifiers.Create();
					target.Name.Value = modifier.Name;
					target.Value.Value = modifier.Value;
					target.ModifierType.Value = modifier.ModifierType;
					target.Source.Value = modifier.Source;
				}
			}
		}

		// Pet collection/synergy bonuses are re-applied by their systems every session; skill buffs are timed.
		private static bool IsRuntimeOnlySource(ModifierSource source)
		{
			if (source == ModifierSource.RpgHero || source == ModifierSource.RpgItem || source == ModifierSource.RpgFirstKill) return true;
			return source == ModifierSource.PetCollection || source == ModifierSource.PetSynergy || source == ModifierSource.Skill;
		}

		private void SavePosition()
		{
			// Native ELF VA 0x145da48; original override takes precedence even without a live Unit.
			if (SavePositionOverride.HasValue)
				Data.LastPosition.Value = SavePositionOverride.Value;
			else if (Unit != null)
				Data.LastPosition.Value = Unit.transform.position;
		}

		public void InitializeUnit(PlayerUnit unit)
		{
			// Native ELF VA 0x145dbbc. Injection occurs in the original scene bootstrap.
			Unit = unit;
			Unit.Stats = LastStats.Select(stat => new Stat(Lifetime, stat.BaseValue, stat.Type, stat.Modifiers)).ToList();
			Unit.BuildStatLookup();
			ItemModel startingWeapon = new ItemModel(Lifetime, config.StartingWeapon);
			// Pet mode: build the player's own attack item; it is not added to Items.
			if (config.PlayerAttackItem != null)
			{
				PlayerAttackItem = new ItemModel(Lifetime, config.PlayerAttackItem);
				PlayerAttackItem.AbilityDamageMultiplier = 1f;
				PlayerAttackItem.AbilityAttackSpeedMultiplier = 1f;
				PlayerAttackItem.AbilityCritMultiplier = 1f;
			}
			Unit.CurrentHealth = Unit.Stats.First(stat => stat.Type == StatType.Health).Value;
			if (Items.Count == 0)
			{
				AddItem(startingWeapon);
				EquipItem(startingWeapon);
			}
			else
				ApplyEquippedItemsToUnit();
			crystalModel.ApplyAllEquippedToUnit();
			Unit.SetupHealthBar(config.HealthBarPrefab);
		}

		public void AddItem(ItemModel item, bool fromSave = false, bool applySilently = false)
		{
			// Native ELF VA 0x145de6c; duplicate identity is the original config key.
			ItemModel existing = Items.FirstOrDefault(candidate => candidate.Config.Key == item.Config.Key);
			if (existing != null)
			{
				int previousCopies = existing.Copies;
				existing.Copies += cheats.DropMultiplier * item.Copies;
				if (!applySilently)
					GameContext.Get<WeaponRewardView>().Setup(existing, previousCopies, false, true);
				return;
			}

			if (!fromSave && Items.Count > 0)
			{
				if (!applySilently)
					GameContext.Get<WeaponRewardView>().Setup(item, 0, false, false);
				item.ShowNotification = true;
			}

			ItemDataUd saved = Data.Items.Create();
			saved.Key.Value = item.Config.Key;
			saved.Level.Value = item.Level;
			saved.Copies.Value = item.Copies;
			saved.Evolved.Value = item.Evolved;
			item.BindToUserData(saved);
			Items = new List<ItemModel>(Items)
			{
				item
			};
			EquipItem(item);
		}

		public ItemModel CreateItem(ItemConfig config, int copies)
		{
			// Native ELF VA 0x145f44c.
			ItemModel item = new ItemModel(Lifetime, config)
			{
				Copies = copies
			};
			AddItem(item);
			return item;
		}

		public void RemoveItem(ItemModel item)
		{
		}

		public bool EquipItem(ItemModel item)
		{
			// Native ELF VA 0x145e26c, predicate 0x146194c.
			ItemSlotModel slot = ItemSlots.FirstOrDefault(candidate => !candidate.IsEquipped && candidate.IsUnlocked && candidate.SlotType == item.Config.SlotType);
			return slot != null && TryEquipItemToSlot(item, slot);
		}

		public void SaveLoadout()
		{
		}

		public void EquipLoadout()
		{
		}

		public void UnequipItem(ItemModel item)
		{
			// Native ELF VA 0x145f590.
			ItemSlotModel slot = ItemSlots.FirstOrDefault(candidate => candidate.ItemModel == item);
			if (slot == null)
				return;
			slot.Unequip();
			foreach (StatModifier modifier in item.Config.StatModifiers)
				Unit.Stats.First(stat => stat.Type == modifier.StatType).RemoveModifier(modifier);
			abilityExecuteEventSystem.FireEvent(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnEquip, Item = item, ItemUnequipped = true });
			SyncRageAccessFromEquipment();
			version.Invalidate();
		}

		public bool TryEquipItemToSlot(ItemModel item, ItemSlotModel slot)
		{
			// Native ELF VA 0x145fa20.
			if (slot == null || item == null || !slot.IsUnlocked || slot.IsEquipped || slot.SlotType != item.Config.SlotType)
				return false;
			if (ItemSlots.FirstOrDefault(candidate => candidate.ItemModel == item) != null)
				UnequipItem(item);
			slot.Equip(item);
			item.SlotIndex = ItemSlots.IndexOf(slot);
			item.WearingTime = 0f;
			item.ShowNotification = false;
			if (Unit != null)
				foreach (StatModifier modifier in item.Config.StatModifiers)
					Unit.Stats.First(stat => stat.Type == modifier.StatType).AddModifier(modifier);
			abilityExecuteEventSystem.FireEvent(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnEquip, Item = item, ItemUnequipped = false });
			SyncRageAccessFromEquipment();
			version.Invalidate();
			return true;
		}

		private void SyncRageAccessFromEquipment()
		{
			// Native ELF VA 0x1460790, original tail call.
			GrantRageAccessEffect.RecalculateMaxLevel(EquippedItems, rageModel);
		}

		private void ApplyEquippedItemsToUnit()
		{
			// Native ELF VA 0x145e394. Remove previous item modifiers, then reapply equipped slots.
			if (Unit == null)
				return;
			foreach (Stat stat in Unit.Stats)
				foreach (StatModifier modifier in stat.Modifiers.Where(modifier => modifier.Source == ModifierSource.Item).ToList())
					stat.RemoveModifier(modifier);
			foreach (ItemSlotModel slot in ItemSlots.Where(candidate => candidate.IsEquipped))
			{
				ItemModel item = slot.ItemModel;
				item.SlotIndex = ItemSlots.IndexOf(slot);
				item.WearingTime = 0f;
				foreach (StatModifier modifier in item.Config.StatModifiers)
					Unit.Stats.First(stat => stat.Type == modifier.StatType).AddModifier(modifier);
				abilityExecuteEventSystem.FireEvent(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnEquip, Item = item, ItemUnequipped = false });
			}

			SyncRageAccessFromEquipment();
			version.Invalidate();
		}

		public void SetCurrentLocation(int locationNumber)
		{
			// Native ELF VA 0x1460a24.
			Data.CurrentLocationNumber.Value = locationNumber;
			if (Data.UnlockedLocationNumber.Value <= locationNumber)
				VisitedLastUnlockedLocation = true;
			if (Data.UnlockedLocationNumber.Value < locationNumber)
				UnlockLocation(locationNumber);
		}

		public void UnlockLocation(int locationNumber)
		{
			// Native ELF VA 0x1460b1c.
			Data.UnlockedLocationNumber.Value = System.Math.Max(Data.UnlockedLocationNumber.Value, locationNumber);
			if (CurrentLocation.Number < locationNumber)
				VisitedLastUnlockedLocation = false;
		}

		public void AdvanceBossProgressLocation(int locationNumber)
		{
			// Native ELF VA 0x1460c24.
			Data.BossProgressLocationNumber.Value = System.Math.Max(Data.BossProgressLocationNumber.Value, locationNumber);
		}

		public double GetStatValue(StatType type)
		{
			// Native ELF VA 0x1460d00.
			return Unit.GetStatValue(type);
		}
	}
}
