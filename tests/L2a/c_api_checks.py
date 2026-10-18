"""Direct C ABI checks via ctypes (run with xvfb-run on VTK 9.1)."""
import ctypes as c
import sys

vm = c.CDLL(sys.argv[1])
assert vm.vm_abi_version() == 3, "requires UNSTABLE ABI 0.3"
ptr = c.c_void_p
i32, i64 = c.c_int32, c.c_int64
vm.vm_last_error.restype = c.c_char_p
vm.vm_create.argtypes = [i32, i32, i32]
vm.vm_create.restype = ptr
vm.vm_destroy.argtypes = [ptr]
vm.vm_resize.argtypes = [ptr, i32, i32]
vm.vm_render.argtypes = [ptr]
vm.vm_load_grid.argtypes = [ptr, i32, c.POINTER(i64), c.POINTER(c.c_double), i32, c.POINTER(i64), c.POINTER(i32), c.POINTER(i32), i32, c.POINTER(i64)]
vm.vm_frustum_select.argtypes = [ptr, c.POINTER(c.c_double)]
vm.vm_selected_ids.argtypes = [ptr, i32, c.POINTER(c.POINTER(i64)), c.POINTER(i32)]
vm.vm_actor_selectable.argtypes = [ptr, i64, i32]
vm.vm_actor_selection_layer.argtypes = [ptr,i64,i32]
vm.vm_selectable_filter.argtypes = [ptr, c.POINTER(i64), i32]
vm.vm_area_pick.argtypes = [ptr, i32, i32, i32, i32, c.POINTER(c.c_double)]
vm.vm_selection_distance.argtypes = [ptr, i32]
vm.vm_selection_precision.argtypes = [ptr, c.POINTER(c.c_double)]
vm.vm_geometry_pick.argtypes = [ptr, c.POINTER(c.c_double), c.POINTER(i64), i32]
vm.vm_closest_cells.argtypes = [ptr,c.POINTER(c.c_double),c.POINTER(i64),i32,c.POINTER(c.c_double),c.POINTER(c.c_double)]
vm.vm_actor_style.argtypes = [ptr,i64,i32,c.c_double,c.c_double,c.c_double,c.c_double,i32]
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
        def box(lo, hi):
            return (c.c_double*36)(lo,0,0,-1,0,0, hi,0,0,1,0,0,
                0,lo,0,0,-1,0, 0,hi,0,0,1,0, 0,0,lo,0,0,-1, 0,0,hi,0,0,1)
        def selected(association):
            p, n = c.POINTER(i64)(), i32()
            assert vm.vm_selected_ids(view, association, c.byref(p), c.byref(n)) == 0
            return list(p[:n.value])
        assert vm.vm_frustum_select(view, box(-1,2)) == 0
        assert selected(1) == [10,20,30,40] and selected(0) == [9000000001]
        crossing = box(-1,2); crossing[0]=.1; crossing[6]=.2
        assert vm.vm_frustum_select(view, crossing) == 0
        assert selected(1) == [] and selected(0) == [9000000001], ('crossing: cell with no vertex inside',selected(1),selected(0))
        assert vm.vm_frustum_select(view, box(2,3)) == 0 and selected(0) == []
        assert vm.vm_actor_style(view,0,0,0,0,0,1,1) == 0
        assert vm.vm_frustum_select(view, box(-1,2)) == 0 and selected(0) == []
        assert vm.vm_selectable_filter(view,(i64*1)(0),1) == 0
        assert vm.vm_frustum_select(view, box(-1,2)) == 0 and selected(0) == [9000000001]
        assert vm.vm_selectable_filter(view,(i64*1)(-1),0) == 0
        assert vm.vm_frustum_select(view, box(-1,2)) == 0 and selected(0) == []
        assert vm.vm_selectable_filter(view,None,0) == 0
        assert vm.vm_actor_style(view,0,1,.65,.78,.89,1,1) == 0
        assert vm.vm_actor_selectable(view,0,0) == 0
        assert vm.vm_frustum_select(view, box(-1,2)) == 0 and selected(0) == []
        assert vm.vm_actor_selectable(view,0,1) == 0
        assert vm.vm_actor_selection_layer(view,0,1) == 0
        assert vm.vm_frustum_select(view,box(-1,2)) == 0 and selected(0)==[]
        assert vm.vm_actor_selection_layer(view,0,0) == 0
        planes=(c.c_double*36)()
        assert vm.vm_area_pick(view,0,0,127,127,planes) == 0
        assert len(planes) == 36 and all(__import__('math').isfinite(p) for p in planes)
        distance=c.c_double()
        assert vm.vm_selection_distance(view,0) == 0 and vm.vm_selection_precision(view,c.byref(distance)) == 0
        one=distance.value
        assert vm.vm_selection_distance(view,10) == 0 and vm.vm_selection_precision(view,c.byref(distance)) == 0
        assert abs(distance.value/one-10)<1e-9
        element=i64()
        assert vm.vm_geometry_pick(view,(c.c_double*3)(.2,.2,0),c.byref(element),1) == 0
        assert element.value == 9000000001 and sorted(selected(4)) == [10,20,30]
        assert len(selected(3)) == 2
        assert vm.vm_geometry_pick(view,(c.c_double*3)(100,100,100),c.byref(element),0) == 0
        assert element.value==-1 and selected(3)==[] and len(selected(4))==3
        distances=(c.c_double*2)();normals=(c.c_double*6)()
        assert vm.vm_actor_style(view,0,0,0,0,0,1,1) == 0
        assert vm.vm_closest_cells(view,(c.c_double*3)(.2,.2,0),(i64*2)(0,-1),2,distances,normals) == 0
        assert distances[0]==0 and distances[1]==-1 and abs(sum(x*x for x in normals[:3])-1)<1e-12
    finally:
        vm.vm_destroy(view)
print("PASS direct C ABI 0.3: grid validation, whole/crossing/empty frustum, visibility/filter/selectability, area planes, precision, geometry face/edge, repeated dispose")
