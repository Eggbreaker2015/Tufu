"""Extractor fix for ``UnitySerializedDictionary`` keyData/valueData (design Q27, unit P6).

Unity writes a ``List<int>`` as one hex blob of little-endian int32s. Keys 1..4 give the all-digit blob
``01000000020000000300000004000000``, which the YAML resolver read as the float 1e30. The extractor now
quotes keyData/valueData hex scalars before loading; nothing else may change.
"""

from __future__ import annotations

import json
import os
import sqlite3
from pathlib import Path

import pytest

from tufu_balance import extract
from tufu_balance.model import ab
from tufu_balance.unity_yaml import Document, iter_documents

AB_ASSET = extract.ASSETS / "MonoBehaviour" / "ABTest Data Config.asset"


def _doc(body: str) -> Document:
    return Document(114, 11400000, False, body)


def test_load_document_keeps_keydata_text():
    body = (
        "MonoBehaviour:\n"
        "  digits:\n"
        "    keyData: 01000000020000000300000004000000\n"
        "    valueData:\n"
        "    - 60\n"
        "  letters:\n"
        "    keyData: 0100000002000000030000000400000005000000060000000700000008000000090000000a000000\n"
        "  empty:\n"
        "    keyData:\n"
        "    valueData: []\n"
        "  ints:\n"
        "    keyData: 0100000002000000\n"
        "    valueData: 0300000005000000\n"
        "  items:\n"
        "  - keyData: 0500000006000000\n"
        "  other: 01000000020000000300000004000000\n"
    )
    data = extract.load_document(_doc(body))
    assert data["digits"]["keyData"] == "01000000020000000300000004000000"
    assert data["digits"]["valueData"] == [60]
    assert data["letters"]["keyData"].endswith("0a000000")
    assert data["empty"]["keyData"] is None and data["empty"]["valueData"] == []
    assert data["ints"] == {"keyData": "0100000002000000", "valueData": "0300000005000000"}
    assert ab.decode_value_data(data["ints"]["valueData"]) == [3, 5]
    assert data["items"][0]["keyData"] == "0500000006000000"
    assert data["other"] == pytest.approx(1.0000000200000003e30)  # only keyData is touched
    plain = _doc("MonoBehaviour:\n  a: 1\n  b: 0.5\n")
    assert extract.load_document(plain) == plain.load()


def test_rebuild_scripts_are_not_game_data():
    """The simulator's own ScriptableObject (C3c's params asset) must not become a config table."""
    rebuild = "Scripts/Assembly-CSharp/CodeBase/Simulation/Rebuild/SimulationRebuildParamsAsset.cs"
    assert extract.is_rebuild_script(rebuild)
    assert extract.is_rebuild_script("Scripts/Assembly-CSharp/CodeBase/Simulation/Harness/SimulationHarness.cs")
    assert not extract.is_rebuild_script("Scripts/Assembly-CSharp/CodeBase/Simulation/SimulationConfig.cs")
    assert not extract.is_rebuild_script("Scripts/Assembly-CSharp/CodeBase/Simulation/RebuildX/A.cs")
    assert not extract.is_rebuild_script(None)


def test_ab_asset_keydata_decodes():
    if not AB_ASSET.exists():
        pytest.skip(f"{AB_ASSET} missing")
    docs = [d for d in iter_documents(AB_ASSET) if d.class_id == 114]
    assert len(docs) == 1
    data = extract.load_document(docs[0])
    cooldowns = {v["variant"]: v["data"] for v in data["spawnerCooldowns"]["variants"]}
    elite = cooldowns["A"]["elite"]["cooldownMinutes"]["overridesByLocation"]
    non_elite = cooldowns["A"]["nonElite"]["cooldownMinutes"]["overridesByLocation"]
    assert ab.decode_key_data(elite["keyData"], 4) == [1, 2, 3, 4]
    assert ab.decode_key_data(non_elite["keyData"], 13) == list(range(1, 14))
    mult = {v["variant"]: v["data"] for v in data["enemyHpDamageMult"]["variants"]}
    assert ab.decode_key_data(mult["B"]["damageMultiplier"]["overridesByLocation"]["keyData"], 4) == [1, 2, 3, 4]
    assert mult["A"]["damageMultiplier"]["overridesByLocation"]["keyData"] is None


