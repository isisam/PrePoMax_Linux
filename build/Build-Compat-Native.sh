#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Jammy sysroot inside the authorised Ubuntu-24.04 WSL distro. No distro switch,
# no host toolchain replacement, and no modification of clean-check distros.
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
root=${VTKMAX_SYSROOT:-"$HOME/ppm-l2b-jammy"}
vtk=${VTKMAX_PRIVATE_ROOT:-"$HOME/ppm-l2b-vtk"}
out=${1:-"$HOME/ppm-l2b"}
bash "$repo/build/Fetch-VTK.sh" "$vtk"
[[ "$root" == "$HOME/ppm-l2b-jammy" && "$root" != /mnt/* ]] || { echo 'Unexpected sysroot path'; exit 2; }
if [[ ! -f "$root/.rootfs-ready" ]]; then
  mkdir -p "$root"
  archive=${VTKMAX_ROOTFS:-<workspace>/wsl/PPM-L2b-U22.rootfs.tar.gz}
  if [[ ! -f "$archive" ]]; then
    mkdir -p "$HOME/.cache/ppm-rootfs";archive="$HOME/.cache/ppm-rootfs/ubuntu-jammy.tar.gz"
    if [[ ! -f "$archive" ]];then curl -fL --retry 2 --max-time 600 https://cloud-images.ubuntu.com/wsl/releases/jammy/current/ubuntu-jammy-wsl-amd64-wsl.rootfs.tar.gz -o "$archive";fi
  fi
  echo "de9f6149da07b90350a3ccd94b4858b82fef71f0ec2982acb93de583c4c87585  $archive" | sha256sum -c -
  tar -xzf "$archive" -C "$root"
  cp --remove-destination -L /etc/resolv.conf "$root/etc/resolv.conf"
  [[ -c "$root/dev/null" ]] || mknod -m 666 "$root/dev/null" c 1 3
  touch "$root/.rootfs-ready"
fi
if [[ ! -f "$root/.build-deps-ready" ]]; then
  sudo -n env DEBIAN_FRONTEND=noninteractive nice -n 19 chroot "$root" apt-get update -qq
  sudo -n env DEBIAN_FRONTEND=noninteractive nice -n 19 chroot "$root" apt-get install -y --no-install-recommends \
    build-essential cmake ninja-build curl ca-certificates libgl1-mesa-dev libegl1-mesa-dev libx11-dev libxt-dev libxext-dev libxrender-dev
  touch "$root/.build-deps-ready"
fi
mkdir -p "$root/work/vtk" "$root/work/scripts" "$root/work/source"
cp "$vtk/VTK-9.4.2.tar.gz" "$root/work/vtk/"
cp "$repo/build/Build-Private-VTK.sh" "$root/work/scripts/"
if [[ ! -f "$root/work/vtk/install/lib/cmake/vtk-9.4/vtk-config.cmake" ]]; then
  nice -n 19 chroot "$root" bash /work/scripts/Build-Private-VTK.sh /work/vtk
fi
rsync -a "$repo/native/vtkmax/" "$root/work/source/"
nice -n 19 chroot "$root" cmake -S /work/source -B /work/native -G Ninja \
  -DCMAKE_BUILD_TYPE=Release -DVTK_DIR=/work/vtk/install/lib/cmake/vtk-9.4
nice -n 19 chroot "$root" cmake --build /work/native -j8
mkdir -p "$out/native" "$out/app"
bash "$repo/build/Install-Native-Artifact.sh" "$root/work/native" "$out/native"
bash "$repo/build/Install-Native-Artifact.sh" "$root/work/native" "$out/app"
sha256sum "$repo/native/vtkmax/"* > "$out/native/COMPAT_SOURCE.sha256"
cp "$root/work/vtk/build/CMakeCache.txt" "$out/native/COMPAT_VTK_CMakeCache.txt"
echo 'PASS Ubuntu 22.04 baseline native build within Ubuntu-24.04 WSL'
