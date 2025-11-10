# RandomControl モダンUI設計ドキュメント

## 概要
Blender Shape KeysとSkyrim RaceMenuを参考にした、洗練されたスライダーベースのRandomization UIシステム。

## デザインコンセプト
- **BlenderライクなVertical/Horizontal Sliders**
- **RaceMenuライクなリアルタイムプレビュー**
- **カテゴリ別折りたたみ式セクション**
- **値の入力とスライダーの両対応**
- **直感的な操作性とプロフェッショナルな外観**

## Phase 2A: 基本スライダーUI実装

### Position Control Section
```
▼ Position Randomization                    [Reset] [Random]
│ X: [-5.0] ━●━━━━━━━ [+5.0] (Range: ±5.0)
│ Y: [-2.0] ━━━━●━━━━ [+2.0] (Range: ±2.0)
│ Z: [-5.0] ━●━━━━━━━ [+5.0] (Range: ±5.0)
│ 
│ ☑ Relative to Original Position
│ ☐ World Space Coordinates
│ 
│ Current: (2.3, -0.8, 1.7)
```

### Rotation Control Section  
```
▼ Rotation Randomization                    [Reset] [Random]
│ X: [  0°] ━━━━━●━━━━ [360°] (Pitch)
│ Y: [  0°] ━━━━━●━━━━ [360°] (Yaw)  
│ Z: [  0°] ━━━━━●━━━━ [360°] (Roll)
│
│ ☑ Use Euler Angles
│ ☐ Constrain to specific axes
│
│ Current: (45°, 180°, 90°)
```

### Scale Control Section
```
▼ Scale Randomization                       [Reset] [Random]  
│ Uniform: [0.5] ━━●━━━━━━ [2.0] (Scale: 0.85x)
│ 
│ ☐ Individual Axis Control
│   X: [0.8] ━━━●━━━━━ [1.2] (Width)
│   Y: [0.8] ━━━●━━━━━ [1.2] (Height) 
│   Z: [0.8] ━━━●━━━━━ [1.2] (Depth)
│
│ ☑ Maintain Proportions
│ ☐ Allow Negative Scaling
│
│ Current: (0.95, 0.95, 0.95)
```

## Phase 2B: 高度な機能

### リアルタイム3Dプレビュー
- 選択オブジェクトのミニ3Dビューア（150x150px）
- スライダー操作時の即座な変化表示
- ワイヤーフレーム/ソリッド表示切り替え
- カメラ角度の自動調整

### プリセットシステム
```
Presets: [Subtle Variation ▼] [Save] [Load] [Delete]

Built-in Presets:
- Subtle Variation    (±0.5 pos, ±15° rot, 0.9-1.1 scale)
- Dramatic Changes    (±5.0 pos, ±180° rot, 0.5-2.0 scale)  
- Architectural Details (±1.0 pos, ±45° rot, 0.8-1.2 scale)
- Organic Variation  (Curved distributions, natural ranges)
```

### バッチ処理パネル
```
▼ Batch Operations
│ Target: ● Selected Objects (3)
│        ○ Children of Selected  
│        ○ Objects with Tag: [________]
│
│ [Apply Randomization] [Preview Changes] [Undo Last]
│
│ Progress: ████████░░ 80% (8/10 objects)
```

### ランダムシード制御
```
▼ Randomization Settings
│ Seed: [12345   ] [Generate] [Copy] [Paste]
│ 
│ ☑ Use Custom Seed (Reproducible)
│ ☐ Time-based Seed (Always Different)
│
│ Distribution: [Normal ▼] [Linear] [Exponential] [Custom Curve]
```

## Phase 2C: 上級機能

### メッシュ変形システム
```
▼ Mesh Deformation (ProBuilder)
│ Noise Strength: [0.0] ━━●━━━━━━ [2.0]
│ Noise Scale:    [1.0] ━━━━●━━━━ [10.0] 
│ 
│ ☑ Preserve Volume
│ ☑ Smooth Transitions
│ ☐ Apply to Faces Only
```

### マテリアル/カラー制御
```
▼ Material Randomization  
│ Hue Shift:       [-180°] ━━━●━━━━━ [+180°]
│ Saturation:      [0.0] ━━━━●━━━━ [2.0]
│ Brightness:      [0.0] ━━━━●━━━━ [2.0]
│
│ ☑ Random Material from Palette
│ ☐ Generate Procedural Materials
```

## UI技術実装詳細

### UnityエディタGUI要素
- `EditorGUILayout.MinMaxSlider()` - 範囲スライダー
- `EditorGUILayout.Slider()` - 単一値スライダー  
- Custom `GUIStyle` - Blenderライクな外観
- `EditorGUILayout.Foldout()` - 折りたたみセクション
- `GUILayout.BeginHorizontal/Vertical()` - レイアウト制御

### カスタムGUIスキン
```csharp
private GUIStyle sliderStyle;
private GUIStyle labelStyle;  
private GUIStyle sectionHeaderStyle;

// Blender風の色調とフォント
Color backgroundColor = new Color(0.2f, 0.2f, 0.2f);
Color accentColor = new Color(0.4f, 0.6f, 1.0f);
```

### データ構造
```csharp
[System.Serializable]
public class RandomizationSettings
{
    public Vector3 positionMin, positionMax;
    public Vector3 rotationMin, rotationMax;  
    public Vector3 scaleMin, scaleMax;
    public bool useRelativePosition;
    public bool useUniformScaling;
    public int randomSeed;
    public DistributionType distribution;
}
```

## 開発マイルストーン

### Phase 2A (現在): 基本スライダーUI
- [ ] Position制御セクション実装
- [ ] Rotation制御セクション実装  
- [ ] Scale制御セクション実装
- [ ] 基本的な適用機能

### Phase 2B: 高度機能
- [ ] リアルタイムプレビュー
- [ ] プリセットシステム
- [ ] バッチ処理
- [ ] シード制御

### Phase 2C: 完成形
- [ ] メッシュ変形
- [ ] マテリアル制御
- [ ] ゲーム内UI移植
- [ ] パフォーマンス最適化

## 参考資料
- Blender Shape Keys UI: セクション折りたたみ、スライダーレイアウト
- Skyrim RaceMenu: リアルタイムプレビュー、直感的操作
- Unity ProBuilder: メッシュ編集統合
- Unity Editor GUI: カスタムスタイリング手法
