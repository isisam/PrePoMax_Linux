#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Fast iteration after a full Package-Linux-Shell restore; all obj/bin stay on ext4.
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-/root/ppm-l2c}
export DOTNET_PROCESSOR_COUNT=8
rsync -a --exclude=obj --exclude=bin "$repo/PrePoMax.Avalonia/" "$out/src/PrePoMax.Avalonia/"
nice -n 19 dotnet publish "$out/src/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1 -r linux-x64 --self-contained true --no-restore -o "$out/app"
git.exe -C "$(wslpath -w "$repo")" rev-parse HEAD > "$out/app/SOURCE_REVISION.txt"
git.exe -C "$(wslpath -w "$repo")" status --short >> "$out/app/SOURCE_REVISION.txt"
