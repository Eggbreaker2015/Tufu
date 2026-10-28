"""Enum values used by the numeric model (copied from the C# enums).

Sources: ``CodeBase/Unit/StatType.cs``, ``CodeBase/Enemy/WeaponType.cs``,
``CodeBase/Player/Inventory/Rarity.cs``, ``CodeBase/Balance/BalanceInterpolationType.cs``,
``CodeBase/Player/Inventory/ItemSlotType.cs``, ``CodeBase/Unit/ModifierType.cs`` and
``CodeBase/Unit/ModifierSource.cs`` (all unchanged between commit 6af011f and the extracted
``enums`` table; ``tests/test_enums.py`` checks every member against that table).

Owned by stage-5 unit P1 and read-only for the other units: ask P1 to add members.
"""

from __future__ import annotations

from enum import IntEnum


class StatType(IntEnum):
    MovementSpeed = 0
    Health = 1
    HealthRegen = 2
    Damage = 3
    AttackRange = 4
    AttackCooldown = 5
    StabDamageFlat = 6
    StabDamagePercent = 7
    CutDamageFlat = 8
    CutDamagePercent = 9
    MagicDamageFlat = 10
    MagicDamagePercent = 11
    BuckshotDamageFlat = 12
    BuckshotDamagePercent = 13
    StabDefenceFlat = 14
    StabDefencePercent = 15
    CutDefenceFlat = 16
    CutDefencePercent = 17
    MagicDefenceFlat = 18
    MagicDefencePercent = 19
    BuckshotDefenceFlat = 20
    BuckshotDefencePercent = 21
    LifestealFlat = 22
    Evasion = 23
    CritChance = 24
    CritDamagePercent = 25
    UniversalDefenceFlat = 26
    UniversalDefencePercent = 27
    SuperCritChance = 28
    SuperCritDamagePercent = 29
    GlobalAttack = 30
    GlobalDefence = 31
    LifestealPercent = 32
    BlockChance = 33
    BlockAmount = 34
    VisualScaleMultiplier = 35


class WeaponType(IntEnum):
    Stabbing = 0
    Cutting = 1
    Magic = 2
    Buckshot = 3
    Universal = 4


class Rarity(IntEnum):
    Common = 0
    Uncommon = 1
    Rare = 2
    Epic = 3
    Legendary = 4
    Divine = 5
    Mythical = 6


class BalanceInterpolationType(IntEnum):
    ItemEmission = 0
    ItemChainProbability = 1
    StatsFromFeaturesFlat = 2
    StatModifier = 3
    PlayerAbilityDPSMult = 4
    PlayerAblityEHPMult = 5
    ItemLegendaryEmission = 6
    ItemEmissionByType = 7


class ItemSlotType(IntEnum):
    """``ItemConfig.SlotType`` and ``PlayerConfig.Slots[].SlotType``."""
    Weapon = 0
    Helmet = 1
    Shield = 2


SlotType = ItemSlotType  # the field name used in configs and in the stage-5 design


class ModifierType(IntEnum):
    Flat = 0
    PercentAdditive = 1
    PercentMultiplicative = 2
    PostFormulaAdditive = 3
    PreAdditivePostMultiplicative = 4
    PostFormulaMultiplicative = 5
    PostFormulaFlat = 6
    EventPercentModifier = 7


class ModifierSource(IntEnum):
    Enemy = 0
    Item = 1
    AbilityPermanent = 2
    AbilityTemporary = 3
    Shop = 4
    Ads = 5
    Reward = 6
    LevelPass = 7
    Crystal = 8
    CrystalUpgrade = 9
    GraphUpgrade = 10
    MineUpgrade = 11
    CrystalEvent = 12
    CrystalPass = 13
    SoulPass = 14
    Vip = 15
    SecretChest = 16
    CrystalGuardian = 17
    Quest = 18
    Cashback = 19


# C# enum name -> Python class, for checks against the extracted ``enums`` table.
CSHARP_ENUMS = {
    "StatType": StatType,
    "WeaponType": WeaponType,
    "Rarity": Rarity,
    "BalanceInterpolationType": BalanceInterpolationType,
    "ItemSlotType": ItemSlotType,
    "ModifierType": ModifierType,
    "ModifierSource": ModifierSource,
}
