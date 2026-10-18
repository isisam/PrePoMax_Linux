#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-/root/ppm-l2c}
release=${PPM_CCX_RELEASE_SOURCE:-"$repo/../ccx_fat_win64_v1.1_source.zip"}
[[ -f "$release" ]] || release=<workspace>/share_pkg/ccx_fat_win64_v1.1/source/ccx_fat_win64_v1.1_source.zip
[[ -x "$out/solver/ccx_fat" ]] || bash "$repo/build/Build-L2c-Solver.sh" "$release" "$out"
bash "$repo/build/Collect-L2c-Solver-Sources.sh" "$out"
mkdir -p "$out/app/Solver"
cp "$out/solver/ccx_fat" "$out/app/Solver/"
cp "$out/solver/release-source.json" "$out/app/Solver/"
cp "$out/solver/versions.txt" "$out/app/Solver/numerical-source-versions.txt"
cp "$repo/build/viewer-run.sh" "$out/app/prepomax-rcm";chmod 755 "$out/app/prepomax-rcm"
cat > "$out/app/prepomax-rcm.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Name=PrePoMax RCM
Comment=Finite element model and result viewer
Exec=prepomax-rcm %f
Terminal=false
Categories=Science;Engineering;
MimeType=application/x-prepomax;application/x-calculix;
EOF
python3 - "$out/app" "$release" <<'PY'
from pathlib import Path
import sys,json,zipfile,tarfile,io
p=Path(sys.argv[1])
release=Path(sys.argv[2])
notices=p/'Licenses/solver-upstream';notices.mkdir(exist_ok=True)
with zipfile.ZipFile(release) as z:
    for name in z.namelist():
        if name.endswith('README_SOURCE.md'):(notices/'README_SOURCE.md').write_bytes(z.read(name))
        if '/upstream/' in name and name.endswith(('.orig.tar.gz','.orig.tar.bz2','.debian.tar.xz')):
            with tarfile.open(fileobj=io.BytesIO(z.read(name))) as tar:
                for member in tar:
                    base=Path(member.name).name.lower()
                    if member.isfile() and member.size<1024*1024 and base.startswith(('copying','copyright','license','gpl.htm')):
                        dest=notices/(Path(name).name+'-'+Path(member.name).name);dest.write_bytes(tar.extractfile(member).read())
assert len(list(notices.iterdir()))>=5,'Original solver/dependency notices missing'
source=p/'CORRESPONDING-SOURCE.txt'
source.write_text(source.read_text().replace('prepomax-l2b-source','prepomax-rcm-source')+'\nThe adjacent source archive additionally includes ccx_fat_win64_v1.1_source.zip (CalculiX 2.21 and FATCZM) plus solver-numerical-sources (exact Jammy SPOOLES, ARPACK and LAPACK/BLAS .dsc/orig/Debian sources used for static linking). See Solver/numerical-source-versions.txt and Build-L2c-Solver.sh.\n')
(p/'SOURCE-ACCESS.md').write_text((p/'SOURCE-ACCESS.md').read_text().replace('prepomax-l2b-source','prepomax-rcm-source'))
(p/'Licenses/ccx_fat.txt').write_text('ccx_fat: CalculiX 2.21 (Klaus Wittig, Guido Dhondt), FATCZM additions (RCM Lab, NCKU). GPL-2.0-or-later; statically linked SPOOLES (public domain), ARPACK (BSD) and LAPACK/BLAS (BSD). Exact upstream notices and source are in the adjacent release source archive.\n')
(p/'THIRD-PARTY-NOTICES.txt').write_text((p/'THIRD-PARTY-NOTICES.txt').read_text()+'\n'+(p/'Licenses/ccx_fat.txt').read_text())
inventory=json.loads((p/'license-inventory.json').read_text());inventory.append(dict(component='ccx_fat and static numerical dependencies',version='v1.1 / CalculiX 2.21',files=['Solver/ccx_fat'],license='ccx_fat.txt',source='Adjacent ccx_fat_win64_v1.1_source.zip inside prepomax-rcm-source.tar.gz'))
(p/'license-inventory.json').write_text(json.dumps(inventory,indent=2))
(p/'Licenses/README.md').write_text((p/'Licenses/README.md').read_text()+'\n| ccx_fat / CalculiX 2.21 | Solver/ccx_fat | GPL-2.0-or-later; numerical dependency notices in release source | prepomax-rcm-source.tar.gz |\n')
PY
echo 'PASS shell launcher, desktop entry and release-source Linux solver installed'
cp "$out/solver/notices/"* "$out/app/Licenses/solver-upstream/"
