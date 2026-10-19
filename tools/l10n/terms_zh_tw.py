"""Owner-approved M2c presentation terminology (2026-10-19), applied offline."""
REPLACEMENTS = {
    '疲勞內聚區域模型 (FATCZM)': '疲勞內聚力模型（FATCZM）',
    '疲勞內聚區域模型': '疲勞內聚力模型（FATCZM）',
    '罰則': '罰函數', '振幅': '幅值曲線', '脫層': '分層', '裂縫': '裂紋',
    '負載': '載重', '包圍盒': '邊界框', '等值線': '等值圖',
    '重新劃分網格': '重新產生網格', '運動學': '運動耦合',
}

def normalize(text):
    for old, new in REPLACEMENTS.items():
        text = text.replace(old, new)
    return text.replace('等值圖圖', '等值圖').replace('運動耦合 耦合', '運動耦合').replace('運動耦合 聯軸器', '運動耦合')

PREVIEW = {
    '3D ply orientation': '3D 鋪層方向',
    'Reference: {0}': '參考座標：{0}',
    'Spacing not to scale': '層間距為示意比例',
    'Exploded plates with fibre directions in the layup coordinate system; spacing is not to scale.':
        '展開薄板顯示疊層局部座標中的纖維方向；層間距為示意比例。',
}
