import bpy
import bmesh

from tas.util import flatten, get_size

import ctypes, os
from ctypes import POINTER, c_double, c_int, c_char_p, byref, c_ubyte

class pcdInterface():
    
    def __init__(self):
        
        me = os.path.dirname(os.path.realpath(__file__))
        os.environ['PATH'] = me + os.pathsep + os.environ['PATH']

        dll_path = os.path.join(me, "pcd_convert.dll")
        print ("Current working directory: " + os.getcwd())
        print ("Dll path: " + dll_path)
        
        self.lib = ctypes.CDLL(dll_path)
        
        self.lib.export_pcd.argtypes = [c_char_p, c_int, POINTER(c_double), POINTER(c_int)]
        self.lib.export_color_pcd.argtypes = [c_char_p, c_int, POINTER(c_double), POINTER(c_ubyte)]

        self.lib.import_pcd.argtypes = [c_char_p, POINTER(POINTER(c_double)), POINTER(POINTER(c_int))]
        self.lib.import_pcd.restype = c_int
        self.lib.import_color_pcd.argtypes = [c_char_p, POINTER(POINTER(c_double)), POINTER(POINTER(c_int))]
        self.lib.import_color_pcd.restype = c_int

    def export_pointcloud(self, filepath, points, intensities):
        filepathPtr = c_char_p(filepath.encode('utf-8'))
        self.lib.export_pcd(filepathPtr, (c_int)(points.size // 3), points.ctypes.data_as(POINTER(c_double)), intensities.ctypes.data_as(POINTER(c_int)))

    def export_color_pointcloud(self, filepath, points, colors):
        filepathPtr = c_char_p(filepath.encode('utf-8'))
        self.lib.export_color_pcd(filepathPtr, (c_int)(points.size // 3), points.ctypes.data_as(POINTER(c_double)), colors.ctypes.data_as(POINTER(c_ubyte)))     
        
    def import_pointcloud(self, filepath):
        filepathPtr = c_char_p(filepath.encode('utf-8'))
        pointPtr = POINTER(c_double)()
        colorPtr = POINTER(c_int)()
        
        numPoints = self.lib.import_pcd(filepathPtr, byref(pointPtr), byref(colorPtr))
        print (numPoints)
        print('---')
        
        points = []
        colors = []
        j = 0
        for i in range(numPoints):
            points.append((pointPtr[j], pointPtr[j+1], pointPtr[j+2]))
            colors.append(colorPtr[i])
            j += 3
        
        return (points, colors)

    def import_color_pointcloud(self, filepath):
        filepathPtr = c_char_p(filepath.encode('utf-8'))
        pointPtr = POINTER(c_double)()
        colorPtr = POINTER(c_int)()
        
        numPoints = self.lib.import_color_pcd(filepathPtr, byref(pointPtr), byref(colorPtr))
        print (numPoints)
        print('---')
        
        points = []
        colors = []
        j = 0
        for i in range(numPoints):
            points.append((pointPtr[j], pointPtr[j+1], pointPtr[j+2]))
            colors.append((colorPtr[j], colorPtr[j+1], colorPtr[j+2]))
            j += 3
        
        return (points, colors)        

def import_pcd_as_mesh(filepath):  
    if not filepath: return
    bn = os.path.basename(filepath)
    
    exp = pcdInterface()
    res = exp.import_pointcloud(filepath)
    
    bm = bmesh.new()
    [bm.verts.new(x) for x in res[0]]
    
    me = bpy.data.meshes.new(bn)
    
    bm.to_mesh(me)
    bm.free()
    
    obj = bpy.data.objects.new(bn, me)
    
    bpy.context.scene.objects.link(obj)

if __name__ == "__main__":
    test_points = [(0,0,0), (1,0,0), (1,1,0), (0,1,0)]
    test_colors = [255, 128, 200, 255]
    path = "C:/tmp/sample_conversion.pcd"
    
    exp = pcdInterface()
    
    print("Attempting to write pointcloud...")
    exp.export_pointcloud(path, test_points, test_colors)
    print("Done.")
    
    print("Attempting to read pointcloud...")
    res = exp.import_pointcloud(path)
    print("Done.")
    
    print("Attempting to load pointcloud as mesh...")
    import_pcd_as_mesh(path)
    print("Done.")
    

        