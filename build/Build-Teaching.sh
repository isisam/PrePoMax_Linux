#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Public teaching build: fresh ext4 output; no research solver or private SDK.
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
root=${1:?fresh ext4 build root}
phase=${2:-all}
: "${SOURCE_REVISION:?export the clean source commit}"
: "${SOURCE_DATE_EPOCH:?export the source commit timestamp}"
[[ "$root" = /* && "$root" != /mnt/* && "$root" != / ]] || exit 2
export DOTNET_PROCESSOR_COUNT=4 DOTNET_CLI_TELEMETRY_OPTOUT=1
export LP_NUM_THREADS=1 OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1
export PPM_PACKAGE_VERSION=0.4.0 LC_ALL=C TZ=UTC
mkdir -p "$root"
maps="-ffile-prefix-map=$root=/_/build -fdebug-prefix-map=$root=/_/build -fmacro-prefix-map=$root=/_/build -ffile-prefix-map=$repo=/_/src -fdebug-prefix-map=$repo=/_/src -fmacro-prefix-map=$repo=/_/src"
export CFLAGS="$maps" CXXFLAGS="$maps"
if [[ "$phase" = managed || "$phase" = all ]]; then
  test ! -e "$root/app"
  dotnet publish "$repo/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1 \
    -r linux-x64 --self-contained true -o "$root/app" \
    -p:DebugType=None -p:DebugSymbols=false -p:Deterministic=true \
    -p:ContinuousIntegrationBuild=true "-p:PathMap=$repo=/_/src" \
    -p:RestorePackagesWithLockFile=true -p:RestoreLockedMode=true
  printf '%s\n' "$SOURCE_REVISION" > "$root/app/SOURCE_REVISION.txt"
  cp "$repo/build/viewer-run.sh" "$root/app/run.sh"
  chmod 755 "$root/app/run.sh"
fi
if [[ "$phase" = native || "$phase" = all ]]; then
  bash "$repo/build/Build-Private-VTK.sh" "$root/vtk"
  cmake -S "$repo/native/vtkmax" -B "$root/native" -G Ninja \
    -DCMAKE_BUILD_TYPE=Release -DVTK_DIR="$root/vtk/install/lib/cmake/vtk-9.4" \
    -DCMAKE_BUILD_WITH_INSTALL_RPATH=ON '-DCMAKE_INSTALL_RPATH=$ORIGIN/lib'
  cmake --build "$root/native" -j8
fi
if [[ "$phase" = package || "$phase" = all ]]; then
  delivery=${3:?new delivery directory}
  test ! -e "$delivery"
  mkdir -p "$delivery"
  bash "$repo/build/Install-Native-Artifact.sh" "$root/native" "$root/app"
  python3 "$repo/build/Generate-Linux-Notices.py" "$root/app" "$repo" "$root/vtk/VTK-9.4.2"
  python3 "$repo/build/Source-Access.py" "$root/app"
  python3 - "$root/app" <<'PY'
from pathlib import Path
import sys
p=Path(sys.argv[1])
for f in ['SOURCE-ACCESS.md','CORRESPONDING-SOURCE.txt']:
    q=p/f
    q.write_text(q.read_text().replace('prepomax-l2b-source.tar.gz','PrePoMax-Linux-0.4.0-public-source.tar.gz'))
(p/'TEACHING.txt').write_text('PrePoMax Linux L3 0.4.0 teaching build. No solver is bundled. Configure a separately installed stock CalculiX executable. Gmsh meshing is an explicit unsupported stub. Native Linux/VM/GPU acceptance remains pending.\n')
PY
  cat > "$root/app/prepomax-rcm.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Name=PrePoMax Linux Teaching
Comment=Finite element model and result viewer
Exec=prepomax-rcm %f
Terminal=false
Categories=Science;Engineering;
EOF
  source_stage="$root/corresponding-source"
  deb="$root/deb-stage"
  test ! -e "$source_stage" && test ! -e "$deb"
  mkdir -p "$source_stage/prepomax" "$deb/opt/prepomax-rcm" "$deb/DEBIAN" "$deb/usr/bin" "$deb/usr/share/applications" "$deb/usr/share/metainfo"
  rsync -a --exclude=.git --exclude=obj --exclude=bin --exclude=x64 --exclude=packages "$repo/" "$source_stage/prepomax/"
  cp "$root/vtk/VTK-9.4.2.tar.gz" "$source_stage/"
  if [[ ! -s "$root/antlr-3.1.3.tar.gz" ]]; then
    curl -fL --retry 2 https://www.antlr3.org/download/antlr-3.1.3.tar.gz -o "$root/antlr-3.1.3.tar.gz"
  fi
  echo "1fca0fc7304140807e64ec263a58d93ef1b380cfb6407084e6909f8220bc1039  $root/antlr-3.1.3.tar.gz" | sha256sum -c -
  cp "$root/antlr-3.1.3.tar.gz" "$source_stage/"
  cp "$root/app/SOURCE_REVISION.txt" "$root/app/SOURCE-ACCESS.md" "$root/app/SOURCE-REPOSITORIES.json" "$source_stage/"
  rsync -a "$root/app/" "$deb/opt/prepomax-rcm/"
  cat > "$deb/DEBIAN/control" <<'EOF'
Package: prepomax-rcm
Version: 0.4.0+teaching.5
Architecture: amd64
Maintainer: S.Y. Su <isisam@users.noreply.github.com>
Section: science
Priority: optional
Depends: libc6 (>= 2.35), libstdc++6, libgcc-s1, libx11-6, libxext6, libxrender1, libxi6, libice6, libsm6, libxcb1, libfontconfig1, libgl1, libegl1, libglx0, libxrandr2, libxcursor1, libxfixes3, libicu70 | libicu72 | libicu74, libssl3 | libssl3t64, zlib1g
Recommends: fonts-noto-cjk, calculix-ccx
Description: Unofficial PrePoMax Linux 0.4.0 teaching checkpoint
 Existing mesh model and result viewer. No solver bundled; Gmsh is unsupported.
 Native Linux/VM/GPU acceptance remains pending.
EOF
  printf '#!/bin/sh\nexec /opt/prepomax-rcm/run.sh "$@"\n' > "$deb/usr/bin/prepomax-rcm"
  chmod 755 "$deb/usr/bin/prepomax-rcm"
  cp "$root/app/prepomax-rcm.desktop" "$deb/usr/share/applications/"
  cp "$repo/build/org.isisam.PrePoMax.metainfo.xml" "$deb/usr/share/metainfo/"
  cp "$repo/build/org.isisam.PrePoMax.metainfo.xml" "$root/app/"
  cp "$repo/build/org.isisam.PrePoMax.metainfo.xml" "$deb/opt/prepomax-rcm/"
  find "$deb" -exec touch -h -d "@$SOURCE_DATE_EPOCH" {} +
  tar --sort=name --mtime="@$SOURCE_DATE_EPOCH" --owner=0 --group=0 --numeric-owner -cf - -C "$root" app | gzip -n -9 > "$delivery/PrePoMax-Linux-0.4.0-teaching.tar.gz"
  tar --sort=name --mtime="@$SOURCE_DATE_EPOCH" --owner=0 --group=0 --numeric-owner -cf - -C "$source_stage" . | gzip -n -9 > "$delivery/PrePoMax-Linux-0.4.0-public-source.tar.gz"
  dpkg-deb -Zgzip -z9 --root-owner-group --build "$deb" "$delivery/prepomax-rcm_0.4.0+teaching.5_amd64.deb"
  (cd "$delivery" && sha256sum *.tar.gz *.deb > SHA256SUMS)
fi
