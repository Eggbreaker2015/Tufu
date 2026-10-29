"""Top-down ASCII height map of the generated meshes (W/# = wall under/over 12 m, digits = plateau height, s = stairs, | = spire, ~ = water, * = crystal, o = rock)."""
import sys, glob
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import meshdec
from meshlib import ASSETS
D = ASSETS + '/CrystalCanyon/Meshes/'
G=2.0; X0,X1,Z0,Z1=-70,70,-84,84
W=int((X1-X0)/G); H=int((Z1-Z0)/G)
hm=[[0.0]*W for _ in range(H)]; kind=[[' ']*W for _ in range(H)]
def raster(p, tag):
    M=meshdec.load(p); pos=M['pos']
    for sm in M['tris']:
        for i in range(0,len(sm),3):
            a,b,c=[pos[k] for k in sm[i:i+3]]
            xs=[a[0],b[0],c[0]]; zs=[a[2],b[2],c[2]]
            for gz in range(max(0,int((min(zs)-Z0)/G)), min(H-1,int((max(zs)-Z0)/G))+1):
                for gx in range(max(0,int((min(xs)-X0)/G)), min(W-1,int((max(xs)-X0)/G))+1):
                    px=X0+(gx+0.5)*G; pz=Z0+(gz+0.5)*G
                    def s(p1,p2,p3): return (p1[0]-p3[0])*(p2[1]-p3[1])-(p2[0]-p3[0])*(p1[1]-p3[1])
                    P=(px,pz);A=(a[0],a[2]);B=(b[0],b[2]);C=(c[0],c[2])
                    d1,d2,d3=s(P,A,B),s(P,B,C),s(P,C,A)
                    if (d1<0 or d2<0 or d3<0) and (d1>0 or d2>0 or d3>0): continue
                    y=max(a[1],b[1],c[1])
                    if y>hm[gz][gx]: hm[gz][gx]=y; kind[gz][gx]=tag
for p in glob.glob(D+'CC_Wall_*.asset'): raster(p,'W')
raster(D+'CC_Plateaus.asset','P'); raster(D+'CC_Stairs.asset','s'); raster(D+'CC_Spires.asset','|'); raster(D+'CC_Pool.asset','~')
raster(D+'CC_Crystals.asset','*'); raster(D+'CC_Rocks.asset','o')
for gz in range(H-1,-1,-1):
    row=''
    for gx in range(W):
        k=kind[gz][gx]; y=hm[gz][gx]
        if k=='W': row+= '#' if y>12 else 'W'
        elif k=='P': row+= str(min(9,int(round(y))))
        else: row+=k
    print('%4d %s' % (Z0+gz*G, row))
