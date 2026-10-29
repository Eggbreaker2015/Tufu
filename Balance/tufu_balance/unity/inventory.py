"""Inventory acceptance outputs (C6), independent of the shipped snapshot schema."""
from . import schema
schema.register_output("inventory", "C6")


def validate_generator_rows(rows, config):
    """Check G07 read-back against P5 and shipped rows after the C# probe wiped all series."""
    from collections import Counter
    from ..model import evolution_gen as eg

    expected = eg.replay_stored_inputs(config)
    seen = set()
    counts = Counter()
    for row in rows:
        key = ("armor" if row["armor"] else "weapon", row["type"], row["rarity"], row["level"])
        if key in seen or key not in expected:
            raise ValueError(f"G07 duplicate or unexpected row: {key}")
        seen.add(key)
        stored = config.row(*key)
        before = (row["before_value"], row["before_copy_value"])
        if before != (-1, -1) or before[0] == stored.value or before[1] == stored.copy_value:
            raise ValueError(f"G07 row was not poisoned: {key}")
        actual = (row["value"], row["copy_value"])
        if actual != tuple(float(v) for v in expected[key].stored) or actual != (stored.value, stored.copy_value):
            raise ValueError(f"G07 generated values differ: {key}: {actual}")
        counts[key[0]] += 1
    if seen != expected.keys():
        raise ValueError(f"G07 missing rows: {expected.keys() - seen}")
    return dict(counts)
