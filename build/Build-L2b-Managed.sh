#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-"$HOME/ppm-l2b"}
mkdir -p "$out/src"
rsync -a --exclude=.git --exclude=obj --exclude=bin --exclude=x64 --exclude=.vs --exclude=packages "$repo/" "$out/src/"
nice -n 19 dotnet build "$out/src/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1
