#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-"$HOME/ppm-l2c"}
delivery=${2:-"$repo/../logs/L2c/packages"}
version=${PPM_PACKAGE_VERSION:-0.2.1}
vtk=${VTKMAX_PRIVATE_ROOT:-"$HOME/ppm-l2b-vtk"}
sysroot=${VTKMAX_SYSROOT:-"$HOME/ppm-l2b-jammy"}
release=${PPM_CCX_RELEASE_SOURCE:-"$repo/../ccx_fat_win64_v1.1_source.zip"}
[[ -f "$release" ]] || release=<workspace>/share_pkg/ccx_fat_win64_v1.1/source/ccx_fat_win64_v1.1_source.zip
if [[ ${PPM_PACKAGE_REUSE_BUILD:-0} == 1 ]];then
    [[ -s "$out/app/SOURCE_REVISION.txt" && -x "$out/app/Solver/ccx_fat" && -s "$out/app/license-inventory.json" ]]
    [[ $(wc -l < "$out/app/SOURCE_REVISION.txt") == 1 ]] || { echo 'Candidate was built from a dirty source tree';exit 1; }
    current=$(git.exe -C "$(wslpath -w "$repo")" rev-parse HEAD | tr -d '\r')
    [[ $(head -n1 "$out/app/SOURCE_REVISION.txt" | tr -d '\r') == "$current" ]] || { echo 'Candidate revision changed';exit 1; }
    (cd "$out/app" && sha256sum -c "$out/CANDIDATE_SHA256SUMS")
else
    bash "$repo/build/Build-Linux-Viewer.sh" "$out"
    bash "$repo/build/Install-L2c-Shell.sh" "$out"
fi
source_stage=$(mktemp -d "$out/source-XXXXXX")
deb_stage=$(mktemp -d "$out/deb-XXXXXX")
mkdir -p "$delivery" "$source_stage/prepomax" "$deb_stage/opt/prepomax-rcm" "$deb_stage/DEBIAN" "$deb_stage/usr/bin" "$deb_stage/usr/share/doc/prepomax-rcm" "$deb_stage/usr/share/applications"
# rsync without --delete: no computed recursive deletion, no shared tree mutation.
rsync -a --exclude=.git --exclude=obj --exclude=bin --exclude=x64 --exclude=.vs --exclude=packages "$repo/" "$source_stage/prepomax/"
cp "$vtk/VTK-9.4.2.tar.gz" "$source_stage/"
cp "$release" "$source_stage/"
cp -a "$sysroot/work/l2c-solver/numerical-sources" "$source_stage/solver-numerical-sources"
cp "$out/SOURCE_REVISION.txt" "$source_stage/"
cp "$out/app/SOURCE-ACCESS.md" "$out/app/SOURCE-REPOSITORIES.json" "$source_stage/"
cp "$out/app/SOURCE-ACCESS.md" "$out/app/SOURCE-REPOSITORIES.json" "$delivery/"
tar -czf "$delivery/prepomax-rcm-source.tar.gz" -C "$source_stage" .
app=$(readlink -f "$out/app")
tar -czf "$delivery/prepomax-rcm-linux-x64.tar.gz" -C "$(dirname "$app")" app
rsync -a "$out/app/" "$deb_stage/opt/prepomax-rcm/"
cat > "$deb_stage/DEBIAN/control" <<EOF
Package: prepomax-rcm
Version: $version
Architecture: amd64
Maintainer: PrePoMax-RCM contributors (RCM Lab, NCKU)
Section: science
Priority: optional
Depends: libc6 (>= 2.35), libstdc++6, libgcc-s1, libx11-6, libxext6, libxrender1, libxi6, libice6, libsm6, libxcb1, libfontconfig1, libgl1, libegl1, libglx0, libxrandr2, libxcursor1, libxfixes3, libicu70 | libicu72 | libicu74, libssl3 | libssl3t64, zlib1g, libgfortran5, fonts-noto-cjk
Description: PrePoMax Linux Avalonia shell
 Controller-backed PMX/INP/FRD shell and release-source Linux ccx_fat.
 Native ABI is unstable; native Linux/VM/GPU acceptance pending.
EOF
cat > "$deb_stage/usr/bin/prepomax-rcm" <<'EOF'
#!/bin/sh
exec /opt/prepomax-rcm/run.sh "$@"
EOF
chmod 755 "$deb_stage/usr/bin/prepomax-rcm"
cp "$out/app/prepomax-rcm.desktop" "$deb_stage/usr/share/applications/"
cp "$out/app/CORRESPONDING-SOURCE.txt" "$deb_stage/usr/share/doc/prepomax-rcm/"
nice -n 19 dpkg-deb -Zgzip --root-owner-group --build "$deb_stage" "$delivery/prepomax-rcm_${version}_amd64.deb"
native=$(readlink -f "$out/app/libvtkmax.so")
ldd "$native" > "$out/ldd.txt"
if grep -Ei 'libvtk|libmpi|open-rte|open-pal|libtbb|libpython|libQt' "$out/ldd.txt"; then echo 'Forbidden private VTK dependency'; exit 1; fi
nm -D --defined-only "$native" > "$out/exports.txt"
du -sh "$(readlink -f "$out/app")" "$native"
cp "$out/ldd.txt" "$out/exports.txt" "$delivery/"
(cd "$delivery" && sha256sum *.tar.gz *.deb > SHA256SUMS)
python3 "$repo/build/Delivery-Receipt.py" "$app" "$delivery"
echo 'PASS scripted tar/deb/source packaging'
