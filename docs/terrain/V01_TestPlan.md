# V01 TerrainGenerationWindow テスト計画

## 概要

TerrainGenerationWindow(v0) と TerrainGenerationProfile の動作確認テスト計画。
手動テストを中心に、V01 基盤の安定性を検証する。

---

## テスト対象

### コンポーネント

1. **TerrainGenerationWindow** (`Assets/Scripts/Editor/TerrainGenerationWindow.cs`)
   - メニュー: `Tools > Vastcore > Terrain > Terrain Generation (v0)`
2. **TerrainGenerationProfile** (`Assets/Scripts/Generation/TerrainGenerationProfile.cs`)
   - アセット作成: `Create > Vastcore > Terrain > Generation Profile`
3. **TerrainGenerator** (`Assets/MapGenerator/Scripts/TerrainGenerator.cs`)
4. **HeightMapGenerator** (`Assets/MapGenerator/Scripts/HeightMapGenerator.cs`)

---

## テストシナリオ

### 1. Window 起動テスト

| ID | 項目 | 手順 | 期待結果 |
|----|------|------|----------|
| W-01 | Window 起動 | `Tools > Vastcore > Terrain > Terrain Generation (v0)` を選択 | Window が正常に開く |
| W-02 | 複数起動防止 | 同じメニューを再度選択 | 既存 Window にフォーカスが移動 |
| W-03 | 再コンパイル後 | スクリプト変更後 Window を再度開く | エラーなく開く |

### 2. Terrain 生成テスト (Noise モード)

| ID | 項目 | 手順 | 期待結果 |
|----|------|------|----------|
| N-01 | 基本生成 | Noise モード選択 → Generate Terrain クリック | Terrain オブジェクトが生成される |
| N-02 | サイズ変更 | Width/Length を 512 に変更 → 生成 | 指定サイズの Terrain が生成 |
| N-03 | 解像度変更 | Resolution を 257 に変更 → 生成 | 指定解像度で生成 |
| N-04 | ノイズパラメータ | Scale=100, Octaves=4 に変更 → 生成 | 地形形状が変化 |
| N-05 | シード変更 | Randomize Seed → 生成 | 異なる地形が生成 |

### 3. Terrain 生成テスト (HeightMap モード)

| ID | 項目 | 手順 | 期待結果 |
|----|------|------|----------|
| H-01 | HeightMap 生成 | HeightMap モード選択 → HeightMap 設定 → 生成 | HeightMap に基づく Terrain 生成 |
| H-02 | HeightMap 未設定 | HeightMap を null で生成試行 | 警告表示、フラット Terrain 生成 |
| H-03 | HeightScale 変更 | HeightScale = 2.0 で生成 | 高さが倍増 |

### 4. Terrain 生成テスト (Combined モード)

| ID | 項目 | 手順 | 期待結果 |
|----|------|------|----------|
| C-01 | Combined 生成 | NoiseAndHeightMap モード → 生成 | Noise と HeightMap がブレンド |

### 5. Profile 操作テスト

| ID | 項目 | 手順 | 期待結果 |
|----|------|------|----------|
| P-01 | Profile 作成 | Assets 右クリック → Create > Vastcore > Terrain > Generation Profile | Profile アセット作成 |
| P-02 | Profile 読み込み | Window で Profile を選択 → Load Profile | UI に Profile 設定が反映 |
| P-03 | Profile 保存 | 設定変更 → Save to Profile | Profile に変更が保存 |
| P-04 | 新規 Profile 作成 | Create New Profile ボタン | 新規 Profile ダイアログ表示 |
| P-05 | Profile リセット | Profile インスペクタで Reset to Defaults | デフォルト値に復帰 |

### 6. Context セクションテスト

| ID | 項目 | 手順 | 期待結果 |
|----|------|------|----------|
| X-01 | Target Terrain 選択 | シーン内 Terrain を選択 | Window に Terrain が表示 |
| X-02 | Target Terrain クリア | Clear ボタン | Target Terrain が null に |
| X-03 | 新規 Terrain 作成 | Create New Terrain ボタン | 新規 Terrain GameObject 作成 |

### 7. バリデーションテスト

| ID | 項目 | 手順 | 期待結果 |
|----|------|------|----------|
| V-01 | 不正サイズ | Width = 0 で生成試行 | エラーメッセージ、生成中止 |
| V-02 | 不正解像度 | Resolution = 100（非推奨値）で生成 | 警告表示、最寄り値に補正 |
| V-03 | HeightMap なし (HeightMap モード) | HeightMap モードで HeightMap 未設定 | 警告表示 |

