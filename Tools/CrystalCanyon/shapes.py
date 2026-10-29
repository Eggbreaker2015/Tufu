"""Low-poly shape generators for the crystal canyon. Submesh convention for terrain: 0 = rock sides, 1 = ground top."""
import math, random
from meshlib import MeshBuilder, norm, sub, add, mul, cross, dot, offset_poly

# Palette atlas swatches (TextureMain 2_1): (u, v_dark, v_light)
ROCK = (0.36, 0.26, 0.49)        # periwinkle gradient
ROCK_DARK = (0.094, 0.26, 0.49)  # slate gradient
STEP = (0.36, 0.40, 0.49)        # light end of the periwinkle gradient
CYAN = (0.484, 0.78, 0.99)       # teal -> cyan -> white
TEAL = (0.918, 0.33, 0.49)       # deep teal -> cyan
PINK = (0.891, 0.55, 0.74)
VIOLET = (0.985, 0.55, 0.74)
GOLD = (0.828, 0.77, 0.99)
TOP_UV = (0.5, 0.5)

SIDE, TOP = 0, 1
SWATCHES = {'ROCK': ROCK, 'ROCK_DARK': ROCK_DARK, 'STEP': STEP, 'CYAN': CYAN, 'TEAL': TEAL, 'PINK': PINK, 'VIOLET': VIOLET, 'GOLD': GOLD}
SWATCH_NAME = {v: k for k, v in SWATCHES.items()}

# When this is a list, plateaus, columns, boulders and crystals are recorded here as specs and modelled in
# Blender (blender_kit.py) instead of being built below; stairs, roads, grass and water are always built here.
BLENDER = None


def record(mb, kind, rng, **spec):
    spec.update(kind=kind, mb=mb.id, seed=rng.random())
    BLENDER.append(spec)


def guv(sw, t):
    t = max(0.0, min(1.0, t))
    return (sw[0], sw[1] + (sw[2] - sw[1]) * t)


def ring(pts, y):
    return [(p[0], y, p[1]) for p in pts]


def side_band(mb, lo, hi, tlo, thi, sw, centre, s=SIDE):
    """Quads between two rings (same vertex count). tlo/thi: gradient position."""
    n = len(lo)
    for i in range(n):
        j = (i + 1) % n
        a, b, c, d = lo[i], lo[j], hi[j], hi[i]
        mid = mul(add(add(a, b), add(c, d)), 0.25)
        out = sub(mid, (centre[0], mid[1], centre[1]))
        mb.quad(s, a, b, c, d, guv(sw, tlo), guv(sw, tlo), guv(sw, thi), guv(sw, thi), out)


def centroid(pts):
    return (sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts))


def mesa(mb, pts, h, rng, base=-1.0, sw=ROCK, bevel=0.7, jitter=0.6, rows=2, top_sub=TOP, cap=True, notches=()):
    """Faceted cliff block from a footprint polygon (x,z): rough sides, bevelled rim, flat top at y=h."""
    if BLENDER is not None and cap:
        record(mb, 'plateau', rng, pts=[list(p) for p in pts], h=h, base=base, sw=SWATCH_NAME[sw], notches=list(notches))
        return offset_poly(pts, -0.85)
    c = centroid(pts)
    rings = [ring(pts, base)]
    cur = pts
    for r in range(1, rows):
        y = base + (h - bevel - base) * r / rows
        cur = [(p[0] + rng.uniform(-jitter, jitter), p[1] + rng.uniform(-jitter, jitter)) for p in pts]
        cur = offset_poly(cur, rng.uniform(-0.2, 0.5))
        rings.append(ring(cur, y))
    rim = offset_poly(pts, -0.15)
    rings.append(ring(rim, h - bevel))
    top = offset_poly(pts, -0.15 - bevel * 0.9)
    rings.append(ring(top, h))
    total = h - base
    for k in range(len(rings) - 1):
        t0 = (rings[k][0][1] - base) / total
        t1 = (rings[k + 1][0][1] - base) / total
        side_band(mb, rings[k], rings[k + 1], t0, t1, sw, c)
    if cap:
        mb.poly_xz(top_sub, top, h, TOP_UV if top_sub == TOP else guv(sw, 1.0))
    return top


def column(mb, cx, cz, r, h, rng, sides=None, base=-1.0, sw=ROCK, top_sub=TOP, detail=True, whole=False):
    sides = sides or rng.choice([5, 6, 6, 7])
    if BLENDER is not None:
        record(mb, 'column', rng, x=cx, z=cz, r=r, h=h, sides=sides, base=base, sw=SWATCH_NAME[sw], detail=detail, whole=whole)
        return [(cx + math.cos(math.tau * i / sides) * r * 0.8, cz + math.sin(math.tau * i / sides) * r * 0.8) for i in range(sides)], h
    rot = rng.uniform(0, math.tau)
    pts = []
    for i in range(sides):
        a = rot + math.tau * i / sides
        rr = r * rng.uniform(0.85, 1.12)
        pts.append((cx + math.cos(a) * rr, cz + math.sin(a) * rr))
    # columns taper slightly and have a crisp bevel
    return mesa(mb, pts, h, rng, base=base, sw=sw, bevel=min(0.9, r * 0.22), jitter=r * 0.06, rows=2, top_sub=top_sub), h


