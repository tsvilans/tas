import bpy, json

def print_object_properties(obj):
    print(obj.name)
    for k in obj.keys():
        #if k == "speckle": continue
        val = obj[k]
        if hasattr(val, 'to_dict'):
            val = val.to_dict()
        print(k)
        print(json.dumps(val, indent=4, sort_keys=True))
    print()

class PrintCustomPropsOperator(bpy.types.Operator):
	"""Simple operator to scale UV coordinates for target objects"""
	bl_idname = "tas.print_custom_props"
	bl_label = "tas - Print custom properties"
	bl_options = {'REGISTER', 'UNDO'}



	def draw(self, context):
		layout = self.layout

	def execute(self, context):
		for obj in context.selected_objects:
			print_object_properties(obj)

		return {'FINISHED'}		