# TASK_010/011 追加テスト解説ドキュメント

## 概要

TASK_010/011で追加された新機能（HeightMapChannel、Seed決定論、UV Offset/Tiling、InvertHeight）の動作を検証するため、**10個のテスト**を追加しました。

- **ユニットテスト**: `Assets/Tests/EditMode/HeightMapGeneratorTests.cs`（6テスト）
- **統合テスト**: `Assets/Tests/EditMode/TerrainGeneratorIntegrationTests.cs`（4テスト）

---

## テスト一覧

### カテゴリ1: HeightMapChannel（チャンネル選択）テスト

#### テスト1: `GenerateHeights_HeightMapMode_ChannelR_UsesRedChannel`
**目的**: Rチャンネルが正しく使用されることを検証

**検証内容**:
- `HeightMapChannel = R` に設定
- R=1.0、G=0.0、B=0.0、A=0.0 のテクスチャを作成
- 生成された高さ値に0.9以上の値が含まれることを確認

**期待結果**: Rチャンネルが使用されているため、高さ値は1.0に近い値になる

**実装箇所**: `HeightMapGeneratorTests.cs` (211-244行目)

---

#### テスト2: `GenerateHeights_HeightMapMode_ChannelG_UsesGreenChannel`
**目的**: Gチャンネルが正しく使用されることを検証

**検証内容**:
- `HeightMapChannel = G` に設定
- R=0.0、G=1.0、B=0.0、A=0.0 のテクスチャを作成
- 生成された高さ値に0.9以上の値が含まれることを確認

**期待結果**: Gチャンネルが使用されているため、高さ値は1.0に近い値になる

**実装箇所**: `HeightMapGeneratorTests.cs` (246-280行目)

---

### カテゴリ2: Seed決定論（再現性）テスト

#### テスト3: `GenerateHeights_NoiseMode_SameSeed_ProducesSameResult`
**目的**: 同一Seedで同一結果が生成されることを検証（決定論）

**検証内容**:
- `GenerationMode = Noise`
- `Seed = 12345` で1回目を生成
- 同じ `Seed = 12345` で2回目を生成
- 2回の結果が完全に一致することを確認

**期待結果**: 同一Seedなら、すべての高さ値が完全に一致する

**実装箇所**: `HeightMapGeneratorTests.cs` (282-305行目)

**重要**: このテストは**決定論（Determinism）**を保証する。ゲームで地形を再現可能にするために必須。

---

#### テスト4: `GenerateHeights_NoiseMode_DifferentSeed_ProducesDifferentResult`
**目的**: 異なるSeedで異なる結果が生成されることを検証

**検証内容**:
- `GenerationMode = Noise`
- `Seed = 11111` で1回目を生成
- `Seed = 99999` で2回目を生成
- 2回の結果に少なくとも1つの値が異なることを確認

**期待結果**: 異なるSeedなら、少なくとも1つの高さ値が異なる

**実装箇所**: `HeightMapGeneratorTests.cs` (307-332行目)

**重要**: このテストは**Seedの有効性**を保証する。異なるSeedで異なる地形が生成されることを確認。

---

### カテゴリ3: UV Tiling（テクスチャ繰り返し）テスト

#### テスト5: `GenerateHeights_HeightMapMode_UVTiling_AppliesTiling`
**目的**: UV Tilingが正しく適用されることを検証

**検証内容**:
- `GenerationMode = HeightMap`
- `UVTiling = (2.0, 2.0)` に設定（2倍の繰り返し）
- 左上のみ白（Color.white）、他は黒（Color.black）のテクスチャを作成
- 生成された高さ値に、0.5以上の値が**複数箇所**で現れることを確認

**期待結果**: UVTiling=2.0 なので、テクスチャが2x2で繰り返される。そのため、複数の位置で高値が現れる（最低でも2箇所以上）

**実装箇所**: `HeightMapGeneratorTests.cs` (334-375行目)

**重要**: このテストは**テクスチャの繰り返し機能**を検証。小さいテクスチャを大きな地形に適用する際に重要。

