#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
export PPM_PACKAGE_VERSION=0.3.0 DOTNET_PROCESSOR_COUNT=6
bash "$repo/build/Package-Linux-Shell.sh" "${1:-/root/ppm-l3a}" "${2:-$repo/../logs/L3a/packages}"
