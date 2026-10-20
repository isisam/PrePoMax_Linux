# 八層疊層範例 / Eight-ply laminate

作者 / Author: S.Y. Su

將本資料夾複製到自己的工作資料夾。用 PrePoMax-RCM 開啟 Layup.pmx，於模型樹開啟 Layup 疊層，可檢查 [45/90/-45/0]s 的方向、各層顏色、單層與爆炸圖。選分析工作 Layup 執行；預設使用內附 stock ccx.exe。

Layup.inp 是可直接求解的輸入檔：在工作資料夾執行 `ccx -i Layup`。單位為 mm、ton、s、MPa。這是線彈性教學試片，不含破壞或疲勞模型。

Copy this folder to your work area. Open Layup.pmx in PrePoMax-RCM to inspect the eight-ply [45/90/-45/0]s laminate and run its inherited stock-solver job. Layup.inp can also be solved directly with `ccx -i Layup`. Teaching example only; no failure or fatigue model.
