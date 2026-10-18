#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
sudo -n true
sudo -n env DEBIAN_FRONTEND=noninteractive nice -n 19 apt-get install -y --no-install-recommends weston xwayland