---

## エラーハンドリング確認

| ID | シナリオ | 期待結果 |
|----|----------|----------|
| E-01 | 生成中にスクリプトエラー | 例外がキャッチされ、Console にログ出力 |
| E-02 | Profile 保存先不正 | 保存失敗時に警告表示 |
| E-03 | HeightMap 読み込みエラー | テクスチャ読み込み失敗時に警告 |

---

## パフォーマンス確認

| ID | 項目 | 基準 |
|----|------|------|
| PF-01 | 2048x2048 Terrain 生成時間 | 10秒以内 |
| PF-02 | UI 応答性 | パラメータ変更時にラグなし |
| PF-03 | メモリ使用量 | 生成後にメモリリークなし |

---

## テスト実行チェックリスト

```markdown
## テスト実行記録

日付: ____-__-__
実行者: __________
Unity バージョン: ____

### 結果サマリー

- [ ] W-01 ~ W-03: Window 起動
- [ ] N-01 ~ N-05: Noise 生成
- [ ] H-01 ~ H-03: HeightMap 生成
- [ ] C-01: Combined 生成
- [ ] P-01 ~ P-05: Profile 操作
- [ ] X-01 ~ X-03: Context 操作
- [ ] V-01 ~ V-03: バリデーション
- [ ] E-01 ~ E-03: エラーハンドリング
- [ ] PF-01 ~ PF-03: パフォーマンス

### 発見した問題

1. 
2. 
3. 
```

---

## 自動テスト計画 (EditMode)

### 現状

既存テスト34ファイル中、ほとんどがLegacy機能のテスト。V01 Core 専用テストが不足。

### 追加予定テスト

#### 1. TerrainGenerationProfileTests.cs

**場所**: `Assets/Scripts/Tests/Editor/TerrainGenerationProfileTests.cs`

| ID | テスト名 | 検証内容 |
|----|----------|----------|
| TP-01 | Profile_CanBeCreated | ScriptableObject.CreateInstance で作成可能 |
| TP-02 | Profile_DefaultValues | デフォルト値が TerrainGenerationConstants と一致 |
| TP-03 | Profile_ResetToDefaults | Reset 後にデフォルト値に復帰 |
| TP-04 | Profile_Validate_ValidParams | 有効なパラメータで true 返却 |
| TP-05 | Profile_Validate_InvalidWidth | 不正な Width で false 返却 |
| TP-06 | Profile_Validate_InvalidResolution | 不正な Resolution で false 返却 |

#### 2. HeightMapGeneratorTests.cs

**場所**: `Assets/Scripts/Tests/Editor/HeightMapGeneratorTests.cs`

| ID | テスト名 | 検証内容 |
|----|----------|----------|
| HM-01 | GenerateHeightMap_ReturnsCorrectSize | 指定サイズの配列が返る |
| HM-02 | GenerateHeightMap_ValuesInRange | 値が 0-1 の範囲内 |
| HM-03 | GenerateHeightMap_SeedReproducibility | 同一シードで同一結果 |
| HM-04 | GenerateHeightMap_DifferentSeeds | 異なるシードで異なる結果 |

#### 3. TerrainGeneratorIntegrationTests.cs

**場所**: `Assets/Scripts/Tests/Editor/TerrainGeneratorIntegrationTests.cs`

| ID | テスト名 | 検証内容 |
|----|----------|----------|
| TG-01 | GenerateTerrain_CreatesTerrainData | TerrainData が生成される |
| TG-02 | GenerateTerrain_AppliesHeightMap | HeightMap が正しく適用 |
| TG-03 | GenerateTerrain_AppliesTextures | テクスチャが適用される |
| TG-04 | GenerateTerrain_NoiseMode | Noise モードで正常生成 |
| TG-05 | GenerateTerrain_HeightMapMode | HeightMap モードで正常生成 |

### 実装優先順位

1. **High**: TerrainGenerationProfileTests (TP-01 ~ TP-06)
2. **Medium**: HeightMapGeneratorTests (HM-01 ~ HM-04)
3. **Low**: TerrainGeneratorIntegrationTests (TG-01 ~ TG-05)

---

## 関連ドキュメント

- [TerrainGenerationV0_Spec.md](./TerrainGenerationV0_Spec.md) - V0 仕様書
- [Handover_Nov20.md](../progress/Handover_Nov20.md) - 申し送りノート
- [ProjectAudit_Nov25.md](../progress/ProjectAudit_Nov25.md) - 監査レポート

---

- **作成日**: 2025-11-26
- **作成者**: Cascade (AI)
