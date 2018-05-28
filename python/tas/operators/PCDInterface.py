import bpy
import os

from .bpy_pcd_convert import pcdInterface, import_pcd_as_mesh

from bpy_extras.io_utils import ImportHelper
from bpy.props import StringProperty, BoolProperty
from bpy.types import Operator


class ImportPCD(Operator, ImportHelper):
    """This appears in the tooltip of the operator and in the generated docs"""
    bl_idname = "tas.import_pcd_as_mesh"  # important since its how bpy.ops.import_test.some_data is constructed
    bl_label = "tas - Import PCD"

    # ImportHelper mixin class uses this
    filename_ext = ".pcd"

    filter_glob = StringProperty(
            default="*.pcd",
            options={'HIDDEN'},
            maxlen=255,  # Max internal buffer length, longer would be clamped.
            )

    # List of operator properties, the attributes will be assigned
    # to the class instance from the operator settings before calling.
    import_all = BoolProperty(
            name="Import all",
            description="Import all scans in current folder.",
            default=False,
            )

    def execute(self, context):

        if self.import_all:
            directory = os.path.dirname(self.filepath)
            files = os.listdir(directory)
            for f in files: 
                if f.endswith('.pcd'):
                    import_pcd_as_mesh(os.path.join(directory,f))
        else:
            import_pcd_as_mesh(self.filepath)
        return {'FINISHED'}

class ImportPCDPanel(bpy.types.Panel):
    """Creates a Panel in the Object properties window"""
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'TOOLS'
    bl_category = 'tasTools'
    bl_context = "objectmode"
    bl_label = "Import PCD"

    def draw(self, context):
        layout = self.layout
        row = layout.row()
        row.operator("tas.import_pcd_as_mesh", text='Import PCD')