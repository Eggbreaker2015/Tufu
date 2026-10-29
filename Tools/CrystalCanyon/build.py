"""Write Crystal Canyon meshes, materials and scene into the Unity project. Re-runnable: guids persist in guids.json."""
import json, math, os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from meshlib import write_mesh, write_meta, new_guid, ASSETS
import shapes
import subprocess
from layout import build
from shapes import SWATCHES, guv

HERE = os.path.dirname(os.path.abspath(__file__))
A = ASSETS
ROOT = A + '/CrystalCanyon'
SCENE = A + '/Scenes/CrystalCanyon.unity'
GFILE = HERE + '/guids.json'
guids = json.load(open(GFILE)) if os.path.exists(GFILE) else {}


def guid(key):
    if key not in guids:
        guids[key] = new_guid()
    return guids[key]


def ensure_dir(path):
    if not os.path.isdir(path):
        os.makedirs(path)
    meta = path + '.meta'
    if not os.path.exists(meta):
        write_meta(meta, guid('dir:' + os.path.relpath(path, A)), 'folder')


def write_asset_meta(path, key):
    meta = path + '.meta'
    g = guid(key)
    if not os.path.exists(meta) or g not in open(meta).read():
        write_meta(meta, g, 'native')
    return g


ensure_dir(ROOT)
ensure_dir(ROOT + '/Meshes')
ensure_dir(ROOT + '/Materials')

# ------------------------------------------------------------------ materials
TEX_ATLAS = '05b8422524684bf48942175e1b71c8e5'   # TextureMain 2_1
TEX_GROUND1 = 'fbaf0525c25cb1c4e9a48ab98774cf20'  # Ground_1 (cracked stone with pebbles)
TEX_SAND = '0e246413af85dfd4298b3a9ea9b0654e'     # sandHandpaint (soft ripples)


def col(h, a=1.0):
    h = h.lstrip('#')
    return '{r: %.4f, g: %.4f, b: %.4f, a: %s}' % (int(h[0:2], 16) / 255, int(h[2:4], 16) / 255, int(h[4:6], 16) / 255, a)


def material(name, template, colors, floats=None, textures=None):
    s = open(A + '/Material/' + template + '.mat').read()
    s = re.sub(r'm_Name: .*', 'm_Name: ' + name, s, count=1)
    for k, v in colors.items():
        s, n = re.subn(r'(\n      %s: )\{[^}]*\}' % re.escape(k), lambda m: m.group(1) + v, s)
        assert n >= 1, (name, k)
    for k, v in (floats or {}).items():
        s, n = re.subn(r'(\n      %s: )[-\d.eE]+' % re.escape(k), lambda m: m.group(1) + str(v), s)
        assert n >= 1, (name, k)
    for k, g in (textures or {}).items():
        s, n = re.subn(r'(\n      %s:\n        m_Texture: )\{[^}]*\}' % re.escape(k), lambda m: m.group(1) + '{fileID: 2800000, guid: %s, type: 3}' % g, s)
        assert n == 1, (name, k)
    path = ROOT + '/Materials/' + name + '.mat'
    open(path, 'w').write(s)
    return write_asset_meta(path, 'mat:' + name)


MAT = {}
MAT['rock'] = material('CC_Rock', 'ArmyCampCliffs1',
                       {'_BaseColor': col('#c9a8b8'), '_SColor': col('#8a78b0'), '_HColor': col('#ffffff')}, textures={'_BaseMap': TEX_ATLAS})
MAT['crystal'] = material('CC_Crystal', 'VolcanicCrystal',
                          {'_BaseColor': col('#ffffff'), '_SColor': col('#b4c2ff'), '_HColor': col('#ffffff'), '_EmissionColor': col('#123a52')},
                          textures={'_BaseMap': TEX_ATLAS})
MAT['props'] = material('CC_Props', 'ArmyCampSide', {'_BaseColor': col('#ffffff'), '_SColor': col('#9a86c4')}, textures={'_BaseMap': TEX_ATLAS})
MAT['floor'] = material('CC_Floor', 'Grass', {'_MaskColor1': col('#7c6cb0'), '_MaskColor2': col('#d9cdf2')}, {'_UVTile': 0.09},
                        textures={'_Texture0': TEX_GROUND1})
MAT['top'] = material('CC_Top', 'Grass', {'_MaskColor1': col('#8a80c4'), '_MaskColor2': col('#e9e2ff')}, {'_UVTile': -0.06},
                      textures={'_Texture0': TEX_SAND})
