"""Evolution value generator: ``CombatBalanceConfig.EvolutionDamageValues`` / ``EvolutionArmorValues``.

The editor-side generator is empty in the reconstruction (``ApplyEvolutionAutoBalance``,
``FillRemainingEvolutionValues``, ``FillRemainingEvolutionsByLastValue``, ``FillMythicalEvolutionValues``,
``TryCaptureShardsRequirement``, ``SimulationSystem.ApplyAutoBalanceForEvolution``; commit 6af011f). These
are the rules worked back from the shipped tables in stage 4 (``Docs/Balance/Generation/ItemGrowth.md``,
``Balance/analysis/item_growth.py``), design 3.3 G07-G10, G12, R3-R5:

* Generator (exact, G07): with ``H`` = ``ReferenceHeuristicValue`` and ``S`` = shards of level ``L``::

      Value[L]     = nice(max(H, Value[L-1] + MinEvolutionValueFromShardsMultiplier * CopyValue[L-1] * S,
                              armor: MinArmorEvolutionValueFromPreviousEvolutionMultiplier * Value[L-1]))
      CopyValue[L] = max(MinCopyValueGrowthMultiplier * CopyValue[L-1],
                         nice(CopyValueTargetShare * (H - Value[L-1]) / S))     (0 when the share is <= 0)

  Level 0 is the pre-evolution item (``*DamageBaseByRarity`` / ``ArmorBaseByRarity`` and the per-copy
  growth). Rows past the simulated horizon ("filled") use ``H`` = the last simulated ``Value``.
* :func:`nice` keeps 2 significant digits below mantissa 4, steps of 0.5 in [4, 8), 1 from 8; ties go to
  even (C# ``Math.Round``). ``ReferenceStatPercent = Math.Round(100 * Value / ReferenceStatValue)`` (G08).
* Mythical rows have their own closed form (G10); Universal rows of rarities 0-4 are placeholders.
* ``ReferenceStatValue`` is the player's flat stat ``TotalValue`` at ``ReferenceTimeChunkIndex`` (G12).
* Shards: ``Shards[L]`` ~ copies of the most-copied item of that rarity at level ``L - 1`` at the target
  timing (fit, R3). ``H``: weapons ``k_t * M_r * AttackCooldown * ReferenceStatValue``, armor
  ``k_a * ReferenceStatValue`` (fit, R4).

Arithmetic: ``ApplyEvolutionAutoBalance`` takes ``double`` heuristic and stat values and the four levers
are ``double`` fields, so the generator computes in double and stores ``(float)``. The stored rows are
binary32. :func:`nice` is exact (rational arithmetic); the tests show binary32 and float64 arithmetic give
the same rows. No interpolation is involved.
"""

from __future__ import annotations

import json
import math
import sqlite3
import statistics
from dataclasses import dataclass, field, replace
from fractions import Fraction
from typing import Mapping, Sequence

import numpy as np

from .enums import Rarity, StatType, WeaponType
from .f32 import f32, round_half_even
from .items import ItemDef, ItemTables, load_item_defs, load_item_tables
from .registry import Provenance, implements

CBC = "CodeBase/Balance/CombatBalanceConfig.cs"
KINDS = ("weapon", "armor")
USED_TYPES = (WeaponType.Stabbing, WeaponType.Cutting, WeaponType.Buckshot)  # Universal 0-4 are placeholders
TIMED_RARITIES = (Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary)
TABLE_TYPES = (WeaponType.Stabbing, WeaponType.Cutting, WeaponType.Buckshot, WeaponType.Universal)
TABLE_RARITIES = TIMED_RARITIES + (Rarity.Mythical,)
RARITY_NAMES = ("Common", "Uncommon", "Rare", "Epic", "Legendary", "Divine", "Mythical")
FLAT_STAT = {
    ("weapon", WeaponType.Stabbing): StatType.StabDamageFlat,
    ("weapon", WeaponType.Cutting): StatType.CutDamageFlat,
    ("weapon", WeaponType.Buckshot): StatType.BuckshotDamageFlat,
    ("armor", WeaponType.Stabbing): StatType.StabDefenceFlat,
    ("armor", WeaponType.Cutting): StatType.CutDefenceFlat,
    ("armor", WeaponType.Buckshot): StatType.BuckshotDefenceFlat,
}
# Rarity premium of the weapon heuristic (ItemGrowth.md 4.3): the empirical ratios 1/1.2492/1.4980/1.8714/2.4946
# (Stabbing) rounded to these.
WEAPON_RARITY_MULTIPLIER = (1.0, 1.25, 1.5, 1.875, 2.5)
ARMOR_FIT_MIN_TIMING = 150.0  # armor H/sv is read off on-time rows with T >= this (the late asymptote)
TIES = ("even", "up", "down")

Key = tuple  # (kind, damage_type, rarity, evolution_level)


# ---------------------------------------------------------------------------------------------
# Config


@dataclass(frozen=True)
class EvolutionLevers:
    """``CombatBalanceConfig`` generator levers (C# ``double``; ``MaxEvolutionLevel`` int, sync bool)."""

    min_copy_value_growth_multiplier: float
    copy_value_target_share: float
    min_evolution_value_from_shards_multiplier: float
    min_armor_evolution_value_from_previous_evolution_multiplier: float
    max_evolution_level: int
    sync_shield_values_by_leader: bool


