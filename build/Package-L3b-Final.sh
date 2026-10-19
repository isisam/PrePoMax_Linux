#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
export PPM_PACKAGE_VERSION=0.4.0 PPM_PACKAGE_REUSE_BUILD=1 DOTNET_PROCESSOR_COUNT=4
bash "$repo/build/Package-Linux-Shell.sh" /root/ppm-l3b2-final "$repo/../logs/L3b2/final/packages"