def stairs(mb, x0, z0, dx, dz, width, h, run, rng, sw=ROCK, top_sw=STEP, y0=0.0):
    """Stone steps climbing from floor (at x0,z0) along direction (dx,dz) to height h over horizontal length run."""
    steps = max(4, int(round(h / 0.45)))
    d = norm((dx, 0, dz)); side = (d[2], 0, -d[0])
    depth = run / steps
    for i in range(steps):
        y1 = y0 + h * (i + 1) / steps
        s0 = depth * i
        s1 = run if i == steps - 1 else run + 0.05  # the top tread ends flush with the plateau's notch; lower blocks tuck behind it
        corners = []
        for (s, w) in ((s0, -width / 2), (s1, -width / 2), (s1, width / 2), (s0, width / 2)):
            corners.append((x0 + d[0] * s + side[0] * w, z0 + d[2] * s + side[2] * w))
        lo = ring(corners, y0 - 0.5 if i == 0 else y0 + h * i / steps - 0.05)
        hi = ring(corners, y1)
        c = (sum(p[0] for p in corners) / 4, sum(p[1] for p in corners) / 4)
        side_band(mb, lo, hi, 0.2, 0.6, sw, c, s=SIDE)
        mb.poly_xz(SIDE, corners, y1, guv(top_sw, 0.4 + 0.5 * i / steps))
    # low rails on both sides
    for sgn in (-1, 1):
        rail = []
        for (s, w) in ((-0.3, sgn * (width / 2 + 0.1)), (run, sgn * (width / 2 + 0.1)), (run, sgn * (width / 2 + 0.9)), (-0.3, sgn * (width / 2 + 0.9))):
            rail.append((x0 + d[0] * s + side[0] * w, z0 + d[2] * s + side[2] * w))
        # sloped rail: build as a wedge
        a0 = (rail[0][0], y0 - 0.5, rail[0][1]); a1 = (rail[1][0], y0 - 0.5, rail[1][1]); a2 = (rail[2][0], y0 - 0.5, rail[2][1]); a3 = (rail[3][0], y0 - 0.5, rail[3][1])
        b0 = (rail[0][0], y0 + 0.6, rail[0][1]); b1 = (rail[1][0], y0 + h + 0.6, rail[1][1]); b2 = (rail[2][0], y0 + h + 0.6, rail[2][1]); b3 = (rail[3][0], y0 + 0.6, rail[3][1])
        cc = (sum(p[0] for p in rail) / 4, sum(p[1] for p in rail) / 4)
        side_band(mb, [a0, a1, a2, a3], [b0, b1, b2, b3], 0.1, 0.7, sw, cc)
        mb.quad(SIDE, b0, b1, b2, b3, guv(sw, 0.9), guv(sw, 0.9), guv(sw, 0.9), guv(sw, 0.9), (0, 1, 0))


def rock(mb, cx, cz, size, rng, sw=ROCK_DARK, s=SIDE, y0=0.0):
    """Faceted boulder: a jittered low-res sphere, flattened and half buried."""
    if BLENDER is not None:
        record(mb, 'rock', rng, x=cx, z=cz, size=size, y0=y0, sw=SWATCH_NAME[sw])
        return
    rings_n = 3; seg = rng.choice([5, 6, 7])
    rot = rng.uniform(0, math.tau)
    sy = rng.uniform(0.55, 0.8)
    verts = [(cx, y0 - size * 0.25, cz)]
    layers = []
    for r in range(1, rings_n + 1):
        phi = math.pi * r / (rings_n + 1)
        layer = []
        for i in range(seg):
            th = rot + math.tau * (i + 0.5 * (r % 2)) / seg
            rr = size * rng.uniform(0.8, 1.1)
            layer.append((cx + math.sin(phi) * math.cos(th) * rr, y0 + (-math.cos(phi)) * size * sy + size * 0.25, cz + math.sin(phi) * math.sin(th) * rr))
        layers.append(layer)
    apex = (cx + rng.uniform(-0.2, 0.2) * size, y0 + size * sy + size * 0.25, cz + rng.uniform(-0.2, 0.2) * size)
    centre = (cx, y0 + size * 0.25, cz)
    lo, hi = y0 - size * 0.25, apex[1]
    def uvp(p): return guv(sw, (p[1] - lo) / (hi - lo))
    bottom = verts[0]
    first = layers[0]
    for i in range(seg):
        j = (i + 1) % seg
        mb.tri(s, bottom, first[i], first[j], uvp(bottom), uvp(first[i]), uvp(first[j]), sub(mul(add(first[i], first[j]), 0.5), centre))
    for k in range(len(layers) - 1):
        A, B = layers[k], layers[k + 1]
        for i in range(seg):
            j = (i + 1) % seg
            a, b, c, d = A[i], A[j], B[j], B[i]
            out = sub(mul(add(add(a, b), add(c, d)), 0.25), centre)
            mb.quad(s, a, b, c, d, uvp(a), uvp(b), uvp(c), uvp(d), out)
    last = layers[-1]
    for i in range(seg):
        j = (i + 1) % seg
        mb.tri(s, last[i], last[j], apex, uvp(last[i]), uvp(last[j]), uvp(apex), sub(mul(add(add(last[i], last[j]), apex), 1 / 3), centre))


