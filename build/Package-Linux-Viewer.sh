#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-"$HOME/ppm-l2b"}
delivery=${2:-"$repo/../logs/L2b_pkg"}
vtk=${VTKMAX_PRIVATE_ROOT:-"$HOME/ppm-l2b-vtk"}
bash "$repo/build/Build-Linux-Viewer.sh" "$out"
source_stage=$(mktemp -d "$out/source-XXXXXX")
deb_stage=$(mktemp -d "$out/deb-XXXXXX")
mkdir -p "$delivery" "$source_stage/prepomax" "$deb_stage/opt/prepomax-l2b" "$deb_stage/DEBIAN" "$deb_stage/usr/bin" "$deb_stage/usr/share/doc/prepomax-l2b"
# rsync without --delete: no computed recursive deletion, no shared tree mutation.
rsync -a --exclude=.git --exclude=obj --exclude=bin --exclude=x64 --exclude=.vs --exclude=packages "$repo/" "$source_stage/prepomax/"
cp "$vtk/VTK-9.4.2.tar.gz" "$source_stage/"
cp "$out/SOURCE_REVISION.txt" "$source_stage/"
cp "$out/app/SOURCE-ACCESS.md" "$out/app/SOURCE-REPOSITORIES.json" "$source_stage/"
cp "$out/app/SOURCE-ACCESS.md" "$out/app/SOURCE-REPOSITORIES.json" "$delivery/"
tar -czf "$delivery/prepomax-l2b-source.tar.gz" -C "$source_stage" .
app=$(readlink -f "$out/app")
tar -czf "$delivery/prepomax-l2b-linux-x64.tar.gz" -C "$(dirname "$app")" app
rsync -a "$out/app/" "$deb_stage/opt/prepomax-l2b/"
cat > "$deb_stage/DEBIAN/control" <<'EOF'
Package: prepomax-l2b
Version: 0.2.0
Architecture: amd64
Maintainer: Isisam <isisam@users.noreply.github.com>
Section: science
Priority: optional
Depends: libc6 (>= 2.35), libstdc++6, libgcc-s1, libx11-6, libxext6, libxrender1, libxi6, libice6, libsm6, libxcb1, libfontconfig1, libgl1, libegl1, libglx0, libxrandr2, libxcursor1, libxfixes3, libicu70 | libicu72 | libicu74, libssl3 | libssl3t64, zlib1g
Description: PrePoMax Linux viewer L2b development prototype
 Shared L1b core, private minimal VTK and Avalonia viewport.
 ABI 0.2 is unstable; native GPU acceptance pending.
EOF
cat > "$deb_stage/usr/bin/prepomax-l2b" <<'EOF'
#!/bin/sh
exec /opt/prepomax-l2b/run.sh "$@"
EOF
chmod 755 "$deb_stage/usr/bin/prepomax-l2b"
cp "$out/app/CORRESPONDING-SOURCE.txt" "$deb_stage/usr/share/doc/prepomax-l2b/"
dpkg-deb --root-owner-group --build "$deb_stage" "$delivery/prepomax-l2b_0.2.0_amd64.deb"
ldd "$out/app/libvtkmax.so.0.2.0" > "$out/ldd.txt"
if grep -Ei 'libvtk|libmpi|open-rte|open-pal|libtbb|libpython|libQt' "$out/ldd.txt"; then echo 'Forbidden private VTK dependency'; exit 1; fi
nm -D --defined-only "$out/app/libvtkmax.so.0.2.0" > "$out/exports.txt"
du -sh "$(readlink -f "$out/app")" "$out/app/libvtkmax.so.0.2.0"
cp "$out/ldd.txt" "$out/exports.txt" "$delivery/"
(cd "$delivery" && sha256sum *.tar.gz *.deb > SHA256SUMS)
python3 "$repo/build/Delivery-Receipt.py" "$app" "$delivery"
echo 'PASS scripted tar/deb/source packaging'
