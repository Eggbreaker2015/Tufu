"""Item damage / armor, evolution cost and transition, and the "evolved" inference for snapshots.

Ports of the original code (commit 6af011f, ``CodeBase/``):

* ``Player/Inventory/ItemModel.cs`` -- ``EffectiveLevel`` (135-143), ``LevelsToEvolve`` (165),
  ``CanEvolve`` (167-175), ``IsArmor`` (177), ``DPS`` (398-406), ``Damage`` (408-416), ``Armor`` (418-426),
  ``NextEvolutionLevel`` (448), ``CalcDamage`` / ``CalcArmor`` (475-499), ``CalcEvolutionDamage`` /
  ``CalcEvolutionArmor`` (501-518), constructor ``level = copies = 1`` (525-534).
* ``Player/Commands/EvolveItemCommandRunner.cs:25-44`` (native 0x146851c) -- the evolve transition.
* ``Balance/CombatBalanceConfig.cs:147-215`` -- table lookups. Evolution rows and shard rows are found
  with ``List.First(predicate)``, which throws when nothing matches; :class:`MissingTableRow` mirrors that.

Float semantics: every table value and ``FlatDamage`` / ``AttackCooldown`` is C# ``float``; ``CalcDamage``
and friends compute in binary32 and widen the result to ``double``. ``DPS`` is ``double / float`` in double.

ItemModel state: ``Level`` is 1 until the first evolution (constructor and save data), then the
evolution level; ``EffectiveLevel = Evolved ? Level : Copies``. The shipped Simulation Data snapshots
store ``Level = EffectiveLevel`` (an unevolved item always shows ``Level == Copies`` there), so the
snapshot does not say whether an item has evolved. :func:`infer_evolved` decides it from each item's
history with the shards table; see its docstring. :func:`items_evolved_override` writes the result in
the shape of the run request's ``override.items_evolved`` (design 1.4.2).
"""

from __future__ import annotations

import json
import sqlite3
import statistics
from dataclasses import dataclass, field
from typing import Iterable, Mapping, Sequence

import numpy as np

from .enums import ItemSlotType, Rarity, WeaponType
from .f32 import f32
from .registry import Provenance, implements

ITEM_MODEL = "CodeBase/Player/Inventory/ItemModel.cs"
CBC = "CodeBase/Balance/CombatBalanceConfig.cs"


class MissingTableRow(LookupError):
    """``List.First`` found no matching row (C# ``InvalidOperationException``)."""


class CannotEvolve(ValueError):
    pass


# ---------------------------------------------------------------------------------------------
# Config


@dataclass(frozen=True)
class ItemDef:
    """The ``ItemConfig`` fields the formulas read."""

    key: str
    asset_name: str
    rarity: int
    slot_type: int
    damage_type: int
    flat_damage: np.float32
    attack_cooldown: np.float32
    simulation_dps_weapon_multiplier: float = 1.0
    damage_stat_flat: int | None = None
    damage_stat_percent: int | None = None

    @property
    def is_armor(self) -> bool:
        """``ItemModel.IsArmor``: Helmet or Shield."""
        return self.slot_type in (ItemSlotType.Helmet, ItemSlotType.Shield)

    @property
    def kind(self) -> str:
        return "armor" if self.is_armor else "weapon"


@dataclass(frozen=True)
class EvolutionRow:
    """``EvolutionCombatValue`` (only the runtime fields)."""

    damage_type: int
    rarity: int
    evolution_level: int
    value: np.float32
    copy_value: np.float32


@dataclass(frozen=True)
class ShardsRow:
    rarity: int
    evolution_level: int
    value: int


