"""Static pipeline: ``derive_static`` (every static derived table of a ``GameConfig``) and the static half of
the diff pipeline (design 3.1, 6.3 steps 1-2; unit P9).

:func:`derive_static` recomputes, from a config, the tables the stage-3/4 rules derive and compares them
with what the config stores:

==========================  =================================================================  =========
table                       rows                                                               checks
==========================  =================================================================  =========
``island_target_days``     island: stored ITD, closed-form tail, gate OT, start day            G01, G02
``relative_windows``       island: tool window stored / from ITD                              G03
``optimum_time``           SpawnerId: OT stored / from RelativeOptimumTime, exception flag     G03
``evolution_islands``      (rarity, name, k): E_k stored / fill rule                           G04
``evolution_target_days``  (kind, rarity, k): ``*EvolutionTargetTimings`` stored / from E_k     G04
``interpolation_points``   curve: TimingPoints stored / from IslandPoints                      G05
``amulet_level_days``      level: ``LevelTargetDays`` stored / island start                    G06
``evolution_values``       (kind, type, rarity, level): Value, CopyValue, RSP stored / mode    G07-G10
``snapshot_composition``   (list, index): K2, K3, mismatching stat columns                     G11
``bake``                   SpawnerId: recalculated OT, bake day, K4/K5, new Health/Damage      G13
``spawner_runtime``        SpawnerId (registered): ChargesTotal, BaseCooldown, charges limit   (G17b)
``bosses``                 island: the boss spawner                                            (P6)
``item_evolve_jumps``      (item, level): stat before / after evolving at the threshold        (G18)
==========================  =================================================================  =========

G12 is checked on the config's evolution rows and the shipped day snapshots. Every rule is the existing
model function (:mod:`.timeline`, :mod:`.evolution_gen`, :mod:`.items`, :mod:`.spawners`, :mod:`.ab`,
:mod:`.bake`, :mod:`.snapshots`); this module only feeds them the config and tabulates.

Exceptions keep their stored values in the config (6.3); here they are flagged (``exception``) against
the base config given as ``exceptions_from`` (default: the config itself).

A patched config can leave a rule without a value (an island timing past the last island, a whole-array
``IslandTargetDays`` that is too short): that row's derived cells are None and a ``derive_error`` warning
lists them; a section that cannot be derived at all leaves its tables empty.

``evolution_tables`` picks the generator for the ``evolution_values`` table (``frozen`` = the stored rows);
the G07 counts always come from ``replay_stored_inputs``. With ``SyncShieldValuesByLeader`` on, a
regenerating mode either reports the armor cells that differ across types (``armor_sync="report"``,
check ``evolution_values.armor_unsynced_cells`` and a warning) or syncs them to a leader type.
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field
from typing import Any, Mapping

from . import ab as AB
from . import bake as B
from . import config as C
from . import data as MD
from . import evolution_gen as EG
from . import items as IT
from . import patch as P
from . import snapshots as S
from . import spawners as SP
from . import timeline as TL
from .f32 import f32

ITD = "CombatBalanceConfig.IslandTargetDays"
TOOL = "s_SpawnerRelativeOptimumTimeTool__Islands"


@dataclass
class StaticResult:
    tables: dict[str, list[dict]]  # rows sorted by key
    keys: dict[str, tuple[str, ...]]
    checks: dict[str, Any]
    warnings: list[dict] = field(default_factory=list)

    def rows_by_key(self, table: str) -> dict[tuple, dict]:
        return {tuple(r[k] for k in self.keys[table]): r for r in self.tables[table]}


def _sid(scene, file_id) -> str:
    return f"{scene}:{file_id}"


class _Errors(list):
    """Rows (or whole sections) whose rule raised on this config (see module doc)."""

    def call(self, table: str, key, fn, *args):
        try:
            return fn(*args)
        except ValueError as e:
            self.append({"table": table, "key": key, "error": str(e)})
            return None


def _f(x) -> float | None:
    return None if x is None else float(x)


# ---------------------------------------------------------------------------------------------
# Domain views


def simulation_data(cfg: C.GameConfig):
    """The shipped snapshots (not patchable: shared by every config loaded from the same source)."""
    return cfg.base.cached("simulation_data", lambda: MD.load_simulation_data(cfg.base.connection()))


def bake_context(cfg: C.GameConfig, rounding=None) -> B.BakeContext:
    return cfg.cached(("bake_context", rounding), lambda: MD.load_bake_context(
        cfg.connection(), simulation_data=simulation_data(cfg), rounding=rounding))


def spawner_rows(cfg: C.GameConfig) -> list:
    return cfg.cached("spawner_rows", lambda: MD.load_spawner_rows(cfg.connection()))


def spawner_inputs(cfg: C.GameConfig) -> list[B.SpawnerBakeInput]:
    def build():
        armors = MD.load_armor_configs(cfg.connection())
        return [MD.spawner_input_from_row(r, armors) for r in spawner_rows(cfg)]
    return cfg.cached("spawner_inputs", build)


def spawner_defs(cfg: C.GameConfig) -> list[SP.SpawnerDef]:
    return cfg.cached("spawner_defs", lambda: SP.load_spawners(cfg.connection()))


def ab_config(cfg: C.GameConfig) -> AB.ABTestConfig:
    return cfg.cached("ab_config", lambda: AB.load_ab_config(cfg.connection()))


evolution_config = P.evolution_config


# ---------------------------------------------------------------------------------------------
# Tables


def _timeline_tables(cfg: C.GameConfig, base: C.GameConfig, out: dict, checks: dict, err: _Errors) -> None:
    itd = list(cfg.value(ITD))
    gated = [s for s in spawner_defs(cfg) if s.gate_next_locations]
    rows = {s.spawner_id: r for s, r in zip(spawner_defs(cfg), spawner_rows(cfg))}
    from_gates = err.call("island_target_days", "from_gates", TL.island_target_days_from_gates,
                          [(s.location, rows[s.spawner_id]["OptimumTime"], s.is_days) for s in gated]) or {}
    regenerated = err.call("island_target_days", "regenerated", TL.generate_island_target_days,
                           itd[:TL.TAIL_FIRST_ISLAND - 1], len(itd))
    out["island_target_days"] = [{
        "island": i, "stored": itd[i - 1],
        "closed_form": float(TL.island_target_days_closed_form(i)) if i >= TL.TAIL_FIRST_ISLAND else None,
        "regenerated": _f(regenerated[i - 1]) if regenerated else None, "from_gates": _f(from_gates.get(i)),
        "start_day": float(TL.island_start_day(itd, i))} for i in range(1, len(itd) + 1)]
    t = out["island_target_days"]
    checks["G01.tail_f32_exact"] = sum(r["closed_form"] == r["stored"] for r in t if r["closed_form"] is not None)
    checks["G01.regenerated_table_f32_exact"] = sum(r["regenerated"] == r["stored"] for r in t)
    checks["G02.gate_ot_eq_itd"] = sum(r["from_gates"] == r["stored"] for r in t if r["from_gates"] is not None)

    tool_rows = cfg.tables[TOOL].rows
    f_min = C.FIELDS["SpawnerRelativeOptimumTimeTool.Islands[].MinOptimumTime"]
    f_max = C.FIELDS["SpawnerRelativeOptimumTimeTool.Islands[].MaxOptimumTime"]
    f_days = C.FIELDS["SpawnerRelativeOptimumTimeTool.Islands[].IsDays"]
    windows = {}
    rw = []
    for r in tool_rows:
        L = int(r["LocationNumber"])
        lo, hi, days = err.call("relative_windows", L, TL.relative_window, itd, L) or (None, None, None)
        windows[L] = (f_min.decode(r), f_max.decode(r))
        rw.append({"island": L, "min": _f(lo), "max": _f(hi), "is_days": None if days is None else bool(days),
                   "tool_min": f_min.decode(r), "tool_max": f_max.decode(r), "tool_is_days": f_days.decode(r)})
    out["relative_windows"] = sorted(rw, key=lambda r: r["island"])
    checks["G03.window_from_itd_eq_tool"] = sum(
        (r["min"], r["max"], r["is_days"]) == (r["tool_min"], r["tool_max"], r["tool_is_days"]) for r in rw)

    base_exc = None if base is cfg else {r["spawner"] for r in derive_optimum_time(base) if r["exception"]}
    ot = derive_optimum_time(cfg)
    for r in ot:
        r["exception_in_base"] = r["exception"] if base_exc is None else r["spawner"] in base_exc
    out["optimum_time"] = ot
    checks["G03.rel_to_ot_hits"] = sum(not r["exception"] for r in ot)
    checks["G03.exceptions"] = sorted(r["spawner"] for r in ot if r["exception"])

    fill = {(r["Rarity"], r["Name"]): r for r in cfg.tables[f"{C.CBC}__EvolutionIslandFillRuleRows"].rows}
    cbc = cfg.tables[C.CBC].rows[0]
    ei, et = [], []
    for r in cfg.tables[f"{C.CBC}__EvolutionIslandTimingRows"].rows:
        stored = [float(f32(r[f"E{k}"])) for k in range(1, TL.EVOLUTION_LEVELS + 1)]
        fr = fill.get((r["Rarity"], r["Name"]))
        rule = ([float(x) for x in TL.evolution_islands(fr["StartValue"], fr["StepPerEvolution"])]
                if fr is not None else [None] * len(stored))
        for k, (a, b) in enumerate(zip(stored, rule), start=1):
            ei.append({"rarity": int(r["Rarity"]), "name": str(r["Name"]), "k": k, "stored": a, "fill_rule": b})
        array = P._target_array(r)
        stored_days = C.FIELDS[f"CombatBalanceConfig.{array}"].decode(cbc)
        kind = "armor" if "Armor" in array else "weapon"
        days = err.call("evolution_target_days", [kind, int(r["Rarity"])], TL.evolution_target_timings, stored, itd)
        days = [float(x) for x in days] if days is not None else [None] * len(stored_days)
        for k, (a, b) in enumerate(zip(stored_days, days), start=1):
            et.append({"kind": kind, "rarity": int(r["Rarity"]), "k": k, "array": array, "stored": a, "derived": b})
    out["evolution_islands"] = sorted(ei, key=lambda r: (r["rarity"], r["name"], r["k"]))
    out["evolution_target_days"] = sorted(et, key=lambda r: (r["kind"], r["rarity"], r["k"]))
    checks["G04.fill_rule_island_exact"] = sum(r["stored"] == r["fill_rule"] for r in ei)
    checks["G04.target_timings_f32_exact"] = sum(r["stored"] == r["derived"] for r in et)

    ip = []
    knots = close = exact = text_ok = 0
    for r in cfg.tables[f"{C.CBC}__Interpolations.Interpolations"].rows:
        islands = P._island_points(r["IslandPoints"])
        idx = int(r["Interpolations.Interpolations#idx"])
        derived = None
        points = (err.call("interpolation_points", idx, TL.interpolation_timing_points, islands, itd)
                  if islands is not None else None)
        if points is not None:
            derived = TL.format_points(points)
            stored = [float(x) for x in str(r["TimingPoints"]).split(";")]
            if len(stored) != len(points):
                err.append({"table": "interpolation_points", "key": idx, "error": f"{len(stored)} TimingPoints "
                            f"for {len(points)} IslandPoints"})
            for got, s in zip(points, stored):
                knots += 1
                close += abs(float(got) - float(f32(s))) <= 5e-5
                exact += got == f32(s)
            text_ok += derived == r["TimingPoints"]
        ip.append({"idx": idx, "name": r["Name"], "type": int(r["Type"]),
                   "stored": r["TimingPoints"], "derived": derived})
    out["interpolation_points"] = ip
    checks.update({"G05.knots": knots, "G05.knots_le_5e-5": close, "G05.knots_f32_exact": exact,
                   "G05.curves_text_exact": text_ok})

    level_days = C.FIELDS["AmuletConfig.LevelTargetDays"].decode(cfg.tables["c_AmuletConfig"].rows[0])
    derived = [float(x) for x in err.call("amulet_level_days", None, TL.amulet_level_target_days, itd) or []]
    out["amulet_level_days"] = [{"level": k, "island": TL.AMULET_TARGET_ISLANDS[k - 1] if k <= len(derived) else None,
                                 "stored": a, "derived": derived[k - 1] if k <= len(derived) else None}
                                for k, a in enumerate(level_days, start=1)]
    checks["G06.amulet_level_target_days_eq_island_start"] = sum(
        r["derived"] is not None and float(r["stored"]) == r["derived"] for r in out["amulet_level_days"])


def derive_optimum_time(cfg: C.GameConfig) -> list[dict]:
    """Per spawner: OT stored and from RelativeOptimumTime in the tool's window (G03 rule)."""
    def build():
        f_min = C.FIELDS["SpawnerRelativeOptimumTimeTool.Islands[].MinOptimumTime"]
        f_max = C.FIELDS["SpawnerRelativeOptimumTimeTool.Islands[].MaxOptimumTime"]
        windows = {int(r["LocationNumber"]): (f_min.decode(r), f_max.decode(r)) for r in cfg.tables[TOOL].rows}
        out = []
        for r in cfg.tables[C.SPAWNERS].rows:
            L = int(r["LocationNumber"])
            stored = float(f32(r["OptimumTime"]))
            rule = (float(TL.optimum_time_from_relative(r["RelativeOptimumTime"], *windows[L], bool(r["IsDays"])))
                    if L in windows else None)
            out.append({"spawner": _sid(r["scene"], r["file_id"]), "location": L, "is_days": bool(r["IsDays"]),
                        "relative": float(f32(r["RelativeOptimumTime"])), "stored": stored, "rule": rule,
                        "exception": rule != stored})
        return sorted(out, key=lambda r: r["spawner"])
    return [dict(r) for r in cfg.cached("optimum_time", build)]


