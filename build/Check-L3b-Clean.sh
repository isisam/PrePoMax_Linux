#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
case "${PPM_L3B_DISTRO:-}" in PPM-L3b-U22|PPM-L3b-U24|PPM-L3b-D12);; *) echo 'Dedicated L3b test distro required';exit 2;;esac
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
export PPM_PACKAGE_VERSION=0.3.2 DOTNET_PROCESSOR_COUNT=2 LP_NUM_THREADS=1 OMP_NUM_THREADS=1
bash "$repo/build/Check-L2c-Clean-Package.sh" "$repo/../logs/L3b/packages-final"
# Test/debug dependencies live only in these new acceptance distros.
DEBIAN_FRONTEND=noninteractive nice -n 19 apt-get install -y --no-install-recommends python3-pil xdotool imagemagick gdb
for kind in tar deb;do
    app=/root/l2c-tar/app;[[ "$kind" == tar ]] || app=/opt/prepomax-rcm
    xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b/tree_ui.py" \
        "$app" "$repo/../logs/L2b/dcb/dcb.pmx" "$PPM_PACKAGE_EVIDENCE/$kind-tree"
done
# Retain native traces in the environment where the original intermittent crash was reported.
if [[ "${PPM_L3B_DIAGNOSTICS:-0}" == 1 && "$PPM_L3B_DISTRO" == PPM-L3b-U22 ]];then
    mkdir -p /root/l3a-reference
    tar -xzf "$repo/../logs/L3a/packages/prepomax-rcm-linux-x64.tar.gz" -C /root/l3a-reference
    xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b/shutdown_probe.py" \
        /root/l3a-reference/app "$repo/../logs/L2b/dcb/dcb.pmx" "$PPM_PACKAGE_EVIDENCE/l3a-gdb-reference" 12
fi
xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b/shutdown_probe.py" \
    /opt/prepomax-rcm "$repo/../logs/L2b/dcb/dcb.pmx" "$PPM_PACKAGE_EVIDENCE/shutdown-default" 4 --plain --require-clean
echo 'PASS fresh L3b runtime, tar/deb, real tree controls, native picking and clean shutdown'
