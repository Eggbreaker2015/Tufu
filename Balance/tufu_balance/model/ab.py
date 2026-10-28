"""AB test data (``ABTest Data Config.asset``) and the lookups the spawners and the simulation use.

Ports ``CodeBase/ABTests/ABTestService.cs`` (``GetSpawnerCooldownSeconds``, ``GetSpawnerCharges``,
``GetEnemyHpDamageMultiplier``, ``GetFastRPMParams``) and ``ABTestDataGroup.ResolveCurrentValue``,
reading the extracted ``c_ABTestDataConfig*`` tables.

Variant assignment follows the stage-5 sandbox (design 2.3): the hook in ``ABTestModel.SelectVariant``
returns the requested variant for every test that has a non-empty ``rules`` string; tests without
rules (``rewarded_delay_from_start``, ``level_pass``) are never applied and stay unassigned. A test
whose assigned variant has no data uses the C# class defaults (``GetOrCreateDefaultValue``).

Variant data reaches the game as JSON: ``ABTestInitializer.ApplyDevelopTests`` serializes the asset with
``ABTestDataConfig.BuildDataJson`` and ``ABTestDataGroup.ApplyRemote`` reads it back with
``JsonConvert.DeserializeObject<Dictionary<string, T>>`` (default ``ObjectCreationHandling.Auto``, no
global settings in the code). Newtonsoft populates the object a ``new T()`` already holds, so every
``overridesByLocation`` (a ``Dictionary`` subclass) is the field-initialized class-default dictionary with
the asset entries added on top (asset wins per key); scalars are replaced. :func:`_value_by_location`
builds the same merge.

``overridesByLocation`` is a ``UnitySerializedDictionary``: ``keyData`` is a ``List<int>`` that Unity
writes as one hex blob of little-endian int32s. Databases extracted before the extractor fix
(design Q27) hold an all-digit blob such as ``01000000020000000300000004000000`` as the float
``1.0000000200000003e+30``; :func:`decode_key_data` then falls back to keys ``1..N`` after checking that
the float is exactly what that blob would have parsed to, and warns once per process.
"""

from __future__ import annotations

import json
import sqlite3
import struct
import warnings
from dataclasses import dataclass, field
from typing import Any, Mapping

from .f32 import f32, fmul
from .registry import Provenance, implements

# Config field -> ABKeys constant (CodeBase/ABTests/ABKeys.cs).
AB_KEYS = {
    "rewardedDelayFromStart": "rewarded_delay_from_start",
    "enemyHpDamageMult": "enemy_hp_damage_mult_v3",
    "noAds": "no_ads",
    "levelPass": "level_pass",
    "rpmOldAlgorithm": "rpm_old_algorithm",
    "fastRPMParams": "fastRPMParams",
    "soulPass": "soul_pass",
    "crystalPass": "crystal_pass",
    "offerwall": "offerwall",
    "patchCompensationGems": "patch_compensation_gems",
    "commonRewardMultipliers": "common_reward_multipliers",
    "spawnerCooldowns": "spawner_cooldowns_v3",
    "enchantFailureDustCashback": "enchant_failure_dust_cashback",
}
SPAWNER_COOLDOWNS = AB_KEYS["spawnerCooldowns"]
ENEMY_HP_DAMAGE_MULT = AB_KEYS["enemyHpDamageMult"]
FAST_RPM_PARAMS = AB_KEYS["fastRPMParams"]
COMMON_REWARD_MULTIPLIERS = AB_KEYS["commonRewardMultipliers"]

DEFAULT_VARIANT = "A"


class KeyDataFallbackWarning(UserWarning):
    """keyData was extracted as a number; keys 1..N were assumed."""


_warned_fallback = False


def reset_key_data_warning() -> None:
    """Re-arm the once-per-process fallback warning (for tests)."""
    global _warned_fallback
    _warned_fallback = False


def _hex_int32s(blob: str) -> list[int]:
    raw = bytes.fromhex(blob)
    return list(struct.unpack(f"<{len(raw) // 4}i", raw))


def _is_hex_blob(text: str) -> bool:
    return len(text) % 8 == 0 and all(c in "0123456789abcdefABCDEF" for c in text)


