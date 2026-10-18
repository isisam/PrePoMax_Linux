#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
case "${PPM_L3B_DISTRO:-}" in PPM-L3b-U22|PPM-L3b-U24|PPM-L3b-D12);; *) exit 2;;esac
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
for kind in tar deb;do
    app=/root/l2c-tar/app;[[ "$kind" == tar ]] || app=/opt/prepomax-rcm
    xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b/tree_ui.py" \
        "$app" "$repo/../logs/L2b/dcb/dcb.pmx" "$PPM_PACKAGE_EVIDENCE/$kind-tree-verified"
done
