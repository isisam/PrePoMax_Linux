from pathlib import Path
import json,shutil
ROOT=Path(__file__).resolve().parents[1]
LOCAL=Path.home()/'AppData/Local/StudentPkgBuild'
for product in ['PrePoMax-RCM','CalculiX_Win']:
    p=ROOT/'payload'/product
    (p/'VERSION').write_text('0.1.0-rc1g8-win.1 (RC)\n' if product=='PrePoMax-RCM' else '2.21-win64.2\n',encoding='utf-8')
    (p/'SOURCE.md').write_text('''# Source and provenance

Author of student packaging and fork additions: S.Y. Su. Upstream credits are retained.

Public stock CalculiX executable: https://github.com/isisam/CalculiX_Win/releases/tag/v2.21-win64.1
SHA256 of CalculiX-2.21-stock-win64.zip: fcf80d77fc6ba505da2fa36ef7ec9e196ed5c48dca430d2e153c209ef182f0c7
Corresponding solver source: CalculiX-2.21-stock-source.zip, provided with this release and at the original release above.
The stock executable is byte-for-byte unchanged.

PrePoMax student source: https://github.com/isisam/PrePoMax_Linux/tree/v0.1.0-rc1g8-win.1
The matching PrePoMax-RCM_student_source_0.1.0-rc1g8-win.1.zip is supplied with that release.
Its SOURCE_STUDENT.md describes the RC1g8 baseline, stock-only changes, retained dependencies and rebuild recipe.
Third-party notices and exact source locations are retained in Licenses (GUI) or LICENSES (solver).
''',encoding='utf-8')
    if product=='PrePoMax-RCM':
        (p/'Examples/Layup/README.md').write_text('''# 八層疊層範例 / Eight-ply laminate

作者 / Author: S.Y. Su

將本資料夾複製到自己的工作資料夾。用 PrePoMax-RCM 開啟 Layup.pmx，於模型樹開啟 Layup 疊層，可檢查 [45/90/-45/0]s 的方向、各層顏色、單層與爆炸圖。選分析工作 Layup 執行；預設使用內附 stock ccx.exe。

Layup.inp 是可直接求解的輸入檔：在工作資料夾執行 `ccx -i Layup`。單位為 mm、ton、s、MPa。這是線彈性教學試片，不含破壞或疲勞模型。

Copy this folder to your work area. Open Layup.pmx in PrePoMax-RCM to inspect the eight-ply [45/90/-45/0]s laminate and run its inherited stock-solver job. Layup.inp can also be solved directly with `ccx -i Layup`. Teaching example only; no failure or fatigue model.
''',encoding='utf-8')
print('Added source notices and laminate guide')
