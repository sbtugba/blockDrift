"""Onaylı mesh verisini değiştirmeden yerel pivotlu bağımsız FBX kopyaları dışa aktarır."""
import bpy, json, hashlib, array
from pathlib import Path
from mathutils import Matrix

ROOT = Path('C:/Users/sahba/BlockRift')
OUT = ROOT / 'Assets/_BlockRift/Art/Models/Gameplay'
OUT.mkdir(parents=True, exist_ok=True)
SOURCES = {
    'Block_1x1': ['Block_1x1_Export'],
    'Block_2x1': ['Block_2x1_Export'],
    'Block_3x1': ['Block_3x1'],
    'Block_4x1': ['Block_4x1_Export'],
    'Block_2x2': ['Block_2x2_Export'],
    'Special_1x1_Star': ['Special_1x1_Star_Export'],
    'Obstacle_1x1_Stone': ['Obstacle_1x1_Stone_Export'],
    'Board_6x6': ['Board_6x6_Base', 'Board_6x6_Frame', 'Board_6x6_Cells'],
}

def signature(obj):
    mesh = obj.data
    payload = {
        'v': [tuple(v.co) for v in mesh.vertices],
        'faces': [tuple(p.vertices) for p in mesh.polygons],
        'smooth': [p.use_smooth for p in mesh.polygons],
        'material_indices': [p.material_index for p in mesh.polygons],
        'normals': [tuple(n.vector) for n in mesh.corner_normals],
        'uv': [[tuple(v.uv) for v in layer.data] for layer in mesh.uv_layers],
        'matrix': [list(row) for row in obj.matrix_world],
        'materials': [m.name if m else None for m in mesh.materials],
    }
    return hashlib.sha256(json.dumps(payload, sort_keys=True).encode()).hexdigest()

objects = {name: bpy.data.objects[name] for names in SOURCES.values() for name in names}
before = {name: signature(o) for name, o in objects.items()}
blend_hash = hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest()
original_scene = bpy.context.window.scene
selected = list(bpy.context.selected_objects)
active = bpy.context.view_layer.objects.active
manifest = {'source_blend': str(Path(bpy.data.filepath).relative_to(ROOT)), 'blend_sha256': blend_hash,
            'grid_pitch': 1, 'normal_status': 'REVIEW_REQUIRED_UNCHANGED', 'assets': [], 'materials': []}
material_names = set()
temp = bpy.data.scenes.new('BR_Export_Transient')
try:
    bpy.context.window.scene = temp
    temp.unit_settings.scale_length = 1
    for asset, names in SOURCES.items():
        copies = []
        try:
            for name in names:
                source = objects[name]
                assert not source.modifiers, name
                copy = source.copy()  # Mesh paylaşılır; vertex/normal/UV hiçbir zaman değiştirilmez.
                copy.parent = None
                copy.matrix_world = Matrix.Identity(4)
                temp.collection.objects.link(copy)
                copy.hide_set(False)
                copy.hide_viewport = False
                copy.hide_render = False
                copy.select_set(True)
                copies.append(copy)
                material_names.update(m.name for m in source.data.materials if m)
            bpy.context.view_layer.objects.active = copies[0]
            path = OUT / (asset + '.fbx')
            bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'MESH'},
                global_scale=1, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                use_mesh_modifiers=False, mesh_smooth_type='OFF', use_tspace=True,
                bake_anim=False, add_leaf_bones=False, path_mode='AUTO', embed_textures=False)
            manifest['assets'].append({'id': asset, 'sources': names,
                'fbx': str(path.relative_to(ROOT)), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                'triangles': sum(len(p.vertices)-2 for n in names for p in objects[n].data.polygons),
                'source_fingerprints': {n: before[n] for n in names},
                'opposed_normal_corners': sum(1 for n in names for p in objects[n].data.polygons
                    for li in p.loop_indices if objects[n].data.corner_normals[li].vector.dot(p.normal) < -0.0001)})
        finally:
            for copy in copies:
                bpy.data.objects.remove(copy, do_unlink=True)
    for name in sorted(material_names):
        mat = bpy.data.materials[name]
        node = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        base = list(node.inputs['Base Color'].default_value)
        if name == 'BR_Stone_Final_URP':
            mix = next(n for n in mat.node_tree.nodes if n.type == 'MIX_RGB')
            print('STONE_MIX_INPUTS',[(i.name, list(i.default_value) if hasattr(i.default_value,'__len__') else i.default_value) for i in mix.inputs])
            base = list(mix.inputs[2].default_value)
        manifest['materials'].append({'name': name, 'linear_color': base,
            'metallic': node.inputs['Metallic'].default_value, 'roughness': node.inputs['Roughness'].default_value})
finally:
    bpy.context.window.scene = original_scene
    bpy.data.scenes.remove(temp)
    for obj in original_scene.objects:
        obj.select_set(obj in selected)
    bpy.context.view_layer.objects.active = active
after = {name: signature(o) for name, o in objects.items()}
assert before == after, 'Source data changed'
assert hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest() == blend_hash
manifest['sources_unchanged'] = True
(ROOT/'ArtProduction/UnityIntegration/export_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
print(json.dumps({'exported': [a['id'] for a in manifest['assets']], 'sources_unchanged': True, 'normal_status':manifest['normal_status']}))
