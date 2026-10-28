"""``GameConfig``: the static model's inputs as patchable leaves (design 3.1, 6.1-6.3; unit P9).

A ``GameConfig`` holds the rows of every extracted table the static oracle reads (``balance.sqlite``,
opened read-only), with a declared C# type for each patchable column. A patchable field is addressed by
its ``data_dictionary`` node (``extract.dictionary_node``), e.g. ``CombatBalanceConfig.IslandTargetDays``,
``CombatBalanceConfig.EvolutionDamageValues[].Value`` or ``EnemySpawnerConfig.Stats[Health]``. A leaf is
``(node, row, index)``: ``row`` is the row identity (``{}`` for a single asset, ``{"idx": n}`` for list
elements, ``{"scene", "file_id"}`` for spawners, ``{"asset": path}`` for per-asset configs) and
``index`` the element of an array field.

``GameConfig`` is immutable; :mod:`.patch` builds patched copies. The domain objects of the other model
modules come from :meth:`GameConfig.connection`: an in-memory copy of the source database with the
patched tables (and their mirrors ``config_arrays``, ``items``, ``s_EnemySpawner__config.Stats``)
rewritten, so the existing loaders (``data``, ``evolution_gen``, ``items``, ``spawners``, ``ab``) read the
patched values unchanged.

Values: ``float`` fields are binary32 (reported as the widened double), ``double`` fields double,
``int``/``enum`` int, ``bool`` bool, ``string``/``ref`` str or None. Array fields are tuples.
"""

from __future__ import annotations

import json
import sqlite3
import struct
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable, Mapping

from ..extract import dictionary_node
from . import ab as AB
from .enums import BalanceInterpolationType, ItemSlotType, Rarity, StatType, WeaponType
from .f32 import f32

ENUMS = {"WeaponType": WeaponType, "Rarity": Rarity, "StatType": StatType, "ItemSlotType": ItemSlotType,
         "BalanceInterpolationType": BalanceInterpolationType}
SCALAR_TYPES = ("float", "double", "int", "bool", "string", "ref", "enum")
ARRAY_ENCODINGS = ("json", "hex_i32", "key_data")


class ConfigError(ValueError):
    pass


# ---------------------------------------------------------------------------------------------
# Field and table declarations


@dataclass(frozen=True)
class FieldSpec:
    """One patchable column. ``type`` is the C# element type; ``array`` the cell encoding of a list field."""

    sql: str
    column: str
    type: str
    array: str | None = None
    enum: str | None = None  # model.enums class for type "enum"
    modeled: bool | str = True  # read by pipeline.derive_static (a MODELED_WHEN key: only under that condition)
    values_column: str | None = None  # key_data: the sibling valueData column
    csharp: bool = True  # False: Python-only leaves (not sent to the C# patcher, see patch.PatchReport)

    @property
    def node(self) -> str:
        return dictionary_node(self.sql, self.column)

    def modeled_for(self, row: Mapping[str, Any], evolution_tables: str = "frozen") -> bool:
        """Whether ``derive_static`` reacts to this field on ``row`` (see :data:`MODELED_WHEN`)."""
        if isinstance(self.modeled, bool):
            return self.modeled
        return MODELED_WHEN[self.modeled](row, evolution_tables)

    @property
    def is_array(self) -> bool:
        return self.array is not None

    def scalar(self, cell):
        """A cell (or array element) as the typed Python value."""
        if cell is None:
            return None
        t = self.type
        if t == "float":
            return float(f32(cell))
        if t == "double":
            return float(cell)
        if t in ("int", "enum"):
            return int(cell)
        if t == "bool":
            return bool(int(cell))
        return str(cell)

    def decode(self, row: Mapping[str, Any]):
        cell = row.get(self.column)
        if self.array is None:
            return self.scalar(cell)
        if self.array == "json":
            items = json.loads(cell) if cell not in (None, "") else []
        elif self.array == "hex_i32":
            items = _hex_i32(cell)
        else:  # key_data: needs the value count (legacy float cells fall back to keys 1..N, see model.ab)
            n = len(AB.decode_value_data(row.get(self.values_column), self.values_column))
            items = AB.decode_key_data(cell, n, self.column)
        return tuple(self.scalar(x) for x in items)

    def encode(self, value):
        """Typed value -> DB cell, as the extractor would have written it."""
        if self.array is None:
            return _scalar_cell(self.type, value)
        cells = [_scalar_cell(self.type, v) for v in value]
        if self.array == "json":
            return json.dumps(cells)
        return "".join(struct.pack("<i", int(v)).hex() for v in cells)  # hex_i32 and key_data