@dataclass(frozen=True)
class EvolutionValueRow:
    """``EvolutionCombatValue`` (every field; float fields as binary32)."""

    kind: str
    damage_type: int
    rarity: int
    evolution_level: int
    value: np.float32
    copy_value: np.float32
    reference_shards_requirement: int
    reference_stat_percent: int
    reference_time_chunk_index: int
    reference_stat_value: np.float32
    reference_heuristic_value: np.float32

    @property
    def key(self) -> Key:
        return (self.kind, self.damage_type, self.rarity, self.evolution_level)

    @property
    def is_filled(self) -> bool:
        """Past the simulated horizon: ``FillRemainingEvolutionValues`` stores ``H`` as the stat value too."""
        return self.reference_stat_value == self.reference_heuristic_value


@dataclass(frozen=True)
class MythicalRow:
    """``MythicalEvolutionValue``."""

    evolution_level: int
    copies_to_evolve: int
    copy_value: np.float32
    value: np.float32


@dataclass(frozen=True)
class EvolutionConfig:
    """Everything the generator reads, as loaded from the shipped ``Combat Balance Config``."""

    levers: EvolutionLevers
    rows: tuple[EvolutionValueRow, ...]  # weapon rows then armor rows, list order
    shards: Mapping[tuple[str, int, int], int]  # (kind, rarity, level) -> shards (first row wins)
    mythical: tuple[MythicalRow, ...]
    target_timings: Mapping[str, Mapping[int, tuple]]  # kind -> rarity -> 15 binary32 days
    tables: ItemTables
    items: Mapping[str, ItemDef]
    _index: dict = field(default_factory=dict, compare=False, repr=False)

    def __post_init__(self):
        for r in self.rows:
            self._index.setdefault(r.key, r)  # List.First

    def row(self, kind: str, damage_type: int, rarity: int, level: int) -> EvolutionValueRow:
        return self._index[(kind, int(damage_type), int(rarity), int(level))]

    def series(self, kind: str, damage_type: int, rarity: int) -> list[EvolutionValueRow]:
        return [self.row(kind, damage_type, rarity, lv) for lv in range(1, self.levers.max_evolution_level + 1)]

    def start(self, kind: str, damage_type: int, rarity: int) -> tuple[float, float]:
        """Level 0 of the recursion: the pre-evolution item's base and per-copy growth."""
        t = self.tables
        if kind == "armor":
            return float(t.armor_base_value(damage_type, rarity)), float(t.armor_growth_value(damage_type, rarity))
        return float(t.damage_base(damage_type, rarity)), float(t.damage_growth(damage_type, rarity))

    def shards_series(self, kind: str, rarity: int) -> list[int]:
        return [self.shards[(kind, int(rarity), lv)] for lv in range(1, self.levers.max_evolution_level + 1)]

    def target_timing(self, kind: str, rarity: int, level: int) -> np.float32:
        return self.target_timings[kind][int(rarity)][level - 1]

    def weapon_cooldown(self, damage_type: int, rarity: int) -> float:
        """``AttackCooldown`` of the one weapon config of that type and rarity (rarities 0-4 have one each)."""
        found = [it for it in self.items.values()
                 if not it.is_armor and it.damage_type == damage_type and it.rarity == rarity]
        if len(found) != 1:
            raise ValueError(f"expected one weapon config for type {damage_type} rarity {rarity}, got "
                             f"{[it.key for it in found]}")
        return float(found[0].attack_cooldown)


def load_evolution_config(con: sqlite3.Connection) -> EvolutionConfig:
    rf = con.row_factory
    con.row_factory = sqlite3.Row
    try:
        cb = con.execute("SELECT * FROM c_CombatBalanceConfig").fetchone()
        levers = EvolutionLevers(
            float(cb["MinCopyValueGrowthMultiplier"]), float(cb["CopyValueTargetShare"]),
            float(cb["MinEvolutionValueFromShardsMultiplier"]),
            float(cb["MinArmorEvolutionValueFromPreviousEvolutionMultiplier"]),
            int(cb["MaxEvolutionLevel"]), bool(cb["SyncShieldValuesByLeader"]))
        rows = []
        for kind, tab in (("weapon", "EvolutionDamageValues"), ("armor", "EvolutionArmorValues")):
            for r in con.execute(f'SELECT * FROM c_CombatBalanceConfig__{tab} ORDER BY "{tab}#idx"'):
                rows.append(EvolutionValueRow(
                    kind, int(r["DamageType"]), int(r["Rarity"]), int(r["EvolutionLevel"]), f32(r["Value"]),
                    f32(r["CopyValue"]), int(r["ReferenceShardsRequirement"]), int(r["ReferenceStatPercent"]),
                    int(r["ReferenceTimeChunkIndex"]), f32(r["ReferenceStatValue"]),
                    f32(r["ReferenceHeuristicValue"])))
        shards: dict = {}
        for kind, tab in (("weapon", "EvolutionDamageShardsRequirements"), ("armor", "EvolutionArmorShardsRequirements")):
            for r in con.execute(f'SELECT * FROM c_CombatBalanceConfig__{tab} ORDER BY "{tab}#idx"'):
                shards.setdefault((kind, int(r["Rarity"]), int(r["EvolutionLevel"])), int(r["Value"]))
        mythical = tuple(MythicalRow(int(r["EvolutionLevel"]), int(r["CopiesToEvolve"]), f32(r["CopyValue"]),
                                     f32(r["Value"]))
                         for r in con.execute('SELECT * FROM c_CombatBalanceConfig__MythicalEvolutionValues '
                                              'ORDER BY "MythicalEvolutionValues#idx"'))
        timings = {
            "weapon": {int(r): tuple(f32(x) for x in json.loads(cb[f"{RARITY_NAMES[r]}EvolutionTargetTimings"]))
                       for r in TIMED_RARITIES},
            "armor": {int(r): tuple(f32(x) for x in json.loads(cb[f"{RARITY_NAMES[r]}ArmorEvolutionTargetTimings"]))
                      for r in TIMED_RARITIES},
        }
    finally:
        con.row_factory = rf
    return EvolutionConfig(levers, tuple(rows), shards, mythical, timings, load_item_tables(con),
                           load_item_defs(con))