def _evolution_tables(cfg: C.GameConfig, out: dict, checks: dict, warnings: list, mode: str, armor_sync: str) -> None:
    evo = evolution_config(cfg)
    rows, broken, _ = P.regenerated_evolution_rows(cfg, mode, armor_sync)
    stored = {r.key: r for r in evo.rows}
    table = []
    for g in rows:
        s = stored[g.key]
        table.append({"kind": g.kind, "damage_type": g.damage_type, "rarity": g.rarity, "level": g.evolution_level,
                      "stored_value": float(s.value), "stored_copy_value": float(s.copy_value),
                      "stored_percent": s.reference_stat_percent, "value": float(g.value),
                      "copy_value": float(g.copy_value), "percent": g.reference_stat_percent,
                      "stat_value": float(s.reference_stat_value), "heuristic": float(s.reference_heuristic_value),
                      "shards": s.reference_shards_requirement, "time_chunk": s.reference_time_chunk_index})
    out["evolution_values"] = sorted(table, key=lambda r: (r["kind"], r["damage_type"], r["rarity"], r["level"]))
    gen_keys = set(EG.replay_stored_inputs(evo))
    regen = [r for r in table if (r["kind"], r["damage_type"], r["rarity"], r["level"]) in gen_keys]
    checks[f"evolution_values.{mode}"] = {
        f"{k}_{f}": sum(r["kind"] == k and r[f] == r[f"stored_{f}"] for r in regen)
        for k in EG.KINDS for f in ("value", "copy_value")}
    if evo.levers.sync_shield_values_by_leader:
        checks["evolution_values.armor_unsynced_cells"] = len(broken)
        if broken and mode != "frozen" and armor_sync == "report":
            warnings.append({"code": "armor_sync_broken", "message": f"evolution_tables={mode}: {len(broken)} armor "
                             "cells differ across types although SyncShieldValuesByLeader is on",
                             "cells": [list(c) for c in broken]})
    replay = EG.compare_generated(evo, EG.replay_stored_inputs(evo))
    for k in ("weapon_value", "weapon_copy_value", "armor_value", "armor_copy_value"):
        checks[f"G07.{k}"] = replay[k]
    rows8 = [evo.row(*k) for k in gen_keys]
    checks["G08.half_even"] = sum(EG.reference_stat_percent(x.value, x.reference_stat_value) == x.reference_stat_percent
                                  for x in rows8)
    ident = EG.data_identities(evo)
    sync = ident["armor_sync"]
    checks.update({
        "G09.filled_rows": ident["filled_rows"], "G09.filled_h_eq_last_value": ident["filled_h_eq_last_value"],
        "G09.filled_copy_value_frozen": ident["filled_copy_value_frozen"],
        "G09.armor_sync_cells": ident["armor_sync_cells"], "G09.armor_sync_value": sync["value"],
        "G09.armor_sync_copy_value": sync["copy_value"], "G09.armor_sync_heuristic": sync["reference_heuristic_value"],
        "G09.armor_sync_shards": sync["reference_shards_requirement"],
        "G09.armor_sync_time_chunk": sync["reference_time_chunk_index"],
        "G09.armor_sync_stat_value": sync["reference_stat_value"],
        "G09.universal_rows": ident["universal_rows"], "G09.universal_placeholders": ident["universal_placeholders"],
        "G09.reference_shards_rows": ident["reference_shards_rows"],
        "G09.reference_shards_eq_table": ident["reference_shards_eq_table"]})
    m = ident["mythical"]
    checks.update({"G10.value": m["value"], "G10.copies_to_evolve": m["copies_to_evolve"],
                   "G10.copy_value": m["copy_value"], "G10.copied_rows": m["copied_rows_equal"],
                   "G10.shards_rows": m["shards_rows_equal"], "G10.on_nice_grid": m["on_nice_grid"]})
    rc = EG.reference_stat_checks(evo, simulation_data(cfg).time_chunk_snapshots)
    at = rc["at_snapshot_days"]
    checks.update({"G12.rows_at_snapshot_days": len(at), "G12.rel_lt_1e-6": sum(x["rel"] < 1e-6 for x in at),
                   "G12.bracket_checked": rc["bracket_checked"], "G12.bracket_inside": rc["bracket_inside"],
                   "G12.bracket_outside_in_other_type": rc["bracket_outside_in_other_type"],
                   "G12.groups": rc["groups"], "G12.groups_single_valued": rc["groups_single_valued"]})


