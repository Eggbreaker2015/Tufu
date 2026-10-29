"""Crystal Canyon layout: what goes where. World units are metres, floor top at y = 0.05; x = east, z = north."""
import math, random
from meshlib import MeshBuilder, point_in_poly, offset_poly
from shapes import (mesa, column, stairs, rock, cluster, tuft, ROCK, ROCK_DARK, CYAN, TEAL, PINK, VIOLET)

FLOOR_Y = 0.05


def blob(cx, cz, rx, rz, n, rng, wob=0.15, rot=0.0):
    pts = []
    for i in range(n):
        a = rot + math.tau * i / n
        k = 1 + rng.uniform(-wob, wob)
        pts.append((cx + math.cos(a) * rx * k, cz + math.sin(a) * rz * k))
    return pts


def boundary(rng, hx=44.0, hz=60.0, corner=18.0, step=6.0):
    """Inner edge of the canyon walls: a rounded rectangle with noise, as (x, z, outward_nx, outward_nz)."""
    pts = []
    # walk a rounded rectangle by arc length
    straight_x = 2 * (hx - corner); straight_z = 2 * (hz - corner)
    per = 2 * straight_x + 2 * straight_z + math.tau * corner
    n = int(per / step)
    def at(s):
        s %= per
        segs = [('L', straight_z, (-hx, -hz + corner), (0, 1)), ('A', math.pi / 2 * corner, (-hx + corner, hz - corner), math.pi),
                ('L', straight_x, (-hx + corner, hz), (1, 0)), ('A', math.pi / 2 * corner, (hx - corner, hz - corner), math.pi / 2),
                ('L', straight_z, (hx, hz - corner), (0, -1)), ('A', math.pi / 2 * corner, (hx - corner, -hz + corner), 0.0),
                ('L', straight_x, (hx - corner, -hz), (-1, 0)), ('A', math.pi / 2 * corner, (-hx + corner, -hz + corner), -math.pi / 2)]
        for kind, length, p, d in segs:
            if s <= length:
                if kind == 'L':
                    x, z = p[0] + d[0] * s, p[1] + d[1] * s
                    return x, z, -d[1], d[0]  # outward normal of a clockwise walk
                ang = d - s / corner
                return p[0] + math.cos(ang) * corner, p[1] + math.sin(ang) * corner, math.cos(ang), math.sin(ang)
            s -= length
        return at(0)
    for i in range(n):
        x, z, nx, nz = at(i * per / n)
        k = rng.uniform(-2.5, 2.5)
        pts.append((x + nx * k, z + nz * k, nx, nz))
    return pts


