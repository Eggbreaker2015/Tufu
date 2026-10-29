"""Check-only preview renders of an exported scene in Blender (approximates the game's shaders; not game art).

    Blender -b --factory-startup -P preview_render.py -- scene.json out_dir [view ...]

Views: 'overview' (orthographic, top-down) and named game-camera views from VIEWS (60 degree pitch, 35 m, FOV 50,
portrait like the phone). Unity (x, y, z) -> Blender (x, z, y).
"""
import bpy, json, math, os, sys
from mathutils import Vector

WIDE = {  # name: (target x, z, distance, pitch deg, yaw deg) for distant composition shots
    'wide_south': (0, -20, 95, 45, 0), 'wide_north': (0, 20, 95, 45, 180), 'wide_west': (-5, 0, 95, 40, 90),
    'stairs_west': (-17, -1.5, 24, 32, -90), 'stairs_ne': (22, 19, 24, 32, 0), 'stairs_shrine': (31, 35, 20, 35, 0),
    'stairs_se': (16, -38, 22, 32, 90),
}
VIEWS = {  # name: Unity ground target (x, z) the game camera looks at
    'entrance': (0, -38), 'south_gate': (0, -50), 'camp': (-14, -34), 'crossroads': (-3, -4), 'pool': (17, -4),
    'mesa': (-6, 16), 'ruins': (24, 34), 'dig_site': (24, -36), 'north_gate': (-4, 48),
}


def lin(c):
    return [((x / 12.92) if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4) for x in c[:3]]


def u2b(p):
    return (p[0], p[2], p[1])


def node(nt, kind, loc, **props):
    n = nt.nodes.new(kind)
    n.location = loc
    for k, v in props.items():
        setattr(n, k, v)
    return n


def image(path, data=False):
    if not path or not os.path.exists(path):
        return None
    img = bpy.data.images.load(path, check_existing=True)
    if data:
        img.colorspace_settings.name = 'Non-Color'
    return img


def light_value(nt, x=-600, y=-300):
    """White diffuse -> Shader to RGB: the light (sun, shadow, ambient) arriving at the pixel."""
    d = node(nt, 'ShaderNodeBsdfDiffuse', (x, y))
    d.inputs['Color'].default_value = (1, 1, 1, 1)
    s = node(nt, 'ShaderNodeShaderToRGB', (x + 200, y))
    nt.links.new(d.outputs[0], s.inputs[0])
    return s.outputs['Color']


def finish(nt, color_socket, alpha_socket=None):
    out = nt.nodes.get('Material Output') or node(nt, 'ShaderNodeOutputMaterial', (800, 0))
    em = node(nt, 'ShaderNodeEmission', (500, 0))
    nt.links.new(color_socket, em.inputs['Color'])
    if alpha_socket is None:
        nt.links.new(em.outputs[0], out.inputs['Surface'])
        return
    tr = node(nt, 'ShaderNodeBsdfTransparent', (500, -150))
    mix = node(nt, 'ShaderNodeMixShader', (650, 0))
    nt.links.new(alpha_socket, mix.inputs[0])
    nt.links.new(tr.outputs[0], mix.inputs[1])
    nt.links.new(em.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs['Surface'])


def mix(nt, a, b, fac, loc, blend='MIX'):
    m = node(nt, 'ShaderNodeMix', loc, data_type='RGBA', blend_type=blend)
    if isinstance(fac, float):
        m.inputs['Factor'].default_value = fac
    else:
        nt.links.new(fac, m.inputs['Factor'])
    for sock, v in ((m.inputs[6], a), (m.inputs[7], b)):
        if isinstance(v, (list, tuple)):
            sock.default_value = list(v[:3]) + [1]
        else:
            nt.links.new(v, sock)
    return m.outputs[2]


def world_xz(nt, tile, loc):
    g = node(nt, 'ShaderNodeNewGeometry', (loc[0] - 400, loc[1]))
    sc = node(nt, 'ShaderNodeVectorMath', (loc[0] - 200, loc[1]), operation='SCALE')
    nt.links.new(g.outputs['Position'], sc.inputs[0])
    sc.inputs['Scale'].default_value = tile
    return sc.outputs[0]


def make_material(key, m):
    mat = bpy.data.materials.new(m['name'])
    mat.use_nodes = True
    nt = mat.node_tree
    for n in list(nt.nodes):
        if n.type != 'OUTPUT_MATERIAL':
            nt.nodes.remove(n)
    C, F, T = m['colors'], m['floats'], m['tex']
    light = light_value(nt)
    if m['kind'] == 'ground':
        tile = F.get('_UVTile', 0.05)
        tex = node(nt, 'ShaderNodeTexImage', (-300, 200))
        tex.image = image(T.get('_Texture0'), data=True)
        nt.links.new(world_xz(nt, tile, (-300, 200)), tex.inputs['Vector'])
        sep = node(nt, 'ShaderNodeSeparateColor', (-50, 200))
        nt.links.new(tex.outputs['Color'], sep.inputs[0])
        albedo = mix(nt, lin(C.get('_MaskColor1', [0.3] * 3)), lin(C.get('_MaskColor2', [0.8] * 3)), sep.outputs[0], (150, 200))
        finish(nt, mix(nt, albedo, light, 1.0, (350, 100), 'MULTIPLY'))
    elif m['kind'] == 'road':
        tex = node(nt, 'ShaderNodeTexImage', (-300, 200))
        tex.image = image(T.get('_MainTex'))
        nt.links.new(world_xz(nt, F.get('_MainTilingWorld', 0.3), (-300, 200)), tex.inputs['Vector'])
        uv = node(nt, 'ShaderNodeUVMap', (-600, -100))
        sep = node(nt, 'ShaderNodeSeparateXYZ', (-450, -100))
        nt.links.new(uv.outputs[0], sep.inputs[0])
        inv = node(nt, 'ShaderNodeMath', (-300, -50), operation='SUBTRACT')
        inv.inputs[0].default_value = 1.0
        nt.links.new(sep.outputs[0], inv.inputs[1])
        mn = node(nt, 'ShaderNodeMath', (-150, -100), operation='MINIMUM')
        nt.links.new(sep.outputs[0], mn.inputs[0]); nt.links.new(inv.outputs[0], mn.inputs[1])
        dv = node(nt, 'ShaderNodeMath', (0, -100), operation='DIVIDE', use_clamp=True)
        nt.links.new(mn.outputs[0], dv.inputs[0]); dv.inputs[1].default_value = max(0.05, F.get('_BlendWidthX', 0.4))
        finish(nt, mix(nt, tex.outputs['Color'], light, 1.0, (350, 100), 'MULTIPLY'), dv.outputs[0])
        mat.surface_render_method = 'BLENDED'
    elif m['kind'] == 'water':
        col = mix(nt, lin(C.get('_WaterColor', [0.1, 0.6, 0.9])), lin(C.get('_WaterShallowColor', [0.4, 1, 0.9])), 0.5, (0, 100))
        finish(nt, mix(nt, col, light, 0.5, (300, 100), 'MULTIPLY'))
    else:  # TCP2 toon and anything else: albedo * lerp(shadow colour, highlight colour, crisp ramp of the light)
        tex = node(nt, 'ShaderNodeTexImage', (-300, 250))
        tex.image = image(T.get('_BaseMap') or T.get('_MainTex'))
        base = lin(C.get('_BaseColor', C.get('_Color', [1, 1, 1])))
        albedo = mix(nt, tex.outputs['Color'] if tex.image else (1, 1, 1), base, 1.0, (-50, 250), 'MULTIPLY')
        bw = node(nt, 'ShaderNodeRGBToBW', (-200, -300))
        nt.links.new(light, bw.inputs[0])
        ramp = node(nt, 'ShaderNodeValToRGB', (-20, -300))
        ramp.color_ramp.interpolation = 'LINEAR'
        e = ramp.color_ramp.elements
        e[0].position, e[1].position = 0.5, 0.56
        nt.links.new(bw.outputs[0], ramp.inputs[0])
        shade = mix(nt, lin(C.get('_SColor', [0.6, 0.6, 0.7])), lin(C.get('_HColor', [1, 1, 1])), ramp.outputs[0], (200, -200))
        col = mix(nt, albedo, shade, 1.0, (350, 100), 'MULTIPLY')
        em = C.get('_EmissionColor')
        if em and F.get('_UseEmission', 0) > 0.5:
            col = mix(nt, col, lin(em), 1.0, (450, 100), 'ADD')
        finish(nt, col)
    return mat