# ---------------------------------------------------------------------------------------------
# nice() and the percent column


def _decade(x: Fraction) -> int:
    e = math.floor(math.log10(x))  # float estimate, corrected exactly
    while Fraction(10) ** e > x:
        e -= 1
    while Fraction(10) ** (e + 1) <= x:
        e += 1
    return e


def nice_step(x) -> Fraction:
    """Grid step at ``x``: mantissa < 4 -> 0.1, [4, 8) -> 0.5, >= 8 -> 1 (times the decade)."""
    x = Fraction(float(x))
    e = _decade(x)
    m = x / Fraction(10) ** e
    st = Fraction(1, 10) if m < 4 else Fraction(1, 2) if m < 8 else Fraction(1)
    return st * Fraction(10) ** e


@implements("balance.evolution_nice_rounding", provenance=Provenance.RULE)
def nice(x, tie: str = "even") -> float:
    """Round to the 'nice' grid, computed exactly on the binary value of ``x``.

    ``tie="even"`` is C# ``Math.Round`` (``MidpointRounding.ToEven``), the rule the data needs (6 midpoint
    targets in G07); ``"up"`` (half away from zero) and ``"down"`` (half toward zero) are ablations.
    Non-positive inputs give 0.
    """
    if tie not in TIES:
        raise ValueError(f"tie must be one of {TIES}, got {tie!r}")
    if not x > 0:
        return 0.0
    step = nice_step(x)
    q = Fraction(float(x)) / step
    n = math.floor(q)
    frac = q - n
    if frac > Fraction(1, 2) or (frac == Fraction(1, 2) and (tie == "up" or (tie == "even" and n % 2 == 1))):
        n += 1
    return float(n * step)


def nice_is_tie(x) -> bool:
    """``x`` lies exactly half-way between two grid points."""
    return x > 0 and (Fraction(float(x)) / nice_step(x)).denominator == 2


def is_nice(x) -> bool:
    return x > 0 and nice(x) == float(x)


@implements("balance.evolution_reference_stat_percent", provenance=Provenance.RULE)
def reference_stat_percent(value, stat_value, tie: str = "even") -> int:
    """``Math.Round(100.0 * Value / ReferenceStatValue)`` in double (G08)."""
    x = 100.0 * float(value) / float(stat_value)
    if tie == "even":
        return int(round_half_even(x))
    if tie == "up":
        return int(math.floor(x + 0.5))
    if tie == "down":
        return int(math.ceil(x - 0.5))
    raise ValueError(f"tie must be one of {TIES}, got {tie!r}")


# ---------------------------------------------------------------------------------------------
# Generator


@dataclass(frozen=True)
class GeneratedLevel:
    """One generated level. ``value`` / ``copy_value`` are what the generator writes (stored as float)."""

    value: float
    copy_value: float
    heuristic: float
    target: float  # max(H, floors) before nice()
    copy_raw: float  # share * (H - Value[L-1]) / S before nice()
    binding: str  # which term set Value: heuristic | shards_min | armor_prev
    copy_binding: str  # share | floor (MinCopyValueGrowthMultiplier * CopyValue[L-1])

    @property
    def stored(self) -> tuple[np.float32, np.float32]:
        return f32(self.value), f32(self.copy_value)


