// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 PrePoMax-RCM contributors (RCM Lab, NCKU)
// Copyright (c) Matej Borovinšek (ported vtkControl conventions/code)
#include "vtkmax.h"
#include <vtkActor.h>
#include <vtkCamera.h>
#include <vtkCameraInterpolator.h>
#include <vtkCell.h>
#include <vtkCellData.h>
#include <vtkCellPicker.h>
#include <vtkDataSetMapper.h>
#include <vtkDoubleArray.h>
#include <vtkExtractCells.h>
#include <vtkIdList.h>
#include <vtkInteractorStyleTrackballCamera.h>
#include <vtkLookupTable.h>
#include <vtkNew.h>
#include <vtkPNGWriter.h>
#include <vtkPointData.h>
#include <vtkPoints.h>
#include <vtkProperty.h>
#include <vtkRenderer.h>
#include <vtkRenderWindowInteractor.h>
#include <vtkScalarBarActor.h>
#include <vtkSmartPointer.h>
#include <vtkTextProperty.h>
#include <vtkUnstructuredGrid.h>
#include <vtkWindowToImageFilter.h>
#include <vtkGenericOpenGLRenderWindow.h>
#include <vtkGenericRenderWindowInteractor.h>
#include <vtkOpenGLState.h>
#include <vtk_glad.h>
#include <vtkEGLRenderWindow.h>
#ifdef VTKMAX_HAS_X11
#include <vtkXOpenGLRenderWindow.h>
#include <vtkXRenderWindowInteractor.h>
#endif
#include <algorithm>
#include <cmath>
#include <memory>
#include <stdexcept>
#include <string>
#include <unordered_map>
#include <vector>