ROAD_MAT = '61dc6b7219c41bc4ea5cdf40fa78acd3'  # original Volcano_Road 1: violet cobblestones
MAT['water'] = material('CC_Water', 'StylizedWater_Mobile',
                        {'_WaterColor': col('#16b7c9', 0), '_WaterShallowColor': col('#7dfff0'), '_FresnelColor': '{r: 0.75, g: 0.6, b: 1, a: 0.12}'})

MAT['road'] = ROAD_MAT

# ------------------------------------------------------------------ meshes
BLENDER_APP = '/Applications/Blender.app/Contents/MacOS/Blender'
use_blender = '--no-blender' not in sys.argv and os.path.exists(BLENDER_APP)
if use_blender:
    shapes.BLENDER = []
parts, edge, props = build()
if use_blender:
    # model the recorded rock / column / plateau / crystal pieces in Blender, then add them to their meshes
    from meshlib import MeshBuilder
    spec_path, out_path = HERE + '/.blender_specs.json', HERE + '/.blender_out.json'
    json.dump(shapes.BLENDER, open(spec_path, 'w'))
    r = subprocess.run([BLENDER_APP, '-b', '--factory-startup', '-P', HERE + '/blender_kit.py', '--', spec_path, out_path],
                       capture_output=True, text=True)
    log = [l for l in r.stdout.splitlines() if 'blender_kit' in l or 'Error' in l or 'Traceback' in l]
    print('\n'.join(log) or r.stdout[-2000:])
    if r.returncode != 0 or not os.path.exists(out_path):
        sys.exit(r.stderr[-3000:] or 'Blender failed')
    for t in json.load(open(out_path)):
        mb = MeshBuilder.registry[t[0]]
        sw = SWATCHES[t[2]]
        a, b_, c = tuple(t[3:6]), tuple(t[7:10]), tuple(t[11:14])
        mb.tri(t[1], a, b_, c, guv(sw, t[6]) if t[1] == 0 else (0.5, 0.5), guv(sw, t[10]) if t[1] == 0 else (0.5, 0.5),
               guv(sw, t[14]) if t[1] == 0 else (0.5, 0.5), tuple(t[15:18]))
    os.remove(spec_path); os.remove(out_path)
MESH_MATS = {  # mesh name -> materials per submesh
    'Plateaus': ['rock', 'top'], 'Stairs': ['rock'], 'Spires': ['rock', 'top'],
    'Rocks': ['rock'], 'Crystals': ['crystal'], 'Grass': ['props'], 'Pool': ['water'], 'Roads': ['road'],
}
meshes = {}
total_v = 0
for name, mb in parts.items():
    mats = MESH_MATS.get(name, ['rock', 'top'])
    path = ROOT + '/Meshes/CC_' + name + '.asset'
    nv, nt = write_mesh(path, 'CC_' + name, mb)
    total_v += nv
    meshes[name] = (write_asset_meta(path, 'mesh:' + name), mats)
    print('%-12s %6d verts %6d tris' % (name, nv, nt))
print('total verts', total_v)

# ------------------------------------------------------------------ scene
docs = []
_next = [100]


def fid():
    _next[0] += 1
    return _next[0]


def gameobject(name, comps, static=True, layer=0, tag='Untagged'):
    gid = fid()
    lines = ''.join('  - component: {fileID: %d}\n' % c for c in comps)
    docs.append((1, gid, 'GameObject:\n  serializedVersion: 6\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
                         '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Component:\n%s  m_Layer: %d\n  m_Name: %s\n'
                         '  m_TagString: %s\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: %d\n  m_IsActive: 1\n'
                 % (lines, layer, name, tag, 4294967295 if static else 0)))
    return gid


def transform(tid, gid, pos=(0, 0, 0), rot=(0, 0, 0, 1), scale=(1, 1, 1), parent=0, children=(), euler=(0, 0, 0)):
    ch = ''.join('  - {fileID: %d}\n' % c for c in children)
    docs.append((4, tid, 'Transform:\n  serializedVersion: 2\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
                         '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n'
                         '  m_LocalRotation: {x: %s, y: %s, z: %s, w: %s}\n  m_LocalPosition: {x: %s, y: %s, z: %s}\n'
                         '  m_LocalScale: {x: %s, y: %s, z: %s}\n  m_ConstrainProportionsScale: 0\n  m_Children:%s\n  m_Father: {fileID: %d}\n'
                         '  m_LocalEulerAnglesHint: {x: %s, y: %s, z: %s}\n'
                 % ((gid,) + tuple(rot) + tuple(pos) + tuple(scale) + ((' []' if not children else '\n' + ch.rstrip('\n')), parent) + tuple(euler))))


