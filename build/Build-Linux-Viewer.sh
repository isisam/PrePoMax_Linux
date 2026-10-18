#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-"$HOME/ppm-l2b"}
vtk=${VTKMAX_PRIVATE_ROOT:-"$HOME/ppm-l2b-vtk"}
[[ "$out" != /mnt/* ]] || { echo 'Build output must be on Linux ext4, not /mnt'; exit 2; }
mkdir -p "$out/src"
stage=$(mktemp -d "$out/publish-XXXXXX")
app="$stage/app"
# All Linux restore/build work is on ext4, leaving Windows obj/bin untouched.
rsync -a --exclude=.git --exclude=obj --exclude=bin --exclude=x64 --exclude=.vs --exclude=packages "$repo/" "$out/src/"
if [[ -f "$repo/.git" ]] && grep -q 'gitdir: [A-Za-z]:' "$repo/.git"; then
  git.exe -C "$(wslpath -w "$repo")" rev-parse HEAD > "$out/SOURCE_REVISION.txt"
  git.exe -C "$(wslpath -w "$repo")" status --short >> "$out/SOURCE_REVISION.txt"
else
  git -C "$repo" rev-parse HEAD > "$out/SOURCE_REVISION.txt"
  git -C "$repo" status --short >> "$out/SOURCE_REVISION.txt"
fi
bash "$repo/build/Build-Compat-Native.sh" "$out"
nice -n 19 dotnet publish "$out/src/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1 \
  -r linux-x64 --self-contained true -o "$app"
bash "$repo/build/Install-Native-Artifact.sh" "$out/native" "$app"
cp "$out/SOURCE_REVISION.txt" "$app/"
cp "$repo/build/viewer-run.sh" "$app/run.sh"
chmod +x "$app/run.sh"
python3 "$repo/build/Generate-Linux-Notices.py" "$app" "$out/src" "$vtk/VTK-9.4.2"
python3 "$repo/build/Source-Access.py" "$app"
if [[ -d "$out/app" && ! -L "$out/app" ]]; then mv "$out/app" "$stage/previous-app";fi
ln -s "$app" "$out/.app-link"
mv -Tf "$out/.app-link" "$out/app"
echo "Run: $out/app/run.sh /path/model.pmx"
