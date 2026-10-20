from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.colors import HexColor,white
from reportlab.platypus import Paragraph,Table,TableStyle
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
import json,html
import os
OUT=Path(os.environ.get('RC1D_PACKAGE','.'))
ROOT=Path(__file__).resolve().parent
SCREENSHOTS=OUT/'Documentation/screenshots'
pdfmetrics.registerFont(TTFont('JhengHei',r'C:\Windows\Fonts\msjh.ttc',subfontIndex=0))
FONT='JhengHei';W,H=595.28,841.89
style=ParagraphStyle('Body',fontName=FONT,fontSize=10.5,leading=17,textColor=HexColor('#24364b'),wordWrap='CJK',spaceAfter=10)
small=ParagraphStyle('Small',parent=style,fontSize=8.8,leading=13)
navy=HexColor('#152f48');teal=HexColor('#007d86')
records=[]
def para(c,text,y,smalltext=False):
    p=Paragraph(text,small if smalltext else style);_,h=p.wrap(W-88,H)
    if y-h<52:raise ValueError('Page overflow: '+text[:80])
    p.drawOn(c,44,y-h);return y-h-10
def table(c,rows,y,widths):
    data=[[Paragraph(html.escape(str(v)),small) for v in row] for row in rows]
    t=Table(data,colWidths=widths,hAlign='LEFT');t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),HexColor('#e0eff1')),('VALIGN',(0,0),(-1,-1),'TOP'),('GRID',(0,0),(-1,-1),.35,HexColor('#ccd6de')),('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),('TOPPADDING',(0,0),(-1,-1),6),('BOTTOMPADDING',(0,0),(-1,-1),6)]))
    _,h=t.wrap(W-88,H)
    if y-h<52:raise ValueError('Table overflow')
    t.drawOn(c,44,y-h);return y-h-16
def picture(c,name,y,maxheight=305):
    p=SCREENSHOTS/name;im=ImageReader(str(p));iw,ih=im.getSize();scale=min((W-88)/iw,maxheight/ih)
    dw,dh=iw*scale,ih*scale
    c.drawImage(im,44+(W-88-dw)/2,y-dh,dw,dh,mask='auto');return y-dh-14
EN=[
('Start here',[
'PrePoMax-RCM 0.1.0-rc1f combines the Windows PrePoMax interface, a native CalculiX 2.21 solver with FATCZM, editable flat solid composite layups, and selectable Traditional Chinese. This guide assumes basic FEM knowledge, but no knowledge of our implementation.',
'The learning route is: unzip the complete folder, open an example, run its existing analysis, compare its text result, then make a new material or layup in a copy of the model. Keep the original examples as references.',
'Use a Windows 10/11 x64 PC. Windows 10 version 1903 and later and Windows 11 include .NET Framework 4.8 or newer. Python and the native runtimes are supplied in the package; a separate Python, Visual Studio or Linux installation is not needed for the examples. If startup fails on a clean PC, install the Microsoft Visual C++ 2008 SP1 Redistributable (x64) to supply VC90. Clean-PC startup, viewport rendering, meshing and example execution remain to be confirmed.',
'All dimensional example data use N, mm, MPa and seconds, with tonne as the consistent mass unit. FATCZM cycle count N is a state variable and is not the solver time coordinate. The demonstration alpha values are not calibrated material data.'
],None),
('Unzip, start, choose a language',[
'1. Use Windows Extract All on the release zip. Keep PrePoMax.exe, lib, Solver, Python and the other folders together. Extract into a writable folder; do not run the executable inside the zip. Make a separate working folder for each fatigue job.',
'2. Double-click PrePoMax.exe. Chinese Windows (UI culture zh-TW, zh-HK or zh-Hant) starts in 繁體中文; other UI cultures start in English. To switch to English, choose 工具 (Tools) > 設定 > 一般 > 語言 > English and accept. Tools > Settings > General > Language switches back to 繁體中文. A saved choice wins on later launches; Reset follows Windows UI culture. No settings.bin is shipped. First launch and Reset create %USERPROFILE%/Documents/PrePoMax-RCM/Work as the default work directory. Existing custom paths are retained.',
'3. Check Tools > Settings > Solver. The CalculiX executable should resolve to Solver/ccx_fat.exe and the matrix solver should be Spooles. The package also retains Solver/ccx_dynamic.exe for ordinary, non-FATCZM analyses. Select it explicitly if you need the stock solver; it cannot run a FATCZM material.'
], 'settings_en.png'),
('Open and run an example',[
'1. Copy an Examples subfolder to a writable location. In PrePoMax use File > Open and choose its .pmx file. Use Save As before changing it. Each supplied model already contains its mesh, materials, assignments, boundary conditions and analysis job.',
'2. In Settings > Solver, set the work directory to that example copy; the initial default is %USERPROFILE%/Documents/PrePoMax-RCM/Work. Use .pmx folder as work directory remains off by default; enable it to follow the saved model folder. In the Analyses tree, right-click the named job (DCB, ENF or Layup) and choose Run. Use one CPU for reproducible demonstration results. Wait for completion; inspect the solver output for *ERROR before interpreting a result.',
'3. The fatigue steps are deliberately retained as model user keywords so STATIC, DIRECT and the output requests remain exact. The Steps tree can therefore be empty. Use the model keyword editor to inspect these steps; adding another ordinary step would change the intended analysis.',
'4. Compare the generated files with expected.txt. The example files are model snapshots with no development-machine command/import history. They can be edited and saved, but there is no original construction history to regenerate.'
],None),
('Read the results and the checks',[
'Open the completed .frd file through File > Open, or use the analysis Results action. Choose DISP and a displacement component to inspect deformation. For a fatigue result choose the SDV field and SDV1, which is interface damage D. Arm elements do not share this constitutive state. Check selected step/increment before comparing a contour.',
'ENF: all eight integration points of element 2368 first reach D = 1 at solver time 101, which is fatigue jump 100 after the ramp. Read N = 624.92741 from column 3 of the line beginning 2 101 in fatczm_jumps.log. Column 3 belongs to the end of the preceding increment. After jump 300, final N = last column 3 + last column 6 = 1320.9767811.',
'DCB: the 60-jump demonstration ends at N = 18860.013543 and upper loading displacement U2 = 0.7543002 mm. These are regression values for this particular discretization and load, not a published onset-life prediction.',
'Layup: the continuous free-edge coupon gives apparent Ex = 60500.75 MPa from the total x reaction divided by cross-sectional area 2 mm² and prescribed strain 0.0001. Its periodic membrane CLT reference is 64387.645 MPa; the two boundary-value problems are different.'
],None),
('Create a FATCZM material',[
'In the Materials tree choose Create. Name the material with the exact case-sensitive USERFATCZM prefix. Add Fatigue CZM (FATCZM) from the available material properties. This creates 26 ordered constants and DEPVAR = 6. FATCZM preset is read-only: adding this property sets it to True; importing a 26-constant USERFATCZM material also recognizes the preset. Edit State variables (DEPVAR) on the Properties tab and constants on Data points.',
'Enter strengths, toughnesses, physical interface thickness and fatigue coefficients in consistent model units. Descriptive units are metadata, not automatic conversion. Check the full table in Documentation/FATCZM_material.md. Changing strength or toughness does not automatically recalculate a nonzero compression penalty; constant 17 = 0 requests the solver automatic value.',
'The preset alone creates no elements, section or fatigue step. Assign it to a separately prepared C3D8 interface layer with a solid section. An opening pre-crack requires BOTH constant 5 = 0.999999 and constant 19 = 0 (compression-only contact). Changing constant 19 alone leaves the default interface intact; a name suffix alone does not create a pre-crack. Verify in a one-element opening check: D = 1 and zero opening reaction.'
], 'material_en.png'),
('Interface thickness, normal and jumps',[
'Constant 6 must equal the actual interface element thickness. For a 0.001 mm layer use 0.001 in the N-mm-MPa system. Constant 15 is the GLOBAL normal axis: 1 = X, 2 = Y, 3 = Z. The DCB/ENF examples are normal to global Y and use 2; this differs from a typical laminate stacked along global Z.',
'FATCZM ignores *ORIENTATION. A rotated ply coordinate system does not rotate the cohesive normal. Use a flat, axis-aligned, one-element-thick layer of linear C3D8 elements; the arms use their own solid elements. Conforming shared nodes or correctly defined ties must transfer load.',
'Step 1 ramps to the peak load. Step 2 retains that load, uses *STATIC, DIRECT and a unit time increment. For 300 jumps use time period 300 and allow at least 310 increments. Constant 14 = 2 identifies the first fatigue step. Select Spooles, keep Nlgeom off and request SDV output.',
'Constants 13, 14 and 18 must agree across all active FATCZM materials because cycle bookkeeping is global. DEPVAR must be at least 8 when initiation is enabled. Keep exactly one fatigue job per work folder: fatczm_jumps.log has a fixed filename.'
],None),
('Create a composite layup',[
'Start from an existing flat, conforming 3D solid mesh with element faces aligned to every ply boundary. Create an engineering-constants material for each lamina. In Composite Layups choose Create, select the part or element set, choose a rectangular coordinate system, and normally keep Stacking axis = 3.',
'Enter the bottom-to-top ply table: P01 / IM7_8552 / 0.125 mm / 45 deg / 1; then P02 at 90 deg, P03 at -45 deg and P04 at 0 deg. Set Symmetric = True. The complete reversed list is appended, so the centre ply is duplicated and the expanded stack has eight plies, total thickness 1 mm.',
'Use Add, Insert, Delete, Move up, Move down, Duplicate and Reverse to edit rows. The optional Elements value checks existing thickness bands; it does not create or refine mesh. The stack diagram is schematic. Select a row to highlight its elements. Read the validation summary before accepting.'
], 'layup_en.png'),
('Angle convention and validation',[
'For the recommended stacking axis 3, 0 degrees is the FIRST axis of the layup coordinate system. A positive angle is a right-hand rotation about the positive stacking axis. With global coordinates, +30 degrees rotates the fibre toward +Y. A translated coordinate-system origin does not change the orientation vectors.',
'Advanced axis choices use cyclic bases: stacking axis 1 uses CS axis 2 as zero, and stacking axis 2 uses CS axis 3 as zero. To keep the first-axis convention in this guide, use stacking axis 3 and orient the rectangular coordinate system appropriately. The writer exports one *ORIENTATION and one solid section per expanded ply.',
'The stack starts at the minimum node projection on its stacking axis. Its total thickness must match the region extent within the chosen tolerance. Every node of every element must belong to a single ply interval. Crossing/empty plies, ambiguous elements, missing materials and overlaps with another active layup or ordinary solid section are refused.',
'The 64.387645 GPa CLT value is for the supplied IM7/8552 lamina constants and a periodic plane-stress membrane cell. It is not a universal composite modulus. The short free-edge teaching coupon has different apparent stiffness; compare the same model and boundary conditions when reporting convergence.'
],None),
('A safe editing workflow',[
'Save a copy before changing thickness, angle, material or load. Change one item at a time, then inspect the validity summary and exported CalculiX input. Open an untouched example again if you need a baseline. A solver exit alone does not establish a sound model: check reaction balance, deformed shape, increment completion and sensitivity to mesh/boundary conditions.',
'A red validation message often means the physical stack and mesh disagree. Changing a ply thickness does not remesh the region. Fix the mesh or restore the original thickness. Missing material/coordinate-system references remain explicit so they can be repaired. Duplicate active layups covering the same elements are invalid.',
'For a stock CalculiX fallback choose Solver/ccx_dynamic.exe and an appropriate supported matrix solver, then create or replace the job so it uses that choice. Do this only for ordinary models without USERFATCZM. Changing an existing explicit job override requires editing that job as well.',
'This build reads older .pmx files, but unmodified older PrePoMax builds cannot read new FATCZM/layup types. Keep the original file for exchange with older versions. A flattened .inp export transfers solver data, not an editable layup definition.'
],None),
('Limits, support and source',[
'RC1 supports flat laminates and solid elements. It does not generate a layup mesh, insert interface layers automatically, support curved layups, or rotate FATCZM normals through *ORIENTATION. The ply plot and current highlighting are not a proof of local fibre-direction rendering. Shared-node stress contours may average values across layers.',
'The demonstrations verify software behavior and numerical reproducibility. Their alpha = 1 cycle scales are not calibrated life predictions. Confirm model assumptions and material calibration before using results in a thesis. A human review of view rotation, plot readability and engineering Chinese terminology remains useful.',
'To report a problem to your course instructor or maintainer Isisam, provide VERSION, Windows version, UI language, exact steps, a minimal .pmx, exported .inp, solver .log/.sta/.cvg and relevant .dat or fatczm_jumps.log lines. Include expected versus observed behavior. Remove private geometry, personal paths, credentials and unrelated files.',
'The executables are unsigned; antivirus or SmartScreen may flag an unfamiliar build. Verify SHA256SUMS and ask the course distributor if a file is quarantined. Do not disable protection globally. PrePoMax-RCM is GPL-3.0; the corresponding-source git bundle is beside the zip. CalculiX/FATCZM GPL-2 source is in Source/ccx_fat. Preserve Licenses and the source information when sharing.'
],None)]
ZH=[
('從這裡開始',[
'PrePoMax-RCM 0.1.0-rc1f 整合 Windows 原生 PrePoMax、具 FATCZM 材料的 CalculiX 2.21 求解器、可編輯的平板實體複材疊層，以及可切換的繁體中文介面。本手冊假設您懂基本有限元素法，但不熟悉本程式。',
'建議學習順序：完整解壓縮、開啟範例、執行已有分析、對照文字基準值，再在模型副本中建立新材料或疊層。保留原始範例，作為操作與數值比較基準。',
'請使用 Windows 10/11 x64。Windows 10 1903 以後與 Windows 11 已內建 .NET Framework 4.8 或更新版本；套件隨附 Python 與原生執行階段。執行範例不需要另外安裝 Python、Visual Studio 或 Linux。若乾淨電腦無法啟動，安裝 Microsoft Visual C++ 2008 SP1 Redistributable (x64) 可補齊 VC90；乾淨電腦的啟動、視窗繪圖、網格及範例執行仍待確認。',
'範例採用 N、mm、MPa、秒；一致質量單位為 tonne。疲勞循環數 N 是狀態紀錄，不等於求解器時間。範例的 alpha 為示範縮放值，並非已校準的材料壽命參數。'
],None),
('解壓縮、啟動與切換語言',[
'1. 在 Windows 對發行 zip 選「全部解壓縮」。PrePoMax.exe 必須與 lib、Solver、Python 等資料夾一起保留；不要在 zip 內直接啟動。請放在可寫入的資料夾，每個疲勞工作使用獨立工作目錄。',
'2. 雙擊 PrePoMax.exe。繁體中文 Windows（介面文化 zh-TW、zh-HK 或 zh-Hant）首次啟動使用「繁體中文」，其他介面文化預設 English。切換英文：選「工具 (Tools) → 設定 → 一般 → 語言 → English」並確定。英文介面可用 Tools → Settings → General → Language 切回繁中。已儲存的選擇優先；重設則依 Windows 介面文化。發行包不附 settings.bin。首次啟動與重設會建立 %USERPROFILE%/Documents/PrePoMax-RCM/Work 作為預設工作目錄；既有自訂路徑會保留。',
'3. 在「設定」的求解器頁確認 CalculiX 指向 Solver/ccx_fat.exe，矩陣解法選 Spooles。保留的 Solver/ccx_dynamic.exe 是一般模型的原版後備求解器，必須自行選取；它不能執行 FATCZM 材料。'
], 'settings_zh-TW.png'),
('開啟並執行範例',[
'1. 將 Examples 中的一個子資料夾複製到可寫入位置。選「檔案 → 開啟」讀入 .pmx；修改前先另存新檔。三個範例已含網格、材料、指派、邊界條件與分析工作。',
'2. 在「設定 → 求解器」把工作目錄指向該範例副本；初始預設為 %USERPROFILE%/Documents/PrePoMax-RCM/Work。「使用 .pmx 資料夾作為工作目錄」預設維持關閉；開啟後會跟隨已儲存模型的資料夾。在「分析」樹右鍵選 DCB、ENF 或 Layup 工作並執行。為重現示範結果，使用 1 個 CPU。等待完成，先檢查求解輸出有無 *ERROR，再解讀數值。',
'3. 疲勞分析步刻意保留在模型的使用者關鍵字中，以完整保存 STATIC, DIRECT 與輸出要求，因此「分析步」樹可能是空的。請用模型關鍵字編輯器查看；額外新增一般分析步會改變範例設定。',
'4. 對照各資料夾的 expected.txt。這些模型是可攜式快照，未保留開發工作站的建構、匯入與命令歷程；可以繼續編輯儲存，但不能重播原始建構歷程。'
],None),
('讀取結果與對照基準值',[
'分析完成後，可用「檔案 → 開啟」讀入 .frd，或選分析工作的結果動作。選 DISP 與位移分量查看變形。疲勞界面可選 SDV 場的 SDV1，也就是損傷 D；複材臂部不具有相同的材料狀態。比對前先確認分析步與增量。',
'ENF：元素 2368 的八個積分點首次全部達 D = 1，發生在求解器時間 101，也就是載入步之後的第 100 次疲勞跳躍。從 fatczm_jumps.log 的「2 101」列第 3 欄讀取 N = 624.92741。第 3 欄是前一增量結束的循環數。第 300 跳結束時，最後列第 3 欄加第 6 欄，得到 N = 1320.9767811。',
'DCB：本示範完成 60 次跳躍，結束 N = 18860.013543，上方載入點 U2 = 0.7543002 mm。這是此網格與載入條件的回歸值，並非文獻起裂壽命預測。',
'Layup：連續、自由邊界試片的表觀 Ex = 60500.75 MPa，取總 X 反力除以截面積 2 mm²，再除以指定應變 0.0001。週期膜單元的 CLT 參考為 64387.645 MPa；兩者邊界值問題不同。'
],None),
('建立 FATCZM 材料',[
'在「材料」樹選建立，名稱須使用大小寫正確的 USERFATCZM 前綴。在可用材料性質中加入「疲勞內聚區域模型 (FATCZM)」。加入後建立 26 個有順序的常數與 DEPVAR = 6。「FATCZM 預設組」為唯讀：加入此材料性質時設為「是」，匯入含 26 常數的 USERFATCZM 材料也會辨識為預設組。在「屬性」頁編輯「狀態變數 (DEPVAR)」，在「資料點」頁編輯常數。',
'輸入強度、韌性、實際界面厚度與疲勞係數時，須自行保持模型單位一致；說明欄的單位文字不會自動換算數值。完整對照表位於 Documentation/FATCZM_material.md。更動強度或韌性不會自動重算已填入的壓縮罰剛度；常數 17 = 0 可要求求解器自動計算。',
'材料預設組不會建立元素、斷面或疲勞步。須先準備獨立的 C3D8 界面層，再以實體斷面指派。開口預裂區必須同時設定常數 5 = 0.999999 與常數 19 = 0（僅受壓接觸）。只改常數 19，預設界面仍完整；名稱尾碼也不會建立預裂區。請以單元素開口檢查確認 D = 1，且開口方向反力為零。'
], 'material_zh-TW.png'),
('界面厚度、全域法向與跳躍',[
'常數 6 必須等於界面元素的實際厚度。例如在 N-mm-MPa 單位下，0.001 mm 的層填 0.001。常數 15 指定「全域」法向：1 = X、2 = Y、3 = Z。DCB／ENF 範例的界面法向為全域 Y，因此填 2；不要與典型沿 Z 疊層的試片混淆。',
'FATCZM 忽略 *ORIENTATION。改變層片座標系不會旋轉界面法向。請使用平直、與全域軸對齊的線性 C3D8 界面，厚度方向只有一層元素；臂部另用實體元素。相鄰部分必須共用相容節點或建立正確的 tie，才能傳遞載重。',
'第 1 步將載重升到疲勞峰值；第 2 步維持載重，使用 *STATIC, DIRECT 與時間增量 1。若要 300 次跳躍，時間長度填 300，允許增量數至少 310。常數 14 = 2 指定第一個疲勞步。選 Spooles、關閉 Nlgeom（繁中顯示「尼吉姆」），並要求 SDV 輸出。',
'所有啟用的 FATCZM 材料，其常數 13、14、18 必須一致，因為循環帳務是全域的。啟用起始損傷機制時 DEPVAR 至少為 8。每個工作目錄只放一個疲勞分析，因為 fatczm_jumps.log 的檔名固定。'
],None),
('建立複材疊層',[
'先準備既有的平板、相容三維實體網格，每個層界面都須與元素面對齊。建立層片的工程常數材料。在「複材疊層」選建立，指定 part 或元素集、選直角座標系，通常保持「疊層軸 = 3」。',
'層表由下往上填：P01／IM7_8552／0.125 mm／45 度／1；再加入 P02 的 90 度、P03 的 -45 度與 P04 的 0 度。將「對稱」設為「是」，程式會接上完整反向列表；中心層會複製，展開後共有八層，總厚度 1 mm。',
'可用新增、插入、刪除、上移、下移、複製副本與反轉編輯表格。「元素」是選用的既有厚度網格帶數檢查，不會產生或細化網格。右側為示意疊層圖；選取一列可高亮該層元素。按確定前務必閱讀驗證摘要。'
], 'layup_zh-TW.png'),
('角度慣例與幾何檢查',[
'在本手冊建議的疊層軸 3 設定下，0 度為疊層座標系的「第 1 軸」。正角度遵循繞正向疊層軸的右手定則：在全域座標中，+30 度的纖維朝 +Y 偏轉。平移座標系原點不會改變方向向量。',
'進階的其他軸設定採循環基底：疊層軸 1 的零度方向是座標系第 2 軸；疊層軸 2 的零度方向是第 3 軸。若要維持本手冊「第 1 軸為零度」的說法，請用疊層軸 3 並適當定義直角座標系。輸出器會為展開後的每層建立 *ORIENTATION 與實體斷面。',
'疊層從區域節點在疊層軸投影的最小值開始，總厚度必須在容差內等於區域厚度。每個元素的所有節點都須落在同一層區間內。跨層、空層、歸屬不明、缺材料，以及與其他啟用疊層或一般實體斷面重疊，都會被拒絕。',
'64.387645 GPa 的 CLT 參考是由指定的 IM7/8552 層片常數與週期、平面應力膜單元計算，並非通用的複材模數。短自由邊界教學試片的表觀剛性不同；報告網格收斂時應比較相同幾何與邊界條件。'
],None),
('實際編輯的建議流程',[
'更動厚度、角度、材料或載重前先另存副本。每次只改一項，檢查有效性摘要與輸出的 CalculiX 輸入。需要基準時重開原始範例。求解器正常退出仍不足以證明模型正確，還須查看反力平衡、變形形狀、增量是否完成，以及網格與邊界條件的敏感度。',
'紅色驗證訊息常代表疊層與網格不一致。改層厚不會重建網格，須修改網格或恢復厚度。刪除材料或座標系後，缺少的參照會保留下來，供您修復。兩個啟用的疊層不能覆蓋相同元素。',
'若一般模型要改用原版後備求解器，在設定選 Solver/ccx_dynamic.exe 與它支援的矩陣解法，再建立或更新分析工作。僅適用不含 USERFATCZM 的模型；若既有工作設定了明確的 executable 覆寫，亦須編輯該工作。',
'本版可讀舊 .pmx，但未修改的舊 PrePoMax 無法讀取新 FATCZM／疊層型別。與舊版交換時保留原始檔；展平的 .inp 可以交換求解資料，但不能完整還原可編輯的疊層物件。'
],None),
('限制、問題回報與原始碼',[
'RC1 僅支援平板疊層及實體元素，不會自動產生疊層網格或插入界面，也不支援曲面疊層；FATCZM 不藉 *ORIENTATION 旋轉法向。層圖與元素高亮不代表局部纖維方向箭頭已完成。共用節點的應力圖可能跨層平均。',
'示範驗證的是軟體行為與數值重現性，alpha = 1 的循環尺度不是已校準的壽命預測。論文使用前須確認模型假設與材料校準。旋轉外觀、圖表可讀性與工程中文用語，仍適合再做人工觀感確認。',
'請向課程教師或研究助理 Isisam 回報：VERSION、Windows 版本、介面語言、重現步驟、最小 .pmx、匯出的 .inp、求解 .log／.sta／.cvg，以及相關 .dat 或 fatczm_jumps.log 列。說明預期與實際行為，移除私人幾何、個人路徑、憑證與無關檔案。',
'執行檔未簽章，防毒或 SmartScreen 可能將陌生版本列為可疑。先核對 SHA256SUMS；若檔案遭隔離，請洽課程發行者，不要全面關閉防護。PrePoMax-RCM 採 GPL-3.0，對應原始碼 git bundle 放在 zip 旁；CalculiX／FATCZM 的 GPL-2 原始碼位於 Source/ccx_fat。轉交時請保留 Licenses 與原始碼資訊。'
],None)]
def make(lang,pages,filename):
    target=OUT/filename;c=canvas.Canvas(str(target),pagesize=(W,H),pageCompression=1)
    c.setTitle('PrePoMax-RCM | '+('快速入門' if lang=='zh-TW' else 'Quick Start'))
    c.setAuthor('Isisam');c.setSubject('Student quick start, Windows RC1')
    for i,(title,texts,img) in enumerate(pages,1):
        c.setFillColor(navy);c.rect(0,H-116,W,116,fill=1,stroke=0)
        c.setFillColor(HexColor('#80d5d3'));c.setFont(FONT,10);c.drawString(44,H-35,'PREPOMAX-RCM  /  STUDENT GUIDE  /  0.1.0-RC1f')
        c.setFillColor(white);c.setFont(FONT,22);c.drawString(44,H-77,title)
        c.setFillColor(teal);c.roundRect(W-78,H-94,34,34,7,fill=1,stroke=0);c.setFillColor(white);c.setFont(FONT,14);c.drawCentredString(W-61,H-83,f'{i:02}')
        y=H-142
        for t in texts:y=para(c,t,y)
        if img:
            # Tall material editors are deliberately smaller than wide layup screens.
            y=picture(c,img,y,min(400,y-90))
            y=para(c,('本 RC 建置的原生控制項截圖；在隔離且從未切換顯示的桌面擷取。' if lang=='zh-TW' else 'Native controls from this RC build, captured on an isolated desktop never switched into view.'),y,True)
        if i==3:
            rows=([['範例','模型','主要檢查'],['DCB','DCB.pmx','60 jumps; N = 18860.013543'],['ENF','ENF.pmx','300 jumps; onset N = 624.92741'],['Layup','Layup.pmx','[45/90/-45/0]s; Ex = 60500.75 MPa']] if lang=='zh-TW' else [['Example','Model','Key check'],['DCB','DCB.pmx','60 jumps; N = 18860.013543'],['ENF','ENF.pmx','300 jumps; onset N = 624.92741'],['Layup','Layup.pmx','[45/90/-45/0]s; Ex = 60500.75 MPa']])
            y=table(c,rows,y,[72,90,W-250])
        c.setStrokeColor(HexColor('#d2dde3'));c.line(44,43,W-44,43)
        c.setFont(FONT,8);c.setFillColor(navy);c.drawString(44,28,'PrePoMax-RCM  |  Windows x64  |  Author: Isisam');c.drawRightString(W-44,28,f'{i} / 10')
        records.append(dict(language=lang,page=i,bottom_content=round(y,1)))
        c.showPage()
    c.save();print(target)
make('en',EN,'QuickStart_en.pdf');make('zh-TW',ZH,'快速入門_zh-TW.pdf')
(ROOT/'pdf_layout.json').write_text(json.dumps(records,indent=2))