def _is_lfs_pointer(path: Path) -> bool:
    with open(path, "rb") as handle:
        return handle.read(40).startswith(b"version https://git-lfs")


def _strip_keydata(value):
    if isinstance(value, dict):
        return {k: _strip_keydata(v) for k, v in value.items() if k != "keyData"}
    if isinstance(value, list):
        return [_strip_keydata(v) for v in value]
    return value


def _keydata_related(table: str, column: str, row_a: dict, row_b: dict) -> bool:
    if "keyData" in column:
        return True
    if table == "config_values":
        return str(row_a.get("field_path") or "").endswith("keyData")
    if table == "data_dictionary":
        return str(row_a.get("node") or "").endswith(".keyData")
    if table in ("assets", "scene_components", "prefab_components") and column == "data":
        return _strip_keydata(json.loads(row_a[column])) == _strip_keydata(json.loads(row_b[column]))
    return False


# Asset edits merged from main after the analysed original (6af011f). Rows from these files, and data
# dictionary nodes of these component types, are left out of the comparison; any other asset edit fails.
POST_ORIGINAL_EDITS = {
    # 05767f1 removed a stray AmuletUnlockView component from this prefab; UI only, no numbers.
    "Prefabs/UI/Amulet/AmuletSoulHarvestedView.prefab": ("AmuletUnlockView",),
}


def _post_original(table: str, names: list[str], row: tuple) -> bool:
    if table == "data_dictionary":
        node = str(dict(zip(names, row)).get("node") or "")
        return any(node.startswith(f"{t}.") for types in POST_ORIGINAL_EDITS.values() for t in types)
    return any(cell in POST_ORIGINAL_EDITS for cell in row)


@pytest.mark.slow
def test_reextract_changes_only_keydata(db_path, tmp_path, monkeypatch):
    """Re-extract this checkout and diff every table with the database: only keyData cells may differ.

    ``TUFU_EXTRACT_ASSETS`` points the extractor at another checkout's ``Assets`` (e.g. an LFS-smudged one).
    """
    if os.environ.get("TUFU_EXTRACT_ASSETS"):
        monkeypatch.setattr(extract, "ASSETS", Path(os.environ["TUFU_EXTRACT_ASSETS"]))
    scene = extract.ASSETS / extract.SCENES[0]
    if not scene.exists() or _is_lfs_pointer(scene):
        pytest.skip(f"{scene} is missing or an LFS pointer")
    out = tmp_path / "out"
    monkeypatch.setattr("sys.argv", ["extract", "--out", str(out)])
    extract.main()
    old = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
    new = sqlite3.connect(f"file:{out / 'balance.sqlite'}?mode=ro", uri=True)
    tables = lambda con: {r[0] for r in con.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
    assert tables(old) == tables(new)
    offending, keydata_cells = [], 0
    for table in sorted(tables(old)):
        cols_old = [(r[1], r[2]) for r in old.execute(f'PRAGMA table_info("{table}")')]
        cols_new = [(r[1], r[2]) for r in new.execute(f'PRAGMA table_info("{table}")')]
        assert [c for c, _ in cols_old] == [c for c, _ in cols_new], table
        offending += [(table, c, "type") for (c, t1), (_, t2) in zip(cols_old, cols_new) if t1 != t2 and "keyData" not in c]
        names = [c for c, _ in cols_old]
        rows_old = [r for r in old.execute(f'SELECT * FROM "{table}" ORDER BY rowid') if not _post_original(table, names, r)]
        rows_new = [r for r in new.execute(f'SELECT * FROM "{table}" ORDER BY rowid') if not _post_original(table, names, r)]
        assert len(rows_old) == len(rows_new), table
        for a, b in zip(rows_old, rows_new):
            if a == b and all(type(x) is type(y) for x, y in zip(a, b)):
                continue
            ra, rb = dict(zip(names, a)), dict(zip(names, b))
            for c in names:
                if ra[c] != rb[c] or type(ra[c]) is not type(rb[c]):
                    if _keydata_related(table, c, ra, rb):
                        keydata_cells += 1
                    else:
                        offending.append((table, c, ra[c], rb[c]))
    assert offending == []
    assert keydata_cells in (0, 13)  # 0 once the database itself comes from the fixed extractor
