"""Crystal Canyon layout: what goes where. World units are metres, floor top at y = 0.05; x = east, z = north.

The map reads like a level, not a scatter:
  south gate -> main road north -> mining camp (west of the road) -> crossroads with a signpost
  -> branches to the west lookout, to the pool and the north-east ruins, and to the south-east dig site
  -> main road continues past the central crystal mesa and old ruins to the north gate.
Props come from the original game (camp, ruins, gates, lamps); crystals and rocks are grouped at
landmarks, wall feet and road bends; grass lines the road edges.
"""
import math, random
from meshlib import MeshBuilder, point_in_poly, offset_poly
from shapes import (mesa, column, stairs, rock, cluster, tuft, road, ROCK, ROCK_DARK, CYAN, TEAL, PINK, VIOLET)

FLOOR_Y = 0.05
BIG = [CYAN, CYAN, TEAL, PINK]
ROSE = [PINK, PINK, VIOLET, CYAN]
DUSK = [VIOLET, VIOLET, PINK, TEAL]
PALETTES = [BIG, ROSE, DUSK]


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
        k = rng.uniform(-2.0, 2.0)
        pts.append((x + nx * k, z + nz * k, nx, nz))
    return pts


# ---------------------------------------------------------------------- the plan
# Roads: (centre line, width). Branch ends tuck under stairs or into the main road.
ROADS = {
    'MainRoad': ([(0, -70), (0, -54), (-3, -44), (-4, -32), (-2, -20), (-5, -8), (-6, 2), (-11, 12), (-12, 24), (-8, 36),
                  (-3, 46), (-2, 56), (-2, 70)], 7.0),
    'WestPath': ([(-5, -3), (-8, -2), (-11.5, -1.5)], 5.0),
    'EastPath': ([(-5, -3), (4, -1.5), (12, 2.5), (18.5, 6), (22, 10), (22, 14)], 5.0),
    'SouthEastPath': ([(-3, -42), (4, -40), (12.5, -38)], 5.0),
    'CampPath': ([(-4, -33), (-11, -32), (-17, -31)], 4.5),
}
PROP_RADIUS = {'tent': 4.5, 'tent_small': 3.5, 'cart': 3.0, 'rubble': 3.8, 'ruin_wall': 4.5, 'ruin_tall': 4.2, 'ruin_arch': 4.0,
               'planks': 5.0, 'dock': 3.8, 'table': 2.0, 'fence': 3.4, 'lamp': 1.2, 'signpost': 1.5, 'brazier': 1.0, 'crate': 1.3}
# Stairs: (x0, z0) at the foot, climbing along (dx, dz); width, rise, horizontal run, base height, plateau they climb onto.
# The plateau gets a flat notch in its cliff exactly where the top step ends, so the stairs meet it cleanly.
STAIRS = [
    (-11.0, -1.5, -1, 0, 6.5, 4.0, 8.0, 0.0, 'PlateauWest'),           # west lookout, from the crossroads
    (22.0, 13.0, 0, 1, 6.5, 4.0, 9.0, 0.0, 'PlateauNorthEast'),        # north-east ruins, from the east path
    (31.0, 30.5, 0, 1, 4.5, 4.0, 6.5, 4.0, 'PlateauNorthEastHigh'),    # ruins -> crystal shrine on the high terrace
    (12.0, -38.0, 1, 0, 6.0, 3.0, 6.5, 0.0, 'PlateauSouthEast'),       # south-east dig site
]
GATES = {'SouthGate': (0.0, -58.5, 0.0), 'NorthGate': (-2.0, 58.5, 180.0)}  # x, z, yaw


def sample_road(pts, step=1.5):
    """Catmull-Rom through the control points, resampled every ~step metres."""
    P = [pts[0]] + list(pts) + [pts[-1]]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        seg = math.dist(p1, p2)
        n = max(2, int(seg / step))
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in range(2)))
    out.append(pts[-1])
    return out


def dist_to_roads(x, z, sampled):
    best = 1e9
    for pts, w in sampled.values():
        for i in range(len(pts) - 1):
            ax, az = pts[i]; bx, bz = pts[i + 1]
            dx, dz = bx - ax, bz - az
            L = dx * dx + dz * dz or 1
            t = max(0, min(1, ((x - ax) * dx + (z - az) * dz) / L))
            d = math.hypot(x - (ax + dx * t), z - (az + dz * t)) - w / 2
            best = min(best, d)
    return best


