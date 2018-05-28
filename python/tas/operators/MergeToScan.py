import bpy
import bmesh
import os

from .bpy_merge_images_to_scan import merge_to_scan

from bpy.props import (
		BoolProperty,
		BoolVectorProperty,
		CollectionProperty,
		StringProperty,
		)

class MergeImagesToScanOperator(bpy.types.Operator):
	"""Simple operator to scale UV coordinates for target objects"""
	bl_idname = "tas.merge_images_to_scan"
	bl_label = "tas - Merge to Scan"
	bl_options = {'REGISTER', 'UNDO'}

	name = StringProperty(
		name="Name",
		description="Name of output file.",
		default="test",
		)
	src_dir = StringProperty(
		name="Source directory",
		description="Directory with images.",
		default="C:/tmp/rendertest",
		)

	pos_name = StringProperty(
		name="Position name",
		description="Name of position image (with extension).",
		default="PositionPass0001.exr",
		)

	col_name = StringProperty(
		name="Color name",
		description="Name of color image (with extension).",
		default="ColorPass0001.exr",
		)

	out_dir = StringProperty(
		name="Source directory",
		description="Directory with images.",
		default="C:/tmp/rendertest",
		)
	#out_dir = CollectionProperty(
	#	name="Output directory",
	#	type=bpy.types.OperatorFileListElement,
	#	)


	#@classmethod
	#def poll(cls, context):
	#	return (context.mode == 'OBJECT')

	def draw(self, context):
		layout = self.layout
		col = layout.column()
		col.prop(self, "name")
		col.prop(self, "src_dir")
		col.prop(self, "pos_name")
		col.prop(self, "col_name")
		col.prop(self, "out_dir")

	def invoke(self, context, event):
		wm = context.window_manager
		return wm.invoke_props_dialog(self)

	def execute(self, context):
		if os.path.isdir(self.src_dir) and os.path.isdir(self.out_dir):
			pos_path = os.path.join(self.src_dir, self.pos_name)
			col_path = os.path.join(self.src_dir, self.col_name)

			if os.path.exists(pos_path) and os.path.exists(col_path):
				merge_to_scan(self.src_dir, self.pos_name, self.col_name, self.out_dir, self.name)
			else:
				print("Couldn't find %s or %s" % (self.pos_name, self.col_name))
		else:
			print ("Couldn't find source or target directories.")

		return {'FINISHED'}		

class MergeImagesToScanPanel(bpy.types.Panel):
	"""Creates a Panel in the Object properties window"""
	bl_space_type = 'VIEW_3D'
	bl_region_type = 'TOOLS'
	bl_category = 'tasTools'
	bl_context = "objectmode"
	bl_label = "Image Tools"

	def draw(self, context):
		layout = self.layout
		row = layout.row()
		row.operator("tas.merge_images_to_scan", text='Merge to Scan')
		#row.prop(MergeImagesToScanOperator, "name")
		#row.prop(MergeImagesToScanOperator, "src_dir")
		#row.prop(MergeImagesToScanOperator, "pos_name")
		#row.prop(MergeImagesToScanOperator, "col_name")
		#row.prop(MergeImagesToScanOperator, "out_dir")
