"""Editable Blender furniture kit; one UV-mapped, unit-bounds mesh per FBX."""
import bpy, math, os
from mathutils import Vector
ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(ROOT, '../../Assets/MeixiEcho/Resources/ChapterThree/Models'))
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mesh(name, vertices, faces):
    m=bpy.data.meshes.new(name); m.from_pydata(vertices, [], faces); m.update()
    o=bpy.data.objects.new(name,m); bpy.context.collection.objects.link(o)
    bpy.context.view_layer.objects.active=o; o.select_set(True)
    for p in m.polygons: p.use_smooth=True
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.uv.smart_project(island_margin=.015)
    bpy.ops.object.mode_set(mode='OBJECT')
    return o

def cloth(name, pillow=False):
    n=40; vs=[]; fs=[]
    for side in range(2):
        for j in range(n+1):
            y=j/n*2-1
            for i in range(n+1):
                x=i/n*2-1
                edge=max(0,(1-x*x)*(1-y*y))
                # A stitched perimeter with a soft inflated center and shallow wrinkles.
                z=(.02+.20*edge**.45) if pillow else (.04+.022*math.sin(x*18+y*4)+.012*math.sin(y*29-x*8))*edge**.18
                if side: z=-.02-(.11*edge**.5 if pillow else .025)
                vs.append((x*.5*(1-.055*abs(y)**8),y*.5*(1-.055*abs(x)**8),z))
    stride=(n+1)**2
    for s in range(2):
        for j in range(n):
            for i in range(n):
                a=s*stride+j*(n+1)+i; f=(a,a+1,a+n+2,a+n+1)
                fs.append(f if s==0 else f[::-1])
    ring=list(range(n+1))+[j*(n+1)+n for j in range(1,n+1)]+[n*(n+1)+i for i in range(n-1,-1,-1)]+[j*(n+1) for j in range(n-1,0,-1)]
    for a,b in zip(ring,ring[1:]+ring[:1]): fs.append((a,b,b+stride,a+stride))
    return mesh(name,vs,fs)

def lathe(name,profile,pleat=0):
    vs=[]; fs=[]; n=96
    for r,z in profile:
        for i in range(n):
            a=i*2*math.pi/n; radius=r+(pleat*math.cos(a*32) if r>.1 else 0)
            vs.append((radius*math.cos(a),radius*math.sin(a),z))
    for j in range(len(profile)-1):
        for i in range(n):
            a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
    return mesh(name,vs,fs)

objects=[cloth('TailoredQuilt'),cloth('SeamedPillow',True),
 lathe('PorcelainBasin',[(.005,0),(.29,0),(.40,.10),(.49,.38),(.50,.46),(.485,.5),(.445,.5),(.43,.44),(.35,.20),(.22,.13),(.04,.13),(.025,.09),(.005,.09)]),
 lathe('PleatedLampshade',[(.49,0),(.50,.015),(.33,.98),(.32,1),(.30,1),(.31,.98),(.48,.015),(.49,0)],.008),
 lathe('CeramicPlanter',[(.005,0),(.30,0),(.37,.08),(.47,.70),(.50,.91),(.50,.97),(.47,1),(.43,.98),(.43,.92),(.39,.70),(.29,.10),(.005,.10)])]
for o in objects:
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,o.name+'.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'HomestayFurniture.blend'))
print('Exported five furniture meshes:', OUT)