def shard(mb, base, direction, radius, length, sw, rng, s=0, sides=6):
    """Hexagonal crystal prism with a pointed tip. base: (x,y,z) on the ground; direction: unit-ish vector."""
    if BLENDER is not None and sides >= 5:
        record(mb, 'shard', rng, base=list(base), dir=list(direction), radius=radius, length=length, sides=sides, sw=SWATCH_NAME[sw])
        return
    d = norm(direction)
    helper = (1, 0, 0) if abs(d[0]) < 0.9 else (0, 0, 1)
    e1 = norm(cross(d, helper)); e2 = cross(d, e1)
    rot = rng.uniform(0, math.tau)
    tip_len = length * rng.uniform(0.25, 0.38)
    body = length - tip_len
    start = sub(base, mul(d, radius * 0.8))  # sink the root
    def ringp(at, r):
        pts = []
        for i in range(sides):
            a = rot + math.tau * i / sides
            pts.append(add(add(at, mul(e1, math.cos(a) * r)), mul(e2, math.sin(a) * r)))
        return pts
    r0 = ringp(start, radius * 0.92)
    r1 = ringp(add(start, mul(d, body)), radius)
    apex = add(add(start, mul(d, length)), add(mul(e1, rng.uniform(-0.15, 0.15) * radius), mul(e2, rng.uniform(-0.15, 0.15) * radius)))
    axis_c = add(start, mul(d, body * 0.5))
    tb = body / length
    for i in range(sides):
        j = (i + 1) % sides
        a, b, c, dd = r0[i], r0[j], r1[j], r1[i]
        out = sub(mul(add(add(a, b), add(c, dd)), 0.25), axis_c)
        mb.quad(s, a, b, c, dd, guv(sw, 0.0), guv(sw, 0.0), guv(sw, tb), guv(sw, tb), out)
        mb.tri(s, r1[i], r1[j], apex, guv(sw, tb), guv(sw, tb), guv(sw, 1.0), sub(mul(add(add(r1[i], r1[j]), apex), 1 / 3), add(start, mul(d, body))))


def cluster(mb, cx, cy, cz, size, rng, palette, count=None, spread=None, s=0):
    """A crystal cluster: one big shard and smaller ones leaning outwards."""
    count = count or rng.randint(4, 8)
    spread = spread if spread is not None else size * 0.35
    main_sw = palette[0]
    shard(mb, (cx, cy, cz), (rng.uniform(-0.15, 0.15), 1, rng.uniform(-0.15, 0.15)), size * 0.16, size, main_sw, rng, s)
    for k in range(count - 1):
        a = rng.uniform(0, math.tau)
        dist = spread * rng.uniform(0.35, 1.0)
        lean = rng.uniform(0.25, 0.7)
        length = size * rng.uniform(0.3, 0.75)
        sw = palette[rng.randrange(len(palette))]
        shard(mb, (cx + math.cos(a) * dist, cy, cz + math.sin(a) * dist), (math.cos(a) * lean, 1, math.sin(a) * lean),
              length * rng.uniform(0.17, 0.24), length, sw, rng, s)


def tuft(mb, cx, cy, cz, size, rng, s=0):
    """Dry grass tuft: a fan of thin three-sided blades."""
    for k in range(rng.randint(7, 10)):
        a = rng.uniform(0, math.tau)
        lean = rng.uniform(0.2, 0.6)
        length = size * rng.uniform(0.6, 1.0)
        shard(mb, (cx + math.cos(a) * 0.15, cy, cz + math.sin(a) * 0.15), (math.cos(a) * lean, 1, math.sin(a) * lean),
              size * 0.07, length, GOLD, rng, s, sides=3)


def road(mb, pts, width, y, s=0):
    """Flat ribbon along a resampled centre line. UV.x runs 0..1 across the width (the road shader fades
    both edges by UV.x); the texture itself is tiled in world space, so UV.y only tracks the length."""
    n = len(pts)
    left, right = [], []
    for i in range(n):
        a = pts[max(0, i - 1)]; b = pts[min(n - 1, i + 1)]
        dx, dz = b[0] - a[0], b[1] - a[1]
        l = math.hypot(dx, dz) or 1
        nx, nz = -dz / l, dx / l
        left.append((pts[i][0] + nx * width / 2, y, pts[i][1] + nz * width / 2))
        right.append((pts[i][0] - nx * width / 2, y, pts[i][1] - nz * width / 2))
    acc = 0.0
    for i in range(n - 1):
        seg = math.dist(pts[i], pts[i + 1])
        v0, v1 = acc * 0.05, (acc + seg) * 0.05
        acc += seg
        mb.quad(s, left[i], right[i], right[i + 1], left[i + 1], (0.014, v0), (0.986, v0), (0.986, v1), (0.014, v1), (0, 1, 0))