@implements("balance.evolution_value_autobalance", provenance=Provenance.RULE)
@implements("balance.copy_value_rule", provenance=Provenance.RULE)
def generate_level(prev_value: float, prev_copy_value: float, heuristic: float, shards: int, is_armor: bool,
                   levers: EvolutionLevers, *, tie: str = "even", armor_prev: bool = True,
                   min_constraints: bool = True, binary32: bool = False) -> GeneratedLevel:
    """One step of the recursion (module docstring). ``armor_prev`` / ``min_constraints`` / ``tie`` exist for
    the G07 ablations; ``binary32`` evaluates the floors and the copy share in float instead of double."""
    if shards <= 0:
        raise ValueError(f"shards must be positive, got {shards}")
    if binary32:
        vp, cvp, h = f32(prev_value), f32(prev_copy_value), f32(heuristic)
        v_sh = float(f32(vp + f32(f32(f32(levers.min_evolution_value_from_shards_multiplier) * cvp) * f32(shards))))
        v_prev = float(f32(f32(levers.min_armor_evolution_value_from_previous_evolution_multiplier) * vp))
        raw = float(f32(f32(f32(levers.copy_value_target_share) * f32(h - vp)) / f32(shards)))
        cv_floor = float(f32(f32(levers.min_copy_value_growth_multiplier) * cvp))
        h = float(h)
    else:
        vp, cvp, h = float(prev_value), float(prev_copy_value), float(heuristic)
        v_sh = vp + levers.min_evolution_value_from_shards_multiplier * cvp * shards
        v_prev = levers.min_armor_evolution_value_from_previous_evolution_multiplier * vp
        raw = levers.copy_value_target_share * (h - vp) / shards
        cv_floor = levers.min_copy_value_growth_multiplier * cvp
    candidates = [("heuristic", h)]
    if min_constraints:
        candidates.append(("shards_min", v_sh))
        if is_armor and armor_prev:
            candidates.append(("armor_prev", v_prev))
    binding, target = max(candidates, key=lambda c: c[1])  # first wins on equal values
    cv_share = nice(raw, tie) if raw > 0 else 0.0
    copy_binding = "floor" if cv_floor > cv_share else "share"
    return GeneratedLevel(nice(target, tie), max(cv_floor, cv_share), h, target, raw, binding, copy_binding)


@implements("balance.evolution_fill_remaining", provenance=Provenance.RULE,
            code_ref=f"{CBC}:115-117,221-223")
def generate_series(kind: str, start: tuple[float, float], heuristics: Sequence[float | None], shards: Sequence[int],
                    levers: EvolutionLevers, *, previous: Sequence[tuple[float, float]] | None = None,
                    **options) -> list[GeneratedLevel]:
    """Levels 1..n of one (kind, type, rarity) series.

    ``heuristics[L-1]`` is ``H`` of a simulated level or ``None`` for a level past the simulated horizon,
    which uses the last simulated ``Value`` (``FillRemainingEvolutionValues``). The recursion feeds its own
    output forward unless ``previous`` gives ``(Value, CopyValue)`` of levels ``0..n`` to feed in instead
    (one-step replay of stored rows: level ``L`` starts from ``previous[L-1]``; ``previous[0]`` is level 0).
    """
    if len(heuristics) != len(shards):
        raise ValueError("heuristics and shards differ in length")
    if previous is not None and len(previous) != len(shards) + 1:
        raise ValueError("previous needs level 0 plus one entry per level")
    out: list[GeneratedLevel] = []
    vp, cvp = start
    last_sim: float | None = None
    for i, (h, s) in enumerate(zip(heuristics, shards)):
        if previous is not None:
            vp, cvp = previous[i]
        if h is None:
            if last_sim is None:
                raise ValueError(f"{kind}: level {i + 1} is filled but no simulated level precedes it")
            h = last_sim
        g = generate_level(vp, cvp, h, s, kind == "armor", levers, **options)
        out.append(g)
        if heuristics[i] is not None:
            last_sim = float(previous[i + 1][0]) if previous is not None else float(f32(g.value))
        vp, cvp = float(f32(g.value)), float(f32(g.copy_value))
    return out


def _series_keys() -> list[tuple[str, int, int]]:
    return [(k, int(t), int(r)) for k in KINDS for t in USED_TYPES for r in TIMED_RARITIES]


def _stored_previous(cfg: EvolutionConfig, kind: str, t: int, r: int) -> list[tuple[float, float]]:
    rows = cfg.series(kind, t, r)
    return [cfg.start(kind, t, r)] + [(float(x.value), float(x.copy_value)) for x in rows]


@implements("balance.evolution_replay_stored_inputs", provenance=Provenance.RULE)
def replay_stored_inputs(cfg: EvolutionConfig, *, previous: str = "generated", **options) -> dict[Key, GeneratedLevel]:
    """G07: regenerate the 450 Stabbing/Cutting/Buckshot x Common..Legendary rows from the stored ``H`` and
    shards. ``previous="generated"`` chains the recursion; ``"stored"`` feeds each level the stored previous
    row (stage 4's one-step replay; the design's ablation counts are one-step)."""
    if previous not in ("generated", "stored"):
        raise ValueError(f"previous must be 'generated' or 'stored', got {previous!r}")
    out: dict[Key, GeneratedLevel] = {}
    for kind, t, r in _series_keys():
        rows = cfg.series(kind, t, r)
        hs = [float(x.reference_heuristic_value) for x in rows]
        sh = [x.reference_shards_requirement for x in rows]
        prev = _stored_previous(cfg, kind, t, r) if previous == "stored" else None
        for x, g in zip(rows, generate_series(kind, cfg.start(kind, t, r), hs, sh, cfg.levers, previous=prev,
                                              **options)):
            out[x.key] = g
    return out


def compare_generated(cfg: EvolutionConfig, gen: Mapping[Key, GeneratedLevel]) -> dict[str, int]:
    """Counts of generated (Value, CopyValue) equal to the stored binary32 rows, by kind."""
    c = {f"{k}_{f}": 0 for k in KINDS for f in ("value", "copy_value", "rows")}
    for key, g in gen.items():
        row = cfg.row(*key)
        v, cv = g.stored
        c[f"{key[0]}_rows"] += 1
        c[f"{key[0]}_value"] += bool(v == row.value)
        c[f"{key[0]}_copy_value"] += bool(cv == row.copy_value)
    return c


