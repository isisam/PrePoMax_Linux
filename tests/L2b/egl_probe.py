import ctypes as c,sys
v=c.CDLL(sys.argv[1]);p=c.c_void_p
v.vm_create_backend.argtypes=[c.c_int]*3;v.vm_create_backend.restype=p
v.vm_render.argtypes=[p];v.vm_destroy.argtypes=[p];v.vm_gl_info.argtypes=[p];v.vm_gl_info.restype=c.c_char_p
print('EGL create',flush=True);h=v.vm_create_backend(3,300,200);print('EGL created',h,flush=True)
assert h
print('EGL render',flush=True);assert v.vm_render(h)==0;print('EGL rendered',flush=True)
print(v.vm_gl_info(h).decode(),flush=True);v.vm_destroy(h);print('PASS EGL probe',flush=True)