MESH_RENDERER = open(HERE + '/meshrenderer.tmpl').read()


def mesh_object(name, mesh_ref, mat_guids, parent_tid, pos=(0, 0, 0), scale=(1, 1, 1), collider=False, rot=(0, 0, 0, 1)):
    tid, mf, mr = fid(), fid(), fid()
    comps = [tid, mf, mr]
    bc = None
    if collider:
        bc = fid(); comps.append(bc)
    gid = gameobject(name, comps)
    transform(tid, gid, pos=pos, rot=rot, scale=scale, parent=parent_tid)
    docs.append((33, mf, 'MeshFilter:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n'
                         '  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  m_Mesh: %s\n' % (gid, mesh_ref)))
    mats = ''.join('  - {fileID: 2100000, guid: %s, type: 2}\n' % g for g in mat_guids)
    docs.append((23, mr, MESH_RENDERER.replace('{GO}', str(gid)).replace('{MATS}', mats.rstrip('\n'))))
    if bc:
        docs.append((65, bc, 'BoxCollider:\n  serializedVersion: 3\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
                              '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  m_Material: {fileID: 0}\n'
                              '  m_IncludeLayers:\n    serializedVersion: 2\n    m_Bits: 0\n  m_ExcludeLayers:\n    serializedVersion: 2\n    m_Bits: 0\n'
                              '  m_LayerOverridePriority: 0\n  m_IsTrigger: 0\n  m_ProvidesContacts: 0\n  m_Enabled: 1\n  m_Size: {x: 1, y: 1, z: 1}\n'
                              '  m_Center: {x: 0, y: 0, z: 0}\n' % gid))
    return tid


def group(name, parent_tid, build_children):
    tid = fid()
    gid = gameobject(name, [tid])
    kids = build_children(tid)
    transform(tid, gid, parent=parent_tid, children=kids)
    return tid


# Props reused from Main.unity: mesh, materials and world rotation/scale of one instance each (see props.json).
PROPS = json.load(open(HERE + '/props.json'))
ARMYCAMP_SIDE = 'f2a2bb29fa25ebe47a78eeecd4de2400'


def qmul(a, b):
    ax, ay, az, aw = a; bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz)


def qrot(q, v):
    r = qmul(qmul(q, (v[0], v[1], v[2], 0.0)), (-q[0], -q[1], -q[2], q[3]))
    return r[:3]


def ry(deg):
    t = math.radians(deg) / 2
    return (0.0, math.sin(t), 0.0, math.cos(t))


def long_axis_angle(d):
    """World heading (deg, 0 = +z, 90 = +x) of the prop's longest horizontal side as it sits in Main.unity."""
    sc = d['scale']; R = d['rot']
    s = open(A + '/' + d['mesh_path'], errors='ignore').read()
    m = re.findall(r'm_LocalAABB:\n    m_Center: .*\n    m_Extent: \{x: ([-\d.eE]+), y: ([-\d.eE]+), z: ([-\d.eE]+)\}', s)[-1]
    e = [float(x) for x in m]
    best = None
    for i in range(3):
        v = [0.0, 0.0, 0.0]; v[i] = e[i] * sc[i]
        w = qrot(R, v)
        hl = math.hypot(w[0], w[2])
        if best is None or hl > best[0]:
            best = (hl, math.degrees(math.atan2(w[0], w[2])))
    return best[1]


STONE_PROPS = ('ruin_wall', 'ruin_arch', 'ruin_tall', 'rubble', 'rock_decal')  # stone props take the canyon rock colour


def mats_of(d, key=''):
    out = []
    for m in d['mats']:
        g = re.search(r'guid: (\w+)', m).group(1)
        out.append((MAT['rock'] if key in STONE_PROPS else MAT['props']) if g == ARMYCAMP_SIDE else g)
    return out


def place(key, x, z, kw, parent, name=None):
    d = PROPS[key]
    k = kw.get('scale', 1.0)
    theta = kw['along'] - long_axis_angle(d) if 'along' in kw else kw.get('yaw', 0.0)
    q = qmul(ry(theta), tuple(d['rot']))
    c = qrot(ry(theta), [v * k for v in d['wcenter']])
    bottom = d['pos'][1] + d['wmin_y']
    sink = 0.0 if key == 'dock' else max(-2.5, min(0.0, bottom - 0.05))
    ground = kw.get('y', 0.05)
    y = ground + sink * k - d['wmin_y'] * k + kw.get('lift', 0.0)
    return mesh_object(name or key, d['mesh'], mats_of(d, key), parent, pos=(x - c[0], y, z - c[2]), rot=q, scale=[v * k for v in d['scale']])