def _composition(cfg: C.GameConfig, out: dict, checks: dict, rounding) -> None:
    sd = simulation_data(cfg)
    interp = bake_context(cfg, rounding).interpolations
    rows = []
    c = {"k2": 0, "k3": 0, "cells": 0, "sff": 0, "pm": 0, "tf": 0, "tv": 0, "other_cells": 0, "other": 0}
    short = {"stats_from_features_flat": "sff", "percent_multiplier": "pm", "total_flat_value": "tf", "total_value": "tv"}
    for name, lst in (("time_chunk_snapshots", sd.time_chunk_snapshots), ("snapshots", sd.snapshots)):
        for i, s in enumerate(lst):
            k2, k3 = S.composition_coefficients(interp, s.time_chunk_index, rounding)
            bad = S.composition_mismatches(s, interp, rounding)
            main = [st for st in s.stats if st.key in S.MAIN_INTERPOLATION_STATS]
            bad_main = {(m.key, m.field) for m in bad if m.key in S.MAIN_INTERPOLATION_STATS}
            c["k2"] += all((st.key, "stats_from_features_flat") not in bad_main for st in main)
            c["k3"] += all((st.key, "percent_multiplier") not in bad_main for st in main)
            c["cells"] += len(main)
            for fld, k in short.items():
                c[k] += sum((st.key, fld) not in bad_main for st in main)
            others = [st for st in s.stats if st.key not in S.MAIN_INTERPOLATION_STATS]
            bad_other = {m.key for m in bad if m.key not in S.MAIN_INTERPOLATION_STATS}
            c["other_cells"] += len(others)
            c["other"] += sum(st.key not in bad_other for st in others)
            rows.append({"list": name, "index": i, "time_chunk": s.time_chunk_index, "k2": k2, "k3": k3,
                         "mismatches": len(bad)})
    out["snapshot_composition"] = rows
    checks.update({f"G11.{k}": v for k, v in c.items()})