@dataclass(frozen=True)
class ItemTables:
    """The ``CombatBalanceConfig`` tables used by ``ItemModel`` (list order kept for ``First``)."""

    cut_damage_base: tuple
    pierce_damage_base: tuple
    smash_damage_base: tuple
    cut_damage_growth: tuple
    pierce_damage_growth: tuple
    smash_damage_growth: tuple
    armor_base: tuple
    armor_growth: tuple
    evolution_damage_values: tuple[EvolutionRow, ...]
    evolution_armor_values: tuple[EvolutionRow, ...]
    damage_shards: tuple[ShardsRow, ...]
    armor_shards: tuple[ShardsRow, ...]
    max_evolution_level: int
    _index: dict = field(default_factory=dict, compare=False, repr=False)

    def __post_init__(self):
        idx = self._index
        for name, rows in (("damage", self.evolution_damage_values), ("armor", self.evolution_armor_values)):
            for r in rows:
                idx.setdefault((name, r.damage_type, r.rarity, r.evolution_level), r)  # First()
        for name, rows in (("damage_shards", self.damage_shards), ("armor_shards", self.armor_shards)):
            for r in rows:
                idx.setdefault((name, r.rarity, r.evolution_level), r.value)

    @implements("items.evolution_level_clamp", provenance=Provenance.PORT, code_ref=f"{CBC}:147-150")
    def existing_evolution_level(self, evolution_level: int) -> int:
        """``GetExistingEvolutionLevel``: clamps above only."""
        return evolution_level if evolution_level < self.max_evolution_level else self.max_evolution_level

    def _arrays(self, damage_type: int, growth: bool) -> tuple:
        if damage_type == WeaponType.Buckshot:
            return self.smash_damage_growth if growth else self.smash_damage_base
        if damage_type == WeaponType.Cutting:
            return self.cut_damage_growth if growth else self.cut_damage_base
        return self.pierce_damage_growth if growth else self.pierce_damage_base

    def damage_base(self, damage_type: int, rarity: int) -> np.float32:
        return self._arrays(damage_type, False)[rarity]

    def damage_growth(self, damage_type: int, rarity: int) -> np.float32:
        return self._arrays(damage_type, True)[rarity]

    def armor_base_value(self, damage_type: int, rarity: int) -> np.float32:
        return self.armor_base[rarity]

    def armor_growth_value(self, damage_type: int, rarity: int) -> np.float32:
        return self.armor_growth[rarity]

    def evolution_row(self, is_armor: bool, damage_type: int, rarity: int, evolution_level: int) -> EvolutionRow:
        lvl = self.existing_evolution_level(evolution_level)
        row = self._index.get(("armor" if is_armor else "damage", damage_type, rarity, lvl))
        if row is None:
            raise MissingTableRow(f"no Evolution{'Armor' if is_armor else 'Damage'}Values row for "
                                  f"DamageType={damage_type} Rarity={rarity} EvolutionLevel={lvl}")
        return row

    def shards_requirement(self, is_armor: bool, rarity: int, evolution_level: int) -> int:
        """``GetEvolutionShardsRequirement``."""
        lvl = self.existing_evolution_level(evolution_level)
        v = self._index.get(("armor_shards" if is_armor else "damage_shards", rarity, lvl))
        if v is None:
            raise MissingTableRow(f"no Evolution{'Armor' if is_armor else 'Damage'}ShardsRequirements row for "
                                  f"Rarity={rarity} EvolutionLevel={lvl}")
        return v


def _f32_tuple(values: Iterable) -> tuple:
    return tuple(f32(v) for v in values)


def load_item_defs(con: sqlite3.Connection) -> dict[str, ItemDef]:
    """Item configs by ``Key``; the first asset wins (the two duplicated keys are identical copies)."""
    out: dict[str, ItemDef] = {}
    rows = con.execute('SELECT asset, asset_name, Key, Rarity, SlotType, DamageType, FlatDamage, AttackCooldown, '
                       'SimulationDpsWeaponMultiplier, DamageStatTypeFlat, DamageStatTypePercent '
                       'FROM c_ItemConfig ORDER BY asset').fetchall()
    for r in rows:
        key = str(r[2])
        if key in out:
            continue
        out[key] = ItemDef(
            key=key, asset_name=str(r[1]), rarity=int(r[3]), slot_type=int(r[4]), damage_type=int(r[5]),
            flat_damage=f32(r[6] or 0.0), attack_cooldown=f32(r[7]),
            simulation_dps_weapon_multiplier=float(r[8]) if r[8] is not None else 1.0,
            damage_stat_flat=None if r[9] is None else int(r[9]),
            damage_stat_percent=None if r[10] is None else int(r[10]))
    return out


