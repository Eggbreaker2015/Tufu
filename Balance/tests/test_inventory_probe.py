"""Planted G07 faults must fail even when the shipped table already holds every answer."""
from copy import deepcopy

import pytest

from tufu_balance.model import evolution_gen as eg
from tufu_balance.unity.inventory import validate_generator_rows


def test_g07_rejects_noop_and_value_only_generators(db):
    config = eg.load_evolution_config(db)
    rows = []
    for key, result in eg.replay_stored_inputs(config).items():
        kind, damage_type, rarity, level = key
        value, copy_value = map(float, result.stored)
        rows.append(dict(armor=kind == "armor", type=damage_type, rarity=rarity, level=level,
                         before_value=-1, before_copy_value=-1, value=value, copy_value=copy_value))
    assert validate_generator_rows(rows, config) == {"weapon": 225, "armor": 225}

    unchanged_shipped = deepcopy(rows)
    for row in unchanged_shipped:
        row["before_value"], row["before_copy_value"] = row["value"], row["copy_value"]
    with pytest.raises(ValueError, match="not poisoned"):
        validate_generator_rows(unchanged_shipped, config)

    noop = deepcopy(rows)
    for row in noop:
        row["value"], row["copy_value"] = row["before_value"], row["before_copy_value"]
    with pytest.raises(ValueError, match="generated values differ"):
        validate_generator_rows(noop, config)

    value_only = deepcopy(rows)
    value_only[-1]["copy_value"] = -1
    with pytest.raises(ValueError, match="generated values differ"):
        validate_generator_rows(value_only, config)