def _flag(r, name) -> bool:
    return bool(int(r[name] or 0))


def _bake(cfg: C.GameConfig, out: dict, checks: dict, warnings: list, rounding) -> None:
    ctx = bake_context(cfg, rounding)
    table, errors = [], []
    mm = []
    for r, sp in zip(spawner_rows(cfg), spawner_inputs(cfg)):
        row = {"spawner": _sid(*sp.spawner_id), "key": sp.key, "is_days": sp.is_days,
               "hand_balance": _flag(r, "HandBalance"), "stored_health": sp.health_base,
               "stored_damage": sp.damage_base}
        try:
            rep = B.bake_spawner(ctx, sp)
        except ValueError as e:  # BakeError, or a rule that has no value on this config
            errors.append(row["spawner"])
            row.update({"error": str(e)})
            table.append(row)
            continue
        row.update({"recalc_optimum_time": rep.recalc_optimum_time, "day": rep.day, "armor_type": rep.armor_type,
                    "ability_dps": rep.ability_dps, "ability_ehp": rep.ability_ehp,
                    "new_health": rep.new_health, "new_damage": rep.new_damage, "error": None})
        table.append(row)
        if not sp.is_days:
            mm.append((r, sp, rep))
    out["bake"] = sorted(table, key=lambda r: r["spawner"])
    if errors:
        warnings.append({"code": "bake_error", "message": f"{len(errors)} spawner(s) cannot be baked",
                         "spawners": errors})
    hp = [x for x in mm if x[1].health_base == x[2].new_health]
    band = [x for x in mm if float(x[1].optimum_time) <= 120.0001
            and not (_flag(x[0], "IsEvent") or str(x[0]["go_name"]).lower().startswith("event")
                     or _flag(x[0], "IsDummy") or _flag(x[0], "HandBalance"))]
    bi = [x for x in mm if x[0]["bake.Health"] is not None]
    checks.update({
        "G13.minute_n": len(mm), "G13.minute_hp_exact": len(hp),
        "G13.minute_damage_exact": sum(sp.damage_base == rep.new_damage for _, sp, rep in mm),
        "G13.minute_both_exact": sum(sp.health_base == rep.new_health and sp.damage_base == rep.new_damage
                                     for _, sp, rep in mm),
        "G13.minute_hp_exact_set": sorted(_sid(*sp.spawner_id) for _, sp, _ in hp),
        "G13.ot_0_120_n": len(band), "G13.ot_0_120_hp_exact": sum(sp.health_base == rep.new_health for _, sp, rep in band),
        "G13.bakeinfo_health_eq_pred": sum(float(r["bake.Health"]) == rep.new_health for r, _, rep in bi),
        "G13.bakeinfo_damage_eq_pred": sum(float(r["bake.Damage"]) == rep.new_damage for r, _, rep in bi)})