def load_item_tables(con: sqlite3.Connection) -> ItemTables:
    con_rf = con.row_factory
    con.row_factory = sqlite3.Row
    try:
        cb = con.execute("SELECT * FROM c_CombatBalanceConfig").fetchone()

        def arr(name):
            return _f32_tuple(json.loads(cb[name]))

        def evo(tab):
            return tuple(EvolutionRow(int(r["DamageType"]), int(r["Rarity"]), int(r["EvolutionLevel"]),
                                      f32(r["Value"]), f32(r["CopyValue"]))
                         for r in con.execute(f'SELECT * FROM c_CombatBalanceConfig__{tab} ORDER BY "{tab}#idx"'))

        def shards(tab):
            return tuple(ShardsRow(int(r["Rarity"]), int(r["EvolutionLevel"]), int(r["Value"]))
                         for r in con.execute(f'SELECT * FROM c_CombatBalanceConfig__{tab} ORDER BY "{tab}#idx"'))

        return ItemTables(
            cut_damage_base=arr("CutDamageBaseByRarity"), pierce_damage_base=arr("PierceDamageBaseByRarity"),
            smash_damage_base=arr("SmashDamageBaseByRarity"),
            cut_damage_growth=arr("CutDamageGrowthPerLevelByRarity"),
            pierce_damage_growth=arr("PierceDamageGrowthPerLevelByRarity"),
            smash_damage_growth=arr("SmashDamageGrowthPerLevelByRarity"),
            armor_base=arr("ArmorBaseByRarity"), armor_growth=arr("ArmorGrowthPerLevelByRarity"),
            evolution_damage_values=evo("EvolutionDamageValues"), evolution_armor_values=evo("EvolutionArmorValues"),
            damage_shards=shards("EvolutionDamageShardsRequirements"),
            armor_shards=shards("EvolutionArmorShardsRequirements"),
            max_evolution_level=int(cb["MaxEvolutionLevel"]))
    finally:
        con.row_factory = con_rf


# ---------------------------------------------------------------------------------------------
# State and formulas


@dataclass(frozen=True)
class ItemState:
    """``ItemModel`` fields: ``level`` stays 1 until the first evolution."""

    evolved: bool = False
    level: int = 1
    copies: int = 1

    @property
    @implements("items.effective_level", provenance=Provenance.PORT, code_ref=f"{ITEM_MODEL}:135-143")
    def effective_level(self) -> int:
        return self.level if self.evolved else self.copies

    @property
    def next_evolution_level(self) -> int:
        """``NextEvolutionLevel = Level + (Evolved ? 1 : 0)`` (ItemModel.cs:448)."""
        return self.level + (1 if self.evolved else 0)

    @classmethod
    def from_snapshot(cls, level: int, copies: int, evolved: bool) -> "ItemState":
        """A snapshot item ``(Level = EffectiveLevel, Copies)`` plus the evolved flag."""
        if evolved:
            return cls(True, int(level), int(copies))
        if level != copies:
            raise ValueError(f"an unevolved snapshot item has Level == Copies, got Level={level} Copies={copies}")
        return cls(False, 1, int(copies))

    def to_snapshot(self) -> tuple[int, int]:
        """``(Level, Copies)`` as Simulation Data stores them."""
        return self.effective_level, self.copies


@implements("items.weapon_damage_pre_evolution", provenance=Provenance.PORT, code_ref=f"{ITEM_MODEL}:475-484")
@implements("items.weapon_damage_evolved", provenance=Provenance.PORT, code_ref=f"{ITEM_MODEL}:501-509")
def calc_damage(item: ItemDef, tables: ItemTables, level: int, copies: int, evolved: bool) -> np.float32:
    """``ItemModel.CalcDamage(level, copies)`` in binary32 (the caller widens to double)."""
    if evolved:
        row = tables.evolution_row(False, item.damage_type, item.rarity, level)
        result = f32(row.value + f32(row.copy_value * f32(copies)))
        return f32(result + item.flat_damage)
    base = tables.damage_base(item.damage_type, item.rarity)
    scaled = f32(tables.damage_growth(item.damage_type, item.rarity) * f32(level))
    return f32(f32(base + scaled) + item.flat_damage)


