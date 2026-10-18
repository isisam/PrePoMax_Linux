#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=/root/ppm-l3b2
logs="$repo/../logs/L3b2/forms"
export DOTNET_PROCESSOR_COUNT=4 LP_NUM_THREADS=1 OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1
[[ $(df -PB1 /mnt/c | awk 'NR==2{print $4}') -ge 21474836480 ]]
mkdir -p "$out/src" "$logs"
rsync -a --exclude=.git --exclude=obj --exclude=bin --exclude=x64 --exclude=.vs --exclude=packages "$repo/" "$out/src/"
nice -n 19 dotnet publish "$out/src/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1 -r linux-x64 --self-contained true -o "$out/app"
# Selection continuation uses ABI 0.3. Do not silently load the historical 0.2
# artifact with the newer managed wrapper. Build-L3b2-Select.sh prepares this.
native=${PPM_FORMS_NATIVE:-"$out/native-select"}
[[ -f "$native/libvtkmax.so.0.3.0" ]]
bash "$repo/build/Install-Native-Artifact.sh" "$native" "$out/app"
sha256sum "$native/libvtkmax.so.0.3.0" "$out/app/libvtkmax.so.0.3.0" > "$logs/native-sha256.txt"
if [[ ${PPM_FORMS_SKIP_METADATA:-0} != 1 ]]; then
    nice -n 19 dotnet build "$out/src/tests/L3b2/FormContracts.csproj" -f net10.0 -c Release -p:CoreTargetFrameworks=net8.0 -m:1
    xvfb-run -a nice -n 19 dotnet "$out/src/tests/L3b2/bin/Release/net10.0/FormContracts.dll" "$out/app" "$logs/contracts-linux.json"
fi
for pair in DCB M2a; do
    fixture="$repo/../logs/L2b/dcb/dcb.pmx"
    [[ "$pair" != M2a ]] || fixture="$repo/../logs/M2b_acceptance/M2b_layup.pmx"
    nice -n 19 "$out/app/PrePoMax.Avalonia" --tree-check "$fixture" "$logs/tree-$pair.json"
    extra=()
    # The original M2a layup covers all elements. This documented operation case
    # keeps its definition but deactivates it before adding an ordinary section.
    if [[ "$pair" == M2a ]]; then extra+=(--deactivate-layups); fi
    if [[ "$pair" == DCB && -n ${PPM_FORMS_DCB_CHECKPOINT:-} ]]; then
        fixture="$PPM_FORMS_DCB_CHECKPOINT"
        if [[ ${PPM_FORMS_DCB_REPAIR:-0} == 1 ]]; then extra+=(--repair-node-face); else extra+=(--resume-after-section); fi
    fi
    if [[ "$pair" == M2a && -n ${PPM_FORMS_M2A_CHECKPOINT:-} ]]; then
        fixture="$PPM_FORMS_M2A_CHECKPOINT"; extra+=(--repair-node-face --deactivate-layups)
    fi
    xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b2/forms_ui.py" "$out/app" "$fixture" "$logs/ui-$pair" "${extra[@]}"
done
xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b/tree_ui.py" "$out/app" "$repo/../logs/L2b/dcb/dcb.pmx" "$logs/tree-ui"
