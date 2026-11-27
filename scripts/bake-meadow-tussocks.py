"""Bake the game-ready meadow clump atlas from the two Poly Haven source scenes.

Run with regular Python (Pillow installed), passing Blender, the directory with
the grass_medium_01 and grass_medium_02 Blender downloads, and a bake directory.
Keep the original textures folders alongside their .blend files.
"""

import argparse
import json
import math
from pathlib import Path
import subprocess
import sys


TILE = 256
RECIPES = (
    # Each entry specifies the source clump, east and north offsets, scale and yaw.
    ("grass_medium_01", (
        ("large_a_LOD0", -.12, -.09, .94, 28), ("large_c_LOD0", .12, .08, 1.02, 113),
        ("mid_b_LOD0", .05, -.10, .77, 60), ("small_a_LOD0", -.10, .13, .86, 118),
    )),
    ("grass_medium_01", (
        ("large_b_LOD0", -.13, -.06, 1.12, 32), ("mid_a_LOD0", .03, .04, 1.2, 139),
        ("large_c_LOD0", .12, .10, 1.02, 249), ("tall_b_LOD0", -.05, .03, 1.2, 61),
    )),
    ("grass_medium_02", (
        ("e", -.065, 0, 1.06, 15), ("e", .082, .022, .93, 123),
        ("d", -.106, -.051, 1.04, 69), ("d", .110, .079, 1.03, 185),
        ("c", .064, -.100, .91, 289), ("b", -.070, .090, .91, 211),
    )),
    ("grass_medium_02", (
        ("d", -.12, .045, 1.04, 37), ("b", .13, -.074, 1.25, 109),
        ("c", .09, .10, .92, 155), ("e", -.025, -.025, .94, 231),
    )),
)


def source_image(name):
    import bpy

    return next(image for image in bpy.data.images if image.filepath and name in image.name)


def create_bake_material(dense_crown):
    import bpy

    material = bpy.data.materials.new("Meadow cutout bake")
    material.use_nodes = True
    tree = material.node_tree
    tree.nodes.clear()
    rgb = tree.nodes.new("ShaderNodeTexImage")
    rgb.image = source_image("_diff.")
    mask = tree.nodes.new("ShaderNodeTexImage")
    mask.image = source_image("_alpha.")
    clip = tree.nodes.new("ShaderNodeMath")
    clip.operation = "GREATER_THAN"
    clip.inputs[1].default_value = .36
    tree.links.new(mask.outputs["Color"], clip.inputs[0])

    # Ambient occlusion stays in the asset. The game lights the crown for its actual sun direction.
    ao = tree.nodes.new("ShaderNodeAmbientOcclusion")
    ao.samples = 6
    ao.inputs["Distance"].default_value = .095
    tint = tree.nodes.new("ShaderNodeMixRGB")
    tint.blend_type = "MULTIPLY"
    tint.inputs[0].default_value = .52
    tree.links.new(rgb.outputs["Color"], tint.inputs[1])
    tree.links.new(ao.outputs["AO"], tint.inputs[2])
    diffuse = tint.outputs["Color"]
    if dense_crown:
        # Match the live, shaded inner leaves to the meadow. The dry outer variant keeps its source color.
        crown = tree.nodes.new("ShaderNodeMixRGB")
        crown.blend_type = "MULTIPLY"
        crown.inputs[0].default_value = 1
        crown.inputs[2].default_value = (.45, .63, .40, 1)
        tree.links.new(diffuse, crown.inputs[1])
        diffuse = crown.outputs["Color"]
    color = tree.nodes.new("ShaderNodeEmission")
    tree.links.new(diffuse, color.inputs["Color"])
    mix = tree.nodes.new("ShaderNodeMixShader")
    tree.links.new(clip.outputs[0], mix.inputs[0])
    tree.links.new(tree.nodes.new("ShaderNodeBsdfTransparent").outputs[0], mix.inputs[1])
    tree.links.new(color.outputs[0], mix.inputs[2])
    tree.links.new(mix.outputs[0], tree.nodes.new("ShaderNodeOutputMaterial").inputs["Surface"])
    return material


