#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
root=${1:-"$HOME/ppm-l2b-vtk"};mkdir -p "$root"
archive="$root/VTK-9.4.2.tar.gz"
if [[ ! -s "$archive" ]]; then
  curl -fL --retry 2 --connect-timeout 20 --max-time 300 https://vtk.org/files/release/9.4/VTK-9.4.2.tar.gz -o "$archive"
fi
echo "36c98e0da96bb12a30fe53708097aa9492e7b66d5c3b366e1c8dc251e2856a02  $archive" | sha256sum -c -
if [[ ! -d "$root/VTK-9.4.2" ]];then tar -xzf "$archive" -C "$root";fi
