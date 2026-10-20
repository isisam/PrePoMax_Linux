# L2c：Avalonia shell 與 L1b view contracts

工作分支 `rcm/l2c`，由 `rcm/l2b` 的 `2fadaed8` 建立。六個任務項目各一個 commit；交付 patch 在 `../../patches/l2c/`。完整驗收、數據與未實作成員清單見 [L2c public viewport contract](L2c-viewport-unsupported.txt)。作者為 S.Y. Su。

## 使用

解開 `prepomax-rcm-linux-x64.tar.gz` 後執行：

```bash
./app/prepomax-rcm model.pmx
./app/prepomax-rcm model.inp
./app/prepomax-rcm result.frd
```

deb 安裝後使用 `prepomax-rcm` 或 Applications 中的 PrePoMax RCM。tar 版本需要與 deb control 相同的系統依賴，繁中選單需 CJK 字型。deb 會安裝 `fonts-noto-cjk`；不附系統 GL/EGL、fontconfig、ICU 或 libgfortran。

File → Open 可開 PMX/INP/FRD；Save 經 L1b Controller 寫完整 PMX container；Export 匯出 PNG。模型樹使用 Controller.Model，勾選切換零件顯示，數值欄為 opacity 0–1。View 有六方向、正等角、fit 及邊線開關。S/U 使用 FRD 最新共同 increment，變形比例使用原始位移，重複設定不累加。數值單位由模型狀態列顯示，不猜測 Undefined 的物理單位。

Run analysis 先由 Controller 匯出目前模型到獨立 analysis directory，再以 nice 19 呼叫 Linux `ccx_fat`；可選 1–4 執行緒。stdout/stderr 即時進入訊息區，並寫 `solver.log`。完成後自動讀回 FRD；非零 exit 的部分結果會標示失敗。Cancel 或關閉 shell 會終止正在執行的 solver tree。

solver 搜尋順序：明確的 `PPM_CCX_FAT`、`app/Solver/ccx_fat`、`app/ccx_fat`、PATH。拒絕非 ELF；不會默默使用普通 ccx。套件內提供由 v1.1 release 對應來源建出的 Linux ELF；其 hash、原始 zip hash 及靜態數值庫來源版本都在 Solver 目錄與執行日誌。

Language 選單可切換 English／繁體中文；直接重用 `rcm/l10n` (`84d8eff7`) 的 JSON，使用 System.Text.Json，不引用 WinForms/System.Web。既有 catalogue 沒有的新 shell 文字保留英文；不改數字文化設定。

## 建置與驗證

Windows 僅執行隱藏 console；Windows build 的 obj/bin 留在本 worktree。Linux source staging、restore、obj/bin、native 與 publish 都在 ext4 `/root/ppm-l2c`，不共用 Windows 的中間檔。WSL build 使用 nice 19、native 最多 `-j8`。產品 launcher 不設定 DOTNET/OMP/LP/VTK 的執行緒上限。

從 Windows PowerShell 呼叫 Ubuntu-24.04：

```powershell
wsl -d Ubuntu-24.04 -- bash <workspace>/prepomax/work_l2c/build/l2b-log.sh <workspace>/prepomax/logs/L2c/package-final.log bash <workspace>/prepomax/work_l2c/build/Package-Linux-Shell.sh /root/ppm-l2c <workspace>/prepomax/logs/L2c/packages
python -B tests/L2c/windows_regression.py
python -B tests/L2c/import_clean.py
python -B tests/L2c/clean_packages.py
```

在 Ubuntu-24.04，以同一個 l2b-log.sh 包住下列命令：

```bash
bash build/Publish-L2c-Headless.sh /root/ppm-l2c
bash build/Check-L2c-Contract.sh /root/ppm-l2c
bash build/Check-L2c-Native.sh /root/ppm-l2c
bash build/Check-L2c-Analysis.sh /root/ppm-l2c
bash build/Check-L2c-Shell.sh /root/ppm-l2c
bash build/Check-L2c-Carryovers.sh /root/ppm-l2c
bash build/Run-L2c-Matrix.sh /root/ppm-l2c/app ../logs/L2c/comparison/dcb.pmx ../logs/L2c/embedding-release
```

Windows baseline 圖由 `tests/L2c/hidden_desktop_capture.py` 在 CreateDesktop 建立的獨立桌面產生，不呼叫 SwitchDesktop。先產生 Windows baseline，才跑 Check-L2c-Shell；PNG 的並排與數值核對使用 `tests/L2c/compare_dcb.py`。`windows_regression.py` 包含 Windows build、M1/M2a/M2b、L1a 與完整 L1b Windows→Linux replay/save→Windows 測試。

`Package-Linux-Shell.sh` 產生 tar、deb、同附 source archive、SHA256SUMS 與 delivery-receipt。source archive 包括專案、VTK 9.4.2、原 ccx_fat v1.1 source zip，以及實際靜態連結的 Jammy SPOOLES/ARPACK/LAPACK 原始來源包。`Collect-L2c-Solver-Sources.sh` 保存確切來源版本與原授權文字。Linux solver 建置也可明確指定 release source zip 為 Build-L2c-Solver.sh 第一個參數。

## 實作邊界

VtkMaxView 是 `IViewport3D` adapter，內部預設 generic OpenGL，`VTKMAX_EMBED=x11` 保留 L2b native child 路徑。所有 GL 工作排入 current-context queue。主視窗明確實作完整 `IMainView`，轉交 ShellMainView；檔案解析期間暫停 legacy draw callbacks，完成後以 Controller.Model 的真實各零件建立 volume actors。尚未移植的編輯表單沿用 L1b placeholder，屬 L3。

IViewport3D 的 177 個方法／屬性宣告中，57 個有 L2 實作，120 個明確拋 NotSupportedException；另實作兩個事件，合計 59/179。反射層 188 個 accessors/event methods 中 68 個有實作，120 個均實際呼叫驗證拒絕。完整清單在報告及 [L2c-viewport-unsupported.txt](L2c-viewport-unsupported.txt)。

條件限制：不支援 box pick、ModelEdges/Wireframe mode、任意相機動畫、animated fit、負等角、非零 roll 或自訂／反轉 palette；相應參數會明確拒絕。支援 Windows 標準方向的非阻塞動畫。節點需在 10 physical pixel 容差內，元素中央可能回傳 NodeId=-1。FRD 需有共同 S/U increment，只有位移的檔案不會冒作含 S 結果。

`--evidence` 僅在 `PPM_L2C_EVIDENCE=1` 啟用；舊 `PPM_L2B_EVIDENCE=1` 保留相容。其 20 ms sampler 只供測試，視窗關閉即停。一般操作沒有 evidence timer；X11 使用 fd/eventfd 等待。效能量測分別標示 Avalonia frame-loop 與 EGL render-loop，不當作 GPU／scanout FPS。

原生 GPU、真 VM／native install、完整編輯器與視覺樣式 parity、BinaryFormatter 替換仍待後續。DCB 畫面用既有未完成終載求解的共同 increment 70，不能當作完整 DCB 科學求解通過。