---

### カテゴリ4: InvertHeight（高さ反転）テスト

#### テスト6: `GenerateHeights_HeightMapMode_InvertHeight_InvertsHeights`
**目的**: InvertHeightが正しく高さを反転することを検証

**検証内容**:
- `GenerationMode = HeightMap`
- グラデーションテクスチャを作成（0.0 ~ 1.0 の値）
- `InvertHeight = false` で1回目を生成
- `InvertHeight = true` で2回目を生成
- 2回の結果が異なることを確認

**期待結果**: InvertHeight=true の場合、高さの分布が変化する（完全に反転するわけではないが、分布は変化）

**実装箇所**: `HeightMapGeneratorTests.cs` (377-420行目)

**重要**: このテストは**高さ反転機能**を検証。地形の高低を反転させる機能が正しく動作することを確認。

---

### カテゴリ5: 統合テスト（実際のTerrain生成）

#### テスト7: `GenerateTerrain_NoiseMode_WithSeed_ProducesDeterministicResult`
**目的**: 実際のTerrain生成でSeed決定論が機能することを検証

**検証内容**:
- `GenerationMode = Noise`
- `Seed = 12345` で1回目のTerrainを生成
- Terrainを破棄し、同じ `Seed = 12345` で2回目のTerrainを生成
- 2回のTerrainDataの高さ値が完全に一致することを確認

**期待結果**: 同一Seedなら、実際に生成されたTerrainの高さ値も完全に一致する

**実装箇所**: `TerrainGeneratorIntegrationTests.cs` (140-178行目)

**重要**: ユニットテスト（テスト3）とは異なり、**実際のTerrain生成パイプライン全体**で決定論が機能することを検証。

---

#### テスト8: `GenerateTerrain_HeightMapMode_WithChannel_AppliesChannelSelection`
**目的**: 実際のTerrain生成でHeightMapChannelが機能することを検証

**検証内容**:
- `GenerationMode = HeightMap`
- R=1.0、G=0.0、B=0.0 のテクスチャを作成
- `HeightMapChannel = R` に設定
- Terrainを生成し、高さ値が高い（TerrainData.size.y * 0.5以上）箇所が存在することを確認

**期待結果**: Rチャンネルが使用されているため、実際のTerrainも高い地形が生成される

**実装箇所**: `TerrainGeneratorIntegrationTests.cs` (180-222行目)

**重要**: ユニットテスト（テスト1）とは異なり、**実際のTerrain生成パイプライン全体**でチャンネル選択が機能することを検証。

---

#### テスト9: `GenerateTerrain_HeightMapMode_WithInvertHeight_InvertsTerrain`
**目的**: 実際のTerrain生成でInvertHeightが機能することを検証

**検証内容**:
- `GenerationMode = HeightMap`
- グラデーションテクスチャを作成
- `InvertHeight = false` で1回目のTerrainを生成し、最大高さを記録
- Terrainを破棄し、`InvertHeight = true` で2回目のTerrainを生成し、最大高さを記録
- 2回の最大高さが異なることを確認

**期待結果**: InvertHeight=true の場合、高さの分布が変化する

**実装箇所**: `TerrainGeneratorIntegrationTests.cs` (224-285行目)

**重要**: ユニットテスト（テスト6）とは異なり、**実際のTerrain生成パイプライン全体**で高さ反転が機能することを検証。

---

#### テスト10: `GenerateTerrain_CombinedMode_WithNewFeatures_WorksCorrectly`
**目的**: 複合モード（NoiseAndHeightMap）で新機能がすべて正しく動作することを検証

**検証内容**:
- `GenerationMode = NoiseAndHeightMap`
- `HeightMapChannel = Luminance`
- `Seed = 54321`
- `UVTiling = (2.0, 2.0)`
- `InvertHeight = false`
- すべての新機能を同時に使用してTerrainを生成
- Terrainが正常に生成されることを確認（エラーなく、解像度が一致）