@implements("items.armor_pre_evolution", provenance=Provenance.PORT, code_ref=f"{ITEM_MODEL}:491-499")
@implements("items.armor_evolved", provenance=Provenance.PORT, code_ref=f"{ITEM_MODEL}:511-518")
def calc_armor(item: ItemDef, tables: ItemTables, level: int, copies: int, evolved: bool) -> np.float32:
    """``ItemModel.CalcArmor(level, copies)`` in binary32."""
    if evolved:
        row = tables.evolution_row(True, item.damage_type, item.rarity, level)
        return f32(row.value + f32(row.copy_value * f32(copies)))
    base = tables.armor_base_value(item.damage_type, item.rarity)
    scaled = f32(tables.armor_growth_value(item.damage_type, item.rarity) * f32(level))
    return f32(base + scaled)


def damage(item: ItemDef, st: ItemState, tables: ItemTables) -> float:
    """``ItemModel.Damage = CalcDamage(EffectiveLevel, Copies)`` (double)."""
    return float(calc_damage(item, tables, st.effective_level, st.copies, st.evolved))


def armor(item: ItemDef, st: ItemState, tables: ItemTables) -> float:
    """``ItemModel.Armor = CalcArmor(EffectiveLevel, Copies)`` (double)."""
    return float(calc_armor(item, tables, st.effective_level, st.copies, st.evolved))


@implements("items.weapon_dps", provenance=Provenance.PORT, code_ref=f"{ITEM_MODEL}:398-406")
def dps(item: ItemDef, st: ItemState, tables: ItemTables) -> float:
    """``ItemModel.DPS = Damage / Config.AttackCooldown`` (double / float, in double)."""
    return damage(item, st, tables) / float(item.attack_cooldown)


def item_value(item: ItemDef, st: ItemState, tables: ItemTables) -> float:
    """Damage for weapons, Armor for helmets and shields."""
    return armor(item, st, tables) if item.is_armor else damage(item, st, tables)


@implements("items.evolution_cost", provenance=Provenance.PORT, code_ref=f"{ITEM_MODEL}:165-175")
def levels_to_evolve(item: ItemDef, st: ItemState, tables: ItemTables) -> int:
    """``LevelsToEvolve``: copies the next evolution costs."""
    return tables.shards_requirement(item.is_armor, item.rarity, st.next_evolution_level)


def can_evolve(item: ItemDef, st: ItemState, tables: ItemTables) -> bool:
    return st.copies >= levels_to_evolve(item, st, tables)


@implements("items.evolve_transition", provenance=Provenance.PORT,
            code_ref="CodeBase/Player/Commands/EvolveItemCommandRunner.cs:25-44", va="0x146851c")
def evolve(item: ItemDef, st: ItemState, tables: ItemTables) -> ItemState:
    """``TryEvolve`` + ``EvolveItemCommandRunner.Execute``: pay ``LevelsToEvolve`` copies; the first
    evolution sets ``Evolved`` (Level stays 1), later ones increment ``Level``."""
    cost = levels_to_evolve(item, st, tables)
    if st.copies < cost:
        raise CannotEvolve(f"{item.key}: {st.copies} copies < {cost} needed for evolution {st.next_evolution_level}")
    if st.evolved:
        return ItemState(True, st.level + 1, st.copies - cost)
    return ItemState(True, st.level, st.copies - cost)


# ---------------------------------------------------------------------------------------------
# G18 / G18b: evolve jump ratios and table coverage


@dataclass(frozen=True)
class EvolveJump:
    key: str
    kind: str
    damage_type: int
    rarity: int
    evolution_level: int  # E: the evolution performed
    shards: int  # copies held before, all consumed
    before: float
    after: float

    @property
    def ratio(self) -> float:
        return self.after / self.before


def g18_items(items: Mapping[str, ItemDef]) -> list[ItemDef]:
    """G18's item set: every config except Divine (``*_div``, no table rows) and Mythical."""
    return [it for _, it in sorted(items.items())
            if it.rarity not in (Rarity.Divine, Rarity.Mythical)]


