# REPORT: TASK_036 DualGrid Inspector Profile Preview

## メタデータ

- **タスクID**: TASK_036
- **Tier**: 2
- **ブランチ**: feature/TASK_036-dualgrid-inspector-preview
- **作成日**: 2026-02-12
- **完了日**: 2026-02-25
- **担当**: AI Agent (Cascade)

## 目的

Inspector から `TerrainGenerationProfile` を通じて DualGrid サンプリング設定をプレビュー生成フローに適用できるようにする。

## 実装内容

### 1. 変更ファイル

- `Assets/Scripts/Terrain/DualGrid/GridDebugVisualizer.cs`

### 2. 実装詳細

#### 2.1 TerrainGenerationProfile 参照の追加

`GridDebugVisualizer` に新しいシリアライズフィールドを追加:

```csharp
[Header("Profile Settings")]
[Tooltip("TerrainGenerationProfile を指定すると、DualGridHeightSamplingSettings が適用されます")]
[SerializeField] private TerrainGenerationProfile m_Profile;
```

#### 2.2 プロファイル駆動のサンプリング設定適用

`InitializeGrid()` メソッド内で、プロファイルが指定されている場合に `DualGridHeightSamplingSettings` を取得し、`VerticalExtrusionGenerator.GenerateFromHeightMap()` に渡すように変更:

```csharp
// プロファイルが指定されている場合は、DualGridHeightSamplingSettings を渡す
DualGridHeightSamplingSettings samplingSettings = m_Profile != null ? m_Profile.DualGridHeightSampling : null;

if (m_UseHeightMap && m_HeightMap != null)
{
    VerticalExtrusionGenerator.GenerateFromHeightMap(m_Grid, m_ColumnStack, m_HeightMap, m_MaxHeight, samplingSettings);
}
else
{
    VerticalExtrusionGenerator.GenerateFromNoise(m_Grid, m_ColumnStack, m_Seed, m_MaxHeight);
}
```

#### 2.3 フォールバック動作

- `m_Profile` が `null` の場合、`samplingSettings` は `null` となり、`VerticalExtrusionGenerator` はレガシー動作（固定 -10～10 レンジ）にフォールバックします
- ノイズ生成パス（`GenerateFromNoise`）は変更なし（プロファイル非対応）

### 3. アセンブリ依存関係

- `Vastcore.Terrain.asmdef` は既に `Vastcore.Generation` への参照を持っているため、追加の asmdef 変更は不要
- `using Vastcore.Generation;` を追加

## 検証結果

### コンパイル確認

```text
Unity Compile Check
Project Path: C:\Users\PLANNER007\VastCore\VastCore
✓ Compilation check passed.
```

### 動作確認項目

以下の動作が期待されます（手動テスト推奨）:

1. **プロファイル未指定時**:
   - Inspector で `m_Profile` を `null` のままにする
   - レガシー動作（固定 -10～10 レンジ、RoundToInt 量子化）で動作

2. **プロファイル指定時**:
   - `TerrainGenerationProfile` アセットを作成
   - `DualGridHeightSampling` 設定を調整（例: `WorldMinXZ`, `WorldMaxXZ`, `HeightQuantization`）
   - Inspector で `m_Profile` に割り当て
   - プロファイルの設定が反映された高さ生成が行われる

3. **HeightMap 使用時**:
   - `m_UseHeightMap` を `true` にし、`m_HeightMap` にテクスチャを割り当て
   - プロファイルのサンプリング設定が適用される

4. **Noise 使用時**:
   - `m_UseHeightMap` を `false` にする
   - ノイズ生成パスはプロファイル非対応のため、従来通りの動作

## DoD 達成状況

- [x] Inspector exposes profile assignment for debug visualizer
- [x] Assigned profile affects sampling behavior in preview generation path
- [x] Null profile keeps legacy behavior
- [x] Report created: `docs/04_reports/REPORT_TASK_036_DualGridInspectorProfilePreview.md`

## 使用方法

### 基本的な使用手順

1. **TerrainGenerationProfile の作成**:
   - Unity Editor で `Assets > Create > Vastcore > Terrain > Generation Profile` を選択
   - プロファイルアセットを作成

2. **DualGridHeightSampling 設定の調整**:
   - 作成したプロファイルを選択
   - Inspector で `DualGrid Height Sampling` セクションを展開
   - `UseProfileBounds`: `true` に設定
   - `WorldMinXZ`, `WorldMaxXZ`: ワールド座標範囲を設定
   - `UvAddressMode`: `Clamp` または `Wrap` を選択
   - `HeightQuantization`: `RoundToInt`, `FloorToInt`, `CeilToInt` から選択

3. **GridDebugVisualizer への適用**:
   - シーンに `GridDebugVisualizer` コンポーネントを持つ GameObject を配置
   - Inspector で `Profile Settings > Profile` に作成したプロファイルを割り当て
   - `Height Settings > Use Height Map` を有効化し、HeightMap テクスチャを割り当て

4. **プレビュー確認**:
   - Scene ビューで Gizmos が有効になっていることを確認
   - プロファイルの設定に応じた高さ生成が反映される

## 技術的メモ

### 設計判断

- **最小限の変更**: 既存の `VerticalExtrusionGenerator` API を活用し、オプショナルパラメータとして `samplingSettings` を渡す設計
- **後方互換性**: `null` チェックにより、プロファイル未指定時はレガシー動作を維持
- **スコープ制限**: ノイズ生成パスはプロファイル非対応のまま（将来の拡張余地を残す）

### 今後の拡張可能性

- ノイズ生成パスへのプロファイル適用（`NoiseScale`, `Octaves` 等のパラメータ統合）
- プロファイルからの `MaxHeight` 取得（現在は Inspector の個別フィールドを使用）
- リアルタイムプレビュー更新（`OnValidate` での自動再生成）

## 関連ドキュメント

- `docs/02_design/DUALGRID_HEIGHTMAP_PROFILE_MAPPING_SPEC.md`: DualGrid プロファイルマッピング仕様
- `docs/tasks/TASK_033_ProfileMappingPrimitives.md`: プロファイルマッピング基礎実装
- `docs/tasks/TASK_034_StaticReviewDualGridProfile.md`: 静的レビュー結果

## 停止条件の確認

- [x] Preview flow can switch between profile-driven and legacy behavior without code changes
- [x] No unresolved issues (all implementations completed successfully)

## まとめ

TASK_036 は正常に完了しました。`GridDebugVisualizer` に `TerrainGenerationProfile` 参照を追加し、プロファイルの `DualGridHeightSamplingSettings` を `VerticalExtrusionGenerator` に渡す実装を行いました。プロファイル未指定時はレガシー動作を維持し、指定時はプロファイル駆動のサンプリング設定が適用されます。コンパイルエラーなく、DoD のすべての項目を達成しています。
