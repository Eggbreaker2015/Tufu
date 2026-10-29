"""Model the Crystal Canyon rock, column, plateau and crystal pieces in Blender (run headless by build.py).

    Blender -b --factory-startup -P blender_kit.py -- specs.json out.json

specs.json is a list of shape specs recorded by shapes.py (positions in Unity space: x east, y up, z north).
out.json gets one flat-shaded triangle list per spec: [builder_id, submesh, swatch, a(x,y,z,t), b(...), c(...), normal(x,y,z)],
where t (0 bottom .. 1 top) picks the palette gradient. Blender is Z-up, so Unity (x, y, z) <-> Blender (x, z, y).
"""
import bpy, bmesh, json, math, random, sys
from mathutils import Vector, Matrix, Quaternion, noise


def to_b(p):  # Unity -> Blender
    return Vector((p[0], p[2], p[1]))


def to_u(v):  # Blender -> Unity
    return (round(v.x, 5), round(v.z, 5), round(v.y, 5))


def clear_scene():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        bpy.data.meshes.remove(me)


def evaluate(bm, modifiers=()):
    """Turn a bmesh into triangles (Blender space) after applying modifiers; returns [(a, b, c, normal)]."""
    me = bpy.data.meshes.new('piece')
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new('piece', me)
    bpy.context.scene.collection.objects.link(ob)
    for setup in modifiers:
        setup(ob)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    eo = ob.evaluated_get(dg)
    em = eo.to_mesh()
    em.calc_loop_triangles()
    tris = []
    for lt in em.loop_triangles:
        a, b, c = (em.vertices[i].co.copy() for i in lt.vertices)
        n = lt.normal.copy()
        if n.length < 1e-9:
            continue
        tris.append((a, b, c, n))
    eo.to_mesh_clear()
    bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.meshes.remove(me)
    return tris


def decimate(ratio, group=None):
    def setup(ob):
        m = ob.modifiers.new('dec', 'DECIMATE')
        m.decimate_type = 'COLLAPSE'
        m.ratio = ratio
        if group:
            m.vertex_group = group
            m.vertex_group_factor = 1.0
    return setup


# ---------------------------------------------------------------- plateau: layered canyon cliff with a flat top

def ccw(pts):
    a = sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))
    return pts if a > 0 else list(reversed(pts))


def resample(pts, step):
    out = []
    n = len(pts)
    for i in range(n):
        a, b = Vector(pts[i]), Vector(pts[(i + 1) % n])
        k = max(1, int((b - a).length / step))
        for j in range(k):
            out.append(a.lerp(b, j / k))
    return out


def normals2d(P):
    n = len(P)
    out = []
    for i in range(n):
        d = (P[(i + 1) % n] - P[i - 1])
        nv = Vector((d.y, -d.x))
        out.append(nv.normalized() if nv.length else Vector((1, 0)))
    return out


