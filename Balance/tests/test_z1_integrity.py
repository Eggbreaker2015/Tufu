import json
import re
from pathlib import Path

from tufu_balance import graph, render_catalog
from tufu_balance.model import registry

ROOT = Path(__file__).resolve().parents[2]
BALANCE = ROOT / 'Balance'


def test_dispatcher_note_inventory():
    ids = json.loads((BALANCE / 'tests/golden/z1_note_ids.json').read_text())
    doc = (ROOT / 'Docs/Balance/Simulator/Design.md').read_text()
    found = re.findall(r'^\| (Z\d\d-\d\d) \|', doc, re.M)
    assert found == ids and len(ids) == len(set(ids))


def test_catalog_refs_and_links():
    catalog = json.loads(graph.CATALOG.read_text())
    formulas = {f['id']: f for f in catalog['formulas']}
    root = ROOT / 'Recovery/StudyProject/Assets/Scripts/Assembly-CSharp/CodeBase'
    assert json.loads(registry.REGISTRY_JSON.read_text())['pending_allowed'] is False
    for f in formulas.values():
        for ref in f.get('code_refs', []):
            assert not ref.startswith('../'), (f['id'], ref)
            match = re.fullmatch(r'(.+?\.cs)(?::([\d,–-]+))?', ref)
            assert match, (f['id'], ref)
            path = root / match[1]
            assert path.is_file(), (f['id'], ref)
            if match[2]:
                assert all(1 <= int(n) <= len(path.read_text().splitlines()) for n in re.findall(r'\d+', match[2])), (f['id'], ref)
        assert all(x in formulas and x != f['id'] for x in f.get('realizes', []))
    registry.import_model_modules()
    implemented = {i.canonical_id for i in registry.REGISTRY.implementations} | {a.formula_id for a in registry.scan_csharp() if a.formula_id}
    for fid in implemented:
        f = formulas[fid]
        assert f.get('inputs') or f.get('outputs') or f.get('realizes'), fid


def test_generated_docs_match(tmp_path, monkeypatch):
    monkeypatch.setattr(render_catalog, 'DOCS', tmp_path)
    render_catalog.main()
    for path in [tmp_path / 'FormulaCatalog.md', *sorted((tmp_path / 'formulas').glob('*.md'))]:
        assert path.read_bytes() == (ROOT / 'Docs/Balance' / path.relative_to(tmp_path)).read_bytes(), path
    catalog = json.loads(graph.CATALOG.read_text()); values = json.loads(graph.VALUES.read_text())
    import sqlite3, os
    db = Path(os.environ['TUFU_BALANCE_DB'])
    with sqlite3.connect(f'{db.as_uri()}?mode=ro', uri=True) as conn:
        dictionary = {row[0] for row in conn.execute('SELECT node FROM data_dictionary')}
    g = graph.Graph(catalog, values, dictionary)
    assert not g.problems
    monkeypatch.setattr(graph, 'DOC', tmp_path / 'DependencyGraph.md')
    graph.render_doc(g)
    assert graph.DOC.read_bytes() == (ROOT / 'Docs/Balance/DependencyGraph.md').read_bytes()


def test_coverage_export(tmp_path):
    path = tmp_path / 'coverage.json'
    registry.main(['coverage', '--write', str(path)])
    entries = json.loads(path.read_text())
    assert len(entries) == len(registry.scan_csharp())
    assert registry.coverage(entries) == registry.coverage()