def build(seed=7):
    rng = random.Random(seed)
    out = {}

    # ---------------------------------------------------------------- plateaus (walkable tops)
    plateaus = {
        'PlateauWest': (blob(-28, 8, 11, 14, 9, rng, rot=0.3), 4.0),
        'PlateauNorthEast': (blob(25, 33, 13, 11, 9, rng, rot=0.1), 4.0),
        'PlateauNorthEastHigh': (blob(31, 41, 6.5, 6, 7, rng, rot=0.5), 8.0),
        'MesaCentre': (blob(1, 15, 6.5, 6, 7, rng, rot=0.2), 3.0),
        'PlateauSouthEast': (blob(28, -36, 9, 10, 8, rng, rot=0.4), 3.0),
        'MesaSouthWest': (blob(-27, -37, 7, 6, 7, rng, rot=0.6), 2.5),
    }
    pool = blob(23, -7, 8.5, 6.5, 10, rng, wob=0.12)

    def blocked(x, z, pad=1.5):
        for pts, h in plateaus.values():
            if point_in_poly((x, z), offset_poly(pts, pad)):
                return True
        if point_in_poly((x, z), offset_poly(pool, pad)):
            return True
        return False

    mb = MeshBuilder(2)
    tops = {}
    for name, (pts, h) in plateaus.items():
        tops[name] = mesa(mb, pts, h, rng, base=-1.0, sw=ROCK, bevel=0.8, jitter=0.9, rows=3)
        # rock columns hugging the outer side of plateaus (kept away from the stairs)
    out['Plateaus'] = mb

    # stairs: (x0, z0) on the floor, direction towards the plateau, width, height, run
    mb = MeshBuilder(1)
    stairs(mb, -10.0, -1.5, -1, 0, 6.5, 4.0, 9.0, rng)       # up to PlateauWest from the east
    stairs(mb, 22.0, 13.0, 0, 1, 6.5, 4.0, 9.0, rng)        # up to PlateauNorthEast from the south
    stairs(mb, 31.0, 30.5, 0, 1, 4.5, 4.0, 6.5, rng, y0=4.0)        # NorthEast -> High terrace (starts on the 4 m top)
    stairs(mb, 11.0, -38.0, 1, 0, 6.0, 3.0, 7.0, rng)        # up to PlateauSouthEast from the west
    out['Stairs'] = mb

    # ---------------------------------------------------------------- canyon walls: three rows of basalt-like columns
    edge = boundary(rng)
    sectors = [MeshBuilder(2) for _ in range(6)]
    col_tops = []
    for i, (x, z, nx, nz) in enumerate(edge):
        mb = sectors[i * len(sectors) // len(edge)]
        r0 = rng.uniform(3.2, 4.8)
        column(mb, x + nx * r0 * 0.5, z + nz * r0 * 0.5, r0, rng.uniform(8, 13), rng)
        r1 = rng.uniform(4.0, 5.5)
        d1 = r0 + r1 * 0.9
        c1x, c1z = x + nx * d1 + rng.uniform(-1.5, 1.5), z + nz * d1 + rng.uniform(-1.5, 1.5)
        _, h1 = column(mb, c1x, c1z, r1, rng.uniform(12, 17), rng)
        col_tops.append((c1x, h1, c1z, r1))
        r2 = rng.uniform(5, 7)
        d2 = d1 + r1 + r2 * 0.6
        column(mb, x + nx * d2, z + nz * d2, r2, rng.uniform(15, 21), rng)
        if rng.random() < 0.35:  # low step columns in front of the wall
            r3 = rng.uniform(1.6, 2.6)
            column(mb, x - nx * rng.uniform(1.0, 2.5), z - nz * rng.uniform(1.0, 2.5), r3, rng.uniform(2.5, 5.5), rng)
    for k, mb in enumerate(sectors):
        out['Wall_%d' % k] = mb

    # columns hugging plateau edges (outer sides only) and a few lone spires on the floor
    mb = MeshBuilder(2)
    for name, x, z, r, h in [('PlateauWest', -37, 18, 3.5, 9), ('PlateauWest', -36, 0, 3.0, 7.5), ('PlateauNorthEast', 37, 27, 3.5, 9),
                             ('PlateauNorthEast', 14, 40, 3.0, 7), ('MesaCentre', 5.5, 21, 2.2, 5.5), ('PlateauSouthEast', 37, -42, 3.2, 7.5),
                             ('', -14, 34, 2.4, 7.0), ('', 12, -18, 2.0, 5.5), ('', -16, -22, 2.6, 6.5), ('', -6, 46, 2.2, 6.0)]:
        column(mb, x, z, r, h, rng)
    out['Spires'] = mb

    # ---------------------------------------------------------------- boulders
    mb = MeshBuilder(1)
    placed = 0
    tries = 0
    while placed < 26 and tries < 2000:
        tries += 1
        x, z = rng.uniform(-38, 38), rng.uniform(-54, 54)
        if blocked(x, z, 2.0) or abs(x) < 6 and -56 < z < -40:
            continue
        rock(mb, x, z, rng.uniform(0.8, 2.6), rng, sw=ROCK_DARK if rng.random() < 0.5 else ROCK, y0=FLOOR_Y)
        placed += 1
    # pool rim stones
    for i, (x, z) in enumerate(offset_poly(pool, 0.6)):
        rock(mb, x, z, rng.uniform(0.9, 1.6), rng, sw=ROCK, y0=FLOOR_Y - 0.3)
    out['Rocks'] = mb

    # ---------------------------------------------------------------- crystals
    big = [CYAN, CYAN, TEAL, PINK]
    pinkish = [PINK, PINK, VIOLET, CYAN]
    violet = [VIOLET, VIOLET, PINK, TEAL]
    mb = MeshBuilder(1)
    # hero clusters on tops
    cluster(mb, 1.0, 3.0, 15.0, 10.0, rng, big, count=9, spread=4.0)
    cluster(mb, 31.0, 8.0, 41.0, 8.0, rng, pinkish, count=7, spread=3.0)
    cluster(mb, -31.0, 4.0, 13.0, 6.5, rng, big, count=6, spread=3.0)
    cluster(mb, -31.0, 4.0, -1.0, 3.5, rng, violet, count=5, spread=1.8)
    cluster(mb, 30.0, 3.0, -39.0, 6.0, rng, pinkish, count=6, spread=2.5)
    cluster(mb, -27.0, 2.5, -37.0, 5.0, rng, big, count=6, spread=2.2)
    cluster(mb, 20.0, 4.0, 37.0, 4.0, rng, violet, count=5, spread=2.0)
    # clusters on wall column tops (on the inner row) and at the wall foot
    for i, (x, z, nx, nz) in enumerate(edge):
        if i % 5 == 2:
            cluster(mb, x - nx * 1.2, FLOOR_Y, z - nz * 1.2, rng.uniform(3.5, 6.5), rng, rng.choice([big, pinkish, violet]), spread=2.2)
        elif i % 6 == 4:
            cx_, h_, cz_, r_ = col_tops[i]
            cluster(mb, cx_, h_, cz_, rng.uniform(4, 7), rng, rng.choice([big, pinkish]), spread=min(2.5, r_ * 0.5))
    # floor clusters along the path and around the pool
    for (x, z, s) in [(-6, -28, 3.0), (8, -8, 2.5), (-12, 22, 3.5), (6, 40, 3.0), (-20, 44, 4.0), (14, -48, 2.5), (-30, -16, 3.0),
                      (34, 14, 3.5), (36, -22, 3.0)]:
        cluster(mb, x, FLOOR_Y, z, s, rng, rng.choice([big, pinkish, violet]), spread=1.6)
    for (x, z) in offset_poly(pool, 1.8)[::3]:
        cluster(mb, x, FLOOR_Y, z, rng.uniform(1.8, 3.2), rng, [CYAN, TEAL, PINK], count=4, spread=1.0)
    # scattered single shards
    scat = 0
    while scat < 40:
        x, z = rng.uniform(-40, 40), rng.uniform(-56, 56)
        if blocked(x, z, 1.0):
            continue
        cluster(mb, x, FLOOR_Y, z, rng.uniform(0.8, 1.6), rng, rng.choice([big, pinkish, violet]), count=rng.randint(1, 3), spread=0.6)
        scat += 1
    out['Crystals'] = mb

    # ---------------------------------------------------------------- dry grass tufts
    mb = MeshBuilder(1)
    t = 0
    while t < 70:
        x, z = rng.uniform(-40, 40), rng.uniform(-56, 56)
        if blocked(x, z, 0.8):
            continue
        tuft(mb, x, FLOOR_Y, z, rng.uniform(0.6, 1.1), rng)
        t += 1
    for name in ('PlateauWest', 'PlateauNorthEast', 'PlateauSouthEast'):
        pts = tops[name]
        h = plateaus[name][1]
        for k in range(6):
            x, z = rng.uniform(min(p[0] for p in pts), max(p[0] for p in pts)), rng.uniform(min(p[1] for p in pts), max(p[1] for p in pts))
            if point_in_poly((x, z), offset_poly(pts, -1.0)):
                tuft(mb, x, h, z, rng.uniform(0.6, 1.0), rng)
    out['Grass'] = mb

    # ---------------------------------------------------------------- water surface
    mb = MeshBuilder(1)
    mb.poly_xz(0, pool, FLOOR_Y + 0.12, (0.5, 0.5))
    out['Pool'] = mb

    return out, edge
