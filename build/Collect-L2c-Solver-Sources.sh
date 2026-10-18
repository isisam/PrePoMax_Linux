#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Exact Jammy numerical-library sources corresponding to the static .a inputs.
set -euo pipefail
root=${VTKMAX_SYSROOT:-"$HOME/ppm-l2b-jammy"}
out=${1:-/root/ppm-l2c}
mkdir -p "$root/work/l2c-solver/numerical-sources" "$out/solver/notices"
sed -n 's/^deb /deb-src /p' "$root/etc/apt/sources.list" > "$root/etc/apt/sources.list.d/l2c-sources.list"
if [[ ! -f "$root/work/l2c-solver/numerical-sources/.complete" ]];then
    nice -n 19 chroot "$root" apt-get update -qq
    nice -n 19 chroot "$root" bash -se <<'SOURCES'
cd /work/l2c-solver/numerical-sources
dpkg-query -W -f='${source:Package}=${source:Version}\n' libspooles-dev libarpack2-dev liblapack-dev > versions.txt
while read -r spec;do apt-get source --download-only -y "$spec";done < versions.txt
SOURCES
    touch "$root/work/l2c-solver/numerical-sources/.complete"
fi
for package in libspooles-dev libarpack2-dev liblapack-dev;do cp "$root/usr/share/doc/$package/copyright" "$out/solver/notices/$package-copyright";done
cp "$root/usr/share/common-licenses/GPL-2" "$root/usr/share/common-licenses/GPL-3" "$out/solver/notices/"
cp "$root/work/l2c-solver/numerical-sources/versions.txt" "$out/solver/"
echo 'PASS exact static numerical-library sources and original notices collected'
