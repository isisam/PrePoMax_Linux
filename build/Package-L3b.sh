#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
export PPM_PACKAGE_VERSION=0.3.2 DOTNET_PROCESSOR_COUNT=4
# The solver source/ELF did not change; reuse the audited release-source build,
# while rebuilding all managed application assemblies from this checkout.
if [[ ! -d /root/ppm-l3b/solver ]];then cp -a /root/ppm-l3a/solver /root/ppm-l3b/solver;fi
bash "$repo/build/Package-Linux-Shell.sh" /root/ppm-l3b "$repo/../logs/L3b/packages-final"
