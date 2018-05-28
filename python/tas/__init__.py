bl_info = {
    "name": "tasTools",
    "author": "Tom Svilans",
    "version": (1, 0, 0),
    "blender": (2, 79, 0),
    "location": "Object > Tools",
    "warning": "",
    "description": "General toolkit.",
    "wiki_url": "http://www.tomsvilans.com"
                "Scripts/Modeling/tasTools",
    "category": "Object",
}
import bpy
from tas.operators.UvScale import UvScaleOperator
from tas.operators.WeldEdgeSplit import WeldEdgeSplitOperator
from tas.operators.PCDInterface import ImportPCD
from tas.operators.MergeToScan import MergeImagesToScanOperator
from tas.operators.PrintCustomPropsOperator import PrintCustomPropsOperator

from tas.ui import tasModellingPanel, tasScanningPanel

def register():
    bpy.utils.register_class(UvScaleOperator)
    bpy.utils.register_class(WeldEdgeSplitOperator)
    bpy.utils.register_class(ImportPCD)
    bpy.utils.register_class(MergeImagesToScanOperator)
    bpy.utils.register_class(PrintCustomPropsOperator)

    bpy.utils.register_class(tasModellingPanel)
    bpy.utils.register_class(tasScanningPanel)

def unregister():
    bpy.utils.unregister_class(UvScaleOperator)
    bpy.utils.unregister_class(WeldEdgeSplitOperator)
    bpy.utils.unregister_class(ImportPCD)
    bpy.utils.unregister_class(MergeImagesToScanOperator)    

    bpy.utils.unregister_class(tasModellingPanel)
    bpy.utils.unregister_class(tasScanningPanel)    

if __name__ == "__main__":
    register()

