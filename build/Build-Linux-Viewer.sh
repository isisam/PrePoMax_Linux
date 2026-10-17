#!/usr/bin/env bash
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-"$HOME/ppm-l2a"}
native_build=${VTKMAX_BUILD_DIR:-"$out/native"}
export DOTNET_PROCESSOR_COUNT=${DOTNET_PROCESSOR_COUNT:-4}
export OMP_NUM_THREADS=${OMP_NUM_THREADS:-4}
export LP_NUM_THREADS=${LP_NUM_THREADS:-4}
export VTK_SMP_MAX_THREADS=${VTK_SMP_MAX_THREADS:-4}
cmake -S "$repo/native/vtkmax" -B "$native_build" -G Ninja -DCMAKE_BUILD_TYPE=RelWithDebInfo
cmake --build "$native_build" -j 4
dotnet build "$repo/tests/L2a/ViewerChecks.csproj" -c Release -p:CoreTargetFrameworks=net8.0 -m:1
dotnet publish "$repo/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1 \
  -r linux-x64 --self-contained true -o "$out/app"
cp -L "$native_build/libvtkmax.so" "$out/app/libvtkmax.so"
cp "$repo/native/vtkmax/LICENSE" "$out/app/vtkmax-LICENSE"
cat > "$out/app/run.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
export DOTNET_PROCESSOR_COUNT=4 OMP_NUM_THREADS=4 LP_NUM_THREADS=4 VTK_SMP_MAX_THREADS=4
app=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
exec "$app/PrePoMax.Avalonia" "$@"
EOF
chmod +x "$out/app/run.sh"
echo "Run: $out/app/run.sh /path/model.inp"