@implements("items.evolve_jump_ratio", provenance=Provenance.DATA)
def evolve_jumps(items: Iterable[ItemDef], tables: ItemTables) -> list[EvolveJump]:
    """Stat right before and right after evolution E = 1..MaxEvolutionLevel when evolving exactly at the
    threshold: before holds ``LevelsToEvolve`` copies, after holds 0 (design 3.3 G18)."""
    out = []
    for it in items:
        for e in range(1, tables.max_evolution_level + 1):
            shards = tables.shards_requirement(it.is_armor, it.rarity, e)
            before = ItemState(False, 1, shards) if e == 1 else ItemState(True, e - 1, shards)
            assert levels_to_evolve(it, before, tables) == shards
            after = evolve(it, before, tables)
            out.append(EvolveJump(it.key, it.kind, it.damage_type, it.rarity, e, shards,
                                  item_value(it, before, tables), item_value(it, after, tables)))
    return out


def jump_stats(jumps: Sequence[EvolveJump]) -> dict:
    ratios = [j.ratio for j in jumps]
    top = max(jumps, key=lambda j: j.ratio)
    return {"n": len(ratios), "min": min(ratios), "median": statistics.median(ratios), "max": max(ratios),
            "mean": statistics.fmean(ratios),
            "argmax": {"key": top.key, "evolution_level": top.evolution_level, "before": top.before,
                       "after": top.after}}


def table_coverage(items: Mapping[str, ItemDef], tables: ItemTables) -> dict:
    """Configs whose evolution value or shard lookups throw at some level 1..MaxEvolutionLevel (G18b)."""
    missing_values, missing_shards = set(), set()
    for key, it in items.items():
        for e in range(1, tables.max_evolution_level + 1):
            try:
                tables.evolution_row(it.is_armor, it.damage_type, it.rarity, e)
            except MissingTableRow:
                missing_values.add(key)
            try:
                tables.shards_requirement(it.is_armor, it.rarity, e)
            except MissingTableRow:
                missing_shards.add(key)
    return {"missing_value_rows": sorted(missing_values), "missing_shard_rows": sorted(missing_shards),
            "magic_items": sorted(k for k, it in items.items() if it.damage_type == WeaponType.Magic)}


# ---------------------------------------------------------------------------------------------
# Evolved inference


EVOLVED = "level_ne_copies"  # Level != Copies: an unevolved snapshot item always shows Level == Copies
TRAJECTORY = "trajectory"  # every change point consistent with the item's history agrees
AMBIGUOUS = "ambiguous"  # both readings fit the history
INCONSISTENT = "inconsistent"  # no reading fits (the data contradicts the model)


@dataclass(frozen=True)
class EvolvedInference:
    evolved: bool | None
    reason: str
    detail: str = ""


def _shards_sum(item: ItemDef, tables: ItemTables, first: int, last: int) -> int:
    return sum(tables.shards_requirement(item.is_armor, item.rarity, k) for k in range(first, last + 1))


def _feasible(item: ItemDef, tables: ItemTables, obs: Sequence[tuple[int, int]], k: int) -> bool:
    """Observations before ``k`` unevolved, from ``k`` on evolved: does any acquisition history fit?

    Copies only grow except when an evolution pays its cost, and evolution cannot be undone. Unevolved
    observations need ``Level == Copies`` with copies non-decreasing; evolved ones need ``Level >= 1``
    with levels non-decreasing; between two observations the copies held plus the copies gained (>= 0)
    must pay for the evolutions in between: ``c_prev <= sum(shards of the evolutions passed) + c_next``.
    """
    prev: tuple[bool, int, int] | None = None  # (evolved, level, copies) as ItemModel fields
    for j, (level, copies) in enumerate(obs):
        if j < k:
            if level != copies:
                return False
            cur = (False, 1, copies)
        else:
            if level < 1:
                return False
            cur = (True, level, copies)
        if prev is not None:
            p_ev, p_lv, p_c = prev
            c_ev, c_lv, c_c = cur
            if not p_ev and not c_ev:
                ok = c_c >= p_c
            elif not p_ev:
                ok = p_c <= _shards_sum(item, tables, 1, c_lv) + c_c
            elif c_lv < p_lv:
                ok = False
            elif c_lv == p_lv:
                ok = c_c >= p_c
            else:
                ok = p_c <= _shards_sum(item, tables, p_lv + 1, c_lv) + c_c
            if not ok:
                return False
        prev = cur
    return True


