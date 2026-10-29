"""Enum values used by the numeric model (copied from the C# enums).

Sources: ``CodeBase/Unit/StatType.cs``, ``CodeBase/Enemy/WeaponType.cs``,
``CodeBase/Player/Inventory/Rarity.cs`` and ``CodeBase/Balance/BalanceInterpolationType.cs``
(all unchanged between commit 6af011f and the extracted ``enums`` table).
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