def decode_key_data(raw: Any, n_values: int, column: str = "keyData") -> list[int]:
    """Keys of a ``UnitySerializedDictionary<int, T>`` from its extracted ``keyData`` cell."""
    global _warned_fallback
    if raw is None or raw == "":
        return []
    if isinstance(raw, str) and _is_hex_blob(raw.strip()):
        return _hex_int32s(raw.strip())
    if isinstance(raw, (int, float)) and not isinstance(raw, bool):
        keys = list(range(1, n_values + 1))
        blob = "".join(struct.pack("<i", k).hex() for k in keys)
        if not blob or float(raw) != float(blob):
            raise ValueError(f"{column}: extracted keyData {raw!r} is not the YAML float of keys 1..{n_values}; "
                             "re-extract with the keyData fix (tufu_balance.extract.load_document)")
        if not _warned_fallback:
            _warned_fallback = True
            warnings.warn(KeyDataFallbackWarning(
                f"{column}: keyData was extracted as the number {raw!r}; assuming keys 1..{n_values} "
                "(re-extract with the fixed extractor to read the real keys)"), stacklevel=2)
        return keys
    raise ValueError(f"{column}: cannot decode keyData {raw!r}")


def decode_value_data(raw: Any, column: str = "valueData") -> list:
    """Values of a ``UnitySerializedDictionary``: a JSON list (float) or a hex blob (int)."""
    if raw is None or raw == "":
        return []
    if isinstance(raw, str):
        text = raw.strip()
        if text.startswith("["):
            return json.loads(text)
        if _is_hex_blob(text):
            return _hex_int32s(text)
    raise ValueError(f"{column}: cannot decode valueData {raw!r}")


@dataclass(frozen=True)
class ValueByLocation:
    """``ValueByLocation`` (float) or ``IntValueByLocation`` (int)."""

    default: float | int
    overrides: Mapping[int, float | int] = field(default_factory=dict)
    is_int: bool = False

    def get(self, location: int):
        """``ABTestService.GetValue``: the override for the location, else the default."""
        value = self.overrides.get(int(location), self.default)
        return int(value) if self.is_int else f32(value)


def _value_by_location(row: Mapping[str, Any], prefix: str, base: ValueByLocation) -> ValueByLocation:
    """The asset's ``ValueByLocation`` as the game holds it after the JSON round trip: ``defaultValue``
    replaced, ``overridesByLocation`` = ``base`` (the C# field initializer) updated with the asset entries."""
    is_int = base.is_int
    if f"{prefix}.defaultValue" not in row:  # field absent from the data: Newtonsoft keeps the initializer
        return base
    default = row[f"{prefix}.defaultValue"]
    values = decode_value_data(row.get(f"{prefix}.overridesByLocation.valueData"), f"{prefix}.valueData")
    keys = decode_key_data(row.get(f"{prefix}.overridesByLocation.keyData"), len(values), f"{prefix}.keyData")
    asset: dict[int, float | int] = {}
    for k, v in zip(keys, values):  # OnAfterDeserialize pairs up to the shorter list; later keys win
        asset[int(k)] = int(v) if is_int else f32(v)
    overrides = dict(base.overrides)
    overrides.update(asset)  # JSON populate into the existing dictionary: asset wins, defaults stay
    return ValueByLocation(int(default) if is_int else f32(default), overrides, is_int)


@dataclass(frozen=True)
class SpawnerCooldownsGroup:
    cooldown_minutes: ValueByLocation
    charges: ValueByLocation


@dataclass(frozen=True)
class SpawnerCooldownsData:
    non_elite: SpawnerCooldownsGroup
    elite: SpawnerCooldownsGroup

    def group(self, is_elite: bool) -> SpawnerCooldownsGroup:
        return self.elite if is_elite else self.non_elite


@dataclass(frozen=True)
class EnemyHpDamageMultData:
    damage_multiplier: ValueByLocation
    rewarded_value_multiplier: float


@dataclass(frozen=True)
class FastRPMParamsData:
    fight_time_seconds: Any  # float32
    max_health_loss_multiplier: Any  # float32


# C# field initializers (CodeBase/ABTests/*Data.cs), used when the assigned variant has no data.
DEFAULT_SPAWNER_COOLDOWNS = SpawnerCooldownsData(
    non_elite=SpawnerCooldownsGroup(
        ValueByLocation(f32(30), {loc: f32(2 * (loc + 1)) for loc in range(1, 14)}),
        ValueByLocation(3, {}, is_int=True)),
    elite=SpawnerCooldownsGroup(
        ValueByLocation(f32(180), {1: f32(60), 2: f32(90), 3: f32(120), 4: f32(150)}),
        ValueByLocation(2, {}, is_int=True)),
)
DEFAULT_ENEMY_HP_DAMAGE_MULT = EnemyHpDamageMultData(ValueByLocation(f32(1), {}), 0.5)
DEFAULT_FAST_RPM_PARAMS = FastRPMParamsData(f32(10.0), f32(1.0))


