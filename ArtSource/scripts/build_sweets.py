"""スイーツ 3D モデルの手続き生成スクリプト（Blender ヘッドレス実行用）。

実行例:
  /Applications/Blender.app/Contents/MacOS/Blender -b -P ArtSource/scripts/build_sweets.py

規約:
  - 直径 1 m・原点は中心で作成（Unity 側で SweetData.radius に応じて自動スケール）
  - 顔はモデルに含めない（Unity の SweetFace が顔クアッドを被せる）
  - .blend は ArtSource/Sweets/ へ、FBX は PoittoSweets/Assets/Models/Sweets/ へ出力
"""
import os

import bpy

ART_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # ArtSource/
REPO_ROOT = os.path.dirname(ART_ROOT)
FBX_DIR = os.path.join(REPO_ROOT, "PoittoSweets", "Assets", "Models", "Sweets")
BLEND_DIR = os.path.join(ART_ROOT, "Sweets")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block_list in (bpy.data.meshes, bpy.data.materials):
        for block in list(block_list):
            if block.users == 0:
                block_list.remove(block)


def make_material(name, color, roughness=0.45):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    if not mat.node_tree:
        mat.use_nodes = True  # Blender 5.0 では deprecated だがノードツリー生成に必要
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
        out = next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"), None)
        if out is None:
            out = nt.nodes.new("ShaderNodeOutputMaterial")
        nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def add_sphere(name, radius, scale_z, z, mat, segments=24, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=rings, radius=radius, location=(0, 0, z)
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (1, 1, scale_z)
    obj.data.materials.append(mat)
    bpy.ops.object.shade_smooth()
    return obj


def add_cylinder(name, radius, depth, z, mat, vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth, location=(0, 0, z)
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(mat)
    bpy.ops.object.shade_smooth()
    return obj


def join_all(final_name):
    bpy.ops.object.select_all(action="SELECT")
    bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = final_name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return obj


def export(name):
    os.makedirs(FBX_DIR, exist_ok=True)
    os.makedirs(BLEND_DIR, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLEND_DIR, f"{name}.blend"))
    bpy.ops.export_scene.fbx(filepath=os.path.join(FBX_DIR, f"{name}.fbx"), use_selection=False)
    print(f"[build_sweets] exported: {name}")


def build_t01_macaron():
    clear_scene()
    pink = make_material("MacaronPink", (0.93, 0.52, 0.62))
    cream = make_material("MacaronCream", (0.99, 0.95, 0.88), roughness=0.6)

    # 上下のシェル（厚めのぽってり形状）+ 間の薄いクリーム
    add_sphere("ShellTop", 0.5, 0.46, 0.15, pink)
    add_sphere("ShellBottom", 0.5, 0.46, -0.15, pink)
    add_cylinder("Cream", 0.46, 0.10, 0.0, cream)

    join_all("Sweet_T01_Macaron")
    export("Sweet_T01_Macaron")


build_t01_macaron()
print("[build_sweets] DONE")