namespace {
thread_local std::string error;
void require(bool ok, const char* text) { if (!ok) throw std::invalid_argument(text); }
int nodes_per_cell(int t) {
    switch(t) { case 5:return 3; case 9:return 4; case 22:return 6; case 23:return 8;
    case 10:return 4; case 12:return 8; case 13:return 6; case 24:return 10;
    case 25:return 20; case 26:return 15; default:return -1; }
}
bool finite3(const double* p) { return std::isfinite(p[0]) && std::isfinite(p[1]) && std::isfinite(p[2]); }
}
struct actor_state {
    vtkNew<vtkUnstructuredGrid> grid;
    vtkNew<vtkDataSetMapper> mapper;
    vtkNew<vtkActor> actor;
    std::vector<int64_t> nodes, elements;
    std::unordered_map<int64_t, vtkIdType> node_map, element_map;
    std::vector<double> original;
};
struct vm_view : actor_state {
    vtkSmartPointer<vtkOpenGLRenderWindow> window;
    vtkNew<vtkRenderer> renderer;
    vtkNew<vtkLookupTable> lut;
    vtkNew<vtkScalarBarActor> bar;
    vtkNew<vtkActor> highlight;
    vtkSmartPointer<vtkRenderWindowInteractor> interactor;
    std::unordered_map<int64_t,std::unique_ptr<actor_state>> actors;
    vtkNew<vtkCameraInterpolator> animation;
    vtkNew<vtkCamera> animation_start;
    vtkNew<vtkCamera> animation_end;
    bool animating=false;
    vm_selection_callback callback=nullptr;
    void* callback_user=nullptr;
    vm_pick_result last{-1,-1,-1,0,0,0,0,0,-1};
    bool rendered=false, offscreen=true;
    int backend=1;
    std::string gl_info;
    ~vm_view() { if(interactor) { interactor->Disable(); interactor->SetRenderWindow(nullptr); } if(window)window->Finalize(); }
};
actor_state* find_actor(vm_view* v,int64_t id) {
    require(v,"null view");if(id==0)return v;
    auto it=v->actors.find(id);require(it!=v->actors.end(),"unknown actor ID");return it->second.get();
}
class ClickStyle : public vtkInteractorStyleTrackballCamera {
public:
    static ClickStyle* New() { return new ClickStyle; }
    vtkTypeMacro(ClickStyle, vtkInteractorStyleTrackballCamera)
    vm_view* view=nullptr;
    int start[2]={0,0};
    void OnLeftButtonDown() override {
        auto p=GetInteractor()->GetEventPosition(); start[0]=p[0]; start[1]=p[1];
        vtkInteractorStyleTrackballCamera::OnLeftButtonDown();
    }
    void OnLeftButtonUp() override {
        vtkInteractorStyleTrackballCamera::OnLeftButtonUp();
        auto p=GetInteractor()->GetEventPosition();
        if(std::abs(p[0]-start[0])+std::abs(p[1]-start[1])<4) {
            vm_pick_result r;vm_pick(view,p[0],p[1],&r);
        }
    }
};
#define TRY error.clear(); try
#define CATCH catch(const std::exception& e) { error=e.what(); return -1; } catch(...) { error="native exception"; return -1; }
#define VIEW require(v!=nullptr,"null view")
extern "C" {
int32_t vm_abi_version() { return 2; }
const char* vm_last_error() { return error.c_str(); }
vm_view* vm_create(int32_t offscreen,int32_t w,int32_t h) {
    return vm_create_backend(offscreen?1:0,w,h);
}
vm_view* vm_create_backend(int32_t backend,int32_t w,int32_t h) {
    error.clear(); try {
        require(w>0&&h>0&&w<=16384&&h<=16384,"invalid size");
        require(backend>=0&&backend<=3,"invalid backend");
        auto v=std::make_unique<vm_view>(); v->offscreen=backend!=0;v->backend=backend;
        if(backend==2) {
            auto g=vtkSmartPointer<vtkGenericOpenGLRenderWindow>::New();
            g->SetOwnContext(0);g->SetReadyForRendering(true);g->SetIsCurrent(true);
            g->SetSupportsOpenGL(1);g->SetIsDirect(1);g->SetFrameBlitModeToBlitToCurrent();
            v->window=g;
        } else if(backend==3) v->window=vtkSmartPointer<vtkEGLRenderWindow>::New();
        else {
#ifdef VTKMAX_HAS_X11
            v->window=vtkSmartPointer<vtkXOpenGLRenderWindow>::New();
#else
            require(false,"X11 backend disabled at build time; choose EGL or generic");
#endif
        }
        v->window->SetOffScreenRendering(backend==1||backend==3); v->window->SetSize(w,h);
        v->window->SetMultiSamples(0); v->window->AddRenderer(v->renderer);
        v->renderer->SetBackground(0.075,0.09,0.13);
        v->mapper->SetInputData(v->grid); v->mapper->ScalarVisibilityOff();
        v->actor->SetMapper(v->mapper); v->actor->GetProperty()->SetColor(0.65,0.78,0.89);
        v->actor->GetProperty()->SetEdgeColor(0.12,0.16,0.22); v->actor->GetProperty()->EdgeVisibilityOn();
        v->renderer->AddActor(v->actor);
        v->lut->SetNumberOfTableValues(12); v->lut->SetHueRange(0.66,0.0); v->lut->Build();
        v->bar->SetLookupTable(v->lut); v->bar->SetNumberOfLabels(5);
        v->bar->SetWidth(0.10); v->bar->SetHeight(0.72); v->bar->SetPosition(0.88,0.14);
        v->bar->GetTitleTextProperty()->SetFontSize(16); v->bar->GetLabelTextProperty()->SetFontSize(14);
        v->bar->VisibilityOff(); v->renderer->AddActor2D(v->bar);
        v->highlight->PickableOff(); v->highlight->UseBoundsOff();
        v->highlight->GetProperty()->SetColor(1.0,0.72,0.08);
        v->highlight->GetProperty()->SetRepresentationToWireframe();
        v->highlight->GetProperty()->SetLineWidth(4); v->highlight->GetProperty()->LightingOff();
        v->renderer->AddActor(v->highlight); v->highlight->VisibilityOff();
        if(backend==2) {
            v->window->OpenGLInitContext();
            v->interactor=vtkSmartPointer<vtkGenericRenderWindowInteractor>::New();
            v->interactor->SetRenderWindow(v->window);
            auto style=vtkSmartPointer<ClickStyle>::New();style->view=v.get();
            v->interactor->SetInteractorStyle(style);v->interactor->Initialize();
        }
        return v.release();
    } catch(const std::exception& e) { error=e.what(); return nullptr; } catch(...) { error="native exception"; return nullptr; }
}
void vm_destroy(vm_view* v) { delete v; }
uintptr_t vm_attach_x11(vm_view* v,uintptr_t parent) {
    error.clear(); try {
#ifdef VTKMAX_HAS_X11
        VIEW; require(!v->rendered&&!v->offscreen&&parent,"attach requires an unrendered onscreen view and X11 parent");
        auto x=vtkXOpenGLRenderWindow::SafeDownCast(v->window);require(x,"X11 backend required");
        x->SetParentId(static_cast<Window>(parent)); v->window->Render(); v->rendered=true;
        v->interactor=vtkSmartPointer<vtkXRenderWindowInteractor>::New();
        v->interactor->SetRenderWindow(v->window);
        auto style=vtkSmartPointer<ClickStyle>::New(); style->view=v;
        v->interactor->SetInteractorStyle(style); v->interactor->Initialize();
        return static_cast<uintptr_t>(x->GetWindowId());
#else
        (void)v;(void)parent;require(false,"X11 backend disabled at build time");return 0;
#endif
    } catch(const std::exception& e) { error=e.what(); return 0; } catch(...) {error="attach exception";return 0;}
}
int32_t vm_resize(vm_view* v,int32_t w,int32_t h) {
    TRY { VIEW; require(w>0&&h>0&&w<=16384&&h<=16384,"invalid size"); v->window->SetSize(w,h);
        if(v->interactor) v->interactor->UpdateSize(w,h);
        return 0; } CATCH
}
int32_t load_grid(actor_state* v,int32_t np,const int64_t* ids,const double* xyz,int32_t nc,
    const int64_t* eids,const int32_t* types,const int32_t* offsets,int32_t nconn,const int64_t* conn) {
    TRY { VIEW; require(np>0&&nc>0&&nconn>0&&ids&&xyz&&eids&&types&&offsets&&conn,"invalid grid arrays");
        require(offsets[0]==0&&offsets[nc]==nconn,"invalid connectivity offsets");
        vtkNew<vtkUnstructuredGrid> g; vtkNew<vtkPoints> points; points->SetDataTypeToDouble();
        std::unordered_map<int64_t,vtkIdType> nm,em;
        for(int i=0;i<np;++i) { require(ids[i]>=0&&nm.emplace(ids[i],i).second,"duplicate/negative node ID");
            require(finite3(xyz+3*i),"nonfinite coordinate"); points->InsertNextPoint(xyz+3*i); }
        g->SetPoints(points);
        for(int i=0;i<nc;++i) {
            require(eids[i]>=0&&em.emplace(eids[i],i).second,"duplicate/negative element ID");
            int count=nodes_per_cell(types[i]);
            require(count>0&&offsets[i]>=0&&offsets[i+1]<=nconn&&offsets[i+1]-offsets[i]==count,"unsupported type or wrong connectivity size");
            std::vector<vtkIdType> cell(count);
            for(int j=0;j<count;++j) { auto p=conn[offsets[i]+j]; require(p>=0&&p<np,"point index out of range"); cell[j]=p; }
            g->InsertNextCell(types[i],count,cell.data());
        }
        std::vector<int64_t> node_ids(ids,ids+np), element_ids(eids,eids+nc);
        v->grid->ShallowCopy(g); v->nodes=std::move(node_ids); v->elements=std::move(element_ids);
        v->node_map=std::move(nm); v->element_map=std::move(em);
        v->original.assign(xyz,xyz+3*np);
        v->mapper->SetInputData(v->grid);v->actor->SetMapper(v->mapper);
        v->mapper->ScalarVisibilityOff();return 0;
    } CATCH
}
int32_t vm_load_grid(vm_view* v,int32_t np,const int64_t* ids,const double* xyz,int32_t nc,
    const int64_t* eids,const int32_t* types,const int32_t* offsets,int32_t nconn,const int64_t* conn) {
    int result=load_grid(v,np,ids,xyz,nc,eids,types,offsets,nconn,conn);
    if(result==0){v->bar->VisibilityOff();v->highlight->VisibilityOff();}return result;
}
int32_t vm_add_actor(vm_view* v,int64_t id,int32_t np,const int64_t* ids,const double* xyz,int32_t nc,
    const int64_t* eids,const int32_t* types,const int32_t* offsets,int32_t nconn,const int64_t* conn) {
    TRY {VIEW;require(id>0&&!v->actors.count(id),"duplicate/invalid actor ID");
        auto m=std::make_unique<actor_state>();
        if(load_grid(m.get(),np,ids,xyz,nc,eids,types,offsets,nconn,conn)<0)return -1;
        m->actor->GetProperty()->SetColor(.65,.78,.89);m->actor->GetProperty()->SetEdgeColor(.12,.16,.22);
        v->renderer->AddActor(m->actor);v->actors.emplace(id,std::move(m));return 0;} CATCH
}
int32_t vm_remove_actor(vm_view* v,int64_t id) {
    TRY {VIEW;require(id>0,"cannot remove default actor");auto m=find_actor(v,id);
        v->renderer->RemoveActor(m->actor);v->actors.erase(id);v->highlight->VisibilityOff();return 0;} CATCH
}
int32_t vm_actor_style(vm_view* v,int64_t id,int32_t visible,double r,double g,double b,double opacity,int32_t edges) {
    TRY {auto m=find_actor(v,id);for(double x:{r,g,b,opacity})require(std::isfinite(x)&&x>=0&&x<=1,"invalid color/opacity");
        m->actor->SetVisibility(visible!=0);auto p=m->actor->GetProperty();p->SetColor(r,g,b);p->SetOpacity(opacity);p->SetEdgeVisibility(edges!=0);return 0;} CATCH
}
int32_t vm_deformation(vm_view* v,int64_t id,const double* u,int32_t count,double scale) {
    TRY {auto m=find_actor(v,id);require(std::isfinite(scale),"nonfinite deformation scale");
        require((!u&&count==0)||(u&&count==static_cast<int>(m->original.size())),"displacement count mismatch");
        vtkNew<vtkPoints> points;points->SetDataTypeToDouble();
        for(size_t i=0;i<m->original.size();i+=3) {double p[3];
            for(int j=0;j<3;++j){double d=u?u[i+j]:0;require(std::isfinite(d),"nonfinite displacement");p[j]=m->original[i+j]+scale*d;require(std::isfinite(p[j]),"deformation overflow");}
            points->InsertNextPoint(p);}
        m->grid->SetPoints(points);m->grid->Modified();return 0;} CATCH
}
int32_t vm_selection_handler(vm_view* v,vm_selection_callback cb,void* user) {TRY {VIEW;v->callback=cb;v->callback_user=user;return 0;} CATCH}
int32_t vm_contour_bands(vm_view* v,int32_t bands) {
    TRY {VIEW;require(bands>=2&&bands<=256,"bands outside 2..256");v->lut->SetNumberOfTableValues(bands);v->lut->Build();v->bar->SetMaximumNumberOfColors(bands);return 0;} CATCH
}
int32_t vm_display(vm_view* v,int32_t surface,int32_t edges) {
    TRY { VIEW; v->actor->SetVisibility(surface||edges);
        if(surface) v->actor->GetProperty()->SetRepresentationToSurface();
        else v->actor->GetProperty()->SetRepresentationToWireframe();
        v->actor->GetProperty()->SetEdgeVisibility(edges!=0); return 0; } CATCH
}
int32_t vm_scalars(vm_view* v,int32_t association,const double* values,int32_t count,double lo,double hi,const char* title) {
    return vm_actor_scalars(v,0,association,values,count,lo,hi,title);
}
int32_t vm_actor_scalars(vm_view* v,int64_t actor_id,int32_t association,const double* values,int32_t count,double lo,double hi,const char* title) {
    TRY { VIEW; require(association==0||association==1,"invalid scalar association");
        auto m=find_actor(v,actor_id);
        if(!values&&count==0) {m->mapper->ScalarVisibilityOff();v->bar->VisibilityOff();return 0;}
        require(values&&count==static_cast<int>(association?m->nodes.size():m->elements.size())&&count>0,"scalar count mismatch");
        require(std::isfinite(lo)&&std::isfinite(hi)&&lo<=hi,"invalid scalar range");
        vtkNew<vtkDoubleArray> a; a->SetName("vm_scalars");
        for(int i=0;i<count;++i) { require(std::isfinite(values[i]),"nonfinite scalar"); a->InsertNextValue(values[i]); }
        if(association) {m->grid->GetPointData()->SetScalars(a);m->mapper->SetScalarModeToUsePointData();}
        else {m->grid->GetCellData()->SetScalars(a);m->mapper->SetScalarModeToUseCellData();}
        if(lo==hi) hi=lo+1;
        v->lut->SetTableRange(lo,hi); m->mapper->SetLookupTable(v->lut);
        m->mapper->SetScalarRange(lo,hi); m->mapper->ScalarVisibilityOn();
        v->bar->SetTitle(title?title:""); v->bar->VisibilityOn(); return 0; } CATCH
}
int32_t vm_highlight(vm_view* v,int32_t association,const int64_t* ids,int32_t count) {
    return vm_actor_highlight(v,0,association,ids,count);
}
int32_t vm_actor_highlight(vm_view* v,int64_t actor_id,int32_t association,const int64_t* ids,int32_t count) {
    TRY { VIEW; require((association==0||association==1)&&count>=0&&(count==0||ids),"invalid highlight arguments");
        auto mesh=find_actor(v,actor_id);
        if(count==0) {v->highlight->VisibilityOff();return 0;}
        const auto& map=association?mesh->node_map:mesh->element_map;
        vtkNew<vtkIdList> list;
        for(int i=0;i<count;++i) {auto p=map.find(ids[i]);require(p!=map.end(),"unknown highlight ID");list->InsertNextId(p->second);}
        vtkNew<vtkDataSetMapper> m;
        if(association) {
            vtkNew<vtkUnstructuredGrid> g; g->SetPoints(mesh->grid->GetPoints());
            for(vtkIdType i=0;i<list->GetNumberOfIds();++i) {auto p=list->GetId(i);g->InsertNextCell(VTK_VERTEX,1,&p);}
            m->SetInputData(g);v->highlight->GetProperty()->SetRepresentationToPoints();v->highlight->GetProperty()->SetPointSize(10);
        } else {
            vtkNew<vtkExtractCells> extract;extract->SetInputData(mesh->grid);extract->SetCellList(list);extract->Update();
            m->SetInputData(extract->GetOutput());v->highlight->GetProperty()->SetRepresentationToWireframe();
        }
        m->ScalarVisibilityOff();m->SetResolveCoincidentTopologyToPolygonOffset();
        m->SetRelativeCoincidentTopologyLineOffsetParameters(0,-4);
        v->highlight->SetMapper(m);v->highlight->VisibilityOn();return 0; } CATCH
}
int32_t vm_fit(vm_view* v) { TRY {VIEW;v->renderer->ResetCamera();v->renderer->ResetCameraClippingRange();return 0;} CATCH }
int32_t vm_standard_view(vm_view* v,int32_t view) {
    TRY { VIEW; require(view>=0&&view<=6,"invalid standard view");
        const double dirs[][3]={{1,1,1},{0,0,1},{0,0,-1},{0,1,0},{0,-1,0},{1,0,0},{-1,0,0}};
        auto c=v->renderer->GetActiveCamera(); c->SetFocalPoint(0,0,0);c->SetPosition(dirs[view]);
        if(view==3) c->SetViewUp(0,0,-1);else if(view==4) c->SetViewUp(0,0,1);else c->SetViewUp(0,1,0);
        c->ParallelProjectionOn();v->renderer->ResetCamera();v->renderer->ResetCameraClippingRange();return 0; } CATCH
}
int32_t vm_camera(vm_view* v,double* position,double* focal,double* up) {
    TRY {VIEW;require(position&&focal&&up,"null camera output");auto c=v->renderer->GetActiveCamera();
        std::copy_n(c->GetPosition(),3,position);std::copy_n(c->GetFocalPoint(),3,focal);std::copy_n(c->GetViewUp(),3,up);return 0;} CATCH
}
int32_t vm_perspective(vm_view* v,int32_t enabled) {
    TRY {VIEW;v->renderer->GetActiveCamera()->SetParallelProjection(!enabled);v->renderer->ResetCameraClippingRange();return 0;} CATCH
}
int32_t vm_animate_standard_begin(vm_view* v,int32_t view) {
    TRY {VIEW;v->animation_start->DeepCopy(v->renderer->GetActiveCamera());
        if(vm_standard_view(v,view)<0)return -1;
        v->animation_end->DeepCopy(v->renderer->GetActiveCamera());
        v->animation->Initialize();v->animation->SetInterpolationTypeToLinear();
        v->animation->AddCamera(0,v->animation_start);v->animation->AddCamera(1,v->animation_end);
        v->renderer->GetActiveCamera()->DeepCopy(v->animation_start);v->animating=true;return 0;} CATCH
}
int32_t vm_animate_standard_step(vm_view* v,double t) {
    TRY {VIEW;require(v->animating&&std::isfinite(t)&&t>=0&&t<=1,"invalid animation fraction/state");
        // CameraInterpolator uses quaternion orientation interpolation; preserve both
        // position and focal point while interpolating zoom, as in vtkControl.
        if(t==1){v->renderer->GetActiveCamera()->DeepCopy(v->animation_end);v->animating=false;}
        else v->animation->InterpolateCamera(t,v->renderer->GetActiveCamera());
        v->renderer->ResetCameraClippingRange();return 0;} CATCH
}
int32_t vm_rotate(vm_view* v,double az,double el) {
    TRY {VIEW;require(std::isfinite(az)&&std::isfinite(el),"invalid rotation");auto c=v->renderer->GetActiveCamera();
        c->Azimuth(az);c->Elevation(el);c->OrthogonalizeViewUp();v->renderer->ResetCameraClippingRange();return 0;} CATCH
}
int32_t vm_zoom(vm_view* v,double factor) {
    TRY {VIEW;require(std::isfinite(factor)&&factor>0,"invalid zoom");v->renderer->GetActiveCamera()->Zoom(factor);return 0;} CATCH
}
int32_t vm_render(vm_view* v) {TRY {VIEW;v->window->Render();v->rendered=true;return 0;} CATCH}
int32_t vm_render_external(vm_view* v,int32_t framebuffer) {
    TRY {VIEW;require(v->backend==2&&framebuffer>=0,"generic framebuffer required");
        auto s=v->window->GetState();s->Reset();s->Push();
        s->vtkglBindFramebuffer(GL_FRAMEBUFFER,static_cast<unsigned>(framebuffer));
        v->window->Render();v->rendered=true;s->Pop();return 0;} CATCH
}
int32_t vm_input(vm_view* v,int32_t kind,int32_t x,int32_t y,int32_t ctrl,int32_t shift) {
    TRY {VIEW;require(v->interactor&&kind>=0&&kind<=8,"invalid input/interactor");
        auto i=v->interactor;i->SetEventInformation(x,y,ctrl,shift);
        switch(kind) {case 0:i->MouseMoveEvent();break;case 1:i->LeftButtonPressEvent();break;
        case 2:i->LeftButtonReleaseEvent();break;case 3:i->MiddleButtonPressEvent();break;
        case 4:i->MiddleButtonReleaseEvent();break;case 5:i->MouseWheelForwardEvent();break;
        case 6:i->MouseWheelBackwardEvent();break;case 7:i->RightButtonPressEvent();break;
        case 8:i->RightButtonReleaseEvent();break;} return 0;} CATCH
}
const char* vm_gl_info(vm_view* v) {
    if(!v)return "null view";
    v->gl_info=v->window->ReportCapabilities();return v->gl_info.c_str();
}
int32_t vm_project(vm_view* v,const double* xyz,double* out) {
    TRY {VIEW;require(xyz&&out&&finite3(xyz),"invalid projection arrays");v->renderer->SetWorldPoint(xyz[0],xyz[1],xyz[2],1);
        v->renderer->WorldToDisplay();std::copy_n(v->renderer->GetDisplayPoint(),3,out);return 0;} CATCH
}
int32_t vm_pick(vm_view* v,int32_t x,int32_t y,vm_pick_result* r) {
    TRY { VIEW;require(r,"null pick output");*r={-1,-1,-1,0,0,0,0,0,-1};
        require(v->rendered,"render before picking");auto size=v->window->GetSize();
        if(x<0||y<0||x>=size[0]||y>=size[1]) return 0;
        vtkNew<vtkCellPicker> p;p->SetTolerance(0.0005);p->PickFromListOn();p->AddPickList(v->actor);
        for(auto& pair:v->actors) if(pair.second->actor->GetVisibility())p->AddPickList(pair.second->actor);
        if(!p->Pick(x,y,0,v->renderer)||p->GetCellId()<0) return 0;
        actor_state* mesh=v;r->actor_id=0;
        for(auto& pair:v->actors)if(pair.second->actor==p->GetActor()){mesh=pair.second.get();r->actor_id=pair.first;break;}
        auto id=p->GetCellId();require(id<static_cast<vtkIdType>(mesh->elements.size()),"unexpected picker cell mapping");
        auto pos=p->GetPickPosition();r->hit=1;r->element_id=mesh->elements[id];r->x=pos[0];r->y=pos[1];r->z=pos[2];
        auto cell=mesh->grid->GetCell(id);double best=1e100;
        if(cell->GetCellDimension()==2) r->face_id=0;
        else for(int f=0;f<cell->GetNumberOfFaces();++f) {
            auto face=cell->GetFace(f);double closest[3],pc[3],distance;int sub;
            std::vector<double> weights(face->GetNumberOfPoints());
            face->EvaluatePosition(pos,closest,sub,pc,distance,weights.data());
            if(distance<best) {best=distance;r->face_id=f;}
        }
        best=100.0; double bestWorld=1e100; // 10 physical pixel tolerance
        for(vtkIdType i=0;i<cell->GetNumberOfPoints();++i) {
            auto ni=cell->GetPointId(i);double point[3],display[3];mesh->grid->GetPoint(ni,point);vm_project(v,point,display);
            double d=(display[0]-x)*(display[0]-x)+(display[1]-y)*(display[1]-y);
            // Reject nodes on the back of a volume at the same screen position.
            double worlddist=0;for(int k=0;k<3;++k) worlddist+=(point[k]-pos[k])*(point[k]-pos[k]);
            if((d<best-0.01)||(d<100.0&&std::abs(d-best)<=0.01&&worlddist<bestWorld)) {
                best=d;bestWorld=worlddist;r->node_id=mesh->nodes[ni];
            }
        }
        r->sequence=v->last.sequence+1;v->last=*r;if(v->callback)v->callback(r,v->callback_user);return 1;
    } CATCH
}
int32_t vm_write_png(vm_view* v,const char* path) {
    TRY {VIEW;require(path&&*path,"empty PNG path");v->window->Render();v->rendered=true;
        vtkNew<vtkWindowToImageFilter> capture;capture->SetInput(v->window);capture->ReadFrontBufferOff();capture->SetInputBufferTypeToRGB();
        vtkNew<vtkPNGWriter> writer;writer->SetFileName(path);writer->SetInputConnection(capture->GetOutputPort());writer->Write();
        require(writer->GetErrorCode()==0,"PNG write failed");return 0;} CATCH
}
int32_t vm_process_events(vm_view* v) {TRY {VIEW;if(v->interactor)v->interactor->ProcessEvents();return 0;} CATCH}
int32_t vm_last_pick(vm_view* v,vm_pick_result* r) {TRY {VIEW;require(r,"null pick output");*r=v->last;return 0;} CATCH}
}