def _scalar_cell(type_: str, value):
    if value is None:
        return None
    if type_ in ("float", "double"):
        return float(value)
    if type_ in ("int", "enum", "bool"):
        return int(value)
    return str(value)


def _hex_i32(cell) -> list[int]:
    if cell in (None, ""):
        return []
    raw = bytes.fromhex(str(cell))
    return list(struct.unpack(f"<{len(raw) // 4}i", raw))


@dataclass(frozen=True)
class TableSpec:
    """One extracted table: its row identity and its patchable columns."""

    sql: str
    key: tuple[str, ...]  # DB columns of the row identity (empty: one row)
    fields: tuple[FieldSpec, ...]
    selectors: tuple[str, ...] = ()  # extra columns usable in ``where`` (field names)
    order: tuple[str, ...] = ()

    @property
    def singleton(self) -> bool:
        return not self.key

    @property
    def prefix(self) -> str:
        """Node prefix of the table: ``CombatBalanceConfig.EvolutionDamageValues[]``."""
        return dictionary_node(self.sql, "x")[:-2]

    def key_names(self) -> tuple[str, ...]:
        return tuple("idx" if k.endswith("#idx") else k for k in self.key)

    def row_key(self, row: Mapping[str, Any]) -> tuple:
        return tuple(row[k] for k in self.key)

    def row_id(self, row: Mapping[str, Any]) -> dict:
        return {n: row[k] for n, k in zip(self.key_names(), self.key)}

    def name_of(self, column: str) -> str:
        """Field name used in ``where`` and reports (the node without the table prefix)."""
        node = dictionary_node(self.sql, column)
        return node[len(self.prefix) + 1:] if node else column

    def column_of(self, name: str) -> str | None:
        for k, n in zip(self.key, self.key_names()):
            if n == name:
                return k
        for c in tuple(f.column for f in self.fields) + self.selectors:
            if self.name_of(c) == name or c == name:
                return c
        return None


# Interpolation types derive_static reads: GetTotalCoefficient 2/3 (snapshot composition, K2/K3) and 4/5
# (bake K4/K5). The other curves (item emission, tower) only feed the C# simulation.
STATIC_INTERPOLATION_TYPES = (2, 3, 4, 5)

MODELED_WHEN = {
    # generator levers: read only when evolution_tables regenerates the evolution rows
    "evolution_regenerated": lambda row, evolution_tables: evolution_tables != "frozen",
    "static_interpolation": lambda row, evolution_tables: int(row["Type"]) in STATIC_INTERPOLATION_TYPES,
}


def _f(sql, column, type_, array=None, enum=None, modeled=True, values_column=None, csharp=True) -> FieldSpec:
    return FieldSpec(sql, column, type_, array, enum, modeled, values_column, csharp)


def _fields(sql, spec: str, type_: str, **kw) -> tuple[FieldSpec, ...]:
    return tuple(_f(sql, c, type_, **kw) for c in spec.split())


CBC = "c_CombatBalanceConfig"
RARITY_NAMES = ("Common", "Uncommon", "Rare", "Epic", "Legendary")
TARGET_TIMING_ARRAYS = tuple(f"{r}{k}EvolutionTargetTimings" for k in ("", "Armor") for r in RARITY_NAMES)


def _evolution_values(sql) -> TableSpec:
    return TableSpec(sql, (f"{sql.split('__')[1]}#idx",), (
        *_fields(sql, "Value CopyValue ReferenceStatValue ReferenceHeuristicValue", "float"),
        *_fields(sql, "ReferenceShardsRequirement ReferenceStatPercent ReferenceTimeChunkIndex", "int"),
    ), selectors=("DamageType", "Rarity", "EvolutionLevel"))


def _ab_value_by_location(sql, prefix, type_, modeled) -> tuple[FieldSpec, ...]:
    keys, values = f"{prefix}.overridesByLocation.keyData", f"{prefix}.overridesByLocation.valueData"
    return (_f(sql, f"{prefix}.defaultValue", type_, modeled=modeled),
            _f(sql, keys, "int", array="key_data", modeled=modeled, values_column=values),
            _f(sql, values, type_, array="json", modeled=modeled))


