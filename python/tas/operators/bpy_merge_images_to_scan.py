import bpy
import numpy as np
import math
import os

from mathutils import Vector
from .bpy_pcd_convert import pcdInterface
from tas.util import flatten

'''
src_dir: source directory where images are stored
position_name: basename (with extension) of position image
color_name: basename (with extension) of color image
out_dir: directory to output to
name: basename of output file (with '.pcd' extension)
'''

def merge_to_scan(src_dir, position_name, color_name, out_dir, name, color_gain=1.0):
    print("Merging...")

    scn = bpy.context.scene

    pos_path = os.path.join(src_dir, position_name)
    col_path = os.path.join(src_dir, color_name)

    if not os.path.exists(pos_path) or not os.path.exists(col_path):
        print ("Failed to find images to merge.")
        return

    if position_name not in bpy.data.images:
        bpy.data.images.load(pos_path)
    else:
        bpy.data.images[position_name].reload()

    if color_name not in bpy.data.images:
        bpy.data.images.load(col_path)
    else:
        bpy.data.images[color_name].reload()

    imgPos = bpy.data.images[position_name]
    imgCol = bpy.data.images[color_name]

    pxPosition = np.array(imgPos.pixels)
    pxColor = np.array(imgCol.pixels)

    w = imgPos.size[0]
    h = imgPos.size[1]

    imPosition = pxPosition.reshape(h, w, imgPos.channels)
    imColor = pxColor.reshape(h, w, imgCol.channels)

    imPosition = np.delete(imPosition, 3, 2)
    imColor = np.delete(imColor, 3, 2)
    imColor = imColor * color_gain
    imColor = imColor.clip(0,1.0)
    imColor = imColor * 255
    imColor = imColor.astype(np.uint8)


    if not name.endswith('.pcd'):
        name = name + '.pcd'

    pcd = pcdInterface()
    pcd.export_color_pointcloud(os.path.join(out_dir, name), imPosition, imColor)

    print("Merged %s and %s into %s" % (position_name, color_name, name))




