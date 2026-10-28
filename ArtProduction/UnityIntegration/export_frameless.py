"""Mevcut onaylı çerçevesiz varyantı geometri değiştirmeden bağımsız FBX olarak aktarır."""
import bpy, ast, json, hashlib
from pathlib import Path
from mathutils import Matrix

ROOT = Path(__file__).resolve().parents[2]
# Önceki exporter'ın parmak izi yordamı yeniden kullanılır; tüm assetleri yeniden export etmeyiz.
tree = ast.parse((ROOT/'ArtProduction/UnityIntegration/export_approved.py').read_text(encoding='utf8'))
function = next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='signature')
exec(compile(ast.Module(body=[function],type_ignores=[]),'signature','exec'))
names = ['Board_6x6_Frameless_Support','Board_6x6_Frameless_Cells']
protected = {o.name: signature(o) for o in bpy.data.objects if o.type=='MESH'}
source_hash = hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest()
previous = bpy.context.window.scene
selected = list(bpy.context.selected_objects)
active = bpy.context.view_layer.objects.active
temp = bpy.data.scenes.new('BR_Frameless_Export_Transient')
copies=[]
try:
    bpy.context.window.scene=temp
    temp.unit_settings.scale_length=1
    for name in names:
        obj=bpy.data.objects[name].copy();obj.parent=None;obj.matrix_world=Matrix.Identity(4)
        temp.collection.objects.link(obj);obj.hide_set(False);obj.hide_render=False;obj.hide_viewport=False
        obj.select_set(True);copies.append(obj)
    bpy.context.view_layer.objects.active=copies[0]
    output=ROOT/'Assets/_BlockRift/Art/Models/Gameplay/Board_6x6_Frameless.fbx'
    bpy.ops.export_scene.fbx(filepath=str(output),use_selection=True,object_types={'MESH'},global_scale=1,
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=True,
        use_mesh_modifiers=False,mesh_smooth_type='OFF',use_tspace=True,bake_anim=False,add_leaf_bones=False)
finally:
    bpy.context.window.scene=previous
    for obj in copies:bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.scenes.remove(temp)
    for obj in previous.objects:obj.select_set(obj in selected)
    bpy.context.view_layer.objects.active=active
assert protected == {name:signature(bpy.data.objects[name]) for name in protected}
assert hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest()==source_hash
report={'sources':names,'file':str(output.relative_to(ROOT)),'sha256':hashlib.sha256(output.read_bytes()).hexdigest(),
    'source_blend_unchanged':True,'all_meshes_unchanged':True,'logical_cell_pitch':1,'logical_surface':0,
    'visual_cell_top':-0.001,'cells_shared_with_original':bpy.data.objects[names[1]].data==bpy.data.objects['Board_6x6_Cells'].data}
(ROOT/'ArtProduction/UnityIntegration/frameless_export.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
