// SPDX-License-Identifier: BSD-3-Clause
#include "vtkmax.h"
#include <vtkActor.h>
#include <vtkCamera.h>
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
#include <vtkXOpenGLRenderWindow.h>
#include <vtkXRenderWindowInteractor.h>
#include <algorithm>
#include <cmath>
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
struct vm_view {
    vtkNew<vtkXOpenGLRenderWindow> window;
    vtkNew<vtkRenderer> renderer;
    vtkNew<vtkUnstructuredGrid> grid;
    vtkNew<vtkDataSetMapper> mapper;
    vtkNew<vtkActor> actor;
    vtkNew<vtkLookupTable> lut;
    vtkNew<vtkScalarBarActor> bar;
    vtkNew<vtkActor> highlight;
    vtkSmartPointer<vtkRenderWindowInteractor> interactor;
    std::vector<int64_t> nodes, elements;
    std::unordered_map<int64_t, vtkIdType> node_map, element_map;
    vm_pick_result last{-1,-1,-1,0,0,0,0,0};
    bool rendered=false, offscreen=true;
    ~vm_view() { if(interactor) { interactor->Disable(); interactor->SetRenderWindow(nullptr); } window->Finalize(); }
};
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
            vm_pick_result r; if(vm_pick(view,p[0],p[1],&r)>=0) { r.sequence=view->last.sequence+1; view->last=r; }
        }
    }
};
#define TRY error.clear(); try
#define CATCH catch(const std::exception& e) { error=e.what(); return -1; } catch(...) { error="native exception"; return -1; }
#define VIEW require(v!=nullptr,"null view")
extern "C" {
int32_t vm_abi_version() { return 1; }
const char* vm_last_error() { return error.c_str(); }
vm_view* vm_create(int32_t offscreen,int32_t w,int32_t h) {
    error.clear(); try {
        require(w>0&&h>0&&w<=16384&&h<=16384,"invalid size");
        auto v=new vm_view; v->offscreen=offscreen!=0;
        v->window->SetOffScreenRendering(offscreen!=0); v->window->SetSize(w,h);
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
        return v;
    } catch(const std::exception& e) { error=e.what(); return nullptr; } catch(...) { error="native exception"; return nullptr; }
}
void vm_destroy(vm_view* v) { delete v; }
uintptr_t vm_attach_x11(vm_view* v,uintptr_t parent) {
    error.clear(); try {
        VIEW; require(!v->rendered&&!v->offscreen&&parent,"attach requires an unrendered onscreen view and X11 parent");
        v->window->SetParentId(static_cast<Window>(parent)); v->window->Render(); v->rendered=true;
        v->interactor=vtkSmartPointer<vtkXRenderWindowInteractor>::New();
        v->interactor->SetRenderWindow(v->window);
        auto style=vtkSmartPointer<ClickStyle>::New(); style->view=v;
        v->interactor->SetInteractorStyle(style); v->interactor->Initialize();
        return static_cast<uintptr_t>(v->window->GetWindowId());
    } catch(const std::exception& e) { error=e.what(); return 0; } catch(...) {error="attach exception";return 0;}
}
int32_t vm_resize(vm_view* v,int32_t w,int32_t h) {
    TRY { VIEW; require(w>0&&h>0&&w<=16384&&h<=16384,"invalid size"); v->window->SetSize(w,h);
        if(v->interactor) v->interactor->UpdateSize(w,h);
        return 0; } CATCH
}
int32_t vm_load_grid(vm_view* v,int32_t np,const int64_t* ids,const double* xyz,int32_t nc,
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
        v->grid->ShallowCopy(g); v->nodes.assign(ids,ids+np); v->elements.assign(eids,eids+nc);
        v->node_map=std::move(nm); v->element_map=std::move(em);
        v->mapper->ScalarVisibilityOff(); v->bar->VisibilityOff(); v->highlight->VisibilityOff(); return 0;
    } CATCH
}
int32_t vm_display(vm_view* v,int32_t surface,int32_t edges) {
    TRY { VIEW; v->actor->SetVisibility(surface||edges);
        if(surface) v->actor->GetProperty()->SetRepresentationToSurface();
        else v->actor->GetProperty()->SetRepresentationToWireframe();
        v->actor->GetProperty()->SetEdgeVisibility(edges!=0); return 0; } CATCH
}
int32_t vm_scalars(vm_view* v,int32_t association,const double* values,int32_t count,double lo,double hi,const char* title) {
    TRY { VIEW; require(association==0||association==1,"invalid scalar association");
        if(!values&&count==0) {v->mapper->ScalarVisibilityOff();v->bar->VisibilityOff();return 0;}
        require(values&&count==static_cast<int>(association?v->nodes.size():v->elements.size())&&count>0,"scalar count mismatch");
        require(std::isfinite(lo)&&std::isfinite(hi)&&lo<=hi,"invalid scalar range");
        vtkNew<vtkDoubleArray> a; a->SetName("vm_scalars");
        for(int i=0;i<count;++i) { require(std::isfinite(values[i]),"nonfinite scalar"); a->InsertNextValue(values[i]); }
        if(association) {v->grid->GetPointData()->SetScalars(a);v->mapper->SetScalarModeToUsePointData();}
        else {v->grid->GetCellData()->SetScalars(a);v->mapper->SetScalarModeToUseCellData();}
        if(lo==hi) hi=lo+1;
        v->lut->SetTableRange(lo,hi); v->mapper->SetLookupTable(v->lut);
        v->mapper->SetScalarRange(lo,hi); v->mapper->ScalarVisibilityOn();
        v->bar->SetTitle(title?title:""); v->bar->VisibilityOn(); return 0; } CATCH
}
int32_t vm_highlight(vm_view* v,int32_t association,const int64_t* ids,int32_t count) {
    TRY { VIEW; require((association==0||association==1)&&count>=0&&(count==0||ids),"invalid highlight arguments");
        if(count==0) {v->highlight->VisibilityOff();return 0;}
        const auto& map=association?v->node_map:v->element_map;
        vtkNew<vtkIdList> list;
        for(int i=0;i<count;++i) {auto p=map.find(ids[i]);require(p!=map.end(),"unknown highlight ID");list->InsertNextId(p->second);}
        vtkNew<vtkDataSetMapper> m;
        if(association) {
            vtkNew<vtkUnstructuredGrid> g; g->SetPoints(v->grid->GetPoints());
            for(vtkIdType i=0;i<list->GetNumberOfIds();++i) {auto p=list->GetId(i);g->InsertNextCell(VTK_VERTEX,1,&p);}
            m->SetInputData(g);v->highlight->GetProperty()->SetRepresentationToPoints();v->highlight->GetProperty()->SetPointSize(10);
        } else {
            vtkNew<vtkExtractCells> extract;extract->SetInputData(v->grid);extract->SetCellList(list);extract->Update();
            m->SetInputData(extract->GetOutput());v->highlight->GetProperty()->SetRepresentationToWireframe();
        }
        m->ScalarVisibilityOff();m->SetResolveCoincidentTopologyToPolygonOffset();
        m->SetRelativeCoincidentTopologyLineOffsetParameters(0,-4);
        v->highlight->SetMapper(m);v->highlight->VisibilityOn();return 0; } CATCH
}
int32_t vm_fit(vm_view* v) { TRY {VIEW;v->renderer->ResetCamera();v->renderer->ResetCameraClippingRange();return 0;} CATCH }
int32_t vm_standard_view(vm_view* v,int32_t view) {
    TRY { VIEW; require(view>=0&&view<=6,"invalid standard view");
        const double dirs[][3]={{1,-1,1},{0,-1,0},{0,1,0},{0,0,1},{0,0,-1},{1,0,0},{-1,0,0}};
        auto c=v->renderer->GetActiveCamera(); c->SetFocalPoint(0,0,0);c->SetPosition(dirs[view]);
        if(view==3||view==4) c->SetViewUp(0,1,0);else c->SetViewUp(0,0,1);
        c->ParallelProjectionOn();v->renderer->ResetCamera();v->renderer->ResetCameraClippingRange();return 0; } CATCH
}
int32_t vm_rotate(vm_view* v,double az,double el) {
    TRY {VIEW;require(std::isfinite(az)&&std::isfinite(el),"invalid rotation");auto c=v->renderer->GetActiveCamera();
        c->Azimuth(az);c->Elevation(el);c->OrthogonalizeViewUp();v->renderer->ResetCameraClippingRange();return 0;} CATCH
}
int32_t vm_zoom(vm_view* v,double factor) {
    TRY {VIEW;require(std::isfinite(factor)&&factor>0,"invalid zoom");v->renderer->GetActiveCamera()->Zoom(factor);return 0;} CATCH
}
int32_t vm_render(vm_view* v) {TRY {VIEW;v->window->Render();v->rendered=true;return 0;} CATCH}
int32_t vm_project(vm_view* v,const double* xyz,double* out) {
    TRY {VIEW;require(xyz&&out&&finite3(xyz),"invalid projection arrays");v->renderer->SetWorldPoint(xyz[0],xyz[1],xyz[2],1);
        v->renderer->WorldToDisplay();std::copy_n(v->renderer->GetDisplayPoint(),3,out);return 0;} CATCH
}
int32_t vm_pick(vm_view* v,int32_t x,int32_t y,vm_pick_result* r) {
    TRY { VIEW;require(r,"null pick output");*r={-1,-1,-1,0,0,0,0,0};
        require(v->rendered,"render before picking");auto size=v->window->GetSize();
        if(x<0||y<0||x>=size[0]||y>=size[1]) return 0;
        vtkNew<vtkCellPicker> p;p->SetTolerance(0.0005);p->PickFromListOn();p->AddPickList(v->actor);
        if(!p->Pick(x,y,0,v->renderer)||p->GetCellId()<0) return 0;
        auto id=p->GetCellId();require(id<static_cast<vtkIdType>(v->elements.size()),"unexpected picker cell mapping");
        auto pos=p->GetPickPosition();r->hit=1;r->element_id=v->elements[id];r->x=pos[0];r->y=pos[1];r->z=pos[2];
        auto cell=v->grid->GetCell(id);double best=1e100;
        if(cell->GetCellDimension()==2) r->face_id=0;
        else for(int f=0;f<cell->GetNumberOfFaces();++f) {
            auto face=cell->GetFace(f);double closest[3],pc[3],distance;int sub;
            std::vector<double> weights(face->GetNumberOfPoints());
            face->EvaluatePosition(pos,closest,sub,pc,distance,weights.data());
            if(distance<best) {best=distance;r->face_id=f;}
        }
        best=100.0; double bestWorld=1e100; // 10 physical pixel tolerance
        for(vtkIdType i=0;i<cell->GetNumberOfPoints();++i) {
            auto ni=cell->GetPointId(i);double point[3],display[3];v->grid->GetPoint(ni,point);vm_project(v,point,display);
            double d=(display[0]-x)*(display[0]-x)+(display[1]-y)*(display[1]-y);
            // Reject nodes on the back of a volume at the same screen position.
            double worlddist=0;for(int k=0;k<3;++k) worlddist+=(point[k]-pos[k])*(point[k]-pos[k]);
            if((d<best-0.01)||(d<100.0&&std::abs(d-best)<=0.01&&worlddist<bestWorld)) {
                best=d;bestWorld=worlddist;r->node_id=v->nodes[ni];
            }
        }
        return 1;
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