def _ab(test: str) -> str:
    return f"c_ABTestDataConfig__{test}.variants"


SPAWNERS = "spawners"
_SP_FLOAT = "OptimumTime RelativeOptimumTime AverageFightTime Difficulty Cooldown"
_SP_BOOL = "IsDays IsEliteBoss IsEvent IsVip IsDummy DisableAfterFirstClear HandBalance"
_SP_STATS = ("Health", "Damage", "AttackCooldown")

TABLES: tuple[TableSpec, ...] = (
    TableSpec(CBC, (), (
        *_fields(CBC, "MinCopyValueGrowthMultiplier CopyValueTargetShare MinEvolutionValueFromShardsMultiplier "
                      "MinArmorEvolutionValueFromPreviousEvolutionMultiplier", "double", modeled="evolution_regenerated"),
        _f(CBC, "SyncShieldValuesByLeader", "bool", modeled="evolution_regenerated"),
        _f(CBC, "IslandTargetDays", "float", array="json"),
        *_fields(CBC, " ".join(TARGET_TIMING_ARRAYS), "float", array="json"),
        *_fields(CBC, "CutDamageBaseByRarity PierceDamageBaseByRarity SmashDamageBaseByRarity "
                      "CutDamageGrowthPerLevelByRarity PierceDamageGrowthPerLevelByRarity "
                      "SmashDamageGrowthPerLevelByRarity ArmorBaseByRarity ArmorGrowthPerLevelByRarity",
                 "float", array="json"),
    )),
    _evolution_values(f"{CBC}__EvolutionDamageValues"),
    _evolution_values(f"{CBC}__EvolutionArmorValues"),
    *(TableSpec(f"{CBC}__{t}", (f"{t}#idx",), (_f(f"{CBC}__{t}", "Value", "int"),),
                selectors=("Rarity", "EvolutionLevel"))
      for t in ("EvolutionDamageShardsRequirements", "EvolutionArmorShardsRequirements")),
    # read by the C# simulation; derive_static only checks their identities (G10)
    TableSpec(f"{CBC}__MythicalEvolutionValues", ("MythicalEvolutionValues#idx",), (
        _f(f"{CBC}__MythicalEvolutionValues", "CopiesToEvolve", "int", modeled=False),
        *_fields(f"{CBC}__MythicalEvolutionValues", "CopyValue Value", "float", modeled=False),
    ), selectors=("EvolutionLevel",)),
    TableSpec(f"{CBC}__EvolutionIslandFillRuleRows", ("EvolutionIslandFillRuleRows#idx",),
              _fields(f"{CBC}__EvolutionIslandFillRuleRows", "StartValue StepPerEvolution", "float"),
              selectors=("Rarity", "Name")),
    TableSpec(f"{CBC}__EvolutionIslandTimingRows", ("EvolutionIslandTimingRows#idx",),
              _fields(f"{CBC}__EvolutionIslandTimingRows", " ".join(f"E{k}" for k in range(1, 16)), "float"),
              selectors=("Rarity", "Name")),
    TableSpec(f"{CBC}__Interpolations.Interpolations", ("Interpolations.Interpolations#idx",), (
        _f(f"{CBC}__Interpolations.Interpolations", "Use", "bool", modeled="static_interpolation"),
        *_fields(f"{CBC}__Interpolations.Interpolations", "IslandPoints TimingPoints", "string"),
        _f(f"{CBC}__Interpolations.Interpolations", "CoefPoints", "string", modeled="static_interpolation"),
    ), selectors=("Name", "Type")),
    TableSpec(SPAWNERS, ("scene", "file_id"), (
        *_fields(SPAWNERS, _SP_FLOAT, "float"),
        *_fields(SPAWNERS, _SP_BOOL, "bool"),
        *_fields(SPAWNERS, "LocationNumber Count ChargesBeforeBigRespawn", "int"),
        _f(SPAWNERS, "ArmorConfig", "ref"),
        _f(SPAWNERS, "ItemReward", "ref"),
        *(_f(SPAWNERS, f"stat.{s}", "double") for s in _SP_STATS),  # StatBase.BaseValue
        # read by the C# simulation, not by derive_static
        *_fields(SPAWNERS, "PriorityMultiplier BigCooldown", "float", modeled=False),
        *_fields(SPAWNERS, "IsStatic NonSelectable IsSpecial", "bool", modeled=False),
        *_fields(SPAWNERS, "PortalGemReward RewardedPoolIndex RequiredVipLevel", "int", modeled=False),
        _f(SPAWNERS, "CurrencyRewardAmount", "double", modeled=False),
        *_fields(SPAWNERS, "SlotReward SoulReward", "string", modeled=False),
        _f(SPAWNERS, "WeaponType", "enum", enum="WeaponType", modeled=False),
        *(_f(SPAWNERS, f"stat.{s}", "double", modeled=False) for s in ("HealthRegen", "MovementSpeed", "AttackRange")),
    ), selectors=("Key", "go_path", "go_name", "IsStatic", "IsDays"), order=("scene", "file_id")),
    TableSpec("c_AmuletConfig", (), (_f("c_AmuletConfig", "LevelTargetDays", "int", array="hex_i32"),)),
    # a scene editor tool: the game reads only the OptimumTime it wrote, so its leaves stay in Python
    TableSpec("s_SpawnerRelativeOptimumTimeTool__Islands", ("Islands#idx",), (
        _f("s_SpawnerRelativeOptimumTimeTool__Islands", "IsDays", "bool", csharp=False),
        *_fields("s_SpawnerRelativeOptimumTimeTool__Islands", "MinOptimumTime MaxOptimumTime", "float", csharp=False),
    ), selectors=("LocationNumber",)),
    TableSpec("c_ItemConfig", ("asset",), (
        _f("c_ItemConfig", "Rarity", "enum", enum="Rarity"),
        _f("c_ItemConfig", "SlotType", "enum", enum="ItemSlotType"),
        _f("c_ItemConfig", "DamageType", "enum", enum="WeaponType"),
        _f("c_ItemConfig", "FlatDamage", "float"),
        # read by simcalc and the C# simulation, not by derive_static
        *_fields("c_ItemConfig", "DamageStatTypeFlat DamageStatTypePercent", "enum", enum="StatType", modeled=False),
        *_fields("c_ItemConfig", "AttackCooldown SimulationDpsWeaponMultiplier", "float", modeled=False),
    ), selectors=("Key", "asset_name")),
    TableSpec("c_ArmorConfig__Armors", ("asset", "Armors#idx"), (
        _f("c_ArmorConfig__Armors", "Type", "enum", enum="WeaponType"),
        _f("c_ArmorConfig__Armors", "Resist", "float"),
    ), selectors=("asset_name",)),
    TableSpec("c_LocationsConfig__Locations", ("Locations#idx",), (
        _f("c_LocationsConfig__Locations", "PreferredWeaponType", "enum", enum="WeaponType"),
        _f("c_LocationsConfig__Locations", "PreferredArmorType", "enum", enum="WeaponType", modeled=False),
    ), selectors=("Number", "Name")),
    TableSpec("c_SimulationConfig", (), (
        _f("c_SimulationConfig", "EarlySpawnerOptimumTimeOffsetStartLocation", "int"),
        *_fields("c_SimulationConfig", "EarlySpawnerOptimumTimeOffsetRelativePart "
                                       "EarlySpawnerOptimumTimeOffsetMultiplier", "float"),
        *_fields("c_SimulationConfig", "EnemyHpDamageMultiplier SpawnerChargesChunkMinutes TimeChunkDurationMinutes",
                 "float", modeled=False),
        _f("c_SimulationConfig", "UseSpawnerCharges", "bool", modeled=False),
    )),
    TableSpec(_ab("spawnerCooldowns"), ("spawnerCooldowns.variants#idx",), tuple(
        f for group in ("nonElite", "elite") for f in (
            *_ab_value_by_location(_ab("spawnerCooldowns"), f"data.{group}.cooldownMinutes", "float", True),
            *_ab_value_by_location(_ab("spawnerCooldowns"), f"data.{group}.charges", "int", True))),
        selectors=("variant",)),
    TableSpec(_ab("enemyHpDamageMult"), ("enemyHpDamageMult.variants#idx",), (
        *_ab_value_by_location(_ab("enemyHpDamageMult"), "data.damageMultiplier", "float", False),
        _f(_ab("enemyHpDamageMult"), "data.rewardedValueMultiplier", "double", modeled=False),
    ), selectors=("variant",)),
    TableSpec(_ab("fastRPMParams"), ("fastRPMParams.variants#idx",),
              _fields(_ab("fastRPMParams"), "data.fightTimeSeconds data.maxHealthLossMultiplier", "float",
                      modeled=False), selectors=("variant",)),
    TableSpec(_ab("commonRewardMultipliers"), ("commonRewardMultipliers.variants#idx",),
              _fields(_ab("commonRewardMultipliers"), "data.attackStatValueMultiplier data.armorStatValueMultiplier "
                      "data.sustainStatValueMultiplier", "float", modeled=False), selectors=("variant",)),
)

