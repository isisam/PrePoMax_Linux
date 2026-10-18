#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Build Linux ccx_fat from the complete corresponding source in the owner's v1.1 release.
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
root=${VTKMAX_SYSROOT:-"$HOME/ppm-l2b-jammy"}
archive=${1:-${PPM_CCX_RELEASE_SOURCE:-"$repo/../ccx_fat_win64_v1.1_source.zip"}}
[[ -f "$archive" ]] || archive=<workspace>/share_pkg/ccx_fat_win64_v1.1/source/ccx_fat_win64_v1.1_source.zip
out=${2:-"$HOME/ppm-l2c"}
stage="$root/work/l2c-solver"
mkdir -p "$stage"
python3 - "$archive" "$stage" <<'PY'
import zipfile,sys,hashlib,json
from pathlib import Path
archive=Path(sys.argv[1]);stage=Path(sys.argv[2])
with zipfile.ZipFile(archive) as z:
    for name in z.namelist():
        parts=Path(name).parts
        if len(parts)>=3 and parts[1] in ('upstream','fatczm') and not name.endswith('/'):
            target=stage.joinpath(*parts[1:]);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(z.read(name))
(stage/'release-source.json').write_text(json.dumps(dict(archive=str(archive),sha256=hashlib.sha256(archive.read_bytes()).hexdigest()),indent=2))
PY
if ! chroot "$root" test -f /usr/lib/x86_64-linux-gnu/libarpack.a; then
  nice -n 19 chroot "$root" apt-get update -qq
  nice -n 19 chroot "$root" env DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends gfortran libspooles-dev libarpack2-dev liblapack-dev python3
fi
if [[ ! -d "$stage/ccx_2.21" ]];then tar -xjf "$stage/upstream/calculix-ccx_2.21.orig.tar.bz2" -C "$stage";mv "$stage/CalculiX/ccx_2.21" "$stage/ccx_2.21";fi
nice -n 19 chroot "$root" python3 /work/l2c-solver/fatczm/apply_patch.py /work/l2c-solver/ccx_2.21/src /work/l2c-solver/fatczm
rm -f "$stage/ccx_2.21/src/ccx_2.21" # relink when LIBS changes; all outputs stay in the dedicated sysroot
nice -n 19 chroot "$root" make -C /work/l2c-solver/ccx_2.21/src -j8 \
  CC=gcc FC=gfortran 'CFLAGS=-O2 -I/usr/include/spooles -DARCH="Linux" -DSPOOLES -DARPACK -DMATRIXSTORAGE -DUSE_MT=1' \
  'FFLAGS=-O2 -fallow-argument-mismatch' 'LIBS=/usr/lib/x86_64-linux-gnu/libspooles.a -lpthread /usr/lib/x86_64-linux-gnu/libarpack.a /usr/lib/x86_64-linux-gnu/liblapack.a /usr/lib/x86_64-linux-gnu/libblas.a -lm' ccx_2.21
mkdir -p "$out/solver"
cp "$stage/ccx_2.21/src/ccx_2.21" "$out/solver/ccx_fat"
cp "$stage/release-source.json" "$out/solver/"
sha256sum "$out/solver/ccx_fat"
ldd "$out/solver/ccx_fat"
