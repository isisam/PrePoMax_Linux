"""Replay unchanged Windows frusta through the actual vtkmax C ABI under Xvfb."""
from pathlib import Path
import ctypes as c, json, math, sys
vm=c.CDLL(sys.argv[1]); logs=Path(sys.argv[2]); ptr=c.c_void_p; i32=c.c_int32; i64=c.c_int64; dbl=c.c_double
vm.vm_create.argtypes=[i32,i32,i32];vm.vm_create.restype=ptr
vm.vm_destroy.argtypes=[ptr]; vm.vm_render.argtypes=[ptr]
vm.vm_last_error.restype=c.c_char_p
vm.vm_add_actor.argtypes=[ptr,i64,i32,c.POINTER(i64),c.POINTER(dbl),i32,c.POINTER(i64),c.POINTER(i32),c.POINTER(i32),i32,c.POINTER(i64)]
vm.vm_actor_style.argtypes=[ptr,i64,i32,dbl,dbl,dbl,dbl,i32]
vm.vm_actor_pick_geometry.argtypes=vm.vm_add_actor.argtypes
vm.vm_frustum_select.argtypes=[ptr,c.POINTER(dbl)]
vm.vm_selected_ids.argtypes=[ptr,i32,c.POINTER(c.POINTER(i64)),c.POINTER(i32)]
vm.vm_area_pick.argtypes=[ptr,i32,i32,i32,i32,c.POINTER(dbl)]
vm.vm_perspective.argtypes=[ptr,i32];vm.vm_set_camera.argtypes=[ptr,c.POINTER(dbl),c.POINTER(dbl),c.POINTER(dbl),dbl]
vm.vm_geometry_pick.argtypes=[ptr,c.POINTER(dbl),c.POINTER(i64),i32]
def check(status):
    if status<0: raise RuntimeError(vm.vm_last_error().decode())
def array(t,items): return (t*len(items))(*items)
def selected(view,assoc):
    p=c.POINTER(i64)();n=i32();check(vm.vm_selected_ids(view,assoc,c.byref(p),c.byref(n)));return list(p[:n.value])
receipts={}
for name in ['DCB','M2a']:
    data=json.loads((logs/f'windows-selection-{name}.json').read_text(encoding='utf-8-sig'));cam=data['camera']
    view=vm.vm_create(1,cam['width'],cam['height']);assert view
    try:
        for p in data['parts']:
            cells=p['cells'];off=[0]
            for cell in cells:off.append(off[-1]+len(cell))
            check(vm.vm_add_actor(view,p['actor'],len(p['nodes']),array(i64,p['nodes']),array(dbl,[x for xyz in p['xyz'] for x in xyz]),len(cells),array(i64,p['elements']),array(i32,p['types']),array(i32,off),off[-1],array(i64,[i for cell in cells for i in cell])))
            check(vm.vm_actor_style(view,p['actor'],int(p['visible']),.65,.78,.89,1,1))
            if 'visual' in p:
                g=p['visual'];off=[0]
                for cell in g['cells']:off.append(off[-1]+len(cell))
                check(vm.vm_actor_pick_geometry(view,p['actor'],len(g['nodes']),array(i64,g['nodes']),array(dbl,[x for xyz in g['xyz'] for x in xyz]),len(g['cells']),array(i64,g['elements']),array(i32,g['types']),array(i32,off),off[-1],array(i64,[i for cell in g['cells'] for i in cell])))
        check(vm.vm_perspective(view,0));check(vm.vm_set_camera(view,array(dbl,cam['position']),array(dbl,cam['focal']),array(dbl,cam['up']),cam['scale']));check(vm.vm_render(view))
        cases=[]
        for case in data['cases']:
            check(vm.vm_frustum_select(view,array(dbl,[v for plane in case['planes'] for v in plane])))
            points,cells=selected(view,1),selected(view,0)
            exact=points==case['points'] and cells==case['cells']
            native=(dbl*36)();check(vm.vm_area_pick(view,*case['rectangle'],native))
            ownplanes=[list(native[i*6:i*6+6]) for i in range(6)]
            check(vm.vm_frustum_select(view,native));ownpoints,owncells=selected(view,1),selected(view,0)
            # Coordinate components at zero use a common scale, so roundoff does not produce an artificial infinite relative error.
            errors=[max(abs(a-b) for a,b in zip(w,n))/max(1,max(map(abs,w)),max(map(abs,n))) for w,n in zip(case['planes'],ownplanes)]
            def equation(plane):
                normal=plane[3:];return normal+[-sum(a*b for a,b in zip(plane[:3],normal))]
            equations=[max(abs(a-b) for a,b in zip(equation(w),equation(n)))/max(1,max(map(abs,equation(w))),max(map(abs,equation(n)))) for w,n in zip(case['planes'],ownplanes)]
            cases.append(dict(name=case['name'],same_planes_ids_equal=exact,points=points,cells=cells,
                display_ids_equal=ownpoints==case['points'] and owncells==case['cells'],display_points=ownpoints,display_cells=owncells,
                plane_relative_error=errors,equation_relative_error=equations,native_planes=ownplanes,windows_planes=case['planes']))
        geometry=data['geometry'];element=i64()
        check(vm.vm_geometry_pick(view,array(dbl,geometry['point']),c.byref(element),1))
        geom=dict(element=element.value,edge=selected(view,3),face=selected(view,4))
        geom['membership_equal']=geom['element']==geometry['element'] and sorted(geom['edge'])==sorted(geometry['edge']) and sorted(geom['face'])==sorted(geometry['face'])
        receipts[name]=dict(cases=cases,geometry=geom,semantic_passed=all(x['same_planes_ids_equal'] for x in cases),display_passed=all(x['display_ids_equal'] for x in cases))
    finally: vm.vm_destroy(view)
result=dict(passed=all(r['semantic_passed'] and r['display_passed'] and r['geometry']['membership_equal'] for r in receipts.values()),fixtures=receipts,abi=vm.vm_abi_version(),scope='frustum and geometry membership; plane numeric tolerance is recorded separately')
(logs/'native-windows-comparison.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
tolerance=dict(passed=all(max(case['plane_relative_error'])<=1e-9 for r in receipts.values() for case in r['cases']),required_relative_error=1e-9,
    fixtures={name:[dict(name=case['name'],origin_and_normal_relative_error=case['plane_relative_error'],equation_relative_error=case['equation_relative_error'],
        normal_max_absolute_error=max(abs(a-b) for w,n in zip(case['windows_planes'],case['native_planes']) for a,b in zip(w[3:],n[3:]))) for case in r['cases']] for name,r in receipts.items()})
(logs/'plane-tolerance.json').write_text(json.dumps(tolerance,indent=2),encoding='utf-8')
print(json.dumps({n:dict(semantic=r['semantic_passed'],display=r['display_passed'],geometry=r['geometry']['membership_equal'],counts=[(len(x['points']),len(x['cells'])) for x in r['cases']]) for n,r in receipts.items()}))
print('Strict raw plane tolerance <= 1e-9:', tolerance['passed'])
raise SystemExit(0 if result['passed'] and tolerance['passed'] else 1)
