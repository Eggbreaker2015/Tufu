"""model.enums matches the C# enums recorded in the extracted ``enums`` table."""

from __future__ import annotations

from tufu_balance.model.enums import CSHARP_ENUMS, ItemSlotType, SlotType


def test_enums_match_extracted_csharp(db):
    for name, cls in CSHARP_ENUMS.items():
        rows = db.execute("SELECT member, value FROM enums WHERE enum = ? ORDER BY value", (name,)).fetchall()
        assert rows, f"enum {name} missing from the enums table"
        assert {r["member"]: r["value"] for r in rows} == {m.name: m.value for m in cls}, name


def test_slot_type_alias():
    assert SlotType is ItemSlotType and SlotType.Shield == 2
