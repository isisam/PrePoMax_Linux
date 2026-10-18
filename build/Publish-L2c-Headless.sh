#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-/root/ppm-l2c}
export DOTNET_PROCESSOR_COUNT=8
nice -n 19 dotnet publish "$out/src/PrePoMax.Headless" -c Release -f net8.0 -p:CoreTargetFrameworks=net8.0 -m:1 -r linux-x64 --self-contained true -o "$repo/../logs/L2c/headless-linux"
