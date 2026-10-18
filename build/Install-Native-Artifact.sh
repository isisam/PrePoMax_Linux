#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
source_dir=${1:?native source directory};destination=${2:?destination directory}
mkdir -p "$destination"
# Rename avoids changing the inode of a library mapped by a running evidence app.
version=0.3.0
[[ -f "$source_dir/libvtkmax.so.$version" ]] || version=0.2.0
install -m 755 "$source_dir/libvtkmax.so.$version" "$destination/.libvtkmax.so.$version.stage"
mv -Tf "$destination/.libvtkmax.so.$version.stage" "$destination/libvtkmax.so.$version"
ln -sfn "libvtkmax.so.$version" "$destination/libvtkmax.so.0"
ln -sfn libvtkmax.so.0 "$destination/libvtkmax.so"
