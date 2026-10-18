#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-/root/ppm-l3a}
export DOTNET_PROCESSOR_COUNT=8
rsync -a --exclude=obj --exclude=bin "$repo/tests/L3a/" "$out/src/tests/L3a/"
nice -n 19 dotnet build "$out/src/tests/L3a/MetadataProbe.csproj" -c Release -f net8.0 -m:1
nice -n 19 dotnet "$out/src/tests/L3a/bin/Release/net8.0/MetadataProbe.dll" "$out/app" \
    "$out/src/PrePoMax.ViewModels/moved-types.json" "$repo/../logs/L3a/metadata-linux.json"
nice -n 19 dotnet publish "$out/src/PrePoMax.Headless" -c Release -f net8.0 -p:CoreTargetFrameworks=net8.0 -m:1 \
    -r linux-x64 --self-contained true -o "$repo/../logs/L3a/headless-linux"