@implements("items.infer_evolved", provenance=Provenance.INFERRED)
def infer_evolved(item: ItemDef, tables: ItemTables, obs: Sequence[tuple[int, int]]) -> list[EvolvedInference]:
    """Whether the item had evolved at each of its snapshot observations ``(Level, Copies)`` (time order).

    The snapshot stores ``Level = EffectiveLevel``, so ``Level != Copies`` means evolved. ``Level == Copies``
    fits both readings on its own (evolution level L with L copies left), so the item's whole history is
    used: the evolved flag switches once, at some change point ``k``; every ``k`` that passes
    :func:`_feasible` (copy conservation through the shards table) is kept, and an observation is decided
    when all kept ``k`` put it on the same side. Otherwise it is ``None`` with the reason.
    """
    obs = [(int(lv), int(c)) for lv, c in obs]
    n = len(obs)
    ks = [k for k in range(n + 1) if _feasible(item, tables, obs, k)]
    out = []
    for j, (level, copies) in enumerate(obs):
        if not ks:
            out.append(EvolvedInference(None, INCONSISTENT, f"no change point fits {item.key}'s history"))
            continue
        sides = {j >= k for k in ks}
        if sides == {True}:
            out.append(EvolvedInference(True, EVOLVED if level != copies else TRAJECTORY))
        elif sides == {False}:
            out.append(EvolvedInference(False, TRAJECTORY))
        else:
            out.append(EvolvedInference(None, AMBIGUOUS, f"change points {ks} put observation {j} on both sides"))
    return out


def infer_evolved_snapshots(snapshots: Sequence, items: Mapping[str, ItemDef],
                            tables: ItemTables) -> dict[int, dict[str, EvolvedInference]]:
    """:func:`infer_evolved` over a snapshot list (``model.snapshots.SimulationSnapshot``, time order):
    ``{TimeChunkIndex: {item Key: EvolvedInference}}``."""
    tcis = [s.time_chunk_index for s in snapshots]
    if len(set(tcis)) != len(tcis):
        raise ValueError("snapshots must have distinct TimeChunkIndex")
    history: dict[str, list[tuple[int, int, int]]] = {}
    for s in snapshots:
        for it in s.items:
            history.setdefault(it.key, []).append((s.time_chunk_index, it.level, it.copies))
    out: dict[int, dict[str, EvolvedInference]] = {t: {} for t in tcis}
    for key, h in history.items():
        res = infer_evolved(items[key], tables, [(lv, c) for _, lv, c in h])
        for (tci, _, _), r in zip(h, res):
            out[tci][key] = r
    return out


def inference_counts(inferred: Mapping[int, Mapping[str, EvolvedInference]]) -> dict[str, int]:
    counts = {"evolved": 0, "unevolved": 0, "undetermined": 0}
    for per in inferred.values():
        for r in per.values():
            counts["evolved" if r.evolved else "unevolved" if r.evolved is False else "undetermined"] += 1
    return counts


def items_evolved_override(inferred: Mapping[int, Mapping[str, EvolvedInference]],
                           undetermined: str = "unevolved") -> dict[str, dict[str, bool | None]]:
    """The run request's ``override.items_evolved`` (design 1.4.2).

    Shape: ``{"<TimeChunkIndex>": {"<item Key>": true | false}}`` -- one entry per shipped day snapshot
    (keyed by its TimeChunkIndex as a decimal string, ascending) and per item in that snapshot (sorted by
    Key); ``true`` means the item has evolved, so its ItemModel is ``Evolved = true, Level = <snapshot
    Level>``, otherwise ``Evolved = false, Level = 1, Copies = <snapshot Copies>``. Undetermined items
    follow ``undetermined``: ``"unevolved"`` (default; design R2 treats them as unevolved) writes false,
    ``"null"`` writes JSON null, ``"error"`` raises.
    """
    if undetermined not in ("unevolved", "null", "error"):
        raise ValueError(f"undetermined must be 'unevolved', 'null' or 'error', got {undetermined!r}")
    out: dict[str, dict[str, bool | None]] = {}
    for tci in sorted(inferred):
        per = {}
        for key in sorted(inferred[tci]):
            r = inferred[tci][key]
            if r.evolved is None:
                if undetermined == "error":
                    raise ValueError(f"TC{tci} {key}: evolved state undetermined ({r.reason}: {r.detail})")
                per[key] = False if undetermined == "unevolved" else None
            else:
                per[key] = r.evolved
        out[str(tci)] = per
    return out
