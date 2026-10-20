from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]

def put(p,s):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(s,encoding='utf-8')

for product,setup,version in [('PrePoMax-RCM','PrePoMax-RCM_Setup_0.1.0-rc1g8-win.1.exe','0.1.0-rc1g8-win.1 (RC)'),('CalculiX_Win','CalculiX_Win_Setup_2.21.exe','2.21-win64.2')]:
    gui=product=='PrePoMax-RCM'
    repo='PrePoMax_Linux' if gui else 'CalculiX_Win'
    url=f'https://github.com/isisam/{repo}/releases'
    req='Windows 10/11 64 位元、.NET Framework 4.8、支援 OpenGL 的顯示卡與正常驅動。建議至少 8 GB RAM、2 GB 可用磁碟空間；大模型需要更多。' if gui else 'Windows 10/11 64 位元。建議至少 4 GB RAM、200 MB 可用磁碟空間；大型模型需要更多。無須另外安裝 Fortran runtime。'
    can='''- 匯入幾何與網格、設定材料、負載和邊界條件，交給 CalculiX 計算，再看位移與應力等結果。
- 繁體中文介面，可在設定切換英文。
- 疊層複材可看 3D 纖維方向、各層顏色、單層顯示及爆炸圖，方便檢查鋪層角度與順序。
- 已附公開版 stock CalculiX 2.21，預設路徑為 `Solver\\ccx.exe`，矩陣求解器預設 Spooles。
- 附彈簧、樑及疊層教學範例。''' if gui else '''- 用命令列執行 CalculiX 2.21 支援的有限元素分析，例如靜力、模態及熱傳。
- 將 `.inp` 輸入檔算成 `.dat`、`.frd` 等結果；輸出種類依輸入檔的要求決定。
- 附彈簧與樑範例，方便先確認電腦能計算。
- 安裝後會加入目前使用者的 PATH；請重新開啟終端機，再執行 `ccx -i spring1`。'''
    diff='沿用 PrePoMax 原有工作流程，增加繁中介面與上述疊層檢視功能。此學生版本改用公開版 stock 求解器；不需要自己解壓縮或手動設定求解器路徑。' if gui else '與 v2.21-win64.1 公開 zip 使用完全相同的 ccx.exe，沒有修改數值核心。新增每位使用者的安裝、PATH 設定、開始選單說明與解除安裝程式。'
    lim='這是候選版，主人的 150% DPI 人工檢查尚未完成。疊層畫面用來檢查方向與順序，不能據此證明材料模型正確。' if gui else '這是命令列求解器，沒有繪圖介面、CAD 編輯器或前後處理 GUI；可搭配 PrePoMax-RCM。'
    install=f'''1. 到 [GitHub Releases]({url}) 下載 `{setup}`。
2. 雙擊，按「下一步」→「安裝」→「完成」。不需管理員密碼。
3. 預設裝到 `%LOCALAPPDATA%\\Programs\\{product}`。'''
    install+='\n4. 用桌面或開始選單的 PrePoMax-RCM 捷徑開啟；求解器路徑已設定好。' if gui else '\n4. 重新開啟終端機。把 `examples\\spring1.inp` 複製到自己的工作資料夾，在該處執行 `ccx -i spring1`。'
    smart='本包未以商業憑證簽章，因此可能顯示「未知發行者」。確認檔案來自上述 GitHub release，並比對 SHA256SUMS 後，在 SmartScreen 按「其他資訊」→「仍要執行」。若學校政策不允許，請聯絡學校 IT；不必關閉防毒或安全設定。'
    uninstall='在 Windows「設定 → 應用程式 → 已安裝的應用程式」選擇本程式並解除安裝，或執行安裝資料夾的 `Uninstall.exe`。會移除本包檔案、捷徑與登錄項目。'+('' if gui else '僅移除由本包新增的使用者 PATH 項目，保留原有 PATH。')+'請將自己修改的範例另存到工作資料夾；解除安裝會移除本包附的範例，另存的模型與計算結果會保留。'
    zh=f'''# {product} 功能說明

版本：{version}　作者：S.Y. Su（保留原始上游作者及授權聲明）

## 能做什麼

{can}

## 跟原版的差別

{diff}

## 不能做什麼

{lim} 本包不提供私人疲勞與客製材料求解功能。不是商用軟體的完整替代品；支援範圍依 CalculiX 2.21 與模型設定而定。範例只是教學，不能當成工程設計認證。

## 系統需求

{req} 安裝在目前使用者的可寫入資料夾，不需管理員權限。為了讓舊有原生工具正常運作，工作資料夾建議使用簡單英文字母與數字；安裝程式支援空白和中文路徑。

## 怎麼安裝

{install}

教室批次安裝可用 `{setup} /S`。自訂資料夾時，`/D=完整路徑` 必須放最後；不用加引號，例如 `/S /D=C:\\Student Apps\\{product}`。

## 怎麼解除安裝

{uninstall} 靜默解除安裝：`Uninstall.exe /S`。

## SmartScreen 顯示未知發行者

{smart}
'''
    en=f'''# {product} features

Version: {version}. Author: S.Y. Su. Original upstream credits and licences are retained.

## What it does

'''+('''Prepare finite element models, assign materials and loads, run CalculiX, and inspect results. Includes Traditional Chinese / English UI and composite layup views: 3D fibre directions, colours per ply, single-ply display and exploded view. Bundled stock CalculiX 2.21 is selected automatically at `Solver\\ccx.exe`, using Spooles by default. Includes spring, beam and laminate examples.
''' if gui else '''Runs finite element analyses supported by stock CalculiX 2.21, including static, frequency and heat transfer cases. Reads `.inp` and writes requested result files such as `.dat` and `.frd`. Includes spring and beam examples. Adds ccx to the current user's PATH; open a new terminal after installation.
''')+f'''
## Differences from upstream

'''+('''Adds the Chinese UI and composite visualisation features listed above. This student variant uses the public stock solver and a per-user installer with automatic solver setup.
''' if gui else '''Uses the exact ccx.exe from v2.21-win64.1; the numerical core is unchanged. Adds a per-user installer, user PATH integration, Start menu help and an uninstaller.
''')+f'''
## Limitations

'''+('''Release candidate: the owner's manual 150% DPI check is still pending. Layup views help inspect the model but do not validate its physics.
''' if gui else '''Command-line solver only: no CAD editor, meshing GUI or result viewer. Pair with PrePoMax-RCM if a GUI is needed.
''')+f'''No private fatigue or custom material solver extensions are supplied. Supported analyses depend on CalculiX 2.21 and your input model. Teaching examples are not engineering certification.

## Requirements

Windows 10/11 x64. '''+('''.NET Framework 4.8 and an OpenGL-capable graphics driver. Recommended: 8 GB RAM and 2 GB free disk space.''' if gui else '''Recommended: 4 GB RAM and 200 MB free disk space. No separate Fortran runtime installation.''')+f''' Larger jobs need more resources. Use a writable work folder; simple ASCII work paths are recommended for legacy native tools.

## Install

Download `{setup}` from [GitHub Releases]({url}) → double-click → Next → Install → Finish. No administrator password is required. Default folder: `%LOCALAPPDATA%\\Programs\\{product}`.

'''+('''Launch from the Desktop or Start menu. The solver path is already set.
''' if gui else '''Open a new terminal. Copy `examples\\spring1.inp` into a work folder, change to that folder, and run `ccx -i spring1`.
''')+f'''
Silent installation: `{setup} /S`. Optional `/D=full path` must be the last argument and is unquoted, e.g. `/S /D=C:\\Student Apps\\{product}`.

## Uninstall

Use Windows Settings → Apps → Installed apps, or run `Uninstall.exe` in the installation folder (`/S` for silent removal). Removes shipped files, shortcuts and registration. '''+('' if gui else '''Removes only the user PATH entry added by this installer, retaining pre-existing entries. ''')+f'''Save edited examples outside the install folder. Your separately saved models and results remain.

## SmartScreen: unknown publisher

The installer is unsigned. Verify the GitHub release source and SHA256SUMS. If SmartScreen appears, choose More info → Run anyway. If school policy blocks execution, contact school IT. Do not disable antivirus or change security settings.
'''
    folder=ROOT/'docs'/product
    put(folder/'FEATURES_zh-TW.md',zh);put(folder/'FEATURES_en.md',en)
    quick=f'''# {product} 一鍵安裝

作者：S.Y. Su　版本：{version}

**下載 Setup.exe → 雙擊 → 完成**

{install}

若 SmartScreen 顯示未知發行者：{smart}

完成後：'''+('''開啟桌面 PrePoMax-RCM 捷徑。將 `Examples\\Layup\\Layup.pmx` 複製到自己的工作資料夾再開啟。於模型樹開啟疊層項目，可檢查每層方向與顏色。選分析工作後按執行，即使用內附 stock CalculiX。

![RC1g8 原始候選版的疊層操作畫面](screenshots/layup-rc1g8.png)

上圖是 RC1g8 候選包既有的疊層畫面截圖，供辨認功能；不是本次安裝精靈截圖，也不代表 150% DPI 人工檢查已通過。
''' if gui else '''重新開啟終端機。把安裝資料夾 `examples` 的 `spring1.inp` 複製到自己的工作資料夾，在該資料夾執行 `ccx -i spring1`。成功後會產生 `spring1.dat`，節點 2 的 X 位移應為 0.1。

需要圖形介面時，可另裝 PrePoMax-RCM。下圖是該介面 RC1g8 候選包既有的疊層畫面截圖，獨立 CalculiX 安裝包本身沒有 GUI，也不含此功能。

![搭配使用的 PrePoMax-RCM 圖形介面](screenshots/layup-rc1g8.png)
''')+f'''
解除安裝：{uninstall}

詳細功能與需求見 [繁中功能說明](FEATURES_zh-TW.md)／[English features](FEATURES_en.md)。
'''
    put(folder/'INSTALL_一鍵安裝.md',quick)

put(ROOT/'docs/INSTALL_一鍵安裝.md','''# 學生一鍵安裝

作者：S.Y. Su

| 你需要什麼 | 安裝包 | 安裝說明 |
|---|---|---|
| 繁中圖形介面、疊層檢查及求解器 | PrePoMax-RCM_Setup_0.1.0-rc1g8-win.1.exe | [操作步驟](PrePoMax-RCM/INSTALL_一鍵安裝.md) |
| 只要命令列求解器 | CalculiX_Win_Setup_2.21.exe | [操作步驟](CalculiX_Win/INSTALL_一鍵安裝.md) |

下載 Setup.exe → 雙擊 → 完成。不需管理員權限。PrePoMax-RCM 已含求解器，不必再裝獨立版。

![RC1g8 候選版疊層畫面](screenshots/layup-rc1g8.png)

截圖來源是 RC1g8 候選包，供辨認 GUI 功能。150% DPI 人工檢查尚未完成；PrePoMax-RCM 仍為 RC。
''')
print('Wrote bilingual features and installation guides')