def build(data):
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    mats = {g: make_material(g, m) for g, m in data['materials'].items()}
    for o in data['objects']:
        verts = [u2b(v) for v in o['pos']]
        faces, fmat, used = [], [], []
        for s in o['subs']:
            if s['mat'] not in mats:
                continue
            if s['mat'] not in used:
                used.append(s['mat'])
            k = used.index(s['mat'])
            t = s['tris']
            for i in range(0, len(t) - 2, 3):
                faces.append((t[i], t[i + 2], t[i + 1]))
                fmat.append(k)
        if not faces:
            continue
        me = bpy.data.meshes.new(o['name'])
        me.from_pydata(verts, [], faces)
        uvl = me.uv_layers.new(name='UVMap')
        for poly in me.polygons:
            for li in poly.loop_indices:
                vi = me.loops[li].vertex_index
                uvl.data[li].uv = o['uv'][vi]
        for g in used:
            me.materials.append(mats[g])
        me.polygons.foreach_set('material_index', fmat)
        me.polygons.foreach_set('use_smooth', [not o['flat']] * len(faces))
        ob = bpy.data.objects.new(o['name'], me)
        bpy.context.scene.collection.objects.link(ob)

    sc = bpy.context.scene
    for l in data['lights']:
        sun = bpy.data.lights.new('Sun', 'SUN')
        sun.energy = l['intensity'] * 2.2
        sun.color = lin(l['color'])
        sun.angle = math.radians(1.0)
        ob = bpy.data.objects.new('Sun', sun)
        ob.rotation_euler = Vector(u2b(l['forward'])).to_track_quat('-Z', 'Y').to_euler()
        sc.collection.objects.link(ob)
    w = bpy.data.worlds.new('World')
    sc.world = w
    w.use_nodes = True
    wn = w.node_tree
    bg = wn.nodes['Background']
    amb = data['ambient']
    sky, eq = lin(amb.get('m_AmbientSkyColor', [0.4] * 3)), lin(amb.get('m_AmbientEquatorColor', [0.3] * 3))
    bg.inputs['Color'].default_value = [(a + b) / 2 for a, b in zip(sky, eq)] + [1]
    bg.inputs['Strength'].default_value = 1.0
    try:
        sc.render.engine = 'BLENDER_EEVEE'
    except TypeError:
        sc.render.engine = 'BLENDER_EEVEE_NEXT'
    sc.view_settings.view_transform = 'Standard'
    sc.render.film_transparent = False
    if hasattr(sc.eevee, 'use_shadows'):
        sc.eevee.use_shadows = True


def camera(name, target=None, overview=False):
    sc = bpy.context.scene
    cd = bpy.data.cameras.new(name)
    ob = bpy.data.objects.new(name, cd)
    sc.collection.objects.link(ob)
    if name in WIDE:
        tx, tz, dist, pitch, yaw = WIDE[name]
        p, y = math.radians(pitch), math.radians(yaw)
        # yaw 0 looks north; the camera sits behind the target, opposite the view direction
        ob.location = (tx - math.sin(y) * dist * math.cos(p), tz - math.cos(y) * dist * math.cos(p), dist * math.sin(p))
        ob.rotation_euler = (math.radians(90) - p, 0, -y)
        cd.lens_unit = 'FOV'
        cd.angle = math.radians(55)
        cd.clip_end = 2000
        sc.render.resolution_x, sc.render.resolution_y = 1200, 800
    elif overview:
        cd.type = 'ORTHO'
        cd.ortho_scale = 180
        ob.location = (0, -2, 150)
        ob.rotation_euler = (0, 0, 0)
        sc.render.resolution_x, sc.render.resolution_y = 1000, 1200
    else:
        tx, tz = target
        pitch = math.radians(60)
        ob.location = (tx, tz - 35 * math.cos(pitch), 35 * math.sin(pitch))
        ob.rotation_euler = (math.radians(30), 0, 0)
        cd.sensor_fit = 'VERTICAL'
        cd.angle = math.radians(50)
        cd.clip_end = 1000
        sc.render.resolution_x, sc.render.resolution_y = 540, 1169
    sc.render.resolution_percentage = 100
    sc.camera = ob
    return ob


def main():
    argv = sys.argv[sys.argv.index('--') + 1:]
    data = json.load(open(argv[0]))
    out_dir = argv[1]
    views = argv[2:] or ['overview'] + list(WIDE) + list(VIEWS)
    os.makedirs(out_dir, exist_ok=True)
    build(data)
    for v in views:
        camera(v, VIEWS.get(v), overview=(v == 'overview'))
        bpy.context.scene.render.filepath = os.path.join(out_dir, v + '.png')
        bpy.ops.render.render(write_still=True)
        print('preview_render: wrote', v)


main()
