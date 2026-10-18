#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-/root/ppm-l2c}
export DOTNET_PROCESSOR_COUNT=8 LP_NUM_THREADS=4 OMP_NUM_THREADS=4
for name in PrePoMax.Avalonia Vtkmax.Interop; do rsync -a --exclude=bin --exclude=obj "$repo/$name/" "$out/src/$name/";done
nice -n 19 dotnet build "$out/src/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1 --no-restore
LD_LIBRARY_PATH="$out/app" xvfb-run -a nice -n 19 dotnet "$out/src/PrePoMax.Avalonia/bin/Release/net10.0/PrePoMax.Avalonia.dll" --contract-check "$repo/../logs/L2c/contract"
