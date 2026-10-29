import re,struct,math,sys,os
FMT={0:('f',4),1:('e',2),2:('B',1),3:('b',1),4:('H',2),5:('h',2),6:('B',1),7:('b',1),8:('H',2),9:('h',2),10:('I',4),11:('i',4)}
def unpack_bits(hexdata,n,bits):
    data=bytes.fromhex(hexdata) if hexdata else b''
    out=[];ip=0;bp=0
    for i in range(n):
        v=0;b=0
        while b<bits:
            v|=(data[ip]>>bp)<<b
            num=min(bits-b,8-bp); bp+=num; b+=num
            if bp==8: ip+=1; bp=0
        out.append(v&((1<<bits)-1))
    return out
def pfv(block,name):
    m=re.search(r'\n    '+name+r':\n      m_NumItems: (\d+)\n(?:      m_Range: ([-\d.eE]+)\n      m_Start: ([-\d.eE]+)\n)?      m_Data: ?(\w*)\n      m_BitSize: (\d+)',block)
    if not m: return None
    n=int(m.group(1)); bits=int(m.group(5))
    if n==0: return []
    ints=unpack_bits(m.group(4),n,bits)
    if m.group(2) is None: return ints
    rng=float(m.group(2)); st=float(m.group(3)); mx=(1<<bits)-1
    return [st+rng*i/mx for i in ints]
def load(path):
    s=open(path,errors='ignore').read()
    name=re.search(r'm_Name: (.*)',s).group(1)
    subs=[]
    for m in re.finditer(r'firstByte: (\d+)\n    indexCount: (\d+)\n    topology: (\d+)\n    baseVertex: (\d+)\n    firstVertex: (\d+)\n    vertexCount: (\d+)\n    localAABB:\n      m_Center: \{x: ([-\d.eE]+), y: ([-\d.eE]+), z: ([-\d.eE]+)\}\n      m_Extent: \{x: ([-\d.eE]+), y: ([-\d.eE]+), z: ([-\d.eE]+)\}',s):
        g=m.groups(); subs.append(dict(firstByte=int(g[0]),indexCount=int(g[1]),baseVertex=int(g[3]),firstVertex=int(g[4]),vertexCount=int(g[5]),center=[float(x) for x in g[6:9]],extent=[float(x) for x in g[9:12]]))
    aabb=re.search(r'm_LocalAABB:\n    m_Center: \{x: ([-\d.eE]+), y: ([-\d.eE]+), z: ([-\d.eE]+)\}\n    m_Extent: \{x: ([-\d.eE]+), y: ([-\d.eE]+), z: ([-\d.eE]+)\}',s)
    aabb=[float(x) for x in aabb.groups()]
    comp=int(re.search(r'm_MeshCompression: (\d+)',s).group(1))
    readable=re.search(r'm_IsReadable: (\d+)',s).group(1)
    M=dict(name=name,subs=subs,aabb=aabb,comp=comp,readable=readable,pos=[],nrm=[],uv=[],col=[],uv1=[],tris=[])
    vc=int(re.search(r'm_VertexCount: (\d+)',s).group(1))
    if vc>0:
        chans=re.findall(r'- stream: (\d+)\n      offset: (\d+)\n      format: (\d+)\n      dimension: (\d+)',s.split('m_VertexData:')[1])
        chans=[tuple(int(x) for x in c) for c in chans]
        data=bytes.fromhex(re.search(r'_typelessdata: (\w*)',s).group(1))
        # stream strides
        streams={}
        for ci,(st,off,fm,dim) in enumerate(chans):
            if dim==0: continue
            sz=FMT[fm][1]*(dim&0xF)
            streams.setdefault(st,0); streams[st]=max(streams[st],off+sz)
        # stride per stream aligned 4
        stride={k:(v+3)//4*4 for k,v in streams.items()}
        # stream offsets
        soff={};o=0
        for k in sorted(stride): soff[k]=o; o+=stride[k]*vc; o=(o+15)//16*16
        def read(ci):
            st,off,fm,dim=chans[ci]; dim&=0xF
            if dim==0: return []
            f,sz=FMT[fm]; out=[]
            for v in range(vc):
                b=soff[st]+v*stride[st]+off
                vals=struct.unpack('<'+f*dim,data[b:b+sz*dim])
                if fm==2: vals=[x/255 for x in vals]
                out.append(list(vals))
            return out
        M['pos']=read(0); M['nrm']=read(1); M['col']=read(3); M['uv']=read(4); M['uv1']=read(5)
        ib=bytes.fromhex(re.search(r'm_IndexBuffer: (\w*)',s).group(1))
        fmt32=re.search(r'm_IndexFormat: (\d+)',s).group(1)=='1'
        idx=list(struct.unpack('<'+('I' if fmt32 else 'H')*(len(ib)//(4 if fmt32 else 2)),ib))
        for sm in subs:
            st=sm['firstByte']//(4 if fmt32 else 2)
            M['tris'].append([i+sm['baseVertex'] for i in idx[st:st+sm['indexCount']]])
    else:
        cm=s.split('m_CompressedMesh:')[1]
        v=pfv(cm,'m_Vertices') or []
        M['pos']=[v[i:i+3] for i in range(0,len(v),3)]
        nv=len(M['pos'])
        uvinfo=int(re.search(r'm_UVInfo: (\d+)',cm).group(1))
        uv=pfv(cm,'m_UV') or []
        # channel layout
        o=0
        for ch in range(8):
            inf=(uvinfo>>(ch*4))&0xF
            if inf&4:
                dim=(inf&3)+1
                arr=[uv[o+i*dim:o+i*dim+dim] for i in range(nv)]
                if ch==0: M['uv']=arr
                if ch==1: M['uv1']=arr
                o+=dim*nv
        if uvinfo==0 and len(uv)>=2*nv: M['uv']=[uv[i*2:i*2+2] for i in range(nv)]
        nr=pfv(cm,'m_Normals') or []
        sg=pfv(cm,'m_NormalSigns') or []
        for i in range(len(nr)//2):
            x,y=nr[2*i],nr[2*i+1]; z=math.sqrt(max(0,1-x*x-y*y))
            if sg and sg[i]==0: z=-z
            M['nrm'].append([x,y,z])
        fc=pfv(cm,'m_FloatColors') or []
        M['col']=[fc[i:i+4] for i in range(0,len(fc),4)]
        tri=pfv(cm,'m_Triangles') or []
        o=0
        for sm in subs:
            M['tris'].append([i for i in tri[o:o+sm['indexCount']]]); o+=sm['indexCount']
    return M
def stats(M):
    nt=sum(len(t)//3 for t in M['tris']); nv=len(M['pos'])
    # flat shading: count verts per triangle unique
    return nv,nt
if __name__=='__main__':
    import json
    for p in sys.argv[1:]:
        M=load(p); nv,nt=stats(M)
        print(M['name'],'comp',M['comp'],'v',nv,'t',nt,'subs',[(s['vertexCount'],s['indexCount']//3) for s in M['subs']],'uv',len(M['uv']),'col',len(M['col']))