TABLE_BY_SQL: dict[str, TableSpec] = {t.sql: t for t in TABLES}
FIELDS: dict[str, FieldSpec] = {f.node: f for t in TABLES for f in t.fields}


def patchable_fields() -> list[FieldSpec]:
    """Every patchable field, in declaration order."""
    return [f for t in TABLES for f in t.fields]


def field_for(node: str) -> FieldSpec | None:
    return FIELDS.get(node)


# ---------------------------------------------------------------------------------------------
# Config


@dataclass(frozen=True)
class DictionaryEntry:
    node: str
    tables: str
    min: float | None
    max: float | None


@dataclass(frozen=True)
class ConfigTable:
    spec: TableSpec
    rows: tuple[dict, ...]
    _index: dict = field(default_factory=dict, compare=False, repr=False)

    def __post_init__(self):
        for i, r in enumerate(self.rows):
            k = self.spec.row_key(r)
            if k in self._index:
                raise ConfigError(f"{self.spec.sql}: duplicate row key {k}")
            self._index[k] = i

    def row(self, key: tuple) -> dict:
        return self.rows[self._index[tuple(key)]]

    def position(self, key: tuple) -> int:
        return self._index[tuple(key)]


class GameConfig:
    """The patchable inputs plus the read-only source they came from (see module doc)."""

    def __init__(self, source: sqlite3.Connection, tables: Mapping[str, ConfigTable],
                 dictionary: Mapping[str, DictionaryEntry], base: "GameConfig | None" = None):
        self.source = source
        self.tables = dict(tables)
        self.dictionary = dictionary
        self.base = base if base is not None else self  # the config as loaded (patches chain back to it)
        self._cache: dict = {}

    # -- leaves
    def table(self, sql: str) -> ConfigTable:
        return self.tables[sql]

    def value(self, node: str, row: Mapping | None = None, index: int | None = None):
        f = FIELDS[node]
        t = self.tables[f.sql]
        r = t.rows[0] if t.spec.singleton else t.row(tuple(row[n] for n in t.spec.key_names()))
        v = f.decode(r)
        return v if index is None else v[index]

    def changed_tables(self) -> list[str]:
        return sorted(s for s, t in self.tables.items() if t is not self.base.tables[s])

    def with_tables(self, tables: Mapping[str, ConfigTable]) -> "GameConfig":
        return GameConfig(self.source, {**self.tables, **tables}, self.dictionary, self.base)

    # -- views for the domain loaders
    def connection(self, *, rewrite_all: bool = False) -> sqlite3.Connection:
        """In-memory copy of the source with the changed tables rewritten (cached per config).

        ``rewrite_all`` rewrites every config table even if unchanged (round-trip tests)."""
        key = ("connection", rewrite_all)
        if key not in self._cache:
            mem = sqlite3.connect(":memory:")
            self.source.backup(mem)
            mem.row_factory = sqlite3.Row
            names = list(self.tables) if rewrite_all else self.changed_tables()
            for sql in names:
                _rewrite(mem, self.tables[sql])
            mem.commit()
            self._cache[key] = mem
        return self._cache[key]

    def cached(self, key, build):
        if key not in self._cache:
            self._cache[key] = build()
        return self._cache[key]


