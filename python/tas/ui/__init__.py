import bpy

class tasModellingPanel(bpy.types.Panel):
	"""Creates a Panel in the Object properties window"""
	bl_space_type = 'VIEW_3D'
	bl_region_type = 'TOOLS'
	bl_category = 'tasTools'
	bl_context = "objectmode"
	bl_label = "Modelling"

	def draw(self, context):
		layout = self.layout
		col = layout.column()
		col.operator("tas.scale_uvs", text='Uv Scale')
		col.operator("tas.weld_edge_split", text='WeldEdgeSplit')
		col.operator("tas.print_custom_props", text='PrintCustomProps')

class tasScanningPanel(bpy.types.Panel):
    """Creates a Panel in the Object properties window"""
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'TOOLS'
    bl_category = 'tasTools'
    bl_context = "objectmode"
    bl_label = "Import PCD"

    def draw(self, context):
        layout = self.layout
        col = layout.column()
        col.operator("tas.import_pcd_as_mesh", text='Import PCD')
        col.operator("tas.merge_images_to_scan", text='Merge to Scan')		        