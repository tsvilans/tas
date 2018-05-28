import bpy, bmesh

from bpy.props import (
        BoolProperty,
        BoolVectorProperty,
        FloatProperty,
        FloatVectorProperty,
        )

class WeldEdgeSplitOperator(bpy.types.Operator):
    """Simple operator to scale UV coordinates for target objects"""
    bl_idname = "tas.weld_edge_split"
    bl_label = "tas - Weld Mesh and Edge Split"
    bl_options = {'REGISTER', 'UNDO'}

    smooth = BoolProperty(
        name="Smooth",
        description="Set objects to smooth.",
        default=True,
        )

    bevel = BoolProperty(
        name="Bevel",
        description="Add bevel modifier.",
        default=True,
        )

    bevel_width = FloatProperty(
        name="Bevel width",
        description="Width of optional bevel.",
        default=0.003,
        min=0,
        max=1000,
        )

    def draw(self, context):
        layout = self.layout
        col = layout.column()
        col.prop(self, "smooth")
        col.prop(self, "bevel")
        col.prop(self, "bevel_width")

    def execute(self, context):
        bm = bmesh.new()

        for o in context.selected_objects:
            if o.type == 'MESH':
                bm.from_mesh(o.data)
                bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.001)
                bmesh.ops.recalc_face_normals(bm)
                bm.to_mesh(o.data)
                o.data.update()
                bm.clear()

                if self.bevel:
                    mod = o.modifiers.new('Bevel', 'BEVEL')
                    mod.limit_method = 'ANGLE'
                    mod.width = self.bevel_width

                mod = o.modifiers.new("Edge Split", 'EDGE_SPLIT')

                if self.smooth:
                    for f in o.data.polygons:
                        f.use_smooth = True

        bm.free()

        return {'FINISHED'}     