def _quote(name: str) -> str:
    return '"' + name.replace('"', '""') + '"'


def _rewrite(con: sqlite3.Connection, table: ConfigTable) -> None:
    sql = table.spec.sql
    cols = [r[1] for r in con.execute(f"PRAGMA table_info({_quote(sql)})")]
    con.execute(f"DELETE FROM {_quote(sql)}")
    con.executemany(f"INSERT INTO {_quote(sql)} ({', '.join(map(_quote, cols))}) VALUES ({', '.join('?' * len(cols))})",
                    [[r.get(c) for c in cols] for r in table.rows])
    _rewrite_mirrors(con, table)


def _rewrite_mirrors(con: sqlite3.Connection, table: ConfigTable) -> None:
    """Tables the extractor derived from this one and some loader reads."""
    spec = table.spec
    if spec.singleton and "asset" in table.rows[0]:
        row = table.rows[0]
        for f in spec.fields:
            if f.array == "json":
                con.execute("DELETE FROM config_arrays WHERE owner = ? AND field_path = ?", (row["asset"], f.column))
                cell = row.get(f.column)
                values = json.loads(cell) if cell not in (None, "") else []  # raw, as the extractor wrote them
                con.executemany("INSERT INTO config_arrays (owner, class, field_path, idx, value) VALUES (?, ?, ?, ?, ?)",
                                [(row["asset"], spec.prefix, f.column, i, v) for i, v in enumerate(values)])
    if spec.sql == "c_ItemConfig":
        cols = [r[1] for r in con.execute("PRAGMA table_info(items)")]
        con.execute("DELETE FROM items")
        con.executemany(f"INSERT INTO items ({', '.join(map(_quote, cols))}) VALUES ({', '.join('?' * len(cols))})",
                        [[r.get(c) for c in cols] for r in table.rows])
    if spec.sql == SPAWNERS:
        stats = '"s_EnemySpawner__config.Stats"'
        values = {(r["scene"], r["file_id"], int(StatType[c[5:]])): v
                  for r in table.rows for c, v in r.items() if c.startswith("stat.") and v is not None}
        updates = []
        for rowid, scene, file_id, type_, old in con.execute(f"SELECT rowid, scene, file_id, Type, BaseValue FROM {stats}"):
            v = values.get((scene, file_id, int(type_)))
            if v is not None and v != old:
                updates.append((v, rowid))
        con.executemany(f"UPDATE {stats} SET BaseValue = ? WHERE rowid = ?", updates)


