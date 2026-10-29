"""Flatten a generated Unity scene into world-space geometry + material info for preview_render.py.

    python3 preview_export.py [scene.unity] [out.json]

Only for check renders: reads the scene YAML this tool wrote, decodes every mesh (generated and original),
bakes the world transforms in, and records the parameters of each material that preview_render.py imitates.
"""
import glob, json, math, os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import meshdec
from meshlib import ASSETS

SHADERS = {
    '890f3b8f9a3e38c4c92bbc33c8630a4d': 'ground',   # Mobile/Diffuse Ground
    '1dab87691d3e66948845d5978e0f6faa': 'road',     # Custom/Mobile_WorldPosTiling_AlphaX_Shadows
    '2f1bb33d0ccec46479cfc3ac6a7dab10': 'water',    # StylizedWater/Mobile
}


def guid_index():
    idx = {}
    for m in glob.glob(ASSETS + '/**/*.meta', recursive=True):
        try:
            s = open(m, errors='ignore').read(300)
        except OSError:
            continue
        g = re.search(r'guid: ([0-9a-f]{32})', s)
        if g:
            idx[g.group(1)] = m[:-5]
    return idx


def qmat(q):
    x, y, z, w = q
    return [[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
            [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
            [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]]


def trs(p, q, s):
    R = qmat(q)
    return [[R[i][0] * s[0], R[i][1] * s[1], R[i][2] * s[2], p[i]] for i in range(3)] + [[0, 0, 0, 1]]


def mm(A, B):
    return [[sum(A[i][k] * B[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def apply(M, v):
    return [M[i][0] * v[0] + M[i][1] * v[1] + M[i][2] * v[2] + M[i][3] for i in range(3)]


def floats3(s):
    return [float(x) for x in re.findall(r'[-\d.eE]+', s)]


def material(path):
    s = open(path, errors='ignore').read()
    sh = re.search(r'm_Shader: \{fileID: \d+, guid: (\w+)', s)
    kind = SHADERS.get(sh.group(1) if sh else '', 'toon')
    colors = {k: floats3(v)[:4] for k, v in re.findall(r'\n      (_\w+): (\{r: [^}]*\})', s)}
    floats = {k: float(v) for k, v in re.findall(r'\n      (_\w+): ([-\d.eE]+)(?=\n)', s)}
    tex = {}
    for k, g in re.findall(r'\n      (_\w+):\n        m_Texture: \{fileID: \d+, guid: (\w+)', s):
        tex[k] = g
    return {'kind': kind, 'name': os.path.basename(path)[:-4], 'colors': colors, 'floats': floats, 'tex': tex}


CUBE = None


def unit_cube():
    v = []
    faces = [((1, 0, 0), (0, 1, 0), (0, 0, 1)), ((-1, 0, 0), (0, 0, 1), (0, 1, 0)), ((0, 1, 0), (0, 0, 1), (1, 0, 0)),
             ((0, -1, 0), (1, 0, 0), (0, 0, 1)), ((0, 0, 1), (1, 0, 0), (0, 1, 0)), ((0, 0, -1), (0, 1, 0), (1, 0, 0))]
    pos, tris = [], []
    for n, a, b in faces:
        c = [n[i] * 0.5 for i in range(3)]
        quad = [[c[i] + (sa * a[i] + sb * b[i]) * 0.5 for i in range(3)] for sa, sb in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        k = len(pos)
        pos += quad
        tris += [k, k + 1, k + 2, k, k + 2, k + 3]
    return {'pos': pos, 'uv': [[0, 0]] * len(pos), 'tris': [tris]}


def main():
    scene = sys.argv[1] if len(sys.argv) > 1 else ASSETS + '/Scenes/CrystalCanyon.unity'
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.dirname(os.path.abspath(__file__)) + '/.preview/scene.json'
    os.makedirs(os.path.dirname(out), exist_ok=True)
    text = open(scene).read()
    docs = {}
    for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)', text, re.S):
        docs[m.group(2)] = (int(m.group(1)), m.group(3))
    idx = guid_index()
    tr, go_name, mf, mr = {}, {}, {}, {}
    for fid, (cls, b) in docs.items():
        if cls == 4:
            go = re.search(r'm_GameObject: \{fileID: (\d+)', b).group(1)
            p = floats3(re.search(r'm_LocalPosition: (\{[^}]*\})', b).group(1))
            q = floats3(re.search(r'm_LocalRotation: (\{[^}]*\})', b).group(1))
            s = floats3(re.search(r'm_LocalScale: (\{[^}]*\})', b).group(1))
            father = re.search(r'm_Father: \{fileID: (\d+)', b).group(1)
            tr[go] = (fid, p, q, s, father)
        elif cls == 1:
            go_name[fid] = re.search(r'm_Name: (.*)', b).group(1)
        elif cls == 33:
            go = re.search(r'm_GameObject: \{fileID: (\d+)', b).group(1)
            mf[go] = re.search(r'm_Mesh: \{fileID: (\d+), guid: (\w+)', b).groups()
        elif cls == 23:
            go = re.search(r'm_GameObject: \{fileID: (\d+)', b).group(1)
            mr[go] = re.findall(r'  - \{fileID: 2100000, guid: (\w+), type: 2\}', b)
    by_tid = {v[0]: (go, v) for go, v in tr.items()}

    def world(go):
        fid, p, q, s, father = tr[go]
        M = trs(p, q, s)
        if father != '0':
            M = mm(world(by_tid[father][0]), M)
        return M

    lights = []
    cams = []
    for fid, (cls, b) in docs.items():
        if cls in (108, 20):
            go = re.search(r'm_GameObject: \{fileID: (\d+)', b).group(1)
            M = world(go)
            fwd = [M[i][2] for i in range(3)]
            l = math.sqrt(sum(x * x for x in fwd))
            entry = {'pos': [M[i][3] for i in range(3)], 'forward': [x / l for x in fwd]}
            if cls == 108:
                entry['color'] = floats3(re.search(r'm_Color: (\{[^}]*\})', b).group(1))[:3]
                entry['intensity'] = float(re.search(r'm_Intensity: ([-\d.eE]+)', b).group(1))
                lights.append(entry)
            else:
                entry['fov'] = float(re.search(r'field of view: ([-\d.eE]+)', b).group(1))
                cams.append(entry)
    rs = docs.get('2', (0, ''))[1]
    ambient = {k: floats3(v)[:3] for k, v in re.findall(r'(m_Ambient\w+Color): (\{[^}]*\})', rs)}

    mats, objects = {}, []
    mesh_cache = {}
    for go, (mfid, g) in mf.items():
        if go not in mr:
            continue
        if g == '0000000000000000e000000000000000':
            M_ = unit_cube()
            mine = True
        else:
            path = idx.get(g)
            if not path:
                print('missing mesh', g)
                continue
            if path not in mesh_cache:
                mesh_cache[path] = meshdec.load(path)
            M_ = mesh_cache[path]
            mine = '/CrystalCanyon/' in path
        W = world(go)
        pos = [apply(W, v) for v in M_['pos']]
        uv = M_.get('uv') or [[0, 0]] * len(pos)
        slots = mr[go]
        for gm in slots:
            if gm not in mats and gm in idx:
                mats[gm] = material(idx[gm])
        objects.append({'name': go_name.get(go, go), 'flat': mine, 'pos': [[round(c, 4) for c in v] for v in pos],
                        'uv': [[round(c, 5) for c in u[:2]] for u in uv],
                        'subs': [{'mat': slots[min(i, len(slots) - 1)], 'tris': t} for i, t in enumerate(M_['tris'])]})
    for g, m in mats.items():
        m['tex'] = {k: idx.get(tg) for k, tg in m['tex'].items() if idx.get(tg)}
    json.dump({'objects': objects, 'materials': mats, 'lights': lights, 'cameras': cams, 'ambient': ambient}, open(out, 'w'))
    print('exported', len(objects), 'objects,', len(mats), 'materials ->', out)


main()
