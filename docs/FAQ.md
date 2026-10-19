# 常見問題 / Troubleshooting

| 問題 | 處理 |
|---|---|
| GitHub 顯示 404 / 無法下載 | 確認已登入受邀帳號且獲准存取私人倉庫；由課程管理者處理權限。 |
| 只有原始碼、找不到 EXE | 從 Releases 的 Assets 下載平台 ZIP；Source code ZIP 是原始碼。 |
| 解壓後缺 DLL 或啟動失敗 | 重新驗證 SHA-256，完整解壓到可寫入目錄；記錄 Windows 版本與錯誤訊息。 |
| Work directory does not exist | 在 Settings 指定已存在且可寫入的資料夾；舊的自訂目錄不會自動重建。 |
| FATCZM 不辨識／分析失敗 | 使用附帶 ccx_fat；確認 USERFATCZM 名稱、常數順序、DEPVAR、單位與層厚。一般 CalculiX 不包含本 UMAT。 |
| 疊層出現 thickness / crossing / overlap 錯誤 | 檢查實體網格層邊界、總厚度、區域重疊及中間節點；不能用放寬 tolerance 掩蓋網格問題。 |
| 150% DPI 元件位置有問題 | RC1g5 尚待原生 150% DPI 人工確認；記錄實際顯示比例、解析度與匿名截圖。 |
| 求解變慢或不收斂 | 先以單執行緒跑原始範例，再逐項改變輸入；檢查 .sta/.cvg。請勿把截斷計算當成起裂壽命。 |
| 新模型無法在舊版開啟 | 保留原檔並使用相容版本；新型別與疊層不保證舊版可讀。 |

English: verify the exact asset hash, extract all files, select a writable work directory and start from an unchanged example. Check material naming, constants, units and mesh conformity before investigating convergence. RC1g5 still needs the native 150% DPI manual check. Report a minimal anonymized reproducer through Issues.
