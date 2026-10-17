/* SPDX-License-Identifier: BSD-3-Clause */
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
} vm_pick_result;
/* ABI 1. All calls on the creating thread. Arrays copied, UTF-8 strings borrowed
 * for the call. Display coordinates are physical pixels, origin bottom-left.
 * Status: 0 success, -1 error; vm_pick returns 1 hit, 0 miss, -1 error.
 * Offscreen on VTK 9.1 still requires an X server (use Xvfb).
 */
VM_API int32_t vm_abi_version(void);
VM_API const char* vm_last_error(void); /* thread-local; valid until next call */
VM_API vm_view* vm_create(int32_t offscreen, int32_t width, int32_t height);
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
VM_API int32_t vm_display(vm_view* v, int32_t surface, int32_t edges);
/* association 0=elements, 1=nodes. count must match. NULL/0 disables scalars. */
VM_API int32_t vm_scalars(vm_view* v, int32_t association, const double* values,
    int32_t count, double minimum, double maximum, const char* title);
/* association 0=elements, 1=nodes. Unknown IDs are rejected atomically. */
VM_API int32_t vm_highlight(vm_view* v, int32_t association, const int64_t* ids, int32_t count);
VM_API int32_t vm_fit(vm_view* v);
/* 0 iso, 1 front(-Y), 2 back(+Y), 3 top(+Z), 4 bottom(-Z), 5 right(+X), 6 left(-X) */
VM_API int32_t vm_standard_view(vm_view* v, int32_t view);
VM_API int32_t vm_rotate(vm_view* v, double azimuth, double elevation);
VM_API int32_t vm_zoom(vm_view* v, double factor);
VM_API int32_t vm_render(vm_view* v);
VM_API int32_t vm_pick(vm_view* v, int32_t x, int32_t y, vm_pick_result* result);
VM_API int32_t vm_project(vm_view* v, const double* xyz, double* display_xyz);
VM_API int32_t vm_write_png(vm_view* v, const char* path);
/* Nonblocking X11 input pump; embedded host calls on UI thread. Trackball
 * left drag rotates, middle pans, wheel zooms, left click selects. */
VM_API int32_t vm_process_events(vm_view* v);
VM_API int32_t vm_last_pick(vm_view* v, vm_pick_result* result);
#ifdef __cplusplus
}
#endif
#endif