def _spawners(cfg: C.GameConfig, out: dict, checks: dict, variant) -> None:
    ready = SP.expected_ready_spawners(variant, spawners=spawner_defs(cfg), ab_config=ab_config(cfg),
                                       meta=cfg.cached("meta", lambda: SP.load_meta_levels(cfg.connection())))
    out["spawner_runtime"] = [{"spawner": _sid(*k), "charges_total": r.charges_total, "base_cooldown": r.base_cooldown,
                               "charges_limit": r.charges_limit, "uses_ab": r.uses_ab, "alive": r.alive}
                              for k, r in sorted(ready.items())]
    checks["spawners.registered"] = len(ready)
    checks["spawners.uses_ab"] = sum(r.uses_ab for r in ready.values())
    bosses = SP.bosses_by_location(spawner_defs(cfg))
    out["bosses"] = [{"island": n, "spawner": _sid(*s.spawner_id) if s else None} for n, s in sorted(bosses.items())]


def _items(cfg: C.GameConfig, out: dict, checks: dict) -> None:
    evo = evolution_config(cfg)
    jumps = IT.evolve_jumps(IT.g18_items(evo.items), evo.tables)
    out["item_evolve_jumps"] = sorted(({"item": j.key, "level": j.evolution_level, "shards": j.shards,
                                        "before": j.before, "after": j.after, "ratio": j.ratio} for j in jumps),
                                      key=lambda r: (r["item"], r["level"]))
    checks["G18.n"] = len(jumps)