def build(seed=7):
    rng = random.Random(seed)
    out = {}
    props = []  # (key, x, z, dict(yaw=, along=, y=, scale=))

    def prop(key, x, z, **kw):
        props.append((key, x, z, kw))

    sampled = {k: (sample_road(p), w) for k, (p, w) in ROADS.items()}

    # ---------------------------------------------------------------- plateaus (walkable tops)
    plateaus = {
        'PlateauWest': (blob(-28, 8, 11, 14, 9, rng, rot=0.3), 4.0),
        'PlateauNorthEast': (blob(25, 33, 13, 11, 9, rng, rot=0.1), 4.0),
        'PlateauNorthEastHigh': (blob(31, 41, 6.5, 6, 7, rng, rot=0.5), 8.0),
        'MesaCentre': (blob(1, 15, 6.5, 6, 7, rng, rot=0.2), 3.0),
        'PlateauSouthEast': (blob(28, -36, 9, 10, 8, rng, rot=0.4), 3.0),
    }
    pool = blob(23, -7, 8.5, 6.5, 18, rng, wob=0.06)
    camp = [(-36, -48), (-9, -48), (-9, -21), (-36, -21)]

    def blocked(x, z, pad=1.5, road_pad=1.0, camp_too=True):
        for pts, h in plateaus.values():
            if point_in_poly((x, z), offset_poly(pts, pad)):
                return True
        if point_in_poly((x, z), offset_poly(pool, pad)):
            return True
        if camp_too and point_in_poly((x, z), camp):
            return True
        for gx, gz, _ in GATES.values():
            if abs(x - gx) < 10 and abs(z - gz) < 7:
                return True
        for key, px, pz, kw in props:
            if key != 'gate' and math.hypot(x - px, z - pz) < PROP_RADIUS.get(key, 1.5) + pad:
                return True
        return dist_to_roads(x, z, sampled) < road_pad

    mb = MeshBuilder(2)
    tops = {}
    for name, (pts, h) in plateaus.items():
        notches = [dict(x0=s[0], z0=s[1], dx=s[2], dz=s[3], width=s[4], run=s[6]) for s in STAIRS if s[8] == name]
        tops[name] = mesa(mb, pts, h, rng, base=-1.0, sw=ROCK, bevel=0.8, jitter=0.9, rows=3, notches=notches)
    out['Plateaus'] = mb

    mb = MeshBuilder(1)
    for x0, z0, dx, dz, w, h, run, y0, target in STAIRS:
        stairs(mb, x0, z0, dx, dz, w, h, run, rng, y0=y0)
    out['Stairs'] = mb

    mb = MeshBuilder(1)
    for name, (pts, w) in sampled.items():
        road(mb, pts, w, FLOOR_Y + 0.03)
    out['Roads'] = mb

    # ---------------------------------------------------------------- canyon walls: three rows of basalt-like columns
    edge = boundary(rng)
    sectors = [MeshBuilder(2) for _ in range(6)]
    col_tops = []
    for i, (x, z, nx, nz) in enumerate(edge):
        mb = sectors[i * len(sectors) // len(edge)]
        near_gate = any(abs(x - gx) < 10 and abs(z - gz) < 8 for gx, gz, _ in GATES.values())
        r0 = rng.uniform(3.2, 4.8)
        h0 = rng.uniform(8, 13)
        if not near_gate:
            column(mb, x + nx * r0 * 0.5, z + nz * r0 * 0.5, r0, h0, rng)
        r1 = rng.uniform(4.0, 5.5)
        d1 = r0 + r1 * 0.9
        c1x, c1z = x + nx * d1 + rng.uniform(-1.5, 1.5), z + nz * d1 + rng.uniform(-1.5, 1.5)
        _, h1 = column(mb, c1x, c1z, r1, rng.uniform(12, 17), rng, whole=(i % 6 == 3))  # crystal-topped ones stay flat
        col_tops.append((c1x, h1, c1z, r1))
        r2 = rng.uniform(5, 7)
        d2 = d1 + r1 + r2 * 0.6
        column(mb, x + nx * d2, z + nz * d2, r2, rng.uniform(15, 21), rng, detail=False)  # back row: seen mostly from above
        if not near_gate and i % 3 == 1 and dist_to_roads(x - nx * 2, z - nz * 2, sampled) > 3:  # low step columns in front of the wall
            column(mb, x - nx * 1.8, z - nz * 1.8, rng.uniform(1.6, 2.6), rng.uniform(2.5, 5.5), rng)
    for k, mb in enumerate(sectors):
        out['Wall_%d' % k] = mb

    # gate towers: two tall columns either side of each gate opening
    mb = MeshBuilder(2)
    for gx, gz, yaw in GATES.values():
        for sx in (-1, 1):
            column(mb, gx + sx * 10.5, gz + (2.5 if gz < 0 else -2.5), 3.6, 15.0, rng, sides=6, whole=True)
    for name, x, z, r, h in [('PlateauWest', -37, 18, 3.5, 9), ('PlateauWest', -36, 0, 3.0, 7.5), ('PlateauNorthEast', 37, 27, 3.5, 9),
                             ('PlateauNorthEast', 14, 40, 3.0, 7), ('MesaCentre', 6.0, 20.5, 2.2, 5.5), ('PlateauSouthEast', 37, -42, 3.2, 7.5),
                             ('', 12, -20, 2.0, 5.5), ('', 33, 8, 2.4, 6.0)]:
        column(mb, x, z, r, h, rng)
    out['Spires'] = mb

    # ---------------------------------------------------------------- props from the original game
    # gates
    for gx, gz, yaw in GATES.values():
        prop('gate', gx, gz, yaw=yaw)
        for sx in (-1, 1):
            prop('brazier', gx + sx * 7.5, gz + (6 if gz < 0 else -6))
    # mining camp west of the road
    prop('planks', -21, -32, along=0)
    prop('tent', -27, -26, yaw=90)
    prop('tent_small', -30, -40, yaw=30)
    prop('table', -20, -33, along=90)
    prop('barrel_wood', -17.5, -34.5, yaw=10)
    prop('barrel_wood', -22.5, -35.5, yaw=200)
    prop('brazier', -15, -27)
    for (x, z, yaw) in [(-14, -41, 0), (-12, -40.5, 25), (-13, -38.8, 60)]:
        prop('crate', x, z, yaw=yaw)
    prop('crate', -13.4, -40.4, yaw=40, lift=2.3)
    prop('barrel', -22, -43, yaw=0)
    prop('barrel', -20, -44.2, yaw=0)
    prop('barrel', -24, -44, yaw=0)
    prop('cart', -12, -25, along=0)
    prop('cart', -31, -33, along=90)
    prop('rubble', -31, -44, yaw=0)
    prop('rock_decal', -26, -36, yaw=0)
    prop('rock_decal', -15, -30, yaw=70)
    for z in (-46.5, -40, -33.5, -27, -21.5):        # fence along the west side of the camp
        prop('fence', -36, z, along=0)
    for x in (-30, -23.5, -17):                       # and the south side
        prop('fence', x, -48, along=90)
    prop('lamp', -9, -28, yaw=0)
    prop('lamp', -9, -36, yaw=0)
    for (x, z, yaw) in [(-38, 30, 20), (37, -6, 80), (-24, 54, 140), (24, 52, 40), (-40, -14, 100), (38, -50, 10), (8, -24, 60)]:
        prop('rock_decal', x, z, yaw=yaw, scale=1.3)
    # crossroads
    prop('signpost', -0.5, -7.5, yaw=30)
    prop('lamp', -10.5, -7, yaw=90)
    prop('lamp', 1.5, -1, yaw=0)
    # lamps along the main road, alternating sides
    for (x, z, yaw) in [(5, -50, 0), (3, -28, 0), (-15, 20, 0), (-3.5, 30, 0), (-8, 44, 0), (4, 50, 0)]:
        prop('lamp', x, z, yaw=yaw)
    # pool: a small dock
    prop('dock', 16.0, -3.5, along=20, scale=0.5, y=FLOOR_Y + 0.18)
    # west lookout
    prop('crate', -30, -1, yaw=10, y=4.0)
    prop('crate', -32, 0.5, yaw=50, y=4.0)
    prop('lamp', -21.5, 4, yaw=0, y=4.0)
    # north-east ruins and the shrine
    prop('ruin_tall', 18, 37, yaw=-30, y=4.0)
    prop('rubble', 15.5, 31, yaw=60, y=4.0)
    prop('brazier', 27.5, 38.5, y=8.0)
    prop('brazier', 34.5, 38.5, y=8.0)
    # dig site on the south-east plateau
    prop('rubble', 27, -40, yaw=0, y=3.0)
    prop('cart', 31, -31, along=30, y=3.0)
    prop('crate', 22.5, -33, yaw=15, y=3.0)
    prop('crate', 23.5, -31, yaw=60, y=3.0)
    prop('tent_small', 33, -37, yaw=200, y=3.0)
    # old ruins by the north gate
    prop('ruin_arch', -16, 41, yaw=0)
    prop('ruin_wall', 8, 46, yaw=-60)
    prop('ruin_tall', 15, 51, yaw=120)
    prop('rubble', -22, 50, yaw=30)


    # ---------------------------------------------------------------- crystals: landmarks first, then rhythm along the walls
    cr = MeshBuilder(1)
    rk = MeshBuilder(1)
    gr = MeshBuilder(1)

    def rock_group(x, z, size, y=FLOOR_Y, toward=None):
        """Three boulders, large to small, stepping away from the anchor (toward = unit vector)."""
        tx, tz = toward or (rng.uniform(-1, 1), rng.uniform(-1, 1))
        l = math.hypot(tx, tz) or 1
        tx, tz = tx / l, tz / l
        for k, f in enumerate((1.0, 0.6, 0.35)):
            d = size * (0.0, 1.3, 2.1)[k]
            px, pz = x + tx * d + tz * rng.uniform(-0.6, 0.6) * size, z + tz * d - tx * rng.uniform(-0.6, 0.6) * size
            rock(rk, px, pz, size * f, rng, sw=ROCK_DARK if k == 0 else ROCK, y0=y)
        for k in range(2):
            a = rng.uniform(0, math.tau)
            tuft(gr, x + math.cos(a) * size * 1.6, y, z + math.sin(a) * size * 1.6, rng.uniform(1.0, 1.5), rng)

    # hero crystals (visible from far away)
    cluster(cr, 1.0, 3.0, 15.0, 10.0, rng, BIG, count=9, spread=4.0)            # central mesa
    cluster(cr, 31.0, 8.0, 41.0, 8.0, rng, ROSE, count=7, spread=3.0)           # shrine on the high terrace
    cluster(cr, -31.0, 4.0, 12.0, 6.5, rng, BIG, count=6, spread=3.0)           # west lookout
    cluster(cr, 27.0, 3.0, -40.0, 5.0, rng, ROSE, count=6, spread=2.0)          # dig site, breaking out of the rubble
    for gname, (gx, gz, yaw) in GATES.items():                                  # gate towers wear crystal crowns
        for sx in (-1, 1):
            cluster(cr, gx + sx * 10.5, 15.0, gz + (2.5 if gz < 0 else -2.5), 4.5, rng, BIG if sx < 0 else ROSE, count=5, spread=1.6)
    # crystals on some inner wall columns
    for i, (cx_, h_, cz_, r_) in enumerate(col_tops):
        if i % 6 == 3:
            cluster(cr, cx_, h_, cz_, rng.uniform(4, 6.5), rng, PALETTES[i % 3], spread=min(2.5, r_ * 0.5))
    # wall foot: every 4th edge point gets crystals + a rock group, unless a road, gate or plateau is in the way
    for i, (x, z, nx, nz) in enumerate(edge):
        if i % 4 != 0:
            continue
        fx, fz = x - nx * 1.5, z - nz * 1.5
        if blocked(fx, fz, pad=0.5, road_pad=3.0, camp_too=False):
            continue
        cluster(cr, fx, FLOOR_Y, fz, rng.uniform(3.5, 5.5), rng, PALETTES[(i // 4) % 3], spread=2.0)
        rock_group(fx - nz * 3.0, fz + nx * 3.0, rng.uniform(1.3, 2.0), toward=(-nx, -nz))
    # pool shore
    shore = offset_poly(pool, 1.8)
    for k, (x, z) in enumerate(shore):
        if k % 3 == 0 and dist_to_roads(x, z, sampled) > 1.5:
            cluster(cr, x, FLOOR_Y, z, 2.6 if k % 2 else 3.4, rng, [CYAN, TEAL, PINK], count=4, spread=1.0)
    for (x, z) in offset_poly(pool, 0.6):
        rock(rk, x, z, rng.uniform(0.9, 1.5), rng, sw=ROCK, y0=FLOOR_Y - 0.3)
    # road bends: a rock group with a couple of shards on the outside of each bend
    for name, (pts, w) in sampled.items():
        ctrl = ROADS[name][0]
        for j in range(1, len(ctrl) - 1):
            (ax, az), (bx, bz), (cx2, cz2) = ctrl[j - 1], ctrl[j], ctrl[j + 1]
            d1 = (bx - ax, bz - az); d2 = (cx2 - bx, cz2 - bz)
            turn = d1[0] * d2[1] - d1[1] * d2[0]
            l1 = math.hypot(*d1) or 1; l2 = math.hypot(*d2) or 1
            if abs(turn) / (l1 * l2) < 0.25:
                continue
            nx_, nz_ = (d1[0] / l1 - d2[0] / l2), (d1[1] / l1 - d2[1] / l2)  # points to the outside of the bend
            nl = math.hypot(nx_, nz_) or 1
            px, pz = bx + nx_ / nl * (w / 2 + 3.0), bz + nz_ / nl * (w / 2 + 3.0)
            if blocked(px, pz, pad=1.0, road_pad=1.5):
                continue
            rock_group(px, pz, rng.uniform(1.0, 1.5), toward=(nx_, nz_))
            cluster(cr, px + nx_ / nl * 2.5, FLOOR_Y, pz + nz_ / nl * 2.5, rng.uniform(1.8, 2.6), rng, PALETTES[j % 3], count=3, spread=0.9)

    # plateau bases: rock groups where the cliff meets the floor (away from the stairs)
    for x, z in [(-18, 16), (-22, -6), (-39, -4), (13, 27), (37, 21), (19, -30), (36, -27), (-4, 9), (7, 10)]:
        if dist_to_roads(x, z, sampled) > 2.0:
            rock_group(x, z, rng.uniform(1.2, 1.8))

    # grass lines both road edges
    for name, (pts, w) in sampled.items():
        acc = 0.0
        side = 1
        for i in range(1, len(pts)):
            acc += math.dist(pts[i - 1], pts[i])
            if acc < 4.5:
                continue
            acc = 0.0
            side = -side
            dx, dz = pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]
            l = math.hypot(dx, dz) or 1
            off = w / 2 + rng.uniform(0.6, 1.8)
            x, z = pts[i][0] - dz / l * off * side, pts[i][1] + dx / l * off * side
            if not blocked(x, z, pad=0.6, road_pad=0.4, camp_too=False):
                tuft(gr, x, FLOOR_Y, z, rng.uniform(1.0, 1.5), rng)
    for name in ('PlateauWest', 'PlateauNorthEast', 'PlateauSouthEast'):
        pts = tops[name]
        h = plateaus[name][1]
        for k in range(5):
            x = rng.uniform(min(p[0] for p in pts), max(p[0] for p in pts))
            z = rng.uniform(min(p[1] for p in pts), max(p[1] for p in pts))
            if point_in_poly((x, z), offset_poly(pts, -1.5)):
                tuft(gr, x, h, z, rng.uniform(1.0, 1.5), rng)

    # crystals that belong to the props above
    cluster(cr, -15.5, FLOOR_Y, -23.5, 2.2, rng, BIG, count=5, spread=1.0)    # crystal ore unloaded next to the carts
    cluster(cr, -31, FLOOR_Y, -29.5, 2.2, rng, ROSE, count=5, spread=1.0)
    cluster(cr, -31, FLOOR_Y + 1.0, -44, 3.5, rng, DUSK, count=6, spread=1.8)  # ore pile in the rubble
    cluster(cr, 34, 3.0, -29, 2.2, rng, ROSE, count=4, spread=0.8)
    cluster(cr, -18.5, FLOOR_Y, 45, 3.5, rng, DUSK, count=5, spread=1.5)
    cluster(cr, 11, FLOOR_Y, 49.5, 3.0, rng, BIG, count=4, spread=1.2)

    out['Crystals'] = cr
    out['Rocks'] = rk
    out['Grass'] = gr

    mb = MeshBuilder(1)
    mb.poly_xz(0, pool, FLOOR_Y + 0.12, (0.5, 0.5))
    out['Pool'] = mb

    return out, edge, props
