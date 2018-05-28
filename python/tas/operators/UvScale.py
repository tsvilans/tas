import bpy
import bmesh

def scale_uvs(obj, x, y):
	if obj.data.uv_layers.active is None:
		print ("WARNING: %s does not have any UV layers." % obj.name)
		return False

	for loop in obj.data.loops:
		uv_coords = obj.data.uv_layers.active.data[loop.index].uv
		uv_coords[0] = uv_coords[0] * x
		uv_coords[1] = uv_coords[1] * y
		obj.data.uv_layers.active.data[loop.index].uv = uv_coords

	return True

from bpy.props import (
		BoolProperty,
		BoolVectorProperty,
		FloatProperty,
		FloatVectorProperty,
		)

class UvScaleOperator(bpy.types.Operator):
	"""Simple operator to scale UV coordinates for target objects"""
	bl_idname = "tas.scale_uvs"
	bl_label = "tas - Uv Scale"
	bl_options = {'REGISTER', 'UNDO'}

	scale_x = FloatProperty(
		name="ScaleX",
		description="Amount to scale in X-direction.",
		min=0.0,
		max=1000000000.0,
		default=1.0,
		)

	scale_y = FloatProperty(
		name="ScaleY",
		description="Amount to scale in Y-direction.",
		min=0.0,
		max=1000000000.0,
		default=1.0,
		)

	#@classmethod
	#def poll(cls, context):
	#	return (context.mode == 'OBJECT')

	def draw(self, context):
		layout = self.layout
		col = layout.column()
		col.prop(self, "scale_x")
		col.prop(self, "scale_y")

	def execute(self, context):

		for o in context.selected_objects:
			if o is not None and o.type == 'MESH':
				res = scale_uvs(o, self.scale_x, self.scale_y)

		return {'FINISHED'}		