@dataclass(frozen=True)
class CommonRewardMultipliersData:
    """``CommonRewardMultipliersData``: only the three stat-class RPM multipliers are used here."""

    attack_stat_value_multiplier: Any = f32(1.0)  # float32
    armor_stat_value_multiplier: Any = f32(1.0)
    sustain_stat_value_multiplier: Any = f32(1.0)


DEFAULT_COMMON_REWARD_MULTIPLIERS = CommonRewardMultipliersData()


def _scalar(row: Mapping[str, Any], column: str, default, convert):
    return convert(row[column]) if column in row and row[column] is not None else default


def _spawner_cooldowns(row: Mapping[str, Any], base: SpawnerCooldownsData) -> SpawnerCooldownsData:
    def group(name: str, g: SpawnerCooldownsGroup) -> SpawnerCooldownsGroup:
        return SpawnerCooldownsGroup(_value_by_location(row, f"data.{name}.cooldownMinutes", g.cooldown_minutes),
                                     _value_by_location(row, f"data.{name}.charges", g.charges))
    return SpawnerCooldownsData(group("nonElite", base.non_elite), group("elite", base.elite))


def _enemy_hp_damage_mult(row: Mapping[str, Any], base: EnemyHpDamageMultData) -> EnemyHpDamageMultData:
    return EnemyHpDamageMultData(_value_by_location(row, "data.damageMultiplier", base.damage_multiplier),
                                 _scalar(row, "data.rewardedValueMultiplier", base.rewarded_value_multiplier, float))


def _fast_rpm_params(row: Mapping[str, Any], base: FastRPMParamsData) -> FastRPMParamsData:
    return FastRPMParamsData(_scalar(row, "data.fightTimeSeconds", base.fight_time_seconds, f32),
                             _scalar(row, "data.maxHealthLossMultiplier", base.max_health_loss_multiplier, f32))


def _common_reward_multipliers(row: Mapping[str, Any], base: CommonRewardMultipliersData) -> CommonRewardMultipliersData:
    return CommonRewardMultipliersData(
        _scalar(row, "data.attackStatValueMultiplier", base.attack_stat_value_multiplier, f32),
        _scalar(row, "data.armorStatValueMultiplier", base.armor_stat_value_multiplier, f32),
        _scalar(row, "data.sustainStatValueMultiplier", base.sustain_stat_value_multiplier, f32))


_PARSERS = {
    SPAWNER_COOLDOWNS: (_spawner_cooldowns, DEFAULT_SPAWNER_COOLDOWNS),
    ENEMY_HP_DAMAGE_MULT: (_enemy_hp_damage_mult, DEFAULT_ENEMY_HP_DAMAGE_MULT),
    FAST_RPM_PARAMS: (_fast_rpm_params, DEFAULT_FAST_RPM_PARAMS),
    COMMON_REWARD_MULTIPLIERS: (_common_reward_multipliers, DEFAULT_COMMON_REWARD_MULTIPLIERS),
}

Variant = str | Mapping[str, str | None] | None


@dataclass
class ABTestConfig:
    """``ABTest Data Config``: per test (AB key) its rules string and raw variant rows."""

    rules: dict[str, str | None]
    variants: dict[str, dict[str, dict[str, Any]]]  # ab key -> variant -> dotted columns of the row
    _cache: dict = field(default_factory=dict, repr=False)

    def assigned_variant(self, ab_key: str, variant: Variant = DEFAULT_VARIANT) -> str | None:
        """What ``Data.Tests[ab_key]`` holds in the sandbox: the requested variant if the test has rules."""
        if isinstance(variant, Mapping):
            return variant.get(ab_key)
        return variant if self.rules.get(ab_key) else None

    def data(self, ab_key: str, variant: Variant = DEFAULT_VARIANT):
        """``ABTestDataGroup<T>.GetCurrentValue`` for the parsed tests (spawner cooldowns, enemy
        HP/damage multiplier, fast RPM parameters, common reward multipliers)."""
        assigned = self.assigned_variant(ab_key, variant)
        key = (ab_key, assigned)
        if key not in self._cache:
            parse, default = _PARSERS[ab_key]
            row = self.variants.get(ab_key, {}).get(assigned) if assigned else None
            self._cache[key] = parse(row, default) if row is not None else default
        return self._cache[key]

    def spawner_cooldowns(self, variant: Variant = DEFAULT_VARIANT) -> SpawnerCooldownsData:
        return self.data(SPAWNER_COOLDOWNS, variant)

    def enemy_hp_damage_mult(self, variant: Variant = DEFAULT_VARIANT) -> EnemyHpDamageMultData:
        return self.data(ENEMY_HP_DAMAGE_MULT, variant)

    def fast_rpm_params(self, variant: Variant = DEFAULT_VARIANT) -> FastRPMParamsData:
        return self.data(FAST_RPM_PARAMS, variant)

    def common_reward_multipliers(self, variant: Variant = DEFAULT_VARIANT) -> CommonRewardMultipliersData:
        return self.data(COMMON_REWARD_MULTIPLIERS, variant)


def load_ab_config(con: sqlite3.Connection) -> ABTestConfig:
    cur = con.execute("SELECT * FROM c_ABTestDataConfig")
    names = [d[0] for d in cur.description]
    top = dict(zip(names, cur.fetchone()))
    rules = {key: (top[f"{fld}.rules"] or None) for fld, key in AB_KEYS.items() if f"{fld}.rules" in names}
    tables = {r[0] for r in con.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
    variants: dict[str, dict[str, dict[str, Any]]] = {}
    for fld, key in AB_KEYS.items():
        table = f"c_ABTestDataConfig__{fld}.variants"
        if table not in tables:
            continue
        cur = con.execute(f'SELECT * FROM "{table}" ORDER BY "{fld}.variants#idx"')
        cols = [d[0] for d in cur.description]
        variants[key] = {}
        for row in cur:
            r = dict(zip(cols, row))
            variants[key][str(r["variant"])] = r  # BuildVariantsDictionary: a repeated variant, last wins
    return ABTestConfig(rules, variants)


# ---------------------------------------------------------------- ABTestService lookups

@implements("spawners.ab_cooldown_table", provenance=Provenance.PORT,
            code_ref="CodeBase/ABTests/ABTestService.cs:44,46-47,59")
def spawner_cooldown_seconds(data: SpawnerCooldownsData, is_elite: bool, location: int):
    """``GetSpawnerCooldownSeconds``: ``GetValue(cooldownMinutes, L) * 60f`` (float32)."""
    return fmul(data.group(is_elite).cooldown_minutes.get(location), f32(60.0))


@implements("spawners.ab_charges_table", provenance=Provenance.PORT,
            code_ref="CodeBase/ABTests/ABTestService.cs:45,46-47,60")
def spawner_charges(data: SpawnerCooldownsData, is_elite: bool, location: int, unit_count: int) -> int:
    """``GetSpawnerCharges``: ``GetValue(charges, L) * unitCount`` (int32)."""
    return int(data.group(is_elite).charges.get(location)) * int(unit_count)


@implements("pending:ab.enemy_hp_damage_multiplier", provenance=Provenance.PORT,
            code_ref="CodeBase/ABTests/ABTestService.cs:18-19")
def enemy_hp_damage_multiplier(data: EnemyHpDamageMultData, location: int):
    """``GetEnemyHpDamageMultiplier``: override for the location, else the default (float32)."""
    return data.damage_multiplier.get(location)


@implements("simulation.rpm_stat_class_ab_multiplier", provenance=Provenance.PORT,
            code_ref="CodeBase/Simulation/Systems/SimulationSystem.cs:232-237", va="0x13e1468")
def fast_rpm_config(data: FastRPMParamsData) -> tuple[float, float]:
    """``SimulationSystem.GetFastRPMConfig``: the two float parameters widened to double."""
    return float(data.fight_time_seconds), float(data.max_health_loss_multiplier)


_SUSTAIN_STATS = frozenset({1, 2, 22})  # Health, HealthRegen, LifestealFlat
_ATTACK_STATS = frozenset({6, 8, 12})  # Stab/Cut/BuckshotDamageFlat
_ARMOR_STATS = frozenset({14, 16, 20})  # Stab/Cut/BuckshotDefenceFlat


@implements("simulation.rpm_stat_class_ab_multiplier", provenance=Provenance.PORT,
            code_ref="CodeBase/ABTests/ABTestService.cs:40-41,49-58")
def rpm_stat_value_multiplier(data: CommonRewardMultipliersData, stat_type: int):
    """``GetRpmStatValueMultiplier(StatType)`` (float32): sustain, attack or armor class multiplier, else 1."""
    t = int(stat_type)
    if t in _SUSTAIN_STATS:
        return f32(data.sustain_stat_value_multiplier)
    if t in _ATTACK_STATS:
        return f32(data.attack_stat_value_multiplier)
    if t in _ARMOR_STATS:
        return f32(data.armor_stat_value_multiplier)
    return f32(1.0)
