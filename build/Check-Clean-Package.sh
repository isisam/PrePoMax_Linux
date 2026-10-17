#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Run only in a dedicated fresh PPM-L2b-* distro, as root.
set -euo pipefail
delivery=${1:?delivery directory}
export DEBIAN_FRONTEND=noninteractive
cat /etc/os-release
apt-get update -qq
apt-get install -y --no-install-recommends "$delivery/prepomax-l2b_0.2.0_amd64.deb" libgl1-mesa-dri
mkdir -p /root/l2b-tar
tar -xzf "$delivery/prepomax-l2b-linux-x64.tar.gz" -C /root/l2b-tar
for app in /root/l2b-tar/app /opt/prepomax-l2b; do
  ldd "$app/libvtkmax.so.0.2.0"
  env -u DISPLAY "$app/run.sh" --probe /root/l2b-probe.png
done
echo 'PASS fresh-distro tar and deb EGL probe'