KEYS = {
    "island_target_days": ("island",), "relative_windows": ("island",), "optimum_time": ("spawner",),
    "evolution_islands": ("rarity", "name", "k"), "evolution_target_days": ("kind", "rarity", "k"),
    "interpolation_points": ("idx",), "amulet_level_days": ("level",),
    "evolution_values": ("kind", "damage_type", "rarity", "level"), "snapshot_composition": ("list", "index"),
    "bake": ("spawner",), "spawner_runtime": ("spawner",), "bosses": ("island",), "item_evolve_jumps": ("item", "level"),
}


def derive_static(cfg: C.GameConfig, *, evolution_tables: str = "frozen", armor_sync: str = "report",
                  rounding=None, variant=AB.DEFAULT_VARIANT, exceptions_from: C.GameConfig | None = None
                  ) -> StaticResult:
    """Every static derived table of ``cfg`` (see module doc). ``rounding``: K2-K5 interpolation rounding
    (``None`` = ``interpolation_default.json``); ``variant``: AB variant of the spawner tables."""
    if evolution_tables not in EG.EVOLUTION_TABLE_MODES:
        raise ValueError(f"evolution_tables must be one of {EG.EVOLUTION_TABLE_MODES}, got {evolution_tables!r}")
    if armor_sync not in P.CHOICE_STEPS["evolution_armor_sync"]:
        raise ValueError(f"armor_sync must be one of {P.CHOICE_STEPS['evolution_armor_sync']}, got {armor_sync!r}")
    tables: dict = {}
    checks: dict = {}
    warnings: list = []
    err = _Errors()
    sections = (
        ("timeline", lambda: _timeline_tables(cfg, exceptions_from or cfg, tables, checks, err)),
        ("evolution_values", lambda: _evolution_tables(cfg, tables, checks, warnings, evolution_tables, armor_sync)),
        ("snapshot_composition", lambda: _composition(cfg, tables, checks, rounding)),
        ("bake", lambda: _bake(cfg, tables, checks, warnings, rounding)),
        ("spawners", lambda: _spawners(cfg, tables, checks, variant)),
        ("item_evolve_jumps", lambda: _items(cfg, tables, checks)),
    )
    for name, run_section in sections:
        err.call(name, None, run_section)
    for name in KEYS:
        tables.setdefault(name, [])
    if err:
        warnings.append({"code": "derive_error", "message": f"{len(err)} rule(s) have no value on this config "
                         "(cells None; a failed section leaves its tables empty)", "errors": list(err)})
    return StaticResult(tables, dict(KEYS), {k: _plain(v) for k, v in checks.items()}, warnings)


