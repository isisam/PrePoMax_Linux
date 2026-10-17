"""Direct C ABI checks via ctypes (run with xvfb-run on VTK 9.1)."""
import ctypes as c
import sys

vm = c.CDLL(sys.argv[1])
ptr = c.c_void_p
i32, i64 = c.c_int32, c.c_int64
vm.vm_last_error.restype = c.c_char_p
vm.vm_create.argtypes = [i32, i32, i32]
vm.vm_create.restype = ptr
vm.vm_destroy.argtypes = [ptr]
vm.vm_resize.argtypes = [ptr, i32, i32]
vm.vm_render.argtypes = [ptr]
vm.vm_load_grid.argtypes = [ptr, i32, c.POINTER(i64), c.POINTER(c.c_double), i32, c.POINTER(i64), c.POINTER(i32), c.POINTER(i32), i32, c.POINTER(i64)]
assert vm.vm_create(1, -1, 100) is None
assert b"size" in vm.vm_last_error()
assert vm.vm_resize(None, 50, 50) == -1
for attempt in range(3):
    view = vm.vm_create(1, 128, 128)
    assert view
    try:
        ids = (i64 * 4)(10, 20, 30, 40)
        xyz = (c.c_double * 12)(0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1)
        elements = (i64 * 1)(9000000001)
        types = (i32 * 1)(10)
        offsets = (i32 * 2)(0, 4)
        conn = (i64 * 4)(0, 1, 2, 3)
        def load():
            return vm.vm_load_grid(view, 4, ids, xyz, 1, elements, types, offsets, 4, conn)
        assert load() == 0
        ids[1] = ids[0]
        assert load() == -1 and b"duplicate" in vm.vm_last_error()
        ids[1] = 20
        offsets[1] = 5
        assert load() == -1 and b"offsets" in vm.vm_last_error()
        offsets[1] = 4
        conn[2] = 99
        assert load() == -1 and b"index" in vm.vm_last_error()
        conn[2] = 2
        xyz[0] = float("nan")
        assert load() == -1 and b"coordinate" in vm.vm_last_error()
        xyz[0] = 0
        assert load() == 0
        assert vm.vm_render(view) == 0
    finally:
        vm.vm_destroy(view)
print("PASS direct C ABI: null handles, dimensions, duplicate IDs, offsets, connectivity bounds, nonfinite coordinates, repeated render/dispose")
