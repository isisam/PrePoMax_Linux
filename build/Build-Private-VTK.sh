#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
root=${1:-"$HOME/ppm-l2b-vtk"}
mkdir -p "$root"
version=9.4.2
archive="$root/VTK-$version.tar.gz"
if [[ ! -s "$archive" ]]; then
  curl -fL --retry 2 --connect-timeout 20 --max-time 300 "https://www.vtk.org/files/release/9.4/VTK-$version.tar.gz" -o "$archive"
fi
sha256sum "$archive"
echo "36c98e0da96bb12a30fe53708097aa9492e7b66d5c3b366e1c8dc251e2856a02  $archive" | sha256sum -c -
if [[ ! -d "$root/VTK-$version" ]]; then tar -xzf "$archive" -C "$root"; fi
nice -n 19 cmake -S "$root/VTK-$version" -B "$root/build" -G Ninja \
  -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX="$root/install" \
  -DBUILD_SHARED_LIBS=OFF -DCMAKE_POSITION_INDEPENDENT_CODE=ON \
  -DCMAKE_CXX_VISIBILITY_PRESET=hidden -DCMAKE_C_VISIBILITY_PRESET=hidden \
  -DCMAKE_VISIBILITY_INLINES_HIDDEN=ON -DVTK_BUILD_TESTING=OFF \
  -DVTK_BUILD_EXAMPLES=OFF -DVTK_WRAP_PYTHON=OFF -DVTK_WRAP_JAVA=OFF \
  -DVTK_USE_MPI=OFF -DVTK_SMP_IMPLEMENTATION_TYPE=Sequential \
  -DVTK_GROUP_ENABLE_StandAlone=DONT_WANT -DVTK_GROUP_ENABLE_Rendering=DONT_WANT \
  -DVTK_GROUP_ENABLE_Imaging=DONT_WANT -DVTK_GROUP_ENABLE_Views=DONT_WANT \
  -DVTK_GROUP_ENABLE_Qt=NO -DVTK_GROUP_ENABLE_Web=NO \
  -DVTK_USE_X=ON -DVTK_OPENGL_HAS_EGL=ON -DVTK_USE_WAYLAND_OPENGL=OFF \
  -DVTK_MODULE_ENABLE_VTK_CommonCore=YES -DVTK_MODULE_ENABLE_VTK_CommonDataModel=YES \
  -DVTK_MODULE_ENABLE_VTK_FiltersExtraction=YES -DVTK_MODULE_ENABLE_VTK_RenderingCore=YES \
  -DVTK_MODULE_ENABLE_VTK_RenderingOpenGL2=YES -DVTK_MODULE_ENABLE_VTK_RenderingUI=YES \
  -DVTK_MODULE_ENABLE_VTK_RenderingFreeType=YES -DVTK_MODULE_ENABLE_VTK_RenderingAnnotation=YES \
  -DVTK_MODULE_ENABLE_VTK_InteractionStyle=YES -DVTK_MODULE_ENABLE_VTK_IOImage=YES
nice -n 19 cmake --build "$root/build" -j8
nice -n 19 cmake --install "$root/build"