def _plain(v):
    """numpy scalars (counts of numpy comparisons) as Python values, recursively."""
    if isinstance(v, dict):
        return {k: _plain(x) for k, x in v.items()}
    if isinstance(v, (list, tuple)):
        return [_plain(x) for x in v]
    return v.item() if hasattr(v, "item") and not isinstance(v, (str, bytes)) else v


# ---------------------------------------------------------------------------------------------
# Diff (static half of design 6.3)


def _same(a, b) -> bool:
    if isinstance(a, float) and isinstance(b, float) and math.isnan(a) and math.isnan(b):
        return True
    return a == b and type(a) is type(b) or (a == b and {type(a), type(b)} <= {int, float})


def diff_static(base: StaticResult, patched: StaticResult) -> dict[str, list[dict]]:
    """Changed cells per table: ``{key, field, base, patched}`` sorted by key and field; added/removed rows
    appear with the whole row on one side."""
    out: dict[str, list[dict]] = {}
    for name, keys in base.keys.items():
        a, b = base.rows_by_key(name), patched.rows_by_key(name)
        changes = []
        for k in sorted(set(a) | set(b), key=lambda k: tuple((str(type(x)), x) for x in k)):
            ra, rb = a.get(k), b.get(k)
            if ra is None or rb is None:
                changes.append({"key": list(k), "field": None, "base": ra, "patched": rb})
                continue
            for fld in sorted(set(ra) | set(rb)):
                if fld not in keys and not _same(ra.get(fld), rb.get(fld)):
                    changes.append({"key": list(k), "field": fld, "base": ra.get(fld), "patched": rb.get(fld)})
        if changes:
            out[name] = changes
    return out