# ---------------------------------------------------------------------------------------------
# Mythical, placeholders, data identities (G09, G10)


@implements("balance.mythical_evolution_curve", provenance=Provenance.RULE)
def mythical_level(level: int) -> MythicalRow:
    """``Value = 50000 L (L + 1)``, ``CopiesToEvolve = 10 L``, ``CopyValue = 5000`` (not the generator above)."""
    return MythicalRow(level, 10 * level, f32(5000.0), f32(50000.0 * level * (level + 1)))


UNIVERSAL_PLACEHOLDER = (1.0, 0.0)  # Universal x rarities 0-4: Value 1, CopyValue 0, Reference* 0


def is_universal_placeholder(row: EvolutionValueRow) -> bool:
    return bool(row.value == 1 and row.copy_value == 0 and row.reference_shards_requirement == 0
                and row.reference_stat_percent == 0 and row.reference_time_chunk_index == 0
                and row.reference_stat_value == 0 and row.reference_heuristic_value == 0)


@implements("balance.evolution_data_identities", provenance=Provenance.DATA)
def data_identities(cfg: EvolutionConfig) -> dict:
    """G09 and G10 counts on the stored tables."""
    out: dict = {}
    lv = range(1, cfg.levers.max_evolution_level + 1)
    # Filled rows: H equals the last simulated Value, CopyValue frozen at the last simulated one.
    n = h_ok = cv_ok = 0
    for kind, t, r in _series_keys():
        last = None
        for row in cfg.series(kind, t, r):
            if not row.is_filled:
                last = row
                continue
            n += 1
            h_ok += bool(last is not None and row.reference_heuristic_value == last.value)
            cv_ok += bool(last is not None and row.copy_value == last.copy_value)
    out["filled_rows"], out["filled_h_eq_last_value"], out["filled_copy_value_frozen"] = n, h_ok, cv_ok
    # Armor synced across types (SyncShieldValuesByLeader).
    fields = ("value", "copy_value", "reference_heuristic_value", "reference_shards_requirement",
              "reference_time_chunk_index", "reference_stat_value")
    sync = {f: 0 for f in fields}
    cells = 0
    for r in TIMED_RARITIES:
        for L in lv:
            rows = [cfg.row("armor", t, r, L) for t in USED_TYPES]
            cells += 1
            for f in fields:
                sync[f] += len({float(getattr(x, f)) for x in rows}) == 1
    out["armor_sync_cells"], out["armor_sync"] = cells, sync
    # Universal placeholders.
    uni = [cfg.row(k, WeaponType.Universal, r, L) for k in KINDS for r in TIMED_RARITIES for L in lv]
    out["universal_rows"], out["universal_placeholders"] = len(uni), sum(map(is_universal_placeholder, uni))
    # ReferenceShardsRequirement equals the shards table (every non-placeholder row, Mythical included).
    real = [x for x in cfg.rows if x.damage_type in TABLE_TYPES and x.rarity in TABLE_RARITIES
            and not is_universal_placeholder(x)]
    out["reference_shards_rows"] = len(real)
    out["reference_shards_eq_table"] = sum(x.reference_shards_requirement == cfg.shards[(x.kind, x.rarity, x.evolution_level)]
                                           for x in real)
    # Mythical (G10).
    m = {"rows": len(cfg.mythical), "value": 0, "copies_to_evolve": 0, "copy_value": 0, "on_nice_grid": 0,
         "copy_value_eq_share_of_increment": 0}
    prev = 0.0
    off_grid = []
    for row in cfg.mythical:
        want = mythical_level(row.evolution_level)
        m["value"] += bool(row.value == want.value)
        m["copies_to_evolve"] += row.copies_to_evolve == want.copies_to_evolve
        m["copy_value"] += bool(row.copy_value == want.copy_value)
        m["on_nice_grid"] += is_nice(row.value)
        if not is_nice(row.value):
            off_grid.append([row.evolution_level, float(row.value), nice(row.value)])
        m["copy_value_eq_share_of_increment"] += bool(
            float(row.copy_value) == cfg.levers.copy_value_target_share * (float(row.value) - prev) / row.copies_to_evolve)
        prev = float(row.value)
    copied = shards_ok = copied_n = 0
    for row in cfg.mythical:
        L = row.evolution_level
        for kind in KINDS:
            for t in TABLE_TYPES:
                copied_n += 1
                x = cfg._index.get((kind, int(t), int(Rarity.Mythical), L))
                copied += bool(x is not None and x.value == row.value and x.copy_value == row.copy_value
                               and x.reference_shards_requirement == row.copies_to_evolve)
            shards_ok += cfg.shards.get((kind, int(Rarity.Mythical), L)) == row.copies_to_evolve
    m.update(copied_rows=copied_n, copied_rows_equal=copied, shards_rows=2 * len(cfg.mythical),
             shards_rows_equal=shards_ok, off_grid=off_grid)
    out["mythical"] = m
    return out


# ---------------------------------------------------------------------------------------------
# Reference columns (G08, G12, R5)


def simulated_rows(cfg: EvolutionConfig) -> list[EvolutionValueRow]:
    """Rows written by the simulation (not filled): the ones whose Reference* columns are observations."""
    return [cfg.row(k, t, r, L) for k, t, r in _series_keys() for L in range(1, cfg.levers.max_evolution_level + 1)
            if not cfg.row(k, t, r, L).is_filled]


