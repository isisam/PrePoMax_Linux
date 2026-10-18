#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
root="$HOME/ppm-l2b-jammy"
rsync -a "$repo/native/vtkmax/" "$root/work/source/"
nice -n 19 chroot "$root" cmake -S /work/source -B /work/native-no-x -G Ninja -DCMAKE_BUILD_TYPE=Release -DVTKMAX_X11=OFF -DVTK_DIR=/work/vtk/install/lib/cmake/vtk-9.4
nice -n 19 chroot "$root" cmake --build /work/native-no-x -j8
env -u DISPLAY LP_NUM_THREADS=8 python3 "$repo/tests/L2b/egl_probe.py" "$root/work/native-no-x/libvtkmax.so"
