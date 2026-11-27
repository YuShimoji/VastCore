# DS-011: GameProjects の GLB を、Unity が標準で取り込める FBX（形と材質の名前だけ。画像は埋め込まない）へ変換する。
# 本人の選択（2026-11-27）で FBX を Assets/Art/Models/ に commit する経路にした。素材は RegionPropAssets.SurfaceFor の共有の PBR。
# 使い方: blender -b --factory-startup --python scripts/glb-to-fbx.py -- <GLB のルート（…/Assets/Art/Models）> <FBX の出力ルート>
# 出力は同じ <ドメイン>/<カタログ ID>/<名前>.fbx の並びと、出力ルートの _glb_to_fbx_report.json（Assets/Art/Models へ写すのは FBX だけ）。
# Blender の glTF 取り込みは Y 上を Z 上へ直すので、
# FBX は -Z 前・Y 上・空間の変換を焼き込み（Unity で回転 0・倍率 1 で入る）で書く。
import bpy, sys, os, glob, json
argv = sys.argv[sys.argv.index('--') + 1:]
src, dst = argv[0], argv[1]
done = []
for path in sorted(glob.glob(os.path.join(src, '**', '*.glb'), recursive=True)):
    rel = os.path.relpath(path, src)
    out = os.path.join(dst, os.path.splitext(rel)[0] + '.fbx')
    os.makedirs(os.path.dirname(out), exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    for image in list(bpy.data.images):
        bpy.data.images.remove(image)
    bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={'EMPTY', 'MESH'},
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True,
                             axis_forward='-Z', axis_up='Y', mesh_smooth_type='OFF', use_mesh_modifiers=True,
                             add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False)
    tris = sum(len(p.vertices) - 2 for o in bpy.data.objects if o.type == 'MESH' for p in o.data.polygons)
    done.append({'glb': rel, 'fbx': os.path.relpath(out, dst), 'triangles': tris, 'bytes': os.path.getsize(out)})
with open(os.path.join(dst, '_glb_to_fbx_report.json'), 'w', encoding='utf-8') as f:
    json.dump(done, f, indent=1)
print('converted', len(done))
