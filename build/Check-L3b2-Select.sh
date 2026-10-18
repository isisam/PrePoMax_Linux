#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
logs=${PPM_SELECT_LOGS:-"$repo/../logs/L3b2/select"}
app=/root/ppm-l3b2/app
export DOTNET_PROCESSOR_COUNT=4 LP_NUM_THREADS=1 OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1
phase=${1:?ui-DCB / ui-M2a / forms / tree / semantic}
case "$phase" in
  ui-*)
    fixture="$repo/../logs/L2b/dcb/dcb.pmx"
    model=${phase#ui-};model=${model%%-*}
    [[ $model != M2a ]] || fixture="$repo/../logs/M2b_acceptance/M2b_layup.pmx"
    xvfb-run -a -s '-screen 0 2400x1200x24' nice -n 19 python3 "$repo/tests/L3b2/selection_ui.py" "$app" "$fixture" "$logs/$phase" "$logs/geometry-parity/windows-selection-$model.json"
    ;;
  semantic)
    xvfb-run -a nice -n 19 python3 "$repo/tests/L3b2/selection_native.py" "$app/libvtkmax.so" "$logs/geometry-parity"
    ;;
  tree)
    for model in DCB M2a; do
      fixture="$repo/../logs/L2b/dcb/dcb.pmx"
      [[ $model != M2a ]] || fixture="$repo/../logs/M2b_acceptance/M2b_layup.pmx"
      nice -n 19 "$app/PrePoMax.Avalonia" --tree-check "$fixture" "$logs/tree-$model.json"
    done
    xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b/tree_ui.py" "$app" "$repo/../logs/L2b/dcb/dcb.pmx" "$logs/tree-ui"
    ;;
  forms)
    root=/root/ppm-l3b2/src/tests/L3b2
    nice -n 19 dotnet build "$root/FormContracts.csproj" -f net10.0 -c Release -p:CoreTargetFrameworks=net8.0 -m:1
    nice -n 19 dotnet build "$root/GridInteractions.csproj" -c Release -p:CoreTargetFrameworks=net8.0 -m:1
    xvfb-run -a nice -n 19 dotnet "$root/bin/Release/net10.0/FormContracts.dll" "$app" "$logs/contracts-linux.json"
    xvfb-run -a nice -n 19 dotnet "$root/bin/Release/net10.0/FormContracts.dll" "$app" "$logs/apply-linux.json" --apply "$repo/../logs/M2b_acceptance/M2b_layup.pmx"
    xvfb-run -a nice -n 19 dotnet "$root/bin/Release/net10.0/GridInteractions.dll" "$logs/grid.json"
    nice -n 19 python3 "$repo/tests/L3b2/compare_contracts.py" "$repo/../logs/L3b2/forms/strict-p3/contracts-windows-final.json" "$logs/contracts-linux.json" "$logs/metadata-comparison.json"
    nice -n 19 python3 "$repo/tests/L3b2/compare_contracts.py" "$repo/../logs/L3b2/forms/strict-p3/apply-windows-final.json" "$logs/apply-linux.json" "$logs/apply-comparison.json"
    ;;
  *) exit 2;;
esac