def plateau(spec, rng):
    pts = ccw([(p[0], p[1]) for p in spec['pts']])  # Unity (x, z) == Blender (x, y)
    h, base = spec['h'], spec['base']
    P = resample(pts, 1.8)
    N = normals2d(P)
    # stair notches: cliff points in front of (or just behind) a staircase's top end are moved onto one flat plane
    # there and get no noise, strata or chamfer, so the top tread meets a clean straight edge
    flat = [False] * len(P)
    for nt in spec.get('notches', []):
        d = Vector((nt['dx'], nt['dz'])).normalized()
        side = Vector((d.y, -d.x))
        o = Vector((nt['x0'], nt['z0']))
        for i, p in enumerate(P):
            s, lat = (p - o).dot(d), (p - o).dot(side)
            if abs(lat) < nt['width'] / 2 + 1.0 and s < nt['run'] + 4.0:
                P[i] = p + d * (nt['run'] - s)
                flat[i] = True
    seed = Vector((rng.uniform(0, 100), rng.uniform(0, 100), rng.uniform(0, 100)))
    levels = [base, 0.0]
    z = 1.1
    while z < h - 1.0:
        levels.append(z)
        z += 1.1
    levels += [h - 0.45, h]
    bm = bmesh.new()
    dl = bm.verts.layers.deform.verify()
    rings = []
    for li, z in enumerate(levels):
        ring = []
        for pi, (p, nrm) in enumerate(zip(P, N)):
            if flat[pi]:
                off = 0.0
            elif li == len(levels) - 1:
                off = -0.85                      # chamfered rim of the flat top
            elif li == len(levels) - 2:
                off = -0.3
            else:
                band = math.floor((z - 0.0) / 1.6)  # rock strata: each band steps in a little
                n = noise.noise(Vector((p.x * 0.22, p.y * 0.22, z * 0.45)) + seed)
                off = 0.75 * n - 0.18 * band + (0.35 if li == 0 else 0.0)
            q = p + nrm * off
            v = bm.verts.new((q.x, q.y, z))
            v[dl][0] = 0.0 if (li >= len(levels) - 2 or flat[pi]) else 1.0
            ring.append(v)
        rings.append(ring)
    n = len(P)
    for li in range(len(rings) - 1):
        A, B = rings[li], rings[li + 1]
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((A[i], A[j], B[j], B[i]))
    bm.faces.new(rings[-1])
    bm.normal_update()
    # the deform weights above belong to vertex group 0: sides weight 1 (decimated), rim and top weight 0 (kept flat)
    tris = evaluate(bm, [lambda ob: ob.vertex_groups.new(name='side'), decimate(0.33, 'side')])
    out = []
    for a, b, c, nr in tris:
        flat_top = nr.z > 0.995 and min(a.z, b.z, c.z) > h - 0.01
        out.append((1 if flat_top else 0, spec['sw'], (a, b, c), nr, lambda v: (v.z - base) / (h - base)))
    return out


# ---------------------------------------------------------------- column: basalt prism with joints, chamfers, some broken tops

def column(spec, rng):
    cx, cz, r, h, base = spec['x'], spec['z'], spec['r'], spec['h'], spec['base']
    sides = spec['sides']
    rot = rng.uniform(0, math.tau)
    radii = [r * rng.uniform(0.88, 1.1) for _ in range(sides)]
    joint = base + (h - base) * rng.uniform(0.35, 0.7)
    js = rng.uniform(0.94, 1.03)
    broken = h > 5 and rng.random() < 0.3 and not spec.get('whole')
    tilt_axis = Vector((math.cos(rot + 0.7), math.sin(rot + 0.7), 0))
    tilt = math.radians(rng.uniform(8, 16)) if broken else 0.0
    bev = min(0.8, r * 0.18)
    ring_defs = [(base, 1.02), (joint, js), (h - bev, js * 0.98), (h, js * 0.98 - bev / r * 0.9)]
    bm = bmesh.new()
    rings = []
    top_c = Vector((cx, cz, h))
    for zi, (z, k) in enumerate(ring_defs):
        ring = []
        for i in range(sides):
            a = rot + math.tau * i / sides
            p = Vector((cx + math.cos(a) * radii[i] * k, cz + math.sin(a) * radii[i] * k, z))
            if broken and zi >= 2:
                p = top_c + Matrix.Rotation(tilt, 3, tilt_axis) @ (p - top_c)
            ring.append(bm.verts.new(p))
        rings.append(ring)
    for li in range(len(rings) - 1):
        A, B = rings[li], rings[li + 1]
        for i in range(sides):
            j = (i + 1) % sides
            bm.faces.new((A[i], A[j], B[j], B[i]))
    bm.faces.new(rings[-1])
    bm.normal_update()
    # chamfer the vertical edges so every column catches a thin highlight
    if spec.get('detail', True):
        vertical = [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 0.3 and (e.verts[0].co.xy - e.verts[1].co.xy).length < r * 0.6]
        bmesh.ops.bevel(bm, geom=vertical, offset=r * 0.07, segments=1, affect='EDGES', profile=0.5)
    tris = evaluate(bm)
    out = []
    for a, b, c, nr in tris:
        flat_top = (not broken) and nr.z > 0.995 and min(a.z, b.z, c.z) > h - 0.01
        out.append((1 if flat_top else 0, spec['sw'], (a, b, c), nr, lambda v: (v.z - base) / (h - base)))
    return out