**期待結果**: 複合モードで新機能をすべて使用しても、Terrainが正常に生成される

**実装箇所**: `TerrainGeneratorIntegrationTests.cs` (287-319行目)

**重要**: このテストは**統合テスト**として、複数の新機能が同時に動作することを検証。実際の使用シーンに近い。

---

## テスト実行手順

### Unity Editor上での実行

1. **Test Runnerウィンドウを開く**
   - `Window > General > Test Runner`

2. **EditModeタブを選択**
   - Test Runnerウィンドウの上部で「EditMode」タブをクリック

3. **テストクラスを展開**
   - `Vastcore.Tests.EditMode.HeightMapGeneratorTests` を展開
   - `Vastcore.Tests.EditMode.TerrainGeneratorIntegrationTests` を展開

4. **個別実行**
   - 各テスト名の左側のチェックボックスをクリックして選択
   - 「Run Selected」ボタンをクリック

5. **一括実行**
   - 「Run All」ボタンをクリック（すべてのEditModeテストが実行される）

### 実行結果の確認

- **緑色のチェックマーク**: テスト成功
- **赤色のXマーク**: テスト失敗（詳細はクリックして確認）
- **実行時間**: 各テストの実行時間が表示される

### 期待される実行時間

- **ユニットテスト（テスト1-6）**: 各テスト約0.1-0.5秒
- **統合テスト（テスト7-10）**: 各テスト約0.5-2.0秒（Terrain生成のため）

**合計**: 約5-10秒（すべてのテストを実行した場合）

---

## テストの重要性

### 回帰防止

これらのテストは、今後の変更で既存機能が壊れないことを自動検証します。

**例**: 
- 新しい機能を追加した際、Seed決定論が壊れていないか確認
- リファクタリング後、HeightMapChannelが正しく動作するか確認

### ドキュメントとしての役割

テストコードは、各機能の**期待される動作**を明確に示します。

**例**:
- テスト3を見れば、「同一Seedで同一結果が生成される」ことが明確
- テスト5を見れば、「UVTiling=2.0でテクスチャが2x2で繰り返される」ことが明確

### デバッグ支援

テストが失敗した場合、どの機能が壊れているかがすぐに分かります。

**例**:
- テスト3が失敗 → Seed決定論が壊れている
- テスト1が失敗 → Rチャンネル選択が壊れている

---

## トラブルシューティング

### テストが失敗する場合

1. **コンパイルエラーの確認**
   - Unity EditorのConsoleでエラーを確認
   - `Assets/Tests/EditMode/` 配下のファイルに構文エラーがないか確認

2. **テストデータの確認**
   - テクスチャが正しく作成されているか確認
   - `Texture2D` の `Apply()` が呼ばれているか確認

3. **テスト環境の確認**
   - `SetUp()` と `TearDown()` が正しく実行されているか確認
   - 前のテストの残骸が残っていないか確認

### よくある問題

**問題**: テスト7が失敗する（Seed決定論が機能しない）

**原因**: `HeightMapGenerator.GenerateFromNoise` でSeedが正しく適用されていない可能性

**対処**: `HeightMapGenerator.cs` の `GetDeterministicOffsetFromSeed` メソッドを確認

---

**問題**: テスト5が失敗する（UVTilingが機能しない）

**原因**: `HeightMapGenerator.GenerateFromHeightMap` でUVTilingが正しく適用されていない可能性

**対処**: `HeightMapGenerator.cs` の `GenerateFromHeightMap` メソッドでUVTilingの適用を確認

---

## 関連ドキュメント

- **テスト計画**: `docs/terrain/V01_TestPlan.md`
- **仕様書**: `docs/terrain/TerrainGenerationV0_Spec.md`
- **TASK_010**: `docs/tasks/TASK_010_TerrainGenerationWindow_v0_FeatureParity.md`
- **TASK_011**: `docs/tasks/TASK_011_HeightMapGenerator_Determinism_Channel_UV.md`

---

**最終更新**: 2026-01-04  
**作成者**: Orchestrator (Cursor)
