#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-"$HOME/ppm-l2b"}
vtk=${VTKMAX_PRIVATE_ROOT:-"$HOME/ppm-l2b-vtk"}
[[ "$out" != /mnt/* ]] || { echo 'Build output must be on Linux ext4, not /mnt'; exit 2; }
mkdir -p "$out/src" "$out/app"
# All Linux restore/build work is on ext4, leaving Windows obj/bin untouched.
rsync -a --exclude=.git --exclude=obj --exclude=bin --exclude=x64 --exclude=.vs "$repo/" "$out/src/"
if [[ -f "$repo/.git" ]] && grep -q 'gitdir: [A-Za-z]:' "$repo/.git"; then
  git.exe -C "$(wslpath -w "$repo")" rev-parse HEAD > "$out/SOURCE_REVISION.txt"
  git.exe -C "$(wslpath -w "$repo")" status --short >> "$out/SOURCE_REVISION.txt"
else
  git -C "$repo" rev-parse HEAD > "$out/SOURCE_REVISION.txt"
  git -C "$repo" status --short >> "$out/SOURCE_REVISION.txt"
fi
cmake -S "$out/src/native/vtkmax" -B "$out/native" -G Ninja \
  -DCMAKE_BUILD_TYPE=Release -DVTK_DIR="$vtk/install/lib/cmake/vtk-9.4"
nice -n 19 cmake --build "$out/native" -j8
nice -n 19 dotnet publish "$out/src/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1 \
  -r linux-x64 --self-contained true -o "$out/app"
cp -a "$out/native/"libvtkmax.so* "$out/app/"
cp "$out/SOURCE_REVISION.txt" "$out/app/"
cp "$repo/build/viewer-run.sh" "$out/app/run.sh"
chmod +x "$out/app/run.sh"
python3 "$repo/build/Generate-Linux-Notices.py" "$out/app" "$out/src" "$vtk/VTK-9.4.2"
echo "Run: $out/app/run.sh /path/model.pmx"