def place_gate(gx, gz, yaw, parent):
    tid = fid()
    gid = gameobject('Gate', [tid])
    doors = PROPS['gate_doors']
    kids = []
    for part in ('gate_doors', 'gate_left', 'gate_right'):
        d = PROPS[part]
        off = [d['pos'][i] - doors['pos'][i] for i in range(3)]
        o = qrot(ry(yaw), off)
        q = qmul(ry(yaw), tuple(d['rot']))
        kids.append(mesh_object(part, d['mesh'], mats_of(d), tid, pos=(o[0], d['pos'][1] - 0.05 + 0.0, o[2]), rot=q, scale=d['scale']))
    transform(tid, gid, pos=(gx, 0.05, gz), parent=parent, children=kids)
    return tid


def props_group(tid):
    kids = []
    for i, (key, x, z, kw) in enumerate(props):
        if key == 'gate':
            kids.append(place_gate(x, z, kw.get('yaw', 0.0), tid))
        else:
            kids.append(place(key, x, z, kw, tid, name='%s (%d)' % (key, i)))
    return kids


root_tid = fid()
root_gid = gameobject('CrystalCanyon', [root_tid])


def ground(tid):
    kids = []
    i = 0
    for gx in (-45, 0, 45):
        for gz in (-67.5, -22.5, 22.5, 67.5):
            i += 1
            kids.append(mesh_object('Floor (%d)' % i, '{fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}', [MAT['floor']],
                                    tid, pos=(gx, 0, gz), scale=(45, 0.1, 45), collider=True))
    return kids


def terrain(tid):
    kids = []
    for name in ['Plateaus', 'Stairs', 'Spires'] + sorted(k for k in meshes if k.startswith('Wall_')):
        g, mats = meshes[name]
        kids.append(mesh_object(name, '{fileID: 4300000, guid: %s, type: 2}' % g, [MAT[m] for m in mats], tid))
    return kids


def dressing(tid):
    kids = []
    for name in ['Roads', 'Rocks', 'Crystals', 'Grass', 'Pool']:
        g, mats = meshes[name]
        kids.append(mesh_object(name, '{fileID: 4300000, guid: %s, type: 2}' % g, [MAT[m] for m in mats], tid))
    return kids


root_kids = [group('Ground', root_tid, ground), group('Terrain', root_tid, terrain), group('Dressing', root_tid, dressing),
             group('Props', root_tid, props_group)]
transform(root_tid, root_gid, children=root_kids)

# directional light (same as Main.unity's sun) and a game-like camera at the south entrance
light_tid, light_c = fid(), fid()
light_gid = gameobject('Directional Light', [light_tid, light_c], static=False)
transform(light_tid, light_gid, pos=(0, 30, 0), rot=(-0.295, -0.735, 0.257, -0.554))
docs.append((108, light_c, open(HERE + '/light.tmpl').read().replace('{GO}', str(light_gid))))

cam_tid, cam_c, cam_al = fid(), fid(), fid()
cam_gid = gameobject('Main Camera', [cam_tid, cam_c, cam_al], static=False, tag='MainCamera')
# look at (0, 0, -38) from 35 m away, pitched 60 degrees down, facing north

pitch = math.radians(60)
target = (0.0, 0.0, -38.0)
pos = (target[0], target[1] + 35 * math.sin(pitch), target[2] - 35 * math.cos(pitch))
q = (math.sin(pitch / 2), 0.0, 0.0, math.cos(pitch / 2))
transform(cam_tid, cam_gid, pos=pos, rot=q, euler=(60, 0, 0))
docs.append((20, cam_c, open(HERE + '/camera.tmpl').read().replace('{GO}', str(cam_gid))))
docs.append((81, cam_al, 'AudioListener:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n'
                         '  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n' % cam_gid))

header = open(HERE + '/settings.tmpl').read().replace('{SUN}', str(light_c))
body = ''.join('--- !u!%d &%d\n%s' % (cls, i, text) for cls, i, text in docs)
open(SCENE, 'w').write('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n' + header + body)
if not os.path.exists(SCENE + '.meta'):
    write_meta(SCENE + '.meta', guid('scene'), 'scene')
json.dump(guids, open(GFILE, 'w'), indent=1)
print('scene docs', len(docs), 'written', SCENE)
