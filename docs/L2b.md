# L2b 開發與重建

本分支以 L2a `e01b214c` 合併 L1b `a9e8f328` 為基底。Avalonia viewer 使用 L1b Core 的 Controller、FeModel 與 FRD reader。vtkmax 為 GPL-3.0-or-later，ABI **0.2 不穩定**；實體檔 `libvtkmax.so.0.2.0`，SONAME `.so.0`，請把 managed/native 配對交付。

## 建置與套件

在獲准的 Ubuntu-24.04 WSL 執行；不要在 Windows obj/bin 中編譯 Linux。所有 Linux restore/publish 位於 ext4 `/root/ppm-l2b`。Windows 使用本 worktree 的正常 Windows obj/bin。

```bash
cd <workspace>/prepomax/work_l2b
bash build/l2b-log.sh ../logs/L2b/package-final.log \
  bash build/Package-Linux-Viewer.sh /root/ppm-l2b <workspace>/prepomax/logs/L2b_pkg
bash build/l2b-log.sh ../logs/L2b/viewer-final.log bash build/Check-L2b.sh /root/ppm-l2b
bash build/l2b-log.sh ../logs/L2b/legacy-final.log bash build/Check-Legacy-Viewer.sh /root/ppm-l2b
```

Build-Compat-Native 在 Ubuntu-24.04 內建立獨立 Jammy sysroot，以 GCC 11/glibc 2.35 編譯相容 Ubuntu 22.04 的 native library。它不更換 host compiler，也不把 development packages 裝入 acceptance distros。VTK 9.4.2 來源 SHA256 固定；minimal dependency closure 靜態連入 vtkmax，hidden visibility + export map 只公開 `vm_*`。無 MPI/TBB/Qt/Python；GL/EGL/X11/xcb/fontconfig 等由系統提供。native configure/build 均 nice 19，最多 `-j8`。

Package script 自動產生 tar.gz、deb、含完整專案及 VTK 原始碼的 corresponding-source tar.gz、THIRD-PARTY-NOTICES、逐 component 原始授權、每個實際 dll/so 的對照、Licenses/README、ldd、exports、SHA256SUMS。授權缺漏會失敗；上游必要授權已附原始位元組、URL 與 SHA cache。GPL section 6 提供本專案／VTK 隨附來源的 6(a) 路徑，以及 SOURCE-ACCESS.md 的上游來源存取資訊（6(d)）；不宣稱專案 archive 已包含所有 NuGet 上游原始碼，也不虛構三年書面 offer。NuGet/.NET/toolchain 使用官方套件來源；重建需網路或預先填好的 caches。

Windows 隱藏執行：`python -B build/Clean-WSL.py` 建立三個專用 distro，再執行 `python -B build/Check-Clean-WSL.py`。固定名稱為 PPM-L2b-U22、PPM-L2b-U24、PPM-L2b-D12，資料在 `<workspace>\wsl\`。不 unregister，不操作 CalculiXFatigue。套件檢查涵蓋 tar/deb 的 EGL 與真正 Avalonia/Xvfb model render；Xvfb/xauth 僅測試時安裝。

## 嵌入與操作

預設 **OpenGlControlBase + vtkGenericOpenGLRenderWindow**，Avalonia 管理 desktop GL context 與 framebuffer。繪圖/釋放只在 GL callbacks；其他操作排入 render queue。`VTKMAX_EMBED=x11` 切回 NativeControlHost/X11；兩條路徑保留到 L2 結束。`VTKMAX_OVERLAY_POPUPS=0/1` 可控制 popup 實驗。generic 預設同一合成表面 popup；X11 預設獨立 popup window，避免 native airspace 遮蓋。此版本 generic 使用 GLX desktop GL，不接受 GLES context。

```bash
/root/ppm-l2b/app/run.sh model.pmx
VTKMAX_EMBED=x11 /root/ppm-l2b/app/run.sh model.inp
env -u DISPLAY /root/ppm-l2b/app/run.sh --probe /tmp/probe.png
env -u DISPLAY /root/ppm-l2b/app/run.sh --render model.pmx result.frd /tmp/contours
```

左鍵旋轉/選取、中鍵平移、滾輪縮放。S(Mises)、U(magnitude)、deformation scale、parts/edges/scalar bar 已接實際資料。INP/PMX/FRD 在 worker thread 解析，以進度回報更新 UI。沒有 layup 時最多 64 色群；非結構網格改用 per-part palette。L1b BinaryFormatter 相容橋仍存在，尚未完成正式 serializer replacement。

`--evidence` 僅在 `PPM_L2B_EVIDENCE=1` 啟用；這是隔離測試 IPC，不屬於一般操作。20 ms timer 僅供 evidence 取樣；X11 input 改為阻塞 socket/eventfd watch，idle 不輪詢，停止會喚醒並 join watcher。w/s/p/f/e/q 不落到 VTK 預設快捷鍵。camera animation 採 shortest-arc quaternion；UI timer 僅動畫期間執行。

## 測試與限制

```bash
bash build/l2b-log.sh ../logs/L2b/embedding-final.log \
 env PPM_MATRIX_BACKENDS=generic VTKMAX_OVERLAY_POPUPS=1 \
 bash build/Run-Embedding-Matrix.sh /root/ppm-l2b/app <workspace>/prepomax/logs/L2b/dcb/dcb.pmx <workspace>/prepomax/logs/L2b/embedding-selected
```

Run-Embedding-Matrix 建立 private X11 mount namespace；Xvfb 及 nested Weston/XWayland 全部隱藏，不連接 owner's WSLg/可見桌面。以最大可見主視窗避免誤選 Avalonia helper window。測試 1/1.5/2 倍、實際 XTest input、精確 face pick、wheel projection、focus、鍵盤、tooltip/menu、resize、idle pump。延遲是注入到 evidence projection confirmation，包含 20 ms 取樣及 IPC，並非 photon latency。

真 INP 經 FeModel 測試 C3D10/C3D15/C3D20/S8R/C3D6 的獨立預期 node order 與 face ID；另測 occlusion、perspective、雙 view、callback、64-bit IDs、absolute deformation、180 度 camera animation、100k/500k hex。FPS 是 llvmpipe 離屏 render loop，不能推論實體 GPU 使用感。

DCB 原 FRD 只有 DISP/FORC。dcb_result_fixture.py 僅加 U/S 輸出並換行過長 NSET，原模型材料與載荷不變。該 fixture solve 在完整終載前 exit 201；圖比較採共同 S/U increment 70、t=0.8011749983，不能當成成功完整求解或校正。Windows 圖由既有 Windows build 在 CreateDesktop 私有桌面擷取，從未 SwitchDesktop；原圖與比較圖均保留於 `logs/L2b/dcb`。

完整 IViewport3D、clipping、glyph/vector/tensor 等仍屬後續 L2；本次是往完整 viewport 擴充。真 VM/native install、實體 GPU/顯示器、原生 Wayland 與完整科學求解驗收仍由 owner 後續執行。詳細數字、失敗重試與證據見同目錄 L2b_REPORT.md 及外部交付報告。