def diff_checks(base: StaticResult, patched: StaticResult) -> dict[str, dict]:
    """Checks whose value differs: ``{name: {base, patched}}`` (absent on one side: None)."""
    return {k: {"base": base.checks.get(k), "patched": patched.checks.get(k)}
            for k in sorted(set(base.checks) | set(patched.checks))
            if base.checks.get(k) != patched.checks.get(k)}


@dataclass
class PipelineResult:
    config: C.GameConfig
    report: P.PatchReport
    base: StaticResult
    patched: StaticResult
    diff: dict[str, list[dict]]
    warnings: list[dict]
    checks_diff: dict[str, dict] = field(default_factory=dict)


BAKE_OUTPUTS = ("recalc_optimum_time", "day", "new_health", "new_damage", "error")


def exception_spawners(base: StaticResult) -> dict[str, set[str]]:
    """The exception points of design 6.3 step 2 in a base result: OT exceptions (G03), HandBalance spawners
    and bake exceptions (minute spawners whose stored Health/Damage differ from the recomputed bake, G13)."""
    bake = base.tables["bake"]
    return {
        "ot_exception": {r["spawner"] for r in base.tables["optimum_time"] if r["exception"]},
        "hand_balance": {r["spawner"] for r in bake if r["hand_balance"]},
        "bake_exception": {r["spawner"] for r in bake if not r["is_days"] and not r["hand_balance"]
                           and r["error"] is None
                           and (r["stored_health"], r["stored_damage"]) != (r["new_health"], r["new_damage"])},
    }


def _exception_inputs_changed(report: P.PatchReport, diff: Mapping[str, list[dict]]) -> dict[str, set[str]]:
    """Per spawner: its patched leaves and the recomputed cells (OT rule, bake outputs) that moved."""
    by_spawner: dict[str, set[str]] = {}
    for leaf in report.leaves:
        if leaf.node.startswith("EnemySpawnerConfig.") and C.FIELDS[leaf.node].modeled is True:
            by_spawner.setdefault(_sid(leaf.row["scene"], leaf.row["file_id"]), set()).add(leaf.node)
    for table, fields in (("optimum_time", ("rule",)), ("bake", BAKE_OUTPUTS)):
        for c in diff.get(table, []):
            if c["field"] in fields:
                by_spawner.setdefault(c["key"][0], set()).add(f"{table}.{c['field']}")
    return by_spawner


def run(base_cfg: C.GameConfig, patch: P.Patch, **options: Any) -> PipelineResult:
    """Design 6.3 steps 1-2: patch the config, ``derive_static`` on both sides, diff the tables and checks.

    ``options`` go to :func:`derive_static`. ``evolution_tables`` defaults to ``frozen``: the patched config
    already holds the rows the patch's ``regenerate.evolution_tables`` generated; ``armor_sync`` defaults to
    the patch's ``evolution_armor_sync``. Warns when an exception spawner (OT exception, ``HandBalance`` or
    bake exception) has a changed input or a moved recomputation: its stored values stay and no longer match;
    and when the patch changes leaves but no derived table or check moves (``no_static_effect``)."""
    patched_cfg, report = P.apply(base_cfg, patch)
    options.setdefault("evolution_tables", "frozen")
    options.setdefault("armor_sync", patch.regen("evolution_armor_sync"))
    base = derive_static(base_cfg, **options)
    patched = derive_static(patched_cfg, exceptions_from=base_cfg, **options)
    diff, checks = diff_static(base, patched), diff_checks(base, patched)
    warnings = [w.to_json() for w in report.warnings] + patched.warnings
    changed = _exception_inputs_changed(report, diff)
    for kind, ids in exception_spawners(base).items():
        hit = sorted(s for s in changed if s in ids)
        if hit:
            warnings.append({"code": f"{kind}_input_changed", "message": f"{len(hit)} {kind} spawner(s) keep their "
                             "stored values although their inputs or recomputed values changed",
                             "spawners": {s: sorted(changed[s]) for s in hit}})
    if report.leaves and not diff and not checks:
        warnings.append({"code": "no_static_effect", "message": "the patch changes leaves but no derive_static table "
                         "or check moves", "nodes": sorted({x.node for x in report.leaves})})
    return PipelineResult(patched_cfg, report, base, patched, diff, warnings, checks)