def _source_path(con: sqlite3.Connection) -> str:
    for _, name, path in con.execute("PRAGMA database_list"):
        if name == "main":
            return path
    return ""


def _open(db) -> sqlite3.Connection:
    if isinstance(db, sqlite3.Connection):
        return db
    path = Path(db)
    if not path.exists():
        raise FileNotFoundError(path)
    return sqlite3.connect(f"file:{path}?mode=ro", uri=True)


def load_game_config(db) -> GameConfig:
    """Load the patchable tables from ``db`` (a path, opened read-only, or a connection)."""
    con = _open(db)
    tables = {}
    for spec in TABLES:
        cur = con.execute(f"SELECT * FROM {_quote(spec.sql)}"
                          + (f" ORDER BY {', '.join(map(_quote, spec.order or spec.key))}" if spec.key else ""))
        cols = [d[0] for d in cur.description]
        rows = tuple(dict(zip(cols, r)) for r in cur)
        if spec.singleton and len(rows) != 1:
            raise ConfigError(f"{spec.sql}: expected one row, got {len(rows)}")
        missing = [f.column for f in spec.fields if f.column not in cols]
        if missing:
            raise ConfigError(f"{spec.sql}: columns {missing} missing")
        tables[spec.sql] = ConfigTable(spec, rows)
    dictionary = {r[0]: DictionaryEntry(r[0], r[1], r[2], r[3])
                  for r in con.execute('SELECT node, tables, "min", "max" FROM data_dictionary')}
    return GameConfig(con, tables, dictionary)


def source_path(cfg: GameConfig) -> str:
    return _source_path(cfg.source)


def enum_value(spec: FieldSpec, value) -> int:
    """An enum given as int or member name."""
    members = ENUMS[spec.enum]
    if isinstance(value, str):
        try:
            return int(members[value])
        except KeyError:
            raise ConfigError(f"{spec.node}: {value!r} is not a {spec.enum} member") from None
    return int(members(int(value)))


def iter_leaves(cfg: GameConfig, nodes: Iterable[str] | None = None):
    """``(node, row_id, index, value)`` for every leaf of the given (default: all) patchable fields."""
    for node in (nodes if nodes is not None else FIELDS):
        f = FIELDS[node]
        t = cfg.tables[f.sql]
        for r in t.rows:
            v = f.decode(r)
            if f.is_array:
                for i, x in enumerate(v):
                    yield node, t.spec.row_id(r), i, x
            else:
                yield node, t.spec.row_id(r), None, v
