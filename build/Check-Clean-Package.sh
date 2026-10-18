#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Run only in a dedicated fresh PPM-L2b-* distro, as root.
set -euo pipefail
delivery=${1:?delivery directory}
export DEBIAN_FRONTEND=noninteractive
export LP_NUM_THREADS=2 DOTNET_PROCESSOR_COUNT=2 OMP_NUM_THREADS=2 # Three isolated checks share the eight-thread test budget.
cat /etc/os-release
apt-get update -qq
apt-get install --reinstall -y --no-install-recommends "$delivery/prepomax-l2b_0.2.0_amd64.deb" libgl1-mesa-dri
apt-get install -y --no-install-recommends xvfb xauth
mkdir -p /root/l2b-tar
tar -xzf "$delivery/prepomax-l2b-linux-x64.tar.gz" -C /root/l2b-tar
for app in /root/l2b-tar/app /opt/prepomax-l2b; do
  ldd "$app/libvtkmax.so.0.2.0"
  env -u DISPLAY "$app/run.sh" --probe /root/l2b-probe.png
  # Smoke the real Avalonia/Skia/X11 startup too. Xvfb/Xauth are test tools,
  # not runtime dependencies and no VTK or .NET development package is installed.
  . /etc/os-release
  display=113;[[ "$ID:$VERSION_ID" == ubuntu:22.04 ]] && display=111
  [[ "$ID:$VERSION_ID" == ubuntu:24.04 ]] && display=112
  evidence=$(mktemp -d /root/l2b-gui-XXXXXX)
  { echo "git rev: $(head -n1 "$app/SOURCE_REVISION.txt")";echo 'git status --short:';tail -n+2 "$app/SOURCE_REVISION.txt"; } > "$evidence/gui.log"
  setsid env PPM_L2B_EVIDENCE=1 LP_NUM_THREADS=2 DOTNET_PROCESSOR_COUNT=2 xvfb-run -n "$display" \
    -s '-screen 0 1600x1000x24' "$app/run.sh" <workspace>/prepomax/logs/L1a_linux_handoff/data/dcb_fatczm_static.inp --evidence "$evidence" >> "$evidence/gui.log" 2>&1 &
  pid=$!
  for attempt in $(seq 1 200);do [[ -f "$evidence/ready" ]] && break;sleep .1;done
  kill -- "-$pid" 2>/dev/null || true;wait "$pid" || true
  cat "$evidence/gui.log"
  [[ -f "$evidence/ready" && -s "$evidence/embedded-initial.png" ]] || { echo 'GUI startup failed';exit 1; }
  echo 'PASS real Avalonia GUI startup and model render on clean distro'
done
echo 'PASS fresh-distro tar and deb EGL probe'