@implements("balance.evolution_reference_stat_value", provenance=Provenance.RULE)
def reference_stat_type(kind: str, damage_type: int) -> StatType:
    """``ReferenceStatValue`` reads the player's ``<Type>DamageFlat`` (weapons) / ``<Type>DefenceFlat`` (armor)
    ``TotalValue`` at ``ReferenceTimeChunkIndex``."""
    return FLAT_STAT[(kind, WeaponType(damage_type))]


def reference_stat_checks(cfg: EvolutionConfig, day_snapshots: Sequence) -> dict:
    """G12 on the shipped day snapshots (``model.snapshots.SimulationSnapshot``, one per TimeChunkIndex)."""
    by_tc = {s.time_chunk_index: s for s in day_snapshots}
    tcs = sorted(by_tc)
    rows = simulated_rows(cfg)
    at_day = []
    groups: dict[tuple, set] = {}
    for x in rows:
        groups.setdefault((x.kind, x.damage_type, x.reference_time_chunk_index), set()).add(float(x.reference_stat_value))
        s = by_tc.get(x.reference_time_chunk_index)
        if s is not None:
            tv = s.total(reference_stat_type(x.kind, x.damage_type))
            at_day.append({"key": list(x.key), "tc": x.reference_time_chunk_index, "stored": float(x.reference_stat_value),
                           "total_value": tv, "rel": abs(tv - float(x.reference_stat_value)) / tv,
                           "binary32_equal": bool(f32(tv) == x.reference_stat_value)})
    checked = inside = other = 0
    outside = []
    for (kind, t, tc), vals in sorted(groups.items()):
        if tc in by_tc:
            continue
        lo = max((d for d in tcs if d < tc), default=None)
        hi = min((d for d in tcs if d > tc), default=None)
        if lo is None or hi is None:
            continue
        for v in sorted(vals):
            checked += 1
            a = by_tc[lo].total(reference_stat_type(kind, t))
            b = by_tc[hi].total(reference_stat_type(kind, t))
            if a * (1 - 1e-7) <= v <= b * (1 + 1e-7):
                inside += 1
                continue
            fits = [WeaponType(t2).name for t2 in USED_TYPES
                    if by_tc[lo].total(reference_stat_type(kind, t2)) * (1 - 1e-7) <= v
                    <= by_tc[hi].total(reference_stat_type(kind, t2)) * (1 + 1e-7)]
            other += bool(fits)
            outside.append({"kind": kind, "type": WeaponType(t).name, "tc": tc, "sv": v, "fits_bracket_of": fits})
    return {"at_snapshot_days": at_day, "bracket_checked": checked, "bracket_inside": inside,
            "bracket_outside_in_other_type": other, "bracket_outside": outside,
            "groups": len(groups), "groups_single_valued": sum(len(v) == 1 for v in groups.values())}


@implements("balance.evolution_reference_time_chunk", provenance=Provenance.DATA)
def evolved_on_time(row: EvolutionValueRow, target_timing) -> bool:
    """R5: ``ReferenceTimeChunkIndex == ceil(T)`` (the item evolved on the target day)."""
    return row.reference_time_chunk_index == math.ceil(float(target_timing))


def on_time_counts(cfg: EvolutionConfig) -> dict[str, dict[str, int]]:
    out: dict = {}
    for kind in KINDS:
        for t in USED_TYPES:
            rows = [x for x in simulated_rows(cfg) if x.kind == kind and x.damage_type == t]
            T = [cfg.target_timing(kind, x.rarity, x.evolution_level) for x in rows]
            out[f"{kind}|{WeaponType(t).name}"] = {
                "rows": len(rows), "on_time": sum(evolved_on_time(x, tt) for x, tt in zip(rows, T)),
                "early": sum(x.reference_time_chunk_index < math.ceil(float(tt)) for x, tt in zip(rows, T))}
    return out


# ---------------------------------------------------------------------------------------------
# Shards capture (R3)


@dataclass(frozen=True)
class CaptureCandidate:
    key: str
    damage_type: int
    evolves_in_bracket: bool
    copies_at_t: float


@implements("balance.evolution_shards_capture", provenance=Provenance.FIT)
def capture_candidates(cfg: EvolutionConfig, kind: str, rarity: int, level: int, target_timing: float,
                       day_snapshots: Sequence, states: Mapping[int, Mapping[str, tuple[int, int]]],
                       chunk_minutes: float = 1440.0) -> list[CaptureCandidate] | None:
    """Items that could set ``Shards[level]``: same rarity and kind, at evolution level ``level - 1`` on the
    snapshot before ``T`` and at ``level - 1`` or ``level`` on the one after; their copies are interpolated
    linearly to ``T`` (an evolution to ``level`` in the bracket gives the spent shards back). ``states`` maps
    TimeChunkIndex -> item Key -> ``(evolution level, copies)`` (0 = unevolved). ``None`` when ``T`` is not
    bracketed by two snapshots."""
    days = [float(s.total_playtime) / chunk_minutes for s in day_snapshots]
    T = float(target_timing)
    if T > days[-1]:
        return None
    j = max(i for i, d in enumerate(days) if d <= T)
    if j + 1 >= len(day_snapshots):
        return None
    s0, s1 = day_snapshots[j], day_snapshots[j + 1]
    a_all, b_all = states[s0.time_chunk_index], states[s1.time_chunk_index]
    sh = cfg.shards[(kind, int(rarity), level)]
    out = []
    for key, it in sorted(cfg.items.items()):
        if it.rarity != rarity or it.kind != kind or it.damage_type not in USED_TYPES:
            continue
        a, b = a_all.get(key), b_all.get(key)
        if b is None:
            continue
        a = a or (0, 0)
        if a[0] != level - 1 or b[0] not in (level - 1, level):
            continue
        evolves = b[0] == level
        gained = b[1] - a[1] + (sh if evolves else 0)
        out.append(CaptureCandidate(key, it.damage_type, evolves,
                                    a[1] + gained * (T - days[j]) / (days[j + 1] - days[j])))
    return out