# ---------------------------------------------------------------- boulder: noisy icosphere, decimated into big facets

def boulder(spec, rng):
    size = spec['size']
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=4, radius=1.0)
    seed = Vector((rng.uniform(0, 100), rng.uniform(0, 100), rng.uniform(0, 100)))
    sx, sy, sz = rng.uniform(0.9, 1.25), rng.uniform(0.8, 1.1), rng.uniform(0.55, 0.8)
    yaw = rng.uniform(0, math.tau)
    R = Matrix.Rotation(yaw, 3, 'Z')
    for v in bm.verts:
        d = noise.noise(v.co * 1.3 + seed)
        v.co *= 1 + 0.32 * d
        v.co = R @ Vector((v.co.x * sx, v.co.y * sy, v.co.z * sz))
        v.co.z = max(v.co.z, -0.3)
    tris = evaluate(bm, [decimate(0.06)])
    x, z, y0 = spec['x'], spec['z'], spec['y0']
    out = []
    lo, hi = -0.3 * size, max(max(a.z, b.z, c.z) for a, b, c, _ in tris) * size
    off = Vector((x, z, y0 + 0.12 * size))
    for a, b, c, nr in tris:
        A, B, C = a * size + off, b * size + off, c * size + off
        out.append((0, spec['sw'], (A, B, C), nr, lambda v, o=off: (v.z - o.z - lo) / (hi - lo)))
    return out


# ---------------------------------------------------------------- crystal: prism with an uneven, faceted point

def crystal(spec, rng):
    base, d = to_b(spec['base']), to_b(spec['dir']).normalized()
    r, L = spec['radius'], spec['length']
    sides = spec['sides']
    rot = rng.uniform(0, math.tau)
    tip = L * rng.uniform(0.24, 0.36)
    body = L - tip
    radii = [r * rng.uniform(0.85, 1.12) for _ in range(sides)]
    sink = r * 0.8
    bm = bmesh.new()
    lo = [bm.verts.new((math.cos(rot + math.tau * i / sides) * radii[i] * 0.9, math.sin(rot + math.tau * i / sides) * radii[i] * 0.9, -sink))
          for i in range(sides)]
    sh = [bm.verts.new((math.cos(rot + math.tau * i / sides) * radii[i], math.sin(rot + math.tau * i / sides) * radii[i],
                        body - sink + rng.uniform(-0.12, 0.12) * tip)) for i in range(sides)]
    apex = bm.verts.new((rng.uniform(-0.12, 0.12) * r, rng.uniform(-0.12, 0.12) * r, L - sink))
    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((lo[i], lo[j], sh[j], sh[i]))
        bm.faces.new((sh[i], sh[j], apex))
    bm.faces.new(list(reversed(lo)))
    bm.normal_update()
    q = Vector((0, 0, 1)).rotation_difference(d)
    M = q.to_matrix()
    tris = evaluate(bm)
    out = []
    for a, b, c, nr in tris:
        A, B, C = M @ a + base, M @ b + base, M @ c + base
        out.append((0, spec['sw'], (A, B, C), M @ nr, lambda v: max(0.0, (v - base).dot(d) + sink) / L))
    return out


BUILDERS = {'plateau': plateau, 'column': column, 'rock': boulder, 'shard': crystal}


def main():
    argv = sys.argv[sys.argv.index('--') + 1:]
    specs = json.load(open(argv[0]))
    clear_scene()
    result = []
    counts = {}
    for spec in specs:
        rng = random.Random(spec['seed'])
        for sub, sw, (a, b, c), nr, tf in BUILDERS[spec['kind']](spec, rng):
            result.append([spec['mb'], sub, sw] + list(to_u(a)) + [round(tf(a), 4)] + list(to_u(b)) + [round(tf(b), 4)]
                          + list(to_u(c)) + [round(tf(c), 4)] + list(to_u(nr)))
        counts[spec['kind']] = counts.get(spec['kind'], 0) + 1
    json.dump(result, open(argv[1], 'w'))
    print('blender_kit: built', counts, 'triangles', len(result))


main()
