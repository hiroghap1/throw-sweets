"""スイーツ 3D モデルの手続き生成スクリプト（Blender ヘッドレス実行用）。

実行例:
  /Applications/Blender.app/Contents/MacOS/Blender -b -P ArtSource/scripts/build_sweets.py

規約:
  - 直径 1 m・原点は中心で作成（Unity 側で SweetData.radius に応じて自動スケール）
  - 顔はモデルに含めない（Unity の SweetFace が顔クアッドを被せる）
  - .blend は ArtSource/Sweets/ へ、FBX は PoittoSweets/Assets/Models/Sweets/ へ出力
"""
import math
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


def add_cylinder(name, radius, depth, z, mat, vertices=24, loc=None, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth,
        location=loc or (0, 0, z), rotation=rot,
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(mat)
    bpy.ops.object.shade_smooth()
    return obj


def add_cone(name, r_bottom, r_top, depth, z, mat, vertices=24):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices, radius1=r_bottom, radius2=r_top, depth=depth, location=(0, 0, z)
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


def build_t02_choux():
    clear_scene()
    puff = make_material("ChouxPuff", (0.85, 0.62, 0.38), roughness=0.55)
    choco = make_material("ChouxChoco", (0.32, 0.18, 0.10), roughness=0.35)

    # シュー生地 + 上のチョコがけ
    add_sphere("Puff", 0.5, 0.78, -0.05, puff)
    add_sphere("Choco", 0.40, 0.5, 0.22, choco)

    join_all("Sweet_T02_ChocoPuff")
    export("Sweet_T02_ChocoPuff")


def build_t03_cupcake():
    clear_scene()
    cup = make_material("CupcakeCup", (0.45, 0.27, 0.16), roughness=0.5)
    whip = make_material("CupcakeWhip", (0.99, 0.95, 0.88), roughness=0.55)

    # カップ（上広がりの台形） + 3 段ホイップ
    add_cone("Cup", 0.30, 0.42, 0.4, -0.25, cup)
    add_sphere("Whip1", 0.40, 0.55, 0.0, whip)
    add_sphere("Whip2", 0.29, 0.55, 0.18, whip)
    add_sphere("Whip3", 0.17, 0.60, 0.34, whip)

    join_all("Sweet_T03_Cupcake")
    export("Sweet_T03_Cupcake")


def build_t04_rollcake():
    clear_scene()
    outer = make_material("RollOuter", (0.55, 0.33, 0.20), roughness=0.5)
    cream = make_material("RollCream", (0.99, 0.95, 0.88), roughness=0.55)

    # 本体（X 軸に沿って横倒し）+ 両端のクリーム断面 + 上の飾りホイップ
    rot_y90 = (0, math.radians(90), 0)
    add_cylinder("Roll", 0.42, 0.9, 0, outer, vertices=28, rot=rot_y90)
    add_cylinder("CreamL", 0.36, 0.02, 0, cream, loc=(-0.455, 0, 0), rot=rot_y90)
    add_cylinder("CreamR", 0.36, 0.02, 0, cream, loc=(0.455, 0, 0), rot=rot_y90)
    add_sphere("Topping", 0.14, 0.7, 0.46, cream)

    join_all("Sweet_T04_RollCake")
    export("Sweet_T04_RollCake")


build_t01_macaron()
build_t02_choux()
build_t03_cupcake()
build_t04_rollcake()
print("[build_sweets] DONE")
