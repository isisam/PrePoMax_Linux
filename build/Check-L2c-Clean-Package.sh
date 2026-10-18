#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Dedicated DECISIONS 7 acceptance distros only; never a development/personal distro.
set -euo pipefail
delivery=${1:?delivery directory}
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
version=${PPM_PACKAGE_VERSION:-0.2.1}
export DEBIAN_FRONTEND=noninteractive DOTNET_PROCESSOR_COUNT=2 LP_NUM_THREADS=2 OMP_NUM_THREADS=1
cat /etc/os-release
if command -v dotnet;then echo 'Clean acceptance must not have a system .NET SDK/runtime';exit 1;fi
nice -n 19 apt-get update -qq
nice -n 19 apt-get install --reinstall -y --no-install-recommends "$delivery/prepomax-rcm_${version}_amd64.deb" libgl1-mesa-dri
nice -n 19 apt-get install -y --no-install-recommends xvfb xauth
mkdir -p /root/l2c-tar
tar -xzf "$delivery/prepomax-rcm-linux-x64.tar.gz" -C /root/l2c-tar
test -x /usr/bin/prepomax-rcm;test -s /usr/share/applications/prepomax-rcm.desktop
for app in /root/l2c-tar/app /opt/prepomax-rcm;do
    ldd "$app/libvtkmax.so.0.2.0";ldd "$app/Solver/ccx_fat"
    env -u DISPLAY nice -n 19 "$app/prepomax-rcm" --probe /root/l2c-probe.png
    evidence=$(mktemp -d /root/l2c-gui-XXXXXX)
    setsid env PPM_L2C_EVIDENCE=1 xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 "$app/prepomax-rcm" "$repo/../logs/L2b/dcb/dcb.pmx" --evidence "$evidence" > "$evidence/gui.log" 2>&1 &
    pid=$!
    for attempt in $(seq 1 300);do [[ -f "$evidence/ready" ]]&&break;sleep .1;done
    if [[ -f "$evidence/ready" ]];then echo '{"op":"close"}' > "$evidence/command.json";fi
    for attempt in $(seq 1 100);do kill -0 "$pid" 2>/dev/null||break;sleep .1;done
    forced=0
    if kill -0 "$pid" 2>/dev/null;then forced=1;kill -- "-$pid" 2>/dev/null||true;fi
    exit_code=0;wait "$pid"||exit_code=$?
    cat "$evidence/gui.log"
    echo "Shell exit=$exit_code forced=$forced"
    [[ "$exit_code" == 0 && "$forced" == 0 ]]||{ echo 'Shell did not close cleanly';exit 1; }
    [[ -f "$evidence/ready" && -s "$evidence/embedded-initial.png" ]]||{ echo 'Shell DCB open failed';exit 1; }
    if [[ -n "${PPM_PACKAGE_EVIDENCE:-}" ]];then
        kind=deb;[[ "$app" == /root/l2c-tar/* ]]&&kind=tar
        mkdir -p "$PPM_PACKAGE_EVIDENCE/$kind"
        cp -a "$evidence/". "$PPM_PACKAGE_EVIDENCE/$kind/"
        [[ -s "$app/PrePoMax.ViewModels.dll" && -s "$app/PrePoMax.PropertyMetadata.dll" ]]
        sha256sum "$app/PrePoMax.ViewModels.dll" "$app/PrePoMax.PropertyMetadata.dll"
    fi
    echo "PASS tar/deb shell start and DCB PMX/FRD open: $app"
done
echo 'PASS clean distro shell launcher, desktop entry, bundled solver ELF and EGL'