def bake_frames(sources, work):
    import bpy
    from mathutils import Vector

    for index, (source, clumps) in enumerate(RECIPES):
        bpy.ops.wm.open_mainfile(filepath=str(sources / source / f"{source}_1k.blend"), load_ui=False)
        scene = bpy.context.scene
        for obj in scene.objects:
            obj.hide_render = True
        material = create_bake_material(dense_crown=index == 2)
        for mesh, x, y, scale, yaw in clumps:
            original = bpy.data.objects[f"{source}_{mesh}"]
            obj = bpy.data.objects.new("Tussock", original.data.copy())
            scene.collection.objects.link(obj)
            obj.location = (x, y, .013)
            obj.rotation_euler.z = math.radians(yaw)
            obj.scale = original.matrix_world.to_scale() * scale
            obj.data.materials.clear()
            obj.data.materials.append(material)

        camera = bpy.data.objects.new("Meadow atlas camera", bpy.data.cameras.new("Orthographic"))
        scene.collection.objects.link(camera)
        scene.camera = camera
        camera.data.type = "ORTHO"
        camera.data.ortho_scale = .84
        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = 18
        scene.cycles.transparent_max_bounces = 32
        scene.render.film_transparent = True
        scene.render.resolution_x = 512
        scene.render.resolution_y = 352
        scene.render.resolution_percentage = 100
        scene.render.image_settings.file_format = "PNG"
        scene.render.image_settings.color_mode = "RGBA"
        scene.view_settings.view_transform = "Standard"
        scene.view_settings.look = "None"
        scene.view_settings.exposure = 0
        scene.view_settings.gamma = 1
        focus = Vector((0, 0, .235))
        for face, offset in enumerate(((0, -3, .56), (3, 0, .56), (0, 0, 3))):
            camera.location = focus + Vector(offset)
            camera.rotation_euler = ((focus - camera.location).to_track_quat("-Z", "Y").to_euler()
                                     if face < 2 else (0, 0, 0))
            scene.render.filepath = str(work / f"clump-{index}-{face}.png")
            bpy.ops.render.render(write_still=True)


def main():
    from PIL import Image

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("blender", type=Path)
    parser.add_argument("sources", type=Path)
    parser.add_argument("work", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    args.work.mkdir(parents=True, exist_ok=True)
    subprocess.run([
        str(args.blender), "-b", "-Y", "--factory-startup", "-t", "8",
        "--python-exit-code", "2", "--python", str(Path(__file__).resolve()),
        "--", str(args.sources.resolve()), str(args.work.resolve()),
    ], check=True)
    atlas = Image.new("RGBA", (4 * TILE, 4 * TILE))
    frames = []
    for index in range(len(RECIPES)):
        for face in range(3):
            path = args.work / f"clump-{index}-{face}.png"
            with Image.open(path) as frame:
                cell = frame.resize((TILE, TILE), Image.Resampling.LANCZOS)
            # Retain a border around the mip-filtered silhouette while omitting empty card area.
            bounds = cell.getchannel("A").point(lambda alpha: 255 if alpha >= 24 else 0).getbbox()
            if bounds is None:
                raise ValueError(f"Empty grass bake: {path}")
            x0, y0, x1, y1 = bounds
            frames.append({
                "x": round(max(1, x0 - 4) / TILE, 6), "y": round(max(1, TILE - y1 - 4) / TILE, 6),
                "z": round(min(TILE - 1, x1 + 4) / TILE, 6), "w": round(min(TILE - 1, TILE - y0 + 4) / TILE, 6),
            })
            atlas.paste(cell, (face * TILE, index * TILE))
    atlas.save(args.output)
    layout = args.output.with_name(f"{args.output.stem}Layout.json")
    layout.write_text(json.dumps({"frames": frames}, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    if "--" in sys.argv:
        bake_frames(*(Path(arg) for arg in sys.argv[sys.argv.index("--") + 1:]))
    else:
        main()
