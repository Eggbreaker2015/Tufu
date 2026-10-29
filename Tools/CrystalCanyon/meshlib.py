"""Low-poly mesh building and Unity YAML Mesh (.asset) writing."""
import math, random, struct, uuid, time, re, os

ASSETS = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../Recovery/StudyProject/Assets'))
# An uncompressed mesh from the original game, used as the YAML skeleton for every generated mesh.
TEMPLATE = ASSETS + '/Mesh/DetailrOcksCove_0.asset'


def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def mul(a, s): return (a[0] * s, a[1] * s, a[2] * s)
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def norm(a):
    l = math.sqrt(dot(a, a))
    return (a[0] / l, a[1] / l, a[2] / l) if l > 1e-12 else (0.0, 1.0, 0.0)


class MeshBuilder:
    """Flat-shaded: every triangle owns its 3 vertices. Front face: normal = cross(b-a, c-a) (Unity)."""

    def __init__(self, nsub):
        self.subs = [[] for _ in range(nsub)]  # list of (pos, nrm, uv) * 3

    def tri(self, s, a, b, c, ua, ub, uc, out=None):
        n = cross(sub(b, a), sub(c, a))
        if dot(n, n) < 1e-14:
            return
        if out is not None and dot(n, out) < 0:
            b, c = c, b
            ub, uc = uc, ub
            n = mul(n, -1)
        n = norm(n)
        self.subs[s].append(((a, n, ua), (b, n, ub), (c, n, uc)))

    def quad(self, s, a, b, c, d, ua, ub, uc, ud, out):
        # a-b-c-d around the quad; split along the shorter diagonal
        if math.dist(a, c) <= math.dist(b, d):
            self.tri(s, a, b, c, ua, ub, uc, out)
            self.tri(s, a, c, d, ua, uc, ud, out)
        else:
            self.tri(s, a, b, d, ua, ub, ud, out)
            self.tri(s, b, c, d, ub, uc, ud, out)

    def poly_xz(self, s, pts, y, uv, up=True):
        """Flat horizontal polygon (list of (x,z)) at height y, ear-clipped."""
        for i, j, k in earclip(pts):
            a = (pts[i][0], y, pts[i][1]); b = (pts[j][0], y, pts[j][1]); c = (pts[k][0], y, pts[k][1])
            self.tri(s, a, b, c, uv, uv, uv, (0, 1 if up else -1, 0))

    def merge(self, other):
        for i, s in enumerate(other.subs):
            self.subs[i].extend(s)

    def vcount(self):
        return sum(len(s) * 3 for s in self.subs)


def area2(pts):
    return sum(pts[i][0] * pts[(i + 1) % len(pts)][1] - pts[(i + 1) % len(pts)][0] * pts[i][1] for i in range(len(pts)))


def earclip(pts):
    n = len(pts)
    idx = list(range(n))
    if area2(pts) < 0:
        idx.reverse()
    out = []
    guard = 0
    while len(idx) > 3 and guard < 10000:
        guard += 1
        for t in range(len(idx)):
            i0, i1, i2 = idx[t - 1], idx[t], idx[(t + 1) % len(idx)]
            a, b, c = pts[i0], pts[i1], pts[i2]
            cr = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
            if cr <= 1e-9:
                continue
            ok = True
            for j in idx:
                if j in (i0, i1, i2):
                    continue
                if in_tri(pts[j], a, b, c):
                    ok = False
                    break
            if ok:
                out.append((i0, i1, i2))
                idx.pop(t)
                break
        else:
            break
    if len(idx) == 3:
        out.append(tuple(idx))
    return out


def in_tri(p, a, b, c):
    def s(p1, p2, p3): return (p1[0] - p3[0]) * (p2[1] - p3[1]) - (p2[0] - p3[0]) * (p1[1] - p3[1])
    d1, d2, d3 = s(p, a, b), s(p, b, c), s(p, c, a)
    return not ((d1 < 0 or d2 < 0 or d3 < 0) and (d1 > 0 or d2 > 0 or d3 > 0))


def point_in_poly(p, pts):
    x, z = p
    inside = False
    for i in range(len(pts)):
        x1, z1 = pts[i]; x2, z2 = pts[(i + 1) % len(pts)]
        if (z1 > z) != (z2 > z) and x < (x2 - x1) * (z - z1) / (z2 - z1) + x1:
            inside = not inside
    return inside


def offset_poly(pts, d):
    """Move each vertex of a CCW polygon (x,z) outward by d (negative = inward) along the corner bisector."""
    n = len(pts)
    ccw = area2(pts) > 0
    out = []
    for i in range(n):
        p0, p1, p2 = pts[i - 1], pts[i], pts[(i + 1) % n]
        def en(a, b):
            ex, ez = b[0] - a[0], b[1] - a[1]
            l = math.hypot(ex, ez) or 1
            # outward normal for CCW (x right, z up): (ez, -ex)
            return (ez / l, -ex / l) if ccw else (-ez / l, ex / l)
        n1, n2 = en(p0, p1), en(p1, p2)
        bx, bz = n1[0] + n2[0], n1[1] + n2[1]
        bl = math.hypot(bx, bz) or 1
        bx, bz = bx / bl, bz / bl
        cosh = max(0.35, bx * n1[0] + bz * n1[1])
        out.append((p1[0] + bx * d / cosh, p1[1] + bz * d / cosh))
    return out


# ---------------------------------------------------------------- writing

def write_mesh(path, name, mb):
    tmpl = open(TEMPLATE).read()
    verts = []
    indices = []
    submeshes = []
    for s in mb.subs:
        first_v = len(verts)
        first_i = len(indices)
        for t in s:
            for v in t:
                indices.append(len(verts))
                verts.append(v)
        submeshes.append((first_i, len(indices) - first_i, first_v, len(verts) - first_v))
    assert len(verts) < 65535, (name, len(verts))
    stride = 48
    data = bytearray()
    for p, n, uv in verts:
        data += struct.pack('<3f3f4f2f', p[0], p[1], p[2], n[0], n[1], n[2], 1.0, 0.0, 0.0, 1.0, uv[0], uv[1])
    ib = b''.join(struct.pack('<H', i) for i in indices)

    def aabb(vs):
        if not vs:
            return (0, 0, 0), (0, 0, 0)
        xs = [v[0][0] for v in vs]; ys = [v[0][1] for v in vs]; zs = [v[0][2] for v in vs]
        mn = (min(xs), min(ys), min(zs)); mx = (max(xs), max(ys), max(zs))
        return tuple((mn[i] + mx[i]) / 2 for i in range(3)), tuple((mx[i] - mn[i]) / 2 for i in range(3))

    def f(x): return repr(round(float(x), 6))

    sm_yaml = ''
    for fi, ic, fv, vc in submeshes:
        c, e = aabb(verts[fv:fv + vc])
        sm_yaml += ('  - serializedVersion: 2\n    firstByte: %d\n    indexCount: %d\n    topology: 0\n    baseVertex: 0\n'
                    '    firstVertex: %d\n    vertexCount: %d\n    localAABB:\n      m_Center: {x: %s, y: %s, z: %s}\n'
                    '      m_Extent: {x: %s, y: %s, z: %s}\n') % (fi * 2, ic, fv, vc, f(c[0]), f(c[1]), f(c[2]), f(e[0]), f(e[1]), f(e[2]))
    c, e = aabb(verts)
    s = tmpl
    s = re.sub(r'm_Name: .*', 'm_Name: ' + name, s, count=1)
    s = re.sub(r'  m_SubMeshes:\n(?:  - serializedVersion: 2\n(?:    .*\n|      .*\n)+)', '  m_SubMeshes:\n' + sm_yaml, s, count=1)
    s = re.sub(r'm_IndexBuffer: \w*', 'm_IndexBuffer: ' + ib.hex(), s, count=1)
    s = re.sub(r'm_VertexCount: \d+', 'm_VertexCount: %d' % len(verts), s, count=1)
    s = re.sub(r'm_DataSize: \d+', 'm_DataSize: %d' % len(data), s, count=1)
    s = re.sub(r'_typelessdata: \w*', '_typelessdata: ' + data.hex(), s, count=1)
    s = re.sub(r'  m_LocalAABB:\n    m_Center: .*\n    m_Extent: .*\n',
               '  m_LocalAABB:\n    m_Center: {x: %s, y: %s, z: %s}\n    m_Extent: {x: %s, y: %s, z: %s}\n'
               % (f(c[0]), f(c[1]), f(c[2]), f(e[0]), f(e[1]), f(e[2])), s, count=1)
    lod = ''.join('    - serializedVersion: 2\n      m_Levels:\n      - m_IndexStart: 0\n        m_IndexCount: 0\n' for _ in submeshes)
    s = re.sub(r'(    m_NumLevels: 1\n    m_SubMeshes:\n)(?:    - serializedVersion: 2\n      m_Levels:\n      - m_IndexStart: \d+\n        m_IndexCount: \d+\n)+',
               lambda m: m.group(1) + lod, s, count=1)
    assert s.count('m_IndexBuffer: ' + ib.hex()) == 1
    open(path, 'w').write(s)
    return len(verts), sum(x[1] for x in submeshes) // 3


def new_guid():
    return uuid.uuid4().hex


def write_meta(path, guid, kind):
    t = int(time.time())
    if kind == 'native':
        body = ('NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: %s\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
        body = body % ('4300000' if path.endswith('.asset.meta') else '2100000')
    elif kind == 'folder':
        body = 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    elif kind == 'scene':
        body = 'DefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    open(path, 'w').write('fileFormatVersion: 2\nguid: %s\ntimeCreated: %d\nlicenseType: Free\n%s' % (guid, t, body))