def capture_most_copies(cands: Sequence[CaptureCandidate]) -> CaptureCandidate | None:
    """R3's pre-specified rule: the candidate with the most copies, any type."""
    return max(cands, key=lambda c: c.copies_at_t) if cands else None


def shards_capture_checks(cfg: EvolutionConfig, day_snapshots: Sequence,
                          states: Mapping[int, Mapping[str, tuple[int, int]]]) -> dict:
    """R3 counts: the most-copies rule, the best-candidate bound, and the plateau past the horizon."""
    rows = []
    for kind in KINDS:
        for r in TIMED_RARITIES:
            for L in range(1, cfg.levers.max_evolution_level + 1):
                cands = capture_candidates(cfg, kind, r, L, cfg.target_timing(kind, r, L), day_snapshots, states)
                if cands is None:
                    continue
                sh = cfg.shards[(kind, int(r), L)]
                pick = capture_most_copies(cands)
                best = min(cands, key=lambda c: abs(c.copies_at_t / sh - 1)) if cands else None
                rows.append({"kind": kind, "rarity": int(r), "level": L, "shards": sh, "candidates": len(cands),
                             "most_copies": None if pick is None else pick.copies_at_t / sh - 1,
                             "best": None if best is None else best.copies_at_t / sh - 1,
                             "best_nice_equal": best is not None and nice(best.copies_at_t) == sh})
    out: dict = {"levels": len(rows)}
    for kind in KINDS:
        e = [abs(x["most_copies"]) for x in rows if x["kind"] == kind and x["most_copies"] is not None]
        out[kind] = {"levels": sum(x["kind"] == kind for x in rows), "with_candidate": len(e),
                     "within_5pct": sum(v <= 0.05 for v in e), "within_15pct": sum(v <= 0.15 for v in e),
                     "median_abs": statistics.median(e) if e else None, "max_abs": max(e) if e else None}
    b = [abs(x["best"]) for x in rows if x["best"] is not None]
    out["best"] = {"with_candidate": len(b), "within_5pct": sum(v <= 0.05 for v in b),
                   "within_15pct": sum(v <= 0.15 for v in b),
                   "nice_equal": sum(bool(x["best_nice_equal"]) for x in rows)}
    # Plateau: past the last level simulated by any type, the table keeps the last captured value.
    n = ok = 0
    for kind in KINDS:
        for r in TIMED_RARITIES:
            last = max(L for t in USED_TYPES for L in range(1, cfg.levers.max_evolution_level + 1)
                       if not cfg.row(kind, t, r, L).is_filled)
            for L in range(last + 1, cfg.levers.max_evolution_level + 1):
                n += 1
                ok += cfg.shards[(kind, int(r), L)] == cfg.shards[(kind, int(r), last)]
    out["plateau_levels"], out["plateau_equal"] = n, ok
    out["rows"] = rows
    return out


def item_states(day_snapshots: Sequence, inferred: Mapping[int, Mapping]) -> dict[int, dict[str, tuple[int, int]]]:
    """``(evolution level, copies)`` per snapshot item from ``items.infer_evolved_snapshots`` (undetermined
    items count as unevolved, as in R2)."""
    out = {}
    for s in day_snapshots:
        per = inferred[s.time_chunk_index]
        out[s.time_chunk_index] = {it.key: (it.level if per[it.key].evolved else 0, it.copies) for it in s.items}
    return out


# ---------------------------------------------------------------------------------------------
# Heuristic value fit (R4, fitted_h)


@dataclass(frozen=True)
class HeuristicFit:
    """``H_weapon = k_t * M_r * AttackCooldown * sv``; ``H_armor = k_armor * sv`` (sv: ReferenceStatValue)."""

    type_coefficient: Mapping[int, float]  # k_t
    rarity_multiplier: tuple  # M_r
    armor_coefficient: float  # k_armor
    envelope: Mapping[tuple[int, int], tuple[int, float]]  # (t, r) -> (level, min H / (cd * sv))


