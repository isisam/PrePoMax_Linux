"""Build an offline first-pass zh-TW catalogue from PUBLIC upstream UI literals.

Optional --translate uses Google's public translation endpoint for uncovered
literal text only, never files, paths, environment values or credentials.
The shipping application never uses this endpoint. Existing entries are reused.
"""
from pathlib import Path
from collections import Counter
import argparse, concurrent.futures, json, re, time, urllib.parse, urllib.request
from terms_zh_tw import normalize, PREVIEW
ROOT=Path(__file__).resolve().parents[2]
TERMS='''Mesh|網格
Part|零件
Material|材料
Section|斷面
Step|分析步
Boundary condition|邊界條件
Load|載重
Contact|接觸
Results|結果
Composite Layup|複材疊層
Ply|層
Finite element|有限元素
Node|節點
Element|元素
Node set|節點集
Element set|元素集
Surface|表面
Edge|邊線
Face|面
Vertex|頂點
Geometry|幾何
Model|模型
Assembly|組件
Coordinate system|座標系統
Reference point|參考點
Region|區域
Constraint|約束
Initial condition|初始條件
Defined field|預定義場
Field output|場輸出
History output|歷程輸出
Surface interaction|表面交互作用
Contact pair|接觸對
Amplitude|幅值曲線
Distribution|分佈
Analysis|分析
Solver|求解器
Pre-processing|前處理
Post-processing|後處理
Displacement|位移
Rotation|轉角
Velocity|速度
Acceleration|加速度
Force|力
Moment|力矩
Pressure|壓力
Stress|應力
Strain|應變
Young's modulus|楊氏模數
Poisson's ratio|卜松比
Shear modulus|剪切模數
Density|密度
Elasticity|彈性
Plasticity|塑性
Yield stress|降伏應力
Hardening|硬化
Engineering constants|工程常數
Lamina|單層板
Orthotropic|正交異向性
Isotropic|等向性
Anisotropic|異向性
Thickness|厚度
Stacking axis|疊層軸
Symmetry|對稱
Symmetric|對稱
Boundary tolerance|邊界容差
State variable|狀態變數
User material|使用者材料
Fatigue CZM|疲勞內聚力模型（FATCZM）
Cohesive|內聚
Damage|損傷
Fracture toughness|斷裂韌性
Energy release rate|能量釋放率
Crack|裂紋
Delamination|分層
Normal direction|法線方向
Right-hand rule|右手定則
Global|全域
Local|局部
Rectangular|直角
Cylindrical|圓柱
Spherical|球面
Plane stress|平面應力
Plane strain|平面應變
Axisymmetric|軸對稱
Solid|實體
Shell|殼
Beam|梁
Truss|桁架
Spring|彈簧
Rigid body|剛體
Kinematic|運動耦合
Distributing|分佈式
Master surface|主表面
Slave surface|從表面
Friction|摩擦
Friction coefficient|摩擦係數
Small sliding|小滑移
Overclosure|過閉合量
Gap clearance|間隙
Stiffness|勁度
Penalty|罰函數
Thermal conductivity|熱傳導係數
Thermal expansion|熱膨脹
Specific heat|比熱
Temperature|溫度
Heat flux|熱通量
Heat transfer coefficient|熱傳係數
Emissivity|放射率
Radiation|輻射
Convection|對流
Heat flow|熱流
Time increment|時間增量
Increment|增量
Time period|時間區間
Frequency|頻率
Buckling|挫屈
Eigenvalue|特徵值
Mode|模態
Damping|阻尼
Convergence|收斂
Nonlinear|非線性
Linear|線性
Static|靜態
Dynamic|動態
Steady state|穩態
Explicit|顯式
Implicit|隱式
Tetrahedron|四面體
Hexahedron|六面體
Quadrilateral|四邊形
Triangle|三角形
Wedge|楔形
Pyramid|角錐
Parabolic|二次
First order|一階
Second order|二階
Midside node|邊中節點
Mesh grading|網格尺寸漸變
Remeshing|重新產生網格
Mesh optimization|網格最佳化
Element quality|元素品質
Mesh size|網格尺寸
Bounding box|邊界框
Tolerance|容差
Relative error|相對誤差
Deformation|變形
Deformed|變形後
Undeformed|未變形
Contour|等值圖
Color spectrum|色譜
Legend|圖例
Annotation|註記
Viewport|視埠
Wireframe|線架
Section view|剖面視圖
Exploded view|爆炸視圖
Isometric view|等角視圖
Pan|平移視圖
Zoom|縮放
Unit system|單位系統
Length|長度
Area|面積
Volume|體積
Mass|質量
Energy|能量
Power|功率
Equation|方程式
Parameter|參數
Value|數值
Property|屬性
Data point|資料點
Selection|選取
Activate|啟用
Deactivate|停用
Duplicate|複製副本
Regenerate|重新產生
Import|匯入
Export|匯出
Settings|設定
General|一般
Language|語言
Work directory|工作目錄
File|檔案
Edit|編輯
View|檢視
Tools|工具
Help|說明
Result|結果
Load case|載重案例
Strain energy|應變能
Kinetic energy|動能
Contact energy|接觸能
Reaction force|反力
Slip wear|滑動磨耗
Wear coefficient|磨耗係數
Boundary layer|邊界層
Curvature|曲率
Facet|面片
Point|點
Axis|軸
Normal|法線
Vector|向量
Scalar|純量
Component|分量
Magnitude|大小
Phase|相位
Real|實部
Imaginary|虛部
Absolute zero|絕對零度
Conductance|熱傳導度
Film coefficient|薄膜熱傳係數
Tensile force|拉力
Support|支承
Hinge|鉸鏈
Weld|焊接
Joint|接頭
Surface traction|表面牽引力
Gravity load|重力載重
Time|時間
Name|名稱
Description|說明
Category|類別
Available|可用
Selected|已選取
Properties|屬性
Units|單位
Number|數量
Angle|角度
Coordinates|座標
Validation|驗證
Stack|疊層
Layer|層
Mesh Setup|網格設定
Mesh setup item|網格設定項目
Keyword editor|關鍵字編輯器
Keyword|關鍵字
Materials|材料
Parts|零件
Sections|斷面
Steps|分析步
Loads|載重
Contacts|接觸
Plies|層
Composite Layups|複材疊層'''
GLOSSARY={en: normalize(zh) for en, zh in (line.split('|',1) for line in TERMS.splitlines())}
MANUAL='''OK|確定
OK - New|確定－新增
Cancel|取消
Apply|套用
Close|關閉
Yes|是
No|否
None|無
All|全部
Add|新增
Insert|插入
Delete|刪除
Remove|移除
Move up|上移
Move down|下移
Reverse|反轉
New|新增
Open|開啟
Open Recent|最近開啟
Save|儲存
Save As|另存新檔
Exit|結束
Copy|複製
Cut|剪下
Paste|貼上
Undo|復原
Redo|重做
Create|建立
Rename|重新命名
Reset|重設
Reset All|全部重設
Show|顯示
Hide|隱藏
Show All|全部顯示
Hide All|全部隱藏
Show Only|僅顯示
Invert|反向
Expand All|全部展開
Collapse All|全部摺疊
Find|尋找
Search|搜尋
Run|執行
Stop|停止
Kill|終止
Monitor|監控
Ready|就緒
Warning|警告
Error|錯誤
Information|資訊
Continue|繼續
Next|下一步
Previous|上一步
First|第一個
Last|最後一個
More|更多
Less|較少
Default|預設
Automatic|自動
Custom|自訂
User defined|使用者定義
True|真
False|假
On|開啟
Off|關閉
Active|啟用中
Disabled|已停用
Internal|內部
External|外部
General|一般
BC|邊界條件
BCs|邊界條件
FATCZM preset|FATCZM 預設組
State variables (DEPVAR)|狀態變數 (DEPVAR)
Value / equation|數值／方程式
Value/Equation|數值／方程式
Unit / description|單位／說明
Fatigue CZM (FATCZM)|疲勞內聚力模型（FATCZM）
Layup properties|疊層屬性
Plies (bottom to top; Elements is an optional mesh-band count)|層（由下至上；元素數為選填的網格層數）
Ply stack (+ axis upward)|疊層示意（正軸向上）
Validation (updated after every edit)|驗證（每次編輯後更新）
Boundary tolerance|邊界容差
Stacking axis|疊層軸
Number of state variables. FATCZM needs at least 6; initiation needs at least 8.|狀態變數數量。FATCZM 至少需要 6 個；啟裂模型至少需要 8 個。
Edit the ordered constants on the Data points tab. Preset defaults use N, mm, MPa; convert manually for other units.|請在「資料點」頁籤依序編輯常數。預設值使用 N、mm、MPa；其他單位須自行換算。
Append the complete reversed ply list, including the centre ply.|附加完整反序層清單，包含中央層。
Axis normal to the plies. Positive angles follow the right-hand rule about this axis.|垂直於各層的軸；正角度依此軸的右手定則定義。
Rectangular coordinate system. Global uses the model X/Y/Z axes.|直角座標系統。「全域」使用模型的 X/Y/Z 軸。
Relative to total stack thickness; finite and in [0, 0.5). Does not repair a nonconforming mesh.|相對於疊層總厚度；須為有限數值且介於 [0, 0.5)。此設定不會修復不相容的網格。
Select the user interface language. Changes apply when settings are accepted.|選擇介面語言；確認設定後立即套用。
Angle [deg]|角度 [deg]
Thickness [{0}]|厚度 [{0}]
Thickness [|厚度 [
Unit system: |單位系統：
The table contains an invalid value: |表格含有無效數值：
No errors. The layup is valid.|無錯誤。疊層有效。
Bottom|底部
Top|頂部
Left|左側
Right|右側
Front|前方
Back|後方
Top view|俯視圖
Bottom view|仰視圖
Front view|前視圖
Back view|後視圖
Left view|左視圖
Right view|右視圖
Normal view|法線視圖
Vertical view|垂直視圖
Zoom to fit|縮放至適合大小
Direction|方向
Formulation|元素公式
Mechanical|機械
Thermal|熱學
Number format|數字格式
Number formatting|數字格式
Compression only|僅受壓
Gap Conductance|間隙熱傳導度
Surface Behavior|表面行為
Lamina|單層板
Plane-stress orthotropic elasticity defined by lamina constants.|以單層板常數定義平面應力正交異向性彈性。
Orthotropic elasticity defined by engineering constants.|以工程常數定義正交異向性彈性。
Elastic|彈性
Plastic|塑性
Max|最大值
Min|最小值
Maximum|最大值
Minimum|最小值
MeshAdapt|MeshAdapt
Mesh Setup|網格設定
Contact Pair|接觸對
Contact Pairs|接觸對
Node Sets|節點集
Element Sets|元素集
Field Outputs|場輸出
History Outputs|歷程輸出
Reference Points|參考點
Coordinate Systems|座標系統
Surface Interactions|表面交互作用
Initial Conditions|初始條件
Defined Fields|預定義場
Features|特徵
Analyses|分析
Data points|資料點
Material Library|材料庫
Material Models|材料模型
Interaction Models|交互作用模型
Transformation Types|變換類型
Element Quality|元素品質
Solid|實體
Node|節點
Nodes|節點
Element|元素
Elements|元素
Edge|邊線
Edges|邊線
Face|面
Faces|面
Surfaces|表面
Vertices|頂點
Hexahedrons|六面體
Hexahedra|六面體
Tetrahedra|四面體
Pyramids|角錐
Wedges|楔形
Triangles|三角形
Quadrilaterals|四邊形
Point coordinates|點座標
Center|中心
Center of mass|質心
Center of gravity|重心
Brightness|亮度
Saturation|飽和度
Color|色彩
Colors|色彩
Black|黑色
White|白色
Gray|灰色
Gradient|漸層
Appearance|外觀
Graphics|圖形
Navigation|導覽
Output|輸出
Progress|進度
Status|狀態
Status Block|狀態區塊
Parameters|參數
Property editor|屬性編輯器
File name|檔名
Folder|資料夾
Libraries|材料庫
Library Materials|材料庫中的材料
FE Model Materials|有限元素模型中的材料
Recombine|重組
Mapped|映射式
Mortar|Mortar
All flags|所有旗標
Harmonic|諧波
Number of processors|處理器數量
Number of threads|執行緒數量
Solution procedure|求解程序
Time Incrementation|時間增量控制
Enter the value.|輸入數值。
When the program starts open the last file Saved/Opened.|程式啟動時開啟上次儲存或開啟的檔案。
The name of the last file Saved/Opened.|上次儲存或開啟的檔名。
Save the results in the PrePoMax .pmx file.|將結果儲存在 PrePoMax .pmx 檔案中。
Select the .pmx file compression level.|選擇 .pmx 檔案的壓縮等級。
Select the default unit system for new models.|選擇新模型的預設單位系統。
Select yes to always show the amplitude, coordinate system and distribution selection properties.|選擇「是」以一律顯示幅值曲線、座標系統及分佈選取屬性。
Select yes to run existing history postprocessing commands from the results with the same file name.|選擇「是」以執行同名結果檔既有的歷程後處理命令。'''
OVERRIDES={**GLOSSARY,**dict(line.split('|',1) for line in MANUAL.splitlines())}
OVERRIDES.update(dict(line.split('|',1) for line in '''Running|執行中
Compute the value from the master surface: its centroid for the joint centre and, for a cylindrical surface, the direction perpendicular to all its face normals for the joint axis.|由主表面計算數值：以形心作為接頭中心；若為圓柱表面，則以垂直於所有面法線的方向作為接頭軸。
Currently, for a single part, only one active mesh setup item of the type: Shell gmsh, Tetrahedral gmsh, Transfinite mesh, Sweep mesh or Revolve mesh is possible.|目前每個零件只能啟用一項殼 gmsh、四面體 gmsh、超限映射網格、掃掠網格或旋轉掃掠網格設定。
Select the edge angle for the detection of model edges. The angle will be used for future imports.|選擇偵測模型邊線時使用的夾角；此角度套用於之後匯入的模型。
Sweep: every edge of Surface {0} is fixed and they carry {1} elements together, an odd number no quadrilateral mesh can have, and the fallback to gmsh is turned off.|掃掠：表面 {0} 的所有邊線均已固定，合計有 {1} 個元素。四邊形網格無法具有此奇數邊界元素數，且已關閉改用 gmsh 的備援選項。
The cell {0} cannot be meshed by tetrahedra: surface {1} carries elements of gmsh type {2}, and only linear triangles and quadrilaterals can bound the tetrahedra.|單元區塊 {0} 無法建立四面體網格：表面 {1} 包含 gmsh 類型 {2} 的元素；四面體網格的邊界僅能使用線性三角形及四邊形元素。
Delete cad part faces: the part {0} of the compound part {1} could not be imported as a shell part.|刪除 CAD 零件面：複合零件 {1} 的零件 {0} 無法匯入為殼零件。
Elements crossing a ply boundary: {0} (IDs {1}); outside the stack: {2} (IDs {3}); ambiguous/zero extent: {4} (IDs {5}).|跨越層邊界的元素：{0}（編號 {1}）；位於疊層外的元素：{2}（編號 {3}）；範圍不明確或為零的元素：{4}（編號 {5}）。
Sweep: the side Surface {0} is closed along one seam, its rims are split into several edges, and the straight edge splitting it from a vertex of one rim to the other would not stand across it at {1} degrees or more, or would leave it.|掃掠：側表面 {0} 沿一條接縫閉合，其邊界環分為數條邊線。由一個邊界環頂點連至另一邊界環的分割直邊，無法以至少 {1} 度橫跨表面，或會離開該表面。
Sweep: the side Surface {0} runs around the block, and {1} straight edge from a vertex of its source loop to the nearest vertex of its target loop stands across both loops at {2} degrees or more and lies on it; it is split into walls of four sides by {3} such edges or more.|掃掠：側表面 {0} 環繞區塊；從來源環頂點連至目標環最近頂點的 {1} 條直邊，位於該表面上，並以至少 {2} 度橫跨兩個環。至少使用 {3} 條此類邊線將其分割為四邊側面。
The cell {0} cannot be meshed by tetrahedra: the mesh of its boundary is not closed, an element edge on {1} lies on {2} surface elements instead of 2.|單元區塊 {0} 無法建立四面體網格：邊界網格未閉合，{1} 上的一條元素邊線連接 {2} 個表面元素，應為 2 個。
The geometry normals of the part {0} could not be mapped to {1} of {2} mesh nodes, the first one at ({3}, {4}, {5}). The mesh faces do not match the geometry faces.|零件 {0} 的幾何法線無法映射至 {2} 個網格節點中的 {1} 個；第一個節點位於 ({3}, {4}, {5})。網格面與幾何面不相符。
The mesh mirrored onto the part {0} does not have the faces of the part: its {1} faces lie on {2} of the {3} faces of the part{4}. Set the symmetry of the sweep mesh setup item to Off.|鏡射至零件 {0} 的網格與零件的面不相符：網格的 {1} 個面僅落在零件 {4} 共 {3} 個面中的 {2} 個面上。請關閉掃掠網格設定項目的對稱選項。
The nodes QuadWild placed on Edge {0} could not be placed on the edge.|QuadWild 配置於邊線 {0} 的節點無法定位至該邊線。
{0} of {1} stands at an angle of the revolution, but no edge of the source is turned onto it.|{1} 的 {0} 位於旋轉掃掠的某一角度，但沒有任何來源邊線旋轉至該位置。
Interaction|交互作用
Region type|區域類型
Element set name|元素集名稱
Node set name|節點集名稱
Part name|零件名稱
Surface name|表面名稱
True|是
False|否
Bottom → top|由下至上
Symmetric — dashed midplane|對稱－虛線為中面
New model|新增模型
Open file|開啟檔案
Save to file|儲存至檔案
Box zoom|框選縮放
No edges|不顯示邊線
Follower view|跟隨視圖
Color contours|色彩等值圖
Vector plot|向量圖
Transformation|變換
First increment|首個增量
Last increment|末個增量
Show undeformed wireframe model|顯示未變形線架模型
Show undeformed solid model|顯示未變形實體模型
Arrange annotations|排列註記
Remove annotations|移除註記
Hide all parts|隱藏所有零件
Invert visible parts|反轉零件顯示狀態
Capture viewport|擷取視埠
Capture Viewport|擷取視埠
Exploded view|爆炸視圖
Section view|剖面視圖
Set Selection|設定選取項目
Side Panels|側邊面板
Query|查詢
Clear Console|清除主控台
Automatic Color Spectrum Limits|自動色譜範圍
Show legend|顯示圖例
Use temperature dependent data points|使用隨溫度變化的資料點
Read-only CalculiX input file|唯讀 CalculiX 輸入檔
Export&&Close|匯出並關閉
Solid body|實體
Wireframe body|線架體
Time [s]|時間 [s]
Stress|應力
Plastic strain|塑性應變
Youngs modulus|楊氏模數
Poissons ratio|卜松比
Yield stress|降伏應力
Categorized|依類別排序
Alphabetical|依字母排序
Property Pages|屬性頁
ColumnHeader|類型
FrmProperties|屬性
FrmPropertyListView|屬性清單
Unit system: {0}|單位系統：{0}
Undo - {0}|復原－{0}
Redo - {0}|重做－{0}
Valid: {0} expanded plies, {1} elements. Ply boundaries coincide with element layers.|有效：展開後共 {0} 層、{1} 個元素。層邊界與元素層一致。
Item id|項目編號
Material name|材料名稱
Interaction name|交互作用名稱
Type|類型
Method|方法
Current|目前
First|首個
Last|末個
Number of angles|角度數量
Number of frames|影格數量
Increment step|增量間距
CalculiX keywords tree|CalculiX 關鍵字樹
Edit selected keyword|編輯所選關鍵字
Read-only CalculiX input file|唯讀 CalculiX 輸入檔
Model min: 0.0001|模型最小值：0.0001
SHIFT - add items|SHIFT－新增項目
CTRL - remove items|CTRL－移除項目
Finished|已完成
Failed with results|失敗但有結果
Set the number of slip wear smoothing steps using the node averaging method.|設定以節點平均法進行滑動磨耗平滑處理的步數。
Sew faces|縫合面
Shell edge|殼邊線
Shift amplitude|平移幅值曲線
Shift time|平移時間
Show Element Edges|顯示元素邊線
Show GUI switch can only be set to Yes or No.|顯示介面選項只能設為 Yes 或 No。
Show Model Edges|顯示模型邊線
Show Python|顯示 Python
Show all parts|顯示所有零件
Show all properties|顯示所有屬性
Show average value|顯示平均值
Show coordinates|顯示座標
Show edge/surface id|顯示邊線／表面編號
Show edge/surface size|顯示邊線／表面尺寸
No compression|不壓縮
Fastest|最快
Optimal|最佳
Ask|詢問
The preview of the mesh failed.|網格預覽失敗。
 again.| 再次。
along the revolution of|沿旋轉掃掠方向
and|與
Available entities|可用實體
Property|屬性
Name|名稱
Add category|新增類別
Frame|影格
Frames|影格
Count|數量
Column|欄
Rows|列
Row|列
Massless|無質量
Create Mesh|建立網格
Material Library Editor|材料庫編輯器
FE Model|有限元素模型
Edit Settings|編輯設定
Edit Material|編輯材料
Edit Boundary Condition|編輯邊界條件
Edit Load|編輯載重
Edit Section|編輯斷面
Edit Step|編輯分析步
User Material|使用者材料
Composite Layups|複材疊層
Reference Points|參考點
Mesh Types|網格類型
Angle|角度
Elasticity|彈性
Plasticity|塑性
Font|字型
Font size|字型大小
Return|返回
Clear|清除
Clear All|全部清除
Continue|繼續
Read only|唯讀
PlaneStrain|平面應變
PlaneStress|平面應力
ThreeD|三維
TwoD|二維
Multi-layered|多層
View|檢視'''.splitlines()))
# These are symbols, formats, program identifiers or model data, not English prose.
PROTECTED=set('FATCZM USERFATCZM DEPVAR SDV U RF NT PEEQ S E HFL RFL SOF SOM SOAREA CSTR CDIS CELS CNUM ENER ELSE ELKE EVOL HER ERR ZZS PHS PU PCON PNT DA DB DC DD DH DS IA IC ID IG IJ IL IP IR IS IT ME WG FPS UR1 UR2 UR3 Nx Ny Nz Dx Dy Dz AngleAtMax AngleAtMin Gmsh Netgen QuadWild MMG3D HXT RTree She Who'.split())
PROTECTED.update(['English','繁體中文','PrePoMax','PrePoMax v0.0.0','Arial','Calibri','Cambria','Consolas','Segoe UI','Tahoma','Verdana','Times New Roman','Cividis','Inferno','Viridis','Plasma','Jet','Turbo','Kindlmann','Hausdorff','Delaunay','Gauss','Shepard','Laplace 2D'])
term_variants={}
for en,zh in GLOSSARY.items():
    term_variants[en.lower()]=zh
    if not en.lower().endswith('s'):term_variants[en.lower()+'s']=zh
