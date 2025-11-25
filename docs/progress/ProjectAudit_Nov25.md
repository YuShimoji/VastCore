# VastCore プロジェクト総点検レポート (2025-11-25)

## 概要

地形生成エンジン開発の土台を盤石にするため、プロジェクト全体の総点検とリファクタリングを実施した。

---

## 1. 発見された問題と対処

### 1.1 空ファイル（0バイト）: 28件 → **削除完了**

実装が別の場所にあるプレースホルダー、または完全未実装のスタブファイルを削除。

**削除したファイル（主なもの）**:

- `Generation/Map/ArchitecturalGenerator.cs` → 実装は `Terrain/Map/` に存在
- `Generation/Map/BiomeSpecificTerrainGenerator.cs`
- `Generation/Map/CompoundArchitecturalGenerator.cs`
- `Generation/Map/PrimitiveMemoryManager.cs`
- `Generation/Map/PrimitiveTerrainObjectPool.cs`
- `Generation/GPU/GPUTerrainGenerator.cs`
- `Testing/DeformIntegrationTest.cs`
- その他多数（計28ファイル）

### 1.2 空フォルダ: 7件 → **削除完了**

- `Scripts/Utils`
- `Scripts/Generation/GPU`
- `Scripts/Generation/Optimization`
- `Scripts/Generation/Stubs`
- `Scripts/Player/Interaction`
- `Scripts/Terrain/Map/Editor`
- `Scripts/Utilities/Utils/Utilities`

### 1.3 ハードコーディング → **定数クラスに抽出**

`TerrainGenerationConstants.cs` を新規作成し、以下のマジックナンバーを定数化：

- 地形サイズデフォルト値（Width, Height, Depth, Resolution）
- ノイズ生成パラメータ（Scale, Octaves, Persistence, Lacunarity）
- ハイトマップ設定（Scale, Offset）
- ディテール設定（Resolution, Density, Distance）
- ツリー設定（Distance, Billboard距離など）
- プリミティブ生成設定（スケール、変形範囲、ノイズ強度）
- レイヤー名

### 1.4 TODOコメント: 28件（維持）

**対処方針**: レガシー/棚上げ分類に基づき、以下の方針で維持：

- **ProBuilder API変更による機能無効化** (`HighQualityPrimitiveGenerator.cs`): Phase 3 以降で対応
- **テスト系仮実装**: レガシー分類のため、現行フェーズでは修正しない
- **LOD未実装** (`PrimitiveTerrainObject.cs`): レガシー分類

### 1.5 重複ファイル（維持・監視）

`BlendSettings.cs` が2箇所に存在：

- `Generation/Map/BlendSettings.cs` (Vastcore.Generation.Map 名前空間)
- `Terrain/Map/BlendSettings.cs` (Vastcore.Terrain.Map 名前空間)

**対処方針**: 名前空間が異なるため即座のエラーにはならない。将来的な統合を検討。

---

## 2. リファクタリング実施内容

### 2.1 TerrainGenerator.cs

- デフォルト値を `TerrainGenerationConstants` から参照するよう変更
- ハードコーディングされていた数値（2048, 600, 513, 50f, 8, 0.5f, 2f など）を定数に置換
- レイヤー名 "Terrain" を定数に置換

### 2.2 PrimitiveTerrainGenerator.cs

- `PrimitiveGenerationParams.Default()` 内のデフォルト値を定数に置換
- `scale`, `deformationRange`, `noiseIntensity`, `subdivisionLevel` を定数化

---

## 3. 長大スクリプトの状況（200行超）

以下のファイルは単一責任の原則に反する可能性があるが、レガシー/棚上げ分類のため現行フェーズでは手を加えない：

| ファイル | 行数 | 分類 |
|---------|------|------|
| BiomeSpecificTerrainGenerator.cs | 1649 | Legacy |
| HighQualityPrimitiveGenerator.cs | 1447 | Legacy |
| CompoundArchitecturalGenerator.cs | 1252 | Shelved |
| ArchitecturalGenerator.cs | 1079 | Shelved |
| NaturalTerrainFeatures.cs | 943 | Shelved |
| AdvancedPrimitiveLODSystem.cs | 925 | Legacy |
| EnhancedClimbingSystem.cs | 912 | Active (別機能) |

---

## 4. プロジェクト構造の現状

```text
Assets/Scripts/
├── Camera/          # カメラ制御
├── Core/            # 基盤クラス、インターフェース
├── Deform/          # Deform連携（Shelved）
├── Editor/          # エディタ拡張
├── Game/            # ゲームマネージャ、UI
├── Generation/      # 地形生成の核（Active）
│   └── Map/         # プリミティブ生成、地形タイル
├── Player/          # プレイヤー制御
├── Terrain/         # 地形関連（Legacy/Shelved 混在）
│   └── Map/         # 高度な地形機能
├── Testing/         # テストスイート
├── UI/              # UI関連
└── Utilities/       # ユーティリティ
```

---

## 5. 次のステップ

### 5.1 Phase 0.5: TerrainGenerationProfile の作成

`TerrainGenerationV0_Spec.md` に基づき、ScriptableObject を実装：

- 生成モード（Noise/HeightMap/Both）
- サイズ・解像度
- HeightMap 設定
- Noise 設定

### 5.2 Phase 1: TerrainGenerationWindow(v0) の実装

- 既存 `HeightmapTerrainGeneratorWindow` をリファクタリング
- Profile との連携
- セクション分けされた UI

### 5.3 継続的な整理

- レガシー分類ファイルの段階的削除
- テストスイートの整備

---

## 6. コミット履歴

1. `refactor: Remove empty placeholder files and empty folders` - 空ファイル・空フォルダの削除
2. `refactor: Extract magic numbers to TerrainGenerationConstants` - 定数クラスの追加とハードコーディング排除

---

- **最終更新**: 2025-11-25
- **更新者**: Cascade (AI)