@implements("balance.evolution_heuristic_fit", provenance=Provenance.FIT)
def fit_heuristic(cfg: EvolutionConfig, rarity_multiplier: Sequence[float] = WEAPON_RARITY_MULTIPLIER,
                  armor_min_timing: float = ARMOR_FIT_MIN_TIMING) -> HeuristicFit:
    """Weapons: per series the minimum of ``H / (cd * sv)`` over simulated rows (the envelope row, always the
    last simulated level); ``k_t`` is the geometric mean over rarities of ``min / M_r``. Armor: median of
    ``H / sv`` over on-time rows (R5) with ``T >= armor_min_timing`` (ItemGrowth.md 4.3)."""
    env = {}
    for t in USED_TYPES:
        for r in TIMED_RARITIES:
            cd = cfg.weapon_cooldown(t, r)
            rows = [x for x in cfg.series("weapon", t, r) if not x.is_filled]
            ratios = [(x.evolution_level, float(x.reference_heuristic_value) / (cd * float(x.reference_stat_value)))
                      for x in rows]
            env[(int(t), int(r))] = min(ratios, key=lambda p: p[1])
    k = {int(t): math.exp(statistics.fmean(math.log(env[(int(t), int(r))][1] / rarity_multiplier[r])
                                           for r in TIMED_RARITIES)) for t in USED_TYPES}
    ratios = [float(x.reference_heuristic_value) / float(x.reference_stat_value) for x in simulated_rows(cfg)
              if x.kind == "armor"
              and float(cfg.target_timing("armor", x.rarity, x.evolution_level)) >= armor_min_timing
              and evolved_on_time(x, cfg.target_timing("armor", x.rarity, x.evolution_level))]
    return HeuristicFit(k, tuple(rarity_multiplier), statistics.median(ratios), env)


def heuristic_value(fit: HeuristicFit, kind: str, damage_type: int, rarity: int, stat_value: float,
                    cooldown: float | None = None) -> float:
    if kind == "armor":
        return fit.armor_coefficient * float(stat_value)
    if cooldown is None:
        raise ValueError("a weapon heuristic needs the weapon's AttackCooldown")
    return fit.type_coefficient[int(damage_type)] * fit.rarity_multiplier[int(rarity)] * float(cooldown) * float(stat_value)


def heuristic_residuals(cfg: EvolutionConfig, fit: HeuristicFit) -> list[dict]:
    """Weapon ``eps = H / H_fit - 1`` per simulated row, marking the envelope (in-sample) rows."""
    out = []
    for x in simulated_rows(cfg):
        if x.kind != "weapon":
            continue
        cd = cfg.weapon_cooldown(x.damage_type, x.rarity)
        h = heuristic_value(fit, "weapon", x.damage_type, x.rarity, x.reference_stat_value, cd)
        out.append({"key": list(x.key), "tc": x.reference_time_chunk_index,
                    "eps": float(x.reference_heuristic_value) / h - 1,
                    "envelope": fit.envelope[(x.damage_type, x.rarity)][0] == x.evolution_level})
    return out


@implements("balance.evolution_fitted_h", provenance=Provenance.FIT)
def fitted_h(cfg: EvolutionConfig, fit: HeuristicFit | None = None, *, previous: str = "stored",
             **options) -> dict[Key, GeneratedLevel]:
    """Regenerate the 450 rows with the fitted ``H`` (sv = the stored ReferenceStatValue of each simulated
    row; filled rows use the last simulated Value). Not exact by construction (R4). ``previous`` as in
    :func:`replay_stored_inputs`; armor is generated per type (the sync leader is not known, G09)."""
    if previous not in ("generated", "stored"):
        raise ValueError(f"previous must be 'generated' or 'stored', got {previous!r}")
    fit = fit or fit_heuristic(cfg)
    out: dict[Key, GeneratedLevel] = {}
    for kind, t, r in _series_keys():
        rows = cfg.series(kind, t, r)
        cd = None if kind == "armor" else cfg.weapon_cooldown(t, r)
        hs = [None if x.is_filled else heuristic_value(fit, kind, t, r, x.reference_stat_value, cd) for x in rows]
        prev = _stored_previous(cfg, kind, t, r) if previous == "stored" else None
        for x, g in zip(rows, generate_series(kind, cfg.start(kind, t, r), hs,
                                              [x.reference_shards_requirement for x in rows], cfg.levers,
                                              previous=prev, **options)):
            out[x.key] = g
    return out


# ---------------------------------------------------------------------------------------------
# Whole tables


EVOLUTION_TABLE_MODES = ("frozen", "replay_stored_inputs", "fitted_h")


def evolution_tables(cfg: EvolutionConfig, mode: str = "frozen") -> list[EvolutionValueRow]:
    """Both evolution tables in list order for ``RegenerateOptions.evolution_tables`` (design 1.4 / P9):
    ``frozen`` = the shipped rows; ``replay_stored_inputs`` / ``fitted_h`` regenerate Value, CopyValue and
    ReferenceStatPercent of the 450 generated rows (chained recursion) and keep Mythical rows (closed form),
    the Universal placeholders and the other Reference* columns."""
    if mode not in EVOLUTION_TABLE_MODES:
        raise ValueError(f"mode must be one of {EVOLUTION_TABLE_MODES}, got {mode!r}")
    if mode == "frozen":
        return list(cfg.rows)
    gen = replay_stored_inputs(cfg) if mode == "replay_stored_inputs" else fitted_h(cfg, previous="generated")
    out = []
    for row in cfg.rows:
        g = gen.get(row.key)
        if g is not None:
            v, cv = g.stored
            row = replace(row, value=v, copy_value=cv,
                          reference_stat_percent=reference_stat_percent(v, row.reference_stat_value))
        elif row.rarity == Rarity.Mythical:
            m = mythical_level(row.evolution_level)
            row = replace(row, value=m.value, copy_value=m.copy_value)
        out.append(row)
    return out