term_variants.update({'plies':'層','vertices':'頂點','hexahedra':'六面體','tetrahedra':'四面體','properties':'屬性','analyses':'分析','poissons ratio':'卜松比','youngs modulus':'楊氏模數'})
TERM_RE=re.compile(r'(?<![A-Za-z])(?:'+ '|'.join(re.escape(t) for t in sorted(term_variants,key=len,reverse=True))+r')(?![A-Za-z])',re.I)

def protected(text):
    t=text.strip()
    return (not t or t in PROTECTED or not re.search('[a-zA-Z]{2}',t) or
        bool(re.fullmatch(r'[A-Z0-9_]+',t)) or bool(re.fullmatch(r'[\w.-]+\.(?:inp|frd|pmx|stl|brep|vol|mesh|lib|bin|vis|msh|obj|txt|sharp|pmh|py)',t)) or
        bool(re.fullmatch(r'(?:[minkgtonlbf·s²/,°CF ]+)',t)) or
        bool(re.match(r'(?:linkLabel|menuStrip|statusStrip|toolStrip|frmSplash|Column\d|Comp\d|Solid-Part-)',t)) or
        t.startswith('Copyright') or t.startswith('https:') or t.startswith('http:'))

def draft_batch(batch):
    saved={};counter=[0]
    def token(value):
        key='ZXQ'+str(counter[0])+'QXZ';counter[0]+=1;saved[key]=value;return key
    lines=[]
    for index,source in batch:
        text=re.sub(r'\{\d+(?:[^}]*)\}|\r?\n|\[\?\]|\b(?:FATCZM|DEPVAR|USERFATCZM|CalculiX|PrePoMax|Gmsh|gmsh|Netgen|QuadWild|Python|VTK|MPa|mm|N|deg)\b',lambda m:token(m[0]),source)
        text=TERM_RE.sub(lambda m:token(term_variants[m[0].lower()]),text)
        lines.append('⟦'+str(index)+'⟧ '+text)
    query='\n'.join(lines)
    url='https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=zh-TW&dt=t&q='+urllib.parse.quote(query)
    req=urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'})
    for attempt in range(4):
        try:
            data=json.load(urllib.request.urlopen(req,timeout=45)); translated=''.join(row[0] or '' for row in data[0])
            matches=list(re.finditer(r'⟦\s*(\d+)\s*⟧',translated)); result={}
            for i,m in enumerate(matches):
                body=translated[m.end():matches[i+1].start() if i+1<len(matches) else len(translated)].strip()
                for key,value in saved.items():body=re.sub(re.escape(key),lambda m:value,body,flags=re.I)
                if re.search(r'ZXQ\s*\d+\s*QXZ',body,re.I):raise ValueError('Unrestored glossary token')
                # Taiwan-specific wording in the draft; terminology above is authoritative.
                for old,new in [('有限元','有限元素'),('默認','預設'),('質量矩陣','質量矩陣'),('視頻','影片'),('鼠標','滑鼠'),('文件','檔案'),('數據','資料'),('應用程序','應用程式'),('優化','最佳化'),('硬盤','硬碟'),('內存','記憶體'),('激活','啟用'),('泊松','卜松'),('楊氏模量','楊氏模數'),('剛度','勁度')]:body=body.replace(old,new)
                original=next(source for index,source in batch if index==int(m[1]))
                if Counter(re.findall(r'\{[0-9]+\}',original))!=Counter(re.findall(r'\{[0-9]+\}',body)):
                    raise ValueError('Translation changed format arguments')
                if re.search(r'ZXQ|QXZ|XZXZ|ZQZ|QZ[0-9]',body,re.I):raise ValueError('Corrupted glossary token')
                if original.count('\n')!=body.count('\n'):raise ValueError('Translation crossed a batch boundary')
                result[int(m[1])]=body
            if set(result)!=set(i for i,s in batch):raise ValueError('Batch marker mismatch')
            return result
        except Exception:
            if attempt==3:
                if len(batch)>1:
                    half=len(batch)//2
                    return {**draft_batch(batch[:half]),**draft_batch(batch[half:])}
                raise
            time.sleep(1+attempt*2)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--translate',action='store_true');args=parser.parse_args()
    inventory=json.loads((ROOT/'lang/strings.inventory.json').read_text(encoding='utf8'))
    out=ROOT/'lang/zh-TW.json'
    existing=json.loads(out.read_text(encoding='utf8')) if out.exists() else {}
    # Retain reviewed runtime/context entries that the literal scanner cannot infer.
    catalogue=dict(existing); pending=[]; excluded={}
    normalized={k.lower():v for k,v in OVERRIDES.items()}
    for index,source in enumerate(inventory):
        clean=source.strip()
        if clean in OVERRIDES:translated=OVERRIDES[clean]
        elif clean.lower() in normalized:translated=normalized[clean.lower()]
        elif protected(source):excluded[source]='symbol / format / identifier / model data';continue
        elif source in existing:catalogue[source]=existing[source];continue
        else:pending.append((index,source));continue
        catalogue[source]=source[:len(source)-len(source.lstrip())]+translated+source[len(source.rstrip()):]
    if args.translate and pending:
        batches=[];batch=[];size=0
        for item in pending:
            if batch and (size+len(item[1])>2100 or len(batch)>=15):batches.append(batch);batch=[];size=0
            batch.append(item);size+=len(item[1])
        if batch:batches.append(batch)
        sources=dict(pending)
        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as executor:
            futures={executor.submit(draft_batch,b):b for b in batches}
            for number,future in enumerate(concurrent.futures.as_completed(futures),1):
                try:
                    for index,translated in future.result().items():
                        source=sources[index]
                        catalogue[source]=source[:len(source)-len(source.lstrip())]+translated+source[len(source.rstrip()):]
                except Exception as error:print('BATCH ERROR',str(error),[i for i,s in futures[future]],flush=True)
                out.write_text(json.dumps(dict(sorted(catalogue.items())),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
                if number%10==0:print('Translated batches',number,'/',len(batches),flush=True)
    for key,value in OVERRIDES.items():catalogue.setdefault(key,value)
    catalogue['{0}\r\nSet element sizes and numbers of elements that agree on these edges, or switch off the transfinite meshing of the 3-sided and 4-sided surfaces.']='{0}\r\n請設定與這些邊線相容的元素尺寸與數量，或關閉三邊及四邊表面的超限映射網格功能。'
    catalogue['FrmAnimation.btnSaveMovieAs']='另存'
    catalogue['FrmMain.tsmiProperty']='屬性 (Property)'
    catalogue['FrmMain.tsmiInteraction']='交互作用 (Interaction)'
    catalogue['FrmMain.tsmiStepMenu']='分析步 (Step)'
    for key in ['File','Edit','View','Geometry','Model','Mesh','Results','Tools','Help','Analysis']:
        catalogue['FrmMain.tsmi'+key]=OVERRIDES.get(key,GLOSSARY.get(key,key))+' ('+key+')'
    for key in ['File','Edit','View','Tools','Help']:
        catalogue['FrmMain.tsmi'+key]=OVERRIDES[key]
    for english,chinese in [('Top view','俯視圖'),('Bottom view','仰視圖'),('Front view','前視圖'),('Back view','後視圖'),('Left view','左視圖'),('Right view','右視圖'),('Normal view','法線視圖'),('Vertical view','垂直視圖'),('Zoom to fit','縮放至適合大小')]:catalogue[english.title()]=chinese
    catalogue.update(PREVIEW)
    catalogue={key: normalize(value) for key, value in catalogue.items()}
    out.write_text(json.dumps(dict(sorted(catalogue.items())),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    missing=[source for source in inventory if source not in catalogue and source not in excluded]
    (ROOT/'lang/excluded.json').write_text(json.dumps(excluded,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    (ROOT/'lang/missing.json').write_text(json.dumps(missing,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    glossary='# PrePoMax 繁體中文工程術語表\n\n供 Lota 與project contributor審閱。適用臺灣工程教學；僅第一層選單以括號保留英文。\n\n'
    glossary+='「斷面」指材料／元素的 Section 指派；「剖面視圖」指 Section View。Ply 為單一層，Composite Layup 為完整複材疊層。\n'
    glossary+='主／從表面沿用既有 master/slave 定義；內部識別名稱、檔案格式、單位、數字、CalculiX 關鍵字及關鍵字編輯區均維持原樣。\n\n'
    glossary+='| English | 繁體中文 |\n| --- | --- |\n'+''.join('| '+a+' | '+b+' |\n' for a,b in list(GLOSSARY.items())[:150])
    glossary+='\n字庫為第一版翻譯初稿：術語與 FATCZM／複材疊層說明經人工指定，其他公開介面字串由翻譯服務產生並套用本表術語，仍需工程教學語境審閱。執行程式僅讀本機 JSON。\n'
    (ROOT/'lang/GLOSSARY_zh-TW.md').write_text(glossary,encoding='utf8')
    print('Catalogue',len(catalogue),'excluded',len(excluded),'missing',len(missing),'glossary',len(GLOSSARY),flush=True)
    return bool(missing)
if __name__=='__main__':raise SystemExit(main())
