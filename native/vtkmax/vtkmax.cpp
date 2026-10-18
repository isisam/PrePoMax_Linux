// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 PrePoMax-RCM contributors (RCM Lab, NCKU)
// Copyright (c) Matej Borovinšek (ported vtkControl conventions/code)
#include "vtkmax.h"
#include <vtkActor.h>
#include <vtkActor2D.h>
#include <vtkAreaPicker.h>
#include <vtkCellLocator.h>
#include <vtkExtractSelectedFrustum.h>
#include <vtkIdTypeArray.h>
#include <vtkPlanes.h>
#include <vtkPlane.h>
#include <vtkPolyData.h>
#include <vtkPolyDataMapper2D.h>
#include <vtkProperty2D.h>
#include <vtkProp3DCollection.h>
#include <vtkSelectionNode.h>
#include <vtkCamera.h>
#include <vtkQuaternionInterpolator.h>
#include <vtkMath.h>
#include <vtkMatrix4x4.h>
#include <vtkCell.h>
#include <vtkCellArray.h>
#include <vtkCellData.h>
#include <vtkCellPicker.h>
#include <vtkStaticCellLocator.h>
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
#include <vtkglad/include/glad/egl.h>
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
#include <unordered_set>
#include <vector>

namespace {
thread_local std::string error;
void require(bool ok, const char* text) { if (!ok) throw std::invalid_argument(text); }
int nodes_per_cell(int t) {
    switch(t) { case 1:return 1; case 3:return 2; case 21:return 3; case 5:return 3; case 9:return 4; case 22:return 6; case 23:return 8;
    case 10:return 4; case 12:return 8; case 13:return 6; case 24:return 10;
    case 25:return 20; case 26:return 15; default:return -1; }
}
bool finite3(const double* p) { return std::isfinite(p[0]) && std::isfinite(p[1]) && std::isfinite(p[2]); }
}
struct actor_state {
    vtkNew<vtkUnstructuredGrid> grid;
    vtkNew<vtkDataSetMapper> mapper;
    vtkNew<vtkActor> actor;
    vtkNew<vtkStaticCellLocator> locator;
    vtkNew<vtkUnstructuredGrid> surface;
    vtkNew<vtkUnstructuredGrid> pick_faces;
    std::vector<vtkIdType> surface_parent;
    vtkNew<vtkCellLocator> surface_locator;
    bool selectable=true,selection_layer=false;
    std::vector<int64_t> nodes, elements;
    std::unordered_map<int64_t, vtkIdType> node_map, element_map;
    std::vector<double> original;
};
struct vm_view : actor_state {
    vtkSmartPointer<vtkOpenGLRenderWindow> window;
    vtkNew<vtkRenderer> renderer;
    vtkNew<vtkRenderer> selection_renderer;
    vtkNew<vtkLookupTable> lut;
    vtkNew<vtkScalarBarActor> bar;
    vtkNew<vtkActor> highlight;
    vtkSmartPointer<vtkRenderWindowInteractor> interactor;
    std::unordered_map<int64_t,std::unique_ptr<actor_state>> actors;
    vtkNew<vtkQuaternionInterpolator> animation;
    vtkNew<vtkCamera> animation_start;
    vtkNew<vtkCamera> animation_end;
    bool animating=false;
    vm_selection_callback callback=nullptr;
    void* callback_user=nullptr;
    vm_pick_result last{-1,-1,-1,0,0,0,0,0,-1};
    bool rendered=false, offscreen=true;
    bool filter_active=false, selection_mode=false, box_active=false;
    int selection_pixels=7, gesture[7]={};
    std::unordered_set<int64_t> filter;
    std::vector<int64_t> selected[5];
    vtkNew<vtkAreaPicker> area_picker;
    vtkNew<vtkActor2D> rubber_band;
    vtkNew<vtkPoints> rubber_points;
    int backend=1;
    std::string gl_info;
    ~vm_view() { if(interactor) { interactor->Disable(); interactor->SetRenderWindow(nullptr); } if(window)window->Finalize(); }
};
actor_state* find_actor(vm_view* v,int64_t id) {
    require(v,"null view");if(id==0)return v;
    auto it=v->actors.find(id);require(it!=v->actors.end(),"unknown actor ID");return it->second.get();
}
void triangulate_surface(actor_state* a) {
    // Exact AddCellDataToPoly convention from vtkMaxActor, including the
    // quadratic quad diagonal. Keep original faces separately for edge picking.
    a->surface->Initialize();a->surface->Allocate(a->pick_faces->GetNumberOfCells()*6);a->surface->SetPoints(a->pick_faces->GetPoints());
    a->surface->GetPointData()->SetGlobalIds(a->pick_faces->GetPointData()->GetGlobalIds());
    vtkNew<vtkIdTypeArray> ids;ids->SetName("GlobalElementIds");a->surface_parent.clear();
    for(vtkIdType i=0;i<a->pick_faces->GetNumberOfCells();++i){auto cell=a->pick_faces->GetCell(i);std::vector<std::vector<int>> triangles;
        switch(cell->GetCellType()) {
        case 5:triangles={{0,1,2}};break;
        case 9:triangles={{0,1,2},{0,2,3}};break;
        case 22:triangles={{0,3,5},{3,4,5},{3,1,4},{5,4,2}};break;
        case 23:{double p[3],q[3];cell->GetPoints()->GetPoint(4,p);cell->GetPoints()->GetPoint(6,q);double d1=vtkMath::Distance2BetweenPoints(p,q);
            cell->GetPoints()->GetPoint(5,p);cell->GetPoints()->GetPoint(7,q);double d2=vtkMath::Distance2BetweenPoints(p,q);
            triangles=d1<d2?std::vector<std::vector<int>>{{7,0,4},{7,4,6},{7,6,3},{5,4,1},{5,6,4},{5,2,6}}:
                                          std::vector<std::vector<int>>{{4,7,0},{4,5,7},{4,1,5},{6,5,2},{6,7,5},{6,3,7}};break;}
        default:{std::vector<vtkIdType> nodes;for(vtkIdType j=0;j<cell->GetNumberOfPoints();++j)nodes.push_back(cell->GetPointId(j));
            a->surface->InsertNextCell(cell->GetCellType(),nodes.size(),nodes.data());
            ids->InsertNextValue(static_cast<vtkIdType>(a->pick_faces->GetCellData()->GetGlobalIds()->GetTuple1(i)));a->surface_parent.push_back(i);break;}
        }
        for(auto& t:triangles){vtkIdType nodes[3];for(int j=0;j<3;++j)nodes[j]=cell->GetPointId(t[j]);
            a->surface->InsertNextCell(5,3,nodes);ids->InsertNextValue(static_cast<vtkIdType>(a->pick_faces->GetCellData()->GetGlobalIds()->GetTuple1(i)));a->surface_parent.push_back(i);}
    }
    a->surface->GetCellData()->SetGlobalIds(ids);a->surface_locator->SetDataSet(a->surface);a->surface_locator->BuildLocator();
}
void build_surface(actor_state* a) {
    struct face {std::vector<vtkIdType> ids;int type;int64_t element;int count=1;};
    std::vector<face> faces;std::unordered_map<std::string,size_t> seen;
    for(vtkIdType i=0;i<a->grid->GetNumberOfCells();++i){auto cell=a->grid->GetCell(i);
        const int n=cell->GetCellDimension()==3?cell->GetNumberOfFaces():1;
        for(int j=0;j<n;++j){auto f=cell->GetCellDimension()==3?cell->GetFace(j):cell;
            std::vector<vtkIdType> ids;for(vtkIdType k=0;k<f->GetNumberOfPoints();++k)ids.push_back(f->GetPointId(k));
            auto sorted=ids;std::sort(sorted.begin(),sorted.end());std::string key;
            for(auto id:sorted)key+=std::to_string(id)+",";
            auto it=seen.find(key);
            if(it==seen.end()){seen[key]=faces.size();faces.push_back({ids,f->GetCellType(),a->elements[i],1});}
            else ++faces[it->second].count;
        }
    }
    a->pick_faces->Initialize();a->pick_faces->Allocate(faces.size());a->pick_faces->SetPoints(a->grid->GetPoints());
    a->pick_faces->GetPointData()->SetGlobalIds(a->grid->GetPointData()->GetGlobalIds());
    vtkNew<vtkIdTypeArray> ids;ids->SetName("GlobalElementIds");
    for(auto& f:faces)if(f.count==1){a->pick_faces->InsertNextCell(f.type,f.ids.size(),f.ids.data());ids->InsertNextValue(f.element);}
    a->pick_faces->GetCellData()->SetGlobalIds(ids);triangulate_surface(a);
}
int publish_pick(vm_view* v,vm_pick_result* r) {
    r->sequence=v->last.sequence+1;v->last=*r;
    if(v->callback)v->callback(r,v->callback_user);
    return r->hit;
}
bool selectable(vm_view* v, int64_t id, actor_state* a) {
    return a->selectable && !a->selection_layer && (v->filter_active ? v->filter.count(id)!=0 : a->actor->GetVisibility()!=0);
}
void draw_rubber_band(vm_view* v, int x1,int y1,int x2,int y2) {
    const double p[4][3]={{double(x1),double(y1),0},{double(x2),double(y1),0},
                        {double(x2),double(y2),0},{double(x1),double(y2),0}};
    for(int i=0;i<4;++i)v->rubber_points->SetPoint(i,p[i]);
    v->rubber_points->Modified();v->rubber_band->VisibilityOn();
    v->window->Render();
}
class ClickStyle : public vtkInteractorStyleTrackballCamera {
public:
    static ClickStyle* New() { return new ClickStyle; }
    vtkTypeMacro(ClickStyle, vtkInteractorStyleTrackballCamera)
    vm_view* view=nullptr;
    void OnChar() override {} // Host owns w/s/p/f/e/q and all keyboard shortcuts.
    int start[2]={0,0}; bool pressed=false;
    void OnLeftButtonDown() override {
        auto p=GetInteractor()->GetEventPosition(); start[0]=p[0]; start[1]=p[1];
        pressed=true;view->box_active=false;
        if(!view->selection_mode)vtkInteractorStyleTrackballCamera::OnLeftButtonDown();
    }
    void OnMouseMove() override {
        if(pressed && view->selection_mode) {
            auto p=GetInteractor()->GetEventPosition();
            if(std::abs(p[0]-start[0])+std::abs(p[1]-start[1])>3)view->box_active=true;
            if(view->box_active)draw_rubber_band(view,start[0],start[1],p[0],p[1]);
        } else vtkInteractorStyleTrackballCamera::OnMouseMove();
    }
    void OnLeftButtonUp() override {
        if(!view->selection_mode)vtkInteractorStyleTrackballCamera::OnLeftButtonUp();
        auto p=GetInteractor()->GetEventPosition();
        view->gesture[0]=view->box_active;view->gesture[1]=p[0];view->gesture[2]=p[1];
        view->gesture[3]=start[0];view->gesture[4]=start[1];
        view->gesture[5]=GetInteractor()->GetControlKey();view->gesture[6]=GetInteractor()->GetShiftKey();
        if(view->selection_mode && view->box_active) {
            double planes[36];
            if(vm_area_pick(view,p[0],p[1],start[0],start[1],planes)==0) {
                vm_pick_result r{-1,-1,-1,0,0,0,0,0,-1};publish_pick(view,&r);
            }
        } else if(std::abs(p[0]-start[0])+std::abs(p[1]-start[1])<4) {
            vm_pick_result r;vm_pick(view,p[0],p[1],&r);
        }
        pressed=false;view->box_active=false;view->rubber_band->VisibilityOff();view->window->Render();
    }
};
#define TRY error.clear(); try
#define CATCH catch(const std::exception& e) { error=e.what(); return -1; } catch(...) { error="native exception"; return -1; }
#define VIEW require(v!=nullptr,"null view")
extern "C" {
int32_t vm_abi_version() { return 3; }
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
        } else if(backend==3) {
            // Explicit subclass creation bypasses vtkOpenGLRenderWindow::New's
            // runtime backend probe, which normally loads core EGL entry points.
            require(gladLoaderLoadEGL(EGL_NO_DISPLAY)!=0,"system EGL loader unavailable");
            v->window=vtkSmartPointer<vtkEGLRenderWindow>::New();
        }
        else {
#ifdef VTKMAX_HAS_X11
            v->window=vtkSmartPointer<vtkXOpenGLRenderWindow>::New();
#else
            require(false,"X11 backend disabled at build time; choose EGL or generic");
#endif
        }
        v->window->SetOffScreenRendering(backend==1||backend==3); v->window->SetSize(w,h);
        v->window->SetMultiSamples(0);v->window->SetNumberOfLayers(2);v->window->AddRenderer(v->renderer);
        v->selection_renderer->SetLayer(1);v->selection_renderer->SetActiveCamera(v->renderer->GetActiveCamera());
        v->selection_renderer->InteractiveOff();v->window->AddRenderer(v->selection_renderer);
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
        v->selection_renderer->AddActor(v->highlight); v->highlight->VisibilityOff();
        v->rubber_points->SetNumberOfPoints(4);
        for(int i=0;i<4;++i)v->rubber_points->SetPoint(i,0,0,0);
        vtkNew<vtkPolyData> rectangle;rectangle->SetPoints(v->rubber_points);
        vtkNew<vtkCellArray> lines;vtkIdType corners[5]={0,1,2,3,0};lines->InsertNextCell(5,corners);rectangle->SetLines(lines);
        vtkNew<vtkPolyDataMapper2D> rectangle_mapper;rectangle_mapper->SetInputData(rectangle);
        v->rubber_band->SetMapper(rectangle_mapper);v->rubber_band->PickableOff();v->rubber_band->UseBoundsOff();
        v->rubber_band->GetProperty()->SetColor(.25,.85,1);v->rubber_band->GetProperty()->SetLineWidth(2);
        v->rubber_band->VisibilityOff();v->selection_renderer->AddViewProp(v->rubber_band);
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
        vtkNew<vtkIdTypeArray> global_nodes;global_nodes->SetName("GlobalNodeIds");
        vtkNew<vtkIdTypeArray> global_cells;global_cells->SetName("GlobalElementIds");
        for(int i=0;i<np;++i)global_nodes->InsertNextValue(ids[i]);
        for(int i=0;i<nc;++i)global_cells->InsertNextValue(eids[i]);
        g->GetPointData()->SetGlobalIds(global_nodes);g->GetCellData()->SetGlobalIds(global_cells);
        std::vector<int64_t> node_ids(ids,ids+np), element_ids(eids,eids+nc);
        v->grid->ShallowCopy(g); v->nodes=std::move(node_ids); v->elements=std::move(element_ids);
        v->node_map=std::move(nm); v->element_map=std::move(em);
        v->original.assign(xyz,xyz+3*np);
        v->mapper->SetInputData(v->grid);v->actor->SetMapper(v->mapper);
        v->locator->SetDataSet(v->grid);v->locator->BuildLocator();
        build_surface(v);
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
        v->renderer->RemoveActor(m->actor);v->selection_renderer->RemoveActor(m->actor);v->actors.erase(id);v->highlight->VisibilityOff();return 0;} CATCH
}
int32_t vm_actor_style(vm_view* v,int64_t id,int32_t visible,double r,double g,double b,double opacity,int32_t edges) {
    TRY {auto m=find_actor(v,id);for(double x:{r,g,b,opacity})require(std::isfinite(x)&&x>=0&&x<=1,"invalid color/opacity");
        m->actor->SetVisibility(visible!=0);auto p=m->actor->GetProperty();p->SetColor(r,g,b);p->SetOpacity(opacity);p->SetEdgeVisibility(edges!=0&&!m->selection_layer);return 0;} CATCH
}
int32_t vm_deformation(vm_view* v,int64_t id,const double* u,int32_t count,double scale) {
    TRY {auto m=find_actor(v,id);require(std::isfinite(scale),"nonfinite deformation scale");
        require((!u&&count==0)||(u&&count==static_cast<int>(m->original.size())),"displacement count mismatch");
        vtkNew<vtkPoints> points;points->SetDataTypeToDouble();
        for(size_t i=0;i<m->original.size();i+=3) {double p[3];
            for(int j=0;j<3;++j){double d=u?u[i+j]:0;require(std::isfinite(d),"nonfinite displacement");p[j]=m->original[i+j]+scale*d;require(std::isfinite(p[j]),"deformation overflow");}
            points->InsertNextPoint(p);}
        m->grid->SetPoints(points);m->grid->Modified();
        vtkNew<vtkPoints> surface_points;auto ids=m->pick_faces->GetPointData()->GetGlobalIds();
        for(vtkIdType i=0;ids&&i<ids->GetNumberOfTuples();++i){double p[3];points->GetPoint(m->node_map.at(static_cast<int64_t>(ids->GetTuple1(i))),p);surface_points->InsertNextPoint(p);}
        m->pick_faces->SetPoints(surface_points);m->surface->SetPoints(surface_points);m->surface->Modified();return 0;} CATCH
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
        m->mapper->InterpolateScalarsBeforeMappingOn();
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
int32_t vm_camera_scale(vm_view* v,double* scale) {
    TRY {VIEW;require(scale,"null scale");*scale=v->renderer->GetActiveCamera()->GetParallelScale();return 0;} CATCH
}
int32_t vm_set_camera(vm_view* v,const double* position,const double* focal,const double* up,double scale) {
    TRY {VIEW;require(position&&focal&&up&&std::isfinite(scale)&&scale>0,"invalid camera");
        for(int i=0;i<3;++i)require(std::isfinite(position[i])&&std::isfinite(focal[i])&&std::isfinite(up[i]),"nonfinite camera");
        double direction[3];vtkMath::Subtract(position,focal,direction);double cross[3];vtkMath::Cross(direction,up,cross);
        require(vtkMath::Norm(direction)>0&&vtkMath::Norm(cross)>0,"degenerate camera");
        auto c=v->renderer->GetActiveCamera();c->SetPosition(position);c->SetFocalPoint(focal);c->SetViewUp(up);c->SetParallelScale(scale);
        v->renderer->ResetCameraClippingRange();return 0;} CATCH
}
int32_t vm_scalar_bar(vm_view* v,int32_t visible,const char* title) {
    TRY {VIEW;v->bar->SetVisibility(visible!=0);if(title)v->bar->SetTitle(title);return 0;} CATCH
}
int32_t vm_animate_standard_begin(vm_view* v,int32_t view) {
    TRY {VIEW;v->animation_start->DeepCopy(v->renderer->GetActiveCamera());
        if(vm_standard_view(v,view)<0)return -1;
        v->animation_end->DeepCopy(v->renderer->GetActiveCamera());
        v->animation->Initialize();v->animation->SetInterpolationTypeToLinear();
        double a[3][3],b[3][3],qa[4],qb[4];
        for(int i=0;i<3;++i)for(int j=0;j<3;++j){a[i][j]=v->animation_start->GetViewTransformMatrix()->GetElement(i,j);b[i][j]=v->animation_end->GetViewTransformMatrix()->GetElement(i,j);}
        vtkMath::Matrix3x3ToQuaternion(a,qa);vtkMath::Matrix3x3ToQuaternion(b,qb);
        double dot=0;for(int i=0;i<4;++i)dot+=qa[i]*qb[i];if(dot<0)for(double& x:qb)x=-x;
        v->animation->AddQuaternion(0,qa);v->animation->AddQuaternion(1,qb);
        v->renderer->GetActiveCamera()->DeepCopy(v->animation_start);v->animating=true;return 0;} CATCH
}
int32_t vm_animate_standard_step(vm_view* v,double t) {
    TRY {VIEW;require(v->animating&&std::isfinite(t)&&t>=0&&t<=1,"invalid animation fraction/state");
        // vtkControl.cs AnimateCamera: shortest-arc quaternion orientation, focal
        // point/pan and zoom interpolation. A frame clock is supplied by the host.
        if(t==1){v->renderer->GetActiveCamera()->DeepCopy(v->animation_end);v->animating=false;}
        else {
            double q[4],rotation[3][3],fp[3],pos[3],up[3];v->animation->InterpolateQuaternion(t,q);vtkMath::QuaternionToMatrix3x3(q,rotation);
            double distance=(1-t)*v->animation_start->GetDistance()+t*v->animation_end->GetDistance();
            for(int i=0;i<3;++i){fp[i]=(1-t)*v->animation_start->GetFocalPoint()[i]+t*v->animation_end->GetFocalPoint()[i];pos[i]=fp[i]+distance*rotation[2][i];up[i]=rotation[1][i];}
            auto c=v->renderer->GetActiveCamera();c->SetFocalPoint(fp);c->SetPosition(pos);c->SetViewUp(up);
            c->SetParallelScale((1-t)*v->animation_start->GetParallelScale()+t*v->animation_end->GetParallelScale());
            c->SetViewAngle((1-t)*v->animation_start->GetViewAngle()+t*v->animation_end->GetViewAngle());
        }
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
        v->window->Render();v->rendered=true;
        if(v->gl_info.empty()) {
            auto vendor=glGetString(GL_VENDOR),renderer=glGetString(GL_RENDERER),version=glGetString(GL_VERSION);
            v->gl_info=std::string("OpenGL vendor string: ")+(vendor?reinterpret_cast<const char*>(vendor):"unknown")+
                "\nOpenGL renderer string: "+(renderer?reinterpret_cast<const char*>(renderer):"unknown")+
                "\nOpenGL version string: "+(version?reinterpret_cast<const char*>(version):"unknown");
        }
        s->Pop();return 0;} CATCH
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
    if(v->backend==2&&!v->gl_info.empty())return v->gl_info.c_str();
    v->gl_info=v->window->ReportCapabilities();return v->gl_info.c_str();
}
int32_t vm_project(vm_view* v,const double* xyz,double* out) {
    TRY {VIEW;require(xyz&&out&&finite3(xyz),"invalid projection arrays");v->renderer->SetWorldPoint(xyz[0],xyz[1],xyz[2],1);
        v->renderer->WorldToDisplay();std::copy_n(v->renderer->GetDisplayPoint(),3,out);return 0;} CATCH
}
int32_t vm_pick(vm_view* v,int32_t x,int32_t y,vm_pick_result* r) {
    TRY { VIEW;require(r,"null pick output");*r={-1,-1,-1,0,0,0,0,0,-1};
        require(v->rendered,"render before picking");auto size=v->window->GetSize();
        if(x<0||y<0||x>=size[0]||y>=size[1]) return publish_pick(v,r);
        vtkNew<vtkCellPicker> p;p->SetTolerance(0.0005);p->PickFromListOn();
        if(selectable(v,0,v)){p->AddPickList(v->actor);p->AddLocator(v->locator);}
        for(auto& pair:v->actors) if(selectable(v,pair.first,pair.second.get())){p->AddPickList(pair.second->actor);p->AddLocator(pair.second->locator);}
        if(!p->Pick(x,y,0,v->renderer)||p->GetCellId()<0) return publish_pick(v,r);
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
        return publish_pick(v,r);
    } CATCH
}
int32_t vm_write_png(vm_view* v,const char* path) {
    return vm_write_png_scaled(v,path,0,1);
}
int32_t vm_write_png_scaled(vm_view* v,const char* path,int32_t transparent,int32_t magnification) {
    TRY {VIEW;require(path&&*path,"empty PNG path");v->window->Render();v->rendered=true;
        require(magnification>=1&&magnification<=8,"PNG magnification outside 1..8");
        vtkNew<vtkWindowToImageFilter> capture;capture->SetInput(v->window);capture->ReadFrontBufferOff();capture->SetScale(magnification);
        if(transparent)capture->SetInputBufferTypeToRGBA();else capture->SetInputBufferTypeToRGB();
        vtkNew<vtkPNGWriter> writer;writer->SetFileName(path);writer->SetInputConnection(capture->GetOutputPort());writer->Write();
        require(writer->GetErrorCode()==0,"PNG write failed");return 0;} CATCH
}
int32_t vm_process_events(vm_view* v) {TRY {VIEW;if(v->interactor)v->interactor->ProcessEvents();return 0;} CATCH}
int32_t vm_x11_connection(vm_view* v) {
    TRY {VIEW;
#ifdef VTKMAX_HAS_X11
        auto x=vtkXOpenGLRenderWindow::SafeDownCast(v->window);require(x&&x->GetDisplayId(),"X11 connection unavailable");return ConnectionNumber(x->GetDisplayId());
#else
        require(false,"X11 backend disabled");return -1;
#endif
    } CATCH
}
int32_t vm_last_pick(vm_view* v,vm_pick_result* r) {TRY {VIEW;require(r,"null pick output");*r=v->last;return 0;} CATCH}
int32_t vm_actor_selectable(vm_view* v,int64_t id,int32_t enabled) {
    TRY {auto a=find_actor(v,id);a->selectable=enabled!=0;a->actor->SetPickable(a->selectable&&!a->selection_layer);return 0;} CATCH
}
int32_t vm_actor_selection_layer(vm_view* v,int64_t id,int32_t enabled) {
    TRY {auto a=find_actor(v,id);bool layer=enabled!=0;if(a->selection_layer==layer)return 0;
        (a->selection_layer?v->selection_renderer:v->renderer)->RemoveActor(a->actor);
        a->selection_layer=layer;(layer?v->selection_renderer:v->renderer)->AddActor(a->actor);
        a->actor->SetPickable(a->selectable&&!layer);a->actor->SetUseBounds(!layer);
        if(layer)a->actor->GetProperty()->EdgeVisibilityOff();
        a->actor->GetProperty()->SetLighting(!layer);a->actor->GetProperty()->SetPointSize(layer?8:1);a->actor->GetProperty()->SetLineWidth(layer?2:1);return 0;} CATCH
}
int32_t vm_actor_pick_geometry(vm_view* v,int64_t id,int32_t np,const int64_t* nodes,const double* xyz,
    int32_t nc,const int64_t* elements,const int32_t* types,const int32_t* offsets,int32_t nconn,const int64_t* conn) {
    TRY {auto a=find_actor(v,id);require(np>0&&nc>0&&nconn>0&&nodes&&xyz&&elements&&types&&offsets&&conn,"invalid pick geometry arrays");
        require(offsets[0]==0&&offsets[nc]==nconn,"invalid pick geometry offsets");
        vtkNew<vtkPoints> points;vtkNew<vtkUnstructuredGrid> faces;
        vtkNew<vtkIdTypeArray> nids;vtkNew<vtkIdTypeArray> eids;
        for(int i=0;i<np;++i){require(a->node_map.count(nodes[i])&&finite3(xyz+3*i),"invalid pick geometry node");
            points->InsertNextPoint(xyz+3*i);nids->InsertNextValue(nodes[i]);}
        faces->SetPoints(points);faces->GetPointData()->SetGlobalIds(nids);
        for(int i=0;i<nc;++i){int type=types[i];require(type==5||type==9||type==22||type==23,"unsupported pick face type");
            int count=nodes_per_cell(type);require(a->element_map.count(elements[i])&&offsets[i]>=0&&offsets[i+1]<=nconn&&offsets[i+1]-offsets[i]==count,"invalid pick face");
            std::vector<vtkIdType> ids(count);for(int j=0;j<count;++j){auto p=conn[offsets[i]+j];require(p>=0&&p<np,"pick geometry connectivity out of range");ids[j]=p;}
            faces->InsertNextCell(type,count,ids.data());eids->InsertNextValue(elements[i]);}
        faces->GetCellData()->SetGlobalIds(eids);a->pick_faces->ShallowCopy(faces);triangulate_surface(a);return 0;} CATCH
}
int32_t vm_selectable_filter(vm_view* v,const int64_t* ids,int32_t count) {
    TRY {VIEW;require(count>=0 && (count==0||ids),"invalid filter");
        std::unordered_set<int64_t> filter;
        for(int i=0;i<count;++i){find_actor(v,ids[i]);filter.insert(ids[i]);}
        v->filter=std::move(filter);v->filter_active=ids!=nullptr;return 0;} CATCH
}
int32_t vm_area_pick(vm_view* v,int32_t x1,int32_t y1,int32_t x2,int32_t y2,double* output) {
    TRY {VIEW;require(output,"null plane output");
        v->area_picker->AreaPick(x1,y1,x2,y2,v->renderer);
        auto planes=v->area_picker->GetFrustum();require(planes->GetNumberOfPlanes()==6,"expected six frustum planes");
        for(int i=0;i<6;++i) {auto p=planes->GetPlane(i);std::copy_n(p->GetOrigin(),3,output+6*i);std::copy_n(p->GetNormal(),3,output+6*i+3);}
        v->selected[2].clear();auto props=v->area_picker->GetProp3Ds();props->InitTraversal();
        while(auto prop=props->GetNextProp3D()) {
            if(prop==v->actor && selectable(v,0,v) && !v->elements.empty())v->selected[2].push_back(0);
            for(auto& pair:v->actors)if(prop==pair.second->actor && selectable(v,pair.first,pair.second.get()))v->selected[2].push_back(pair.first);
        }
        std::sort(v->selected[2].begin(),v->selected[2].end());return 0;} CATCH
}
int32_t vm_frustum_select(vm_view* v,const double* data) {
    TRY {VIEW;require(data,"null planes");
        vtkNew<vtkPoints> origins;origins->SetDataTypeToDouble();vtkNew<vtkDoubleArray> normals;normals->SetNumberOfComponents(3);
        for(int i=0;i<6;++i){auto p=data+6*i;require(finite3(p)&&finite3(p+3)&&vtkMath::Norm(p+3)>0,"invalid plane");origins->InsertNextPoint(p);normals->InsertNextTuple(p+3);}
        vtkNew<vtkPlanes> planes;planes->SetPoints(origins);planes->SetNormals(normals);
        std::unordered_set<int64_t> point_ids,cell_ids;
        auto extract=[&](int64_t id,actor_state* a) {
            if(!selectable(v,id,a)||a->elements.empty())return;
            auto b=a->grid->GetBounds();bool intersects=false;
            // CaeMesh.BoundingBox.TestBoundsAgainstFrustum: outward normals, inside <= 0.
            for(int i=0;i<6;++i) {auto p=data+6*i;double low=0,high=0;
                for(int k=0;k<3;++k){double n=p[k+3];low+=n*(b[2*k+(n>=0?0:1)]-p[k]);high+=n*(b[2*k+(n>=0?1:0)]-p[k]);}
                if(low>0)return;
                if(high>0)intersects=true;
            }
            if(!intersects){point_ids.insert(a->nodes.begin(),a->nodes.end());cell_ids.insert(a->elements.begin(),a->elements.end());return;}
            vtkNew<vtkExtractSelectedFrustum> extractor;extractor->SetInputData(a->grid);extractor->SetFrustum(planes);extractor->SetContainingCells(0);
            extractor->SetFieldType(vtkSelectionNode::POINT);extractor->Update();
            auto grid=vtkUnstructuredGrid::SafeDownCast(extractor->GetOutput());auto ids=grid->GetPointData()->GetGlobalIds();
            if(ids)for(vtkIdType i=0;i<ids->GetNumberOfTuples();++i)point_ids.insert(static_cast<int64_t>(ids->GetTuple1(i)));
            extractor->SetFieldType(vtkSelectionNode::CELL);extractor->Update();
            grid=vtkUnstructuredGrid::SafeDownCast(extractor->GetOutput());ids=grid->GetCellData()->GetGlobalIds();
            if(ids)for(vtkIdType i=0;i<ids->GetNumberOfTuples();++i)cell_ids.insert(static_cast<int64_t>(ids->GetTuple1(i)));
        };
        extract(0,v);for(auto& pair:v->actors)extract(pair.first,pair.second.get());
        v->selected[0].assign(cell_ids.begin(),cell_ids.end());v->selected[1].assign(point_ids.begin(),point_ids.end());
        for(int a=0;a<2;++a)std::sort(v->selected[a].begin(),v->selected[a].end());
        return 0;} CATCH
}
int32_t vm_selected_ids(vm_view* v,int32_t association,const int64_t** ids,int32_t* count) {
    TRY {VIEW;require(association>=0&&association<5&&ids&&count,"invalid selected IDs output");
        auto& values=v->selected[association];*count=static_cast<int32_t>(values.size());*ids=values.data();return 0;} CATCH
}
int32_t vm_selection_precision(vm_view* v,double* distance) {
    TRY {VIEW;require(distance,"null precision output");double a[3],b[3];
        v->renderer->SetDisplayPoint(0,0,0);v->renderer->DisplayToWorld();std::copy_n(v->renderer->GetWorldPoint(),3,a);
        v->renderer->SetDisplayPoint(v->selection_pixels,0,0);v->renderer->DisplayToWorld();std::copy_n(v->renderer->GetWorldPoint(),3,b);
        *distance=std::sqrt(vtkMath::Distance2BetweenPoints(a,b));return 0;} CATCH
}
int32_t vm_selection_distance(vm_view* v,int32_t pixels) {TRY {VIEW;v->selection_pixels=std::max(1,pixels);return 0;} CATCH}
int32_t vm_selection_mode(vm_view* v,int32_t enabled) {TRY {VIEW;v->selection_mode=enabled!=0;return 0;} CATCH}
int32_t vm_box_selection_active(vm_view* v) {TRY {VIEW;return v->box_active?1:0;} CATCH}
int32_t vm_last_gesture(vm_view* v,int32_t* values) {TRY {VIEW;require(values,"null gesture output");std::copy_n(v->gesture,7,values);return 0;} CATCH}
int32_t vm_geometry_pick(vm_view* v,const double* point,int64_t* element,int32_t unlimited) {
    TRY {VIEW;require(point&&element&&finite3(point),"invalid geometry pick");*element=-1;v->selected[3].clear();v->selected[4].clear();
        double best=VTK_DOUBLE_MAX,closest[3]={};vtkCell* chosen=nullptr;vtkCell* original_face=nullptr;vtkDataSet* dataset=nullptr;
        auto find=[&](int64_t id,actor_state* a){if(!selectable(v,id,a)||a->elements.empty())return;
            a->surface_locator->BuildLocator();
            double hit[3],d;vtkIdType cell_id;int sub;
            a->surface_locator->FindClosestPoint(point,hit,cell_id,sub,d);
            if(d>=0&&d<best){best=d;std::copy_n(hit,3,closest);dataset=a->surface;chosen=dataset->GetCell(cell_id);
                original_face=a->pick_faces->GetCell(a->surface_parent[cell_id]);
                *element=static_cast<int64_t>(dataset->GetCellData()->GetGlobalIds()->GetTuple1(cell_id));}
        };
        find(0,v);for(auto& pair:v->actors){find(pair.first,pair.second.get());if(best==0)break;}
        if(!chosen)return 0;
        double precision;vm_selection_precision(v,&precision);
        if(!unlimited&&best>precision*precision)*element=-1; // vtkControl still returns the closest face properties.
        auto global=dataset->GetPointData()->GetGlobalIds();
        for(vtkIdType i=0;i<chosen->GetNumberOfPoints();++i)v->selected[4].push_back(static_cast<int64_t>(global->GetTuple1(chosen->GetPointId(i))));
        if(*element<0)return 0; // Windows' cell-face callback rejects element -1, so its edge is null.
        double edge_best=VTK_DOUBLE_MAX;std::vector<vtkIdType> edge_ids;
        auto segment=[](const double* p,const double* a,const double* b){double ab[3],ap[3];vtkMath::Subtract(b,a,ab);vtkMath::Subtract(p,a,ap);
            double n=vtkMath::Dot(ab,ab),t=n>0?std::clamp(vtkMath::Dot(ap,ab)/n,0.0,1.0):0;double q[3];for(int k=0;k<3;++k)q[k]=a[k]+t*ab[k];return vtkMath::Distance2BetweenPoints(p,q);};
        for(int e=0;e<original_face->GetNumberOfEdges();++e){auto edge=original_face->GetEdge(e);std::vector<vtkIdType> ids;
            if(edge->GetNumberOfPoints()==3)ids={edge->GetPointId(0),edge->GetPointId(2),edge->GetPointId(1)};
            else ids={edge->GetPointId(0),edge->GetPointId(1)};
            double d=VTK_DOUBLE_MAX;for(size_t j=1;j<ids.size();++j){double p[3],q[3];dataset->GetPoint(ids[j-1],p);dataset->GetPoint(ids[j],q);d=std::min(d,segment(closest,p,q));}
            if(d<edge_best){edge_best=d;edge_ids=ids;}
        }
        if(!edge_ids.empty()&&vtkMath::Distance2BetweenPoints(closest,dataset->GetPoint(edge_ids.back()))<vtkMath::Distance2BetweenPoints(closest,dataset->GetPoint(edge_ids.front())))std::reverse(edge_ids.begin(),edge_ids.end());
        for(auto id:edge_ids)v->selected[3].push_back(static_cast<int64_t>(global->GetTuple1(id)));
        return 0;} CATCH
}
int32_t vm_closest_cells(vm_view* v,const double* point,const int64_t* actors,int32_t count,double* distances,double* normals) {
    TRY {VIEW;require(point&&finite3(point)&&count>=0&&(count==0||(actors&&distances&&normals)),"invalid closest cell query");
        for(int i=0;i<count;++i){distances[i]=-1;std::fill_n(normals+3*i,3,0.0);
            actor_state* a=nullptr;if(actors[i]==0)a=v;else {auto it=v->actors.find(actors[i]);if(it!=v->actors.end())a=it->second.get();}
            if(!a||!a->selectable||a->selection_layer||a->surface->GetNumberOfCells()==0)continue;
            a->surface_locator->BuildLocator();double hit[3],d;vtkIdType cell_id;int sub;
            a->surface_locator->FindClosestPoint(point,hit,cell_id,sub,d);if(d<0||cell_id<0)continue;
            distances[i]=std::sqrt(d);auto cell=a->surface->GetCell(cell_id);if(cell->GetNumberOfPoints()<3)continue;
            double p[3],q[3],r[3],u[3],w[3];cell->GetPoints()->GetPoint(0,p);cell->GetPoints()->GetPoint(1,q);cell->GetPoints()->GetPoint(2,r);
            vtkMath::Subtract(q,p,u);vtkMath::Subtract(r,p,w);vtkMath::Cross(u,w,normals+3*i);vtkMath::Normalize(normals+3*i);
        }return 0;} CATCH
}
}
