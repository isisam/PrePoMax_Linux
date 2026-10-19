#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=/root/ppm-l3b2-final
export PPM_PACKAGE_VERSION=0.4.0 DOTNET_PROCESSOR_COUNT=4 LP_NUM_THREADS=1 OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1
[[ $(df -PB1 /mnt/c | awk 'NR==2{print $4}') -ge 21474836480 ]]
mkdir -p "$out"
[[ -d "$out/solver" ]] || cp -a /root/ppm-l3b/solver "$out/solver"
bash "$repo/build/Build-Linux-Viewer.sh" "$out"
bash "$repo/build/Install-L2c-Shell.sh" "$out"
[[ -f "$out/app/libvtkmax.so.0.3.0" ]]
# Keep the pristine package candidate apart from command-history/test outputs.
test ! -e "$out/acceptance-app"
mkdir "$out/acceptance-app"
rsync -a "$out/app/" "$out/acceptance-app/"
(cd "$out/app" && find . -type f -print0 | sort -z | xargs -0 sha256sum) > "$out/CANDIDATE_SHA256SUMS"
# sha256sum paths are relative to the candidate, for the reuse packaging gate.
echo 'PASS pristine 0.4.0 candidate; native ABI 0.3.0; acceptance copy ready'
