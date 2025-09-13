# 移設用コアファイルリスト

## 移植すべき最小限のファイル（8ファイル）

### 1. ログシステム
- `Assets/Scripts/Core/VastcoreLogger.cs`
- 理由: 安定している、ファイルI/O最適化済み

### 2. セーブシステム  
- `Assets/Scripts/Core/SaveSystem.cs`
- 理由: 基本機能として必要

### 3. プレイヤー制御
- `Assets/Scripts/Player/Controllers/PlayerController.cs`
- 理由: 基本的な移動制御が実装済み

### 4. カメラ制御
- `Assets/Scripts/Player/Camera/CameraController.cs`
- 理由: プレイヤー追従機能が動作

### 5. ノイズ生成
- `Assets/Scripts/Generation/NoiseGenerator.cs`
- 理由: 地形生成の基盤、独立性が高い

### 6. メッシュ生成
- `Assets/Scripts/Terrain/Map/MeshGenerator.cs`
- 理由: 地形メッシュ構築に必要

### 7. UI管理
- `Assets/Scripts/UI/UIManager.cs`
- 理由: 基本的なUI制御

### 8. 地形生成（要改修）
- `Assets/Scripts/Generation/TerrainGenerator.cs`
- 注意: ハードコーディング部分の改修が必要

## 設定ファイル（必須）
```
ProjectSettings/
├── InputManager.asset
├── TagManager.asset
├── Physics.asset
├── QualitySettings.asset
└── TimeManager.asset
```

## 削除すべきファイル（エラーの原因）
- PrimitiveTerrainRule.cs（既に削除済み）
- CrystalStructureGenerator.cs（既に削除済み）
- BiomeDefinition関連（未定義）
- TerrainAlignmentSystem.cs（既に削除済み）

## 移設作業チェックリスト
- [ ] 新規Unityプロジェクト作成
- [ ] Unity Version Control設定
- [ ] コアファイル8個をコピー
- [ ] 名前空間を統一（Vastcore.*）
- [ ] ハードコーディング数値を設定ファイル化
- [ ] 依存関係を最小化
- [ ] コンパイル確認
- [ ] 基本動作テスト
