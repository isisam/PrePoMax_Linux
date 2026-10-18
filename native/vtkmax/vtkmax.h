/* SPDX-License-Identifier: GPL-3.0-or-later
 * Copyright (c) 2026 PrePoMax-RCM contributors (RCM Lab, NCKU)
 * Copyright (c) Matej Borovinšek (ported vtkControl conventions/code) */
#ifndef VTKMAX_H
#define VTKMAX_H
#include <stdint.h>
#if defined(_WIN32)
# define VM_API __declspec(dllexport)
#else
# define VM_API __attribute__((visibility("default")))
#endif
#ifdef __cplusplus
extern "C" {
#endif
typedef struct vm_view vm_view;
typedef struct vm_pick_result {
    int64_t node_id, element_id;
    int32_t face_id, hit; /* zero-based VTK local face; shells = 0; miss IDs = -1 */
    double x, y, z;
    uint64_t sequence;
    int64_t actor_id; /* default actor=0; miss=-1 */
} vm_pick_result;
typedef void (*vm_selection_callback)(const vm_pick_result* result, void* user);
/* ABI 0.3, UNSTABLE: vm_abi_version()==3; .so.0. No compatibility promise until 1.x.
 * All calls on the creating thread. Arrays copied, UTF-8 strings borrowed
 * for the call. Display coordinates are physical pixels, origin bottom-left.
 * Status: 0 success, -1 error; vm_pick returns 1 hit, 0 miss, -1 error.
 * Backend 3 uses EGL without an X server. Backend 1 is X11 offscreen.
 */
VM_API int32_t vm_abi_version(void);
VM_API const char* vm_last_error(void); /* thread-local; valid until next call */
VM_API vm_view* vm_create(int32_t offscreen, int32_t width, int32_t height);
/* 0 native onscreen, 1 native offscreen, 2 externally-owned current GL context,
 * 3 EGL headless (DISPLAY not required).
 * Generic context creation/render/disposal must occur inside host GL callbacks. */
VM_API vm_view* vm_create_backend(int32_t backend, int32_t width, int32_t height);
VM_API int32_t vm_render_external(vm_view* v, int32_t framebuffer);
/* Pointer events in physical pixels, bottom-left. 0 move,1/2 left down/up,
 * 3/4 middle down/up,5/6 wheel forward/backward,7/8 right down/up. */
VM_API int32_t vm_input(vm_view* v, int32_t kind, int32_t x, int32_t y, int32_t ctrl, int32_t shift);
VM_API const char* vm_gl_info(vm_view* v);
VM_API void vm_destroy(vm_view* v);
/* Attach before first render. Returns the VTK-owned X11 child window.
 * The caller owns parent, vtkmax owns child/display/context. */
VM_API uintptr_t vm_attach_x11(vm_view* v, uintptr_t parent);
VM_API int32_t vm_resize(vm_view* v, int32_t width, int32_t height);
/* node_ids/element_ids unique nonnegative IDs; offsets length nc+1, start 0.
 * connectivity uses zero-based point indices; types are VTK cell constants.
 * Supports triangle/quad (linear/quadratic), tet/wedge/hex (linear/quadratic).
 * A rejected load leaves the previous grid intact. */
VM_API int32_t vm_load_grid(vm_view* v, int32_t np, const int64_t* node_ids,
    const double* xyz, int32_t nc, const int64_t* element_ids,
    const int32_t* types, const int32_t* offsets, int32_t nconn, const int64_t* connectivity);
/* Actor 0 is the backwards-compatible default. IDs must be positive and unique.
 * A failed add is atomic. Each actor may reuse node/element IDs; picks carry actor_id. */
VM_API int32_t vm_add_actor(vm_view* v, int64_t actor_id, int32_t np, const int64_t* node_ids,
    const double* xyz, int32_t nc, const int64_t* element_ids,
    const int32_t* types, const int32_t* offsets, int32_t nconn, const int64_t* connectivity);
VM_API int32_t vm_remove_actor(vm_view* v, int64_t actor_id);
VM_API int32_t vm_actor_style(vm_view* v, int64_t actor_id, int32_t visible,
    double red, double green, double blue, double opacity, int32_t edges);
VM_API int32_t vm_actor_scalars(vm_view* v, int64_t actor_id, int32_t association,
    const double* values, int32_t count, double minimum, double maximum, const char* title);
VM_API int32_t vm_contour_bands(vm_view* v, int32_t bands);
/* Absolute displacement from original coordinates, never cumulative. NULL/0 resets. */
VM_API int32_t vm_deformation(vm_view* v, int64_t actor_id, const double* displacement,
    int32_t count, double scale);
VM_API int32_t vm_selection_handler(vm_view* v, vm_selection_callback callback, void* user);
VM_API int32_t vm_perspective(vm_view* v, int32_t enabled);
/* Nonblocking camera animation: begin then step(elapsed/total) from host frame clock. */
VM_API int32_t vm_animate_standard_begin(vm_view* v, int32_t view);
VM_API int32_t vm_animate_standard_step(vm_view* v, double fraction);
VM_API int32_t vm_display(vm_view* v, int32_t surface, int32_t edges);
/* association 0=elements, 1=nodes. count must match. NULL/0 disables scalars. */
VM_API int32_t vm_scalars(vm_view* v, int32_t association, const double* values,
    int32_t count, double minimum, double maximum, const char* title);
/* association 0=elements, 1=nodes. Unknown IDs are rejected atomically. */
VM_API int32_t vm_highlight(vm_view* v, int32_t association, const int64_t* ids, int32_t count);
VM_API int32_t vm_actor_highlight(vm_view* v, int64_t actor_id, int32_t association, const int64_t* ids, int32_t count);
VM_API int32_t vm_fit(vm_view* v);
/* Windows convention: 0 iso(+X,+Y,+Z), 1 front(+Z/up+Y), 2 back(-Z/up+Y),
 * 3 top(+Y/up-Z), 4 bottom(-Y/up+Z), 5 right(+X/up+Y), 6 left(-X/up+Y). */
VM_API int32_t vm_standard_view(vm_view* v, int32_t view);
/* position, focal point, view-up; each output has 3 doubles. */
VM_API int32_t vm_camera(vm_view* v, double* position, double* focal, double* up);
VM_API int32_t vm_camera_scale(vm_view* v, double* scale);
VM_API int32_t vm_set_camera(vm_view* v, const double* position, const double* focal, const double* up, double scale);
VM_API int32_t vm_scalar_bar(vm_view* v, int32_t visible, const char* title);
VM_API int32_t vm_write_png_scaled(vm_view* v, const char* path, int32_t transparent, int32_t magnification);
VM_API int32_t vm_rotate(vm_view* v, double azimuth, double elevation);
VM_API int32_t vm_zoom(vm_view* v, double factor);
VM_API int32_t vm_render(vm_view* v);
VM_API int32_t vm_pick(vm_view* v, int32_t x, int32_t y, vm_pick_result* result);
VM_API int32_t vm_project(vm_view* v, const double* xyz, double* display_xyz);
VM_API int32_t vm_write_png(vm_view* v, const char* path);
/* Nonblocking X11 input pump; embedded host calls on UI thread. Trackball
 * left drag rotates, middle pans, wheel zooms, left click selects. */
VM_API int32_t vm_process_events(vm_view* v);
VM_API int32_t vm_x11_connection(vm_view* v); /* borrowed fd; never close it */
VM_API int32_t vm_last_pick(vm_view* v, vm_pick_result* result);
/* Selection, physical display pixels, bottom-left. Six planes in VTK order,
 * each [origin x,y,z, outward normal x,y,z]. Selected IDs are sorted, unique.
 * vm_selected_ids borrows storage until the next selection/geometry query.
 * association: 0 cells, 1 points, 2 actors, 3 closest edge nodes, 4 face nodes.
 * A NULL filter disables the filter; non-NULL + count 0 permits no actors.
 * Filter overrides visibility for regeneration, as in vtkControl. Actors marked
 * unselectable are always excluded (overlays/backing grids).
 */
VM_API int32_t vm_actor_selectable(vm_view* v, int64_t actor_id, int32_t selectable);
/* Selection actors render on a separate depth-cleared layer, sharing the base
 * camera (vtkControl selection renderer). They never contribute to picking. */
VM_API int32_t vm_actor_selection_layer(vm_view* v, int64_t actor_id, int32_t enabled);
/* Windows visualization faces, before vtkMaxActor's triangle decomposition.
 * Element IDs may repeat per face; volume connectivity remains unchanged. */
VM_API int32_t vm_actor_pick_geometry(vm_view* v, int64_t actor_id, int32_t np, const int64_t* node_ids,
    const double* xyz, int32_t nc, const int64_t* element_ids, const int32_t* types,
    const int32_t* offsets, int32_t nconn, const int64_t* connectivity);
VM_API int32_t vm_selectable_filter(vm_view* v, const int64_t* actors, int32_t count);
VM_API int32_t vm_area_pick(vm_view* v, int32_t x1, int32_t y1, int32_t x2, int32_t y2, double* planes36);
VM_API int32_t vm_frustum_select(vm_view* v, const double* planes36);
VM_API int32_t vm_selected_ids(vm_view* v, int32_t association, const int64_t** ids, int32_t* count);
VM_API int32_t vm_geometry_pick(vm_view* v, const double* point, int64_t* element_id, int32_t unlimited_distance);
/* Named-actor locator queries ignore visibility, as vtkControl regeneration does.
 * Missing IDs have distance -1 and zero normal; count outputs are caller-owned. */
VM_API int32_t vm_closest_cells(vm_view* v, const double* point, const int64_t* actors, int32_t count, double* distances, double* normals3);
VM_API int32_t vm_selection_precision(vm_view* v, double* distance);
VM_API int32_t vm_selection_distance(vm_view* v, int32_t pixels);
VM_API int32_t vm_selection_mode(vm_view* v, int32_t enabled);
VM_API int32_t vm_box_selection_active(vm_view* v);
/* Last native gesture: [box, release x,y, press x,y, ctrl,shift]. Published
 * through the selection callback; a box has hit=0 and a fresh sequence. */
VM_API int32_t vm_last_gesture(vm_view* v, int32_t* values7);
#ifdef __cplusplus
}
#endif
#endif
