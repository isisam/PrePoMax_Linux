#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
app=${1:?app}
out=${2:?output}
mkdir -p "$out"
export DOTNET_PROCESSOR_COUNT=2 LP_NUM_THREADS=2 OMP_NUM_THREADS=1 VTK_SMP_MAX_THREADS=2 PPM_L2C_EVIDENCE=1
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
for n in $(seq 1 "${3:-3}");do
    case_dir="$out/$n";mkdir -p "$case_dir"
    setsid xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 "$app/PrePoMax.Avalonia" "$repo/../logs/L2b/dcb/dcb.pmx" --evidence "$case_dir" > "$case_dir/gui.log" 2>&1 &
    pid=$!
    for i in $(seq 1 400);do [[ -f "$case_dir/ready" ]]&&break;sleep .1;done
    echo '{"op":"close"}' > "$case_dir/command.tmp";mv "$case_dir/command.tmp" "$case_dir/command.json"
    forced=0
    for i in $(seq 1 150);do kill -0 "$pid" 2>/dev/null||break;sleep .1;done
    if kill -0 "$pid" 2>/dev/null;then forced=1;kill -- "-$pid" 2>/dev/null||true;fi
    code=0;wait "$pid"||code=$?
    echo "$n exit=$code forced=$forced" | tee "$case_dir/exit.txt"
done
