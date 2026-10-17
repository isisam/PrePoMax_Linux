"""Assert Windows vtkControl.cs SetFrontBack/TopBottom/LeftRightView conventions."""
import ctypes as c, math, sys
v=c.CDLL(sys.argv[1]); p=c.c_void_p; d=c.POINTER(c.c_double)
v.vm_create.argtypes=[c.c_int]*3; v.vm_create.restype=p
v.vm_destroy.argtypes=[p]; v.vm_standard_view.argtypes=[p,c.c_int]
v.vm_camera.argtypes=[p,d,d,d]
h=v.vm_create(1,100,100); assert h
try:
    expected=[((1,1,1),(0,1,0)),((0,0,1),(0,1,0)),((0,0,-1),(0,1,0)),
              ((0,1,0),(0,0,-1)),((0,-1,0),(0,0,1)),((1,0,0),(0,1,0)),((-1,0,0),(0,1,0))]
    for i,(direction,up) in enumerate(expected):
        assert v.vm_standard_view(h,i)==0
        a,b,u=(c.c_double*3)(),(c.c_double*3)(),(c.c_double*3)()
        assert v.vm_camera(h,a,b,u)==0
        delta=[a[j]-b[j] for j in range(3)]; norm=math.sqrt(sum(x*x for x in delta)); n=math.sqrt(sum(x*x for x in direction))
        assert all(abs(delta[j]/norm-direction[j]/n)<1e-10 for j in range(3)),(i,list(a),list(b))
        assert all(abs(u[j]-up[j])<1e-10 for j in range(3)),(i,list(u))
        print('PASS Windows standard view',i,'direction',direction,'up',up)
finally: v.vm_destroy(h)
