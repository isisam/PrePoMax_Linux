#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
mode=();[[ "${4:-}" != plain ]] || mode=(--plain)
xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b/shutdown_probe.py" \
    "${1:-/root/ppm-l3a/app}" "$repo/../logs/L2b/dcb/dcb.pmx" "$repo/../logs/L3b/shutdown/${2:-l3a-reference}" "${3:-12}" "${mode[@]}"
