#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=/root/ppm-l3b2
logs=${PPM_SELECT_LOGS:-"$repo/../logs/L3b2/select"}
export DOTNET_PROCESSOR_COUNT=4 LP_NUM_THREADS=1 OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1
[[ $(df -PB1 /mnt/c | awk 'NR==2{print $4}') -ge 21474836480 ]]
mkdir -p "$out/src" "$logs"
rsync -a --exclude=.git --exclude=obj --exclude=bin --exclude=x64 --exclude=.vs --exclude=packages "$repo/" "$out/src/"
if [[ ! -f "$out/native-select/build.ninja" ]]; then
  nice -n 19 cmake -S "$repo/native/vtkmax" -B "$out/native-select" -G Ninja \
    -DCMAKE_BUILD_TYPE=Release -DVTK_DIR=/root/ppm-l2b-vtk/install/lib/cmake/vtk-9.4
fi
nice -n 19 cmake --build "$out/native-select" -j8
nice -n 19 dotnet publish "$out/src/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1 -r linux-x64 --self-contained true -o "$out/app"
bash "$repo/build/Install-Native-Artifact.sh" "$out/native-select" "$out/app"
sha256sum "$out/native-select/libvtkmax.so.0.3.0" "$out/app/libvtkmax.so.0.3.0" > "$logs/native-sha256.txt"
xvfb-run -a nice -n 19 python3 "$repo/tests/L2a/c_api_checks.py" "$out/app/libvtkmax.so" > "$logs/c-api.log" 2>&1
nice -n 19 cc -I"$repo/native/vtkmax" "$repo/tests/L2a/c_header.c" -L"$out/app" -lvtkmax -Wl,-rpath,"$out/app" -o "$out/c-header-select"
nice -n 19 "$out/c-header-select"
