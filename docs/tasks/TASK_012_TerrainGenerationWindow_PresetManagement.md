# TASK_012: TerrainGenerationWindow プリセット管理機能

Status: OPEN  
Tier: 2（機能改善 / 既存挙動維持を優先）  
Branch: `feature/TASK_012_terrain-window-preset-management`  
Owner: Worker  

## 背景 / 目的

現状、`TerrainGenerationWindow` は `TerrainGenerationProfile` を使用して設定を保存・読み込みできるが、**よく使う設定を素早く適用する仕組みが不足している**。

本タスクは、Unity Editor上で「よく使う地形設定をプリセットとして保存し、ワンクリックで適用できる」機能を追加することで、**ユーザー体験を大幅に向上**させる。

### ユースケース

- 「山岳地形」「平原地形」「海岸地形」などのプリセットを事前に作成
- プリセットを選択して即座に設定を適用
- 現在の設定を新しいプリセットとして保存

## 参照（SSOT）

- SSOT: `docs/Windsurf_AI_Collab_Rules_latest.md`
- Spec: `docs/terrain/TerrainGenerationV0_Spec.md`
- Editor: `Assets/Scripts/Editor/TerrainGenerationWindow.cs`
- Profile: `Assets/Scripts/Generation/TerrainGenerationProfile.cs`

## Focus Area（変更してよい範囲）

- `Assets/Scripts/Editor/TerrainGenerationWindow.cs`
- `Assets/Scripts/Generation/TerrainGenerationProfile.cs`（必要最小限）
- `Assets/Scripts/Editor/TerrainPresetManager.cs`（新規作成）

## Forbidden Area（変更禁止）

- `Assets/MapGenerator/Scripts/TerrainGenerator.cs`（ランタイムロジックは変更しない）
- `Assets/MapGenerator/Scripts/HeightMapGenerator.cs`（ランタイムロジックは変更しない）
- 既存のテストコード（新規テストは追加可）

## Constraints / DoD

### 必須要件

1. **プリセット保存機能**
   - 現在の設定を新しいプリセットとして保存
   - プリセット名を指定可能
   - `TerrainGenerationProfile` をアセットとして保存

2. **プリセット読み込み機能**
   - 保存済みプリセットの一覧表示
   - プリセットを選択して即座に設定を適用
   - ウィンドウ内のすべての設定項目が更新される

3. **プリセット管理UI**
   - `TerrainGenerationWindow` に「Presets」セクションを追加
   - プリセット一覧（ドロップダウンまたはリスト）
   - 「Save as Preset」ボタン
   - 「Load Preset」ボタン
   - 「Delete Preset」ボタン（オプション）

4. **プリセット保存先**
   - `Assets/TerrainPresets/` フォルダに保存（存在しない場合は自動作成）
   - ファイル名: `TerrainPreset_{PresetName}.asset`

### 非機能要件

- 既存の `TerrainGenerationProfile` 機能との互換性を維持
- プリセット保存時に既存の設定が上書きされない（新しいアセットとして保存）
- エラーハンドリング: プリセット読み込み失敗時に適切なエラーメッセージを表示

### テスト要件

- 手動検証: Unity Editor上でプリセット保存・読み込みが正常に動作することを確認
- 既存テストがすべて成功することを確認

## 停止条件

- `TerrainGenerationProfile` の構造を変更する必要がある場合（互換性維持のため）
- プリセット保存先フォルダの作成に失敗する場合（権限エラー等）

## 実装方針

### Phase 1: プリセット保存機能

1. `TerrainGenerationWindow` に「Save as Preset」ボタンを追加
2. プリセット名入力ダイアログを表示
3. 現在の設定を `TerrainGenerationProfile` に保存
4. `Assets/TerrainPresets/` フォルダにアセットとして保存

### Phase 2: プリセット読み込み機能

1. `Assets/TerrainPresets/` フォルダからすべてのプリセットを検索
2. プリセット一覧をドロップダウンまたはリストで表示
3. プリセット選択時に設定を読み込み、ウィンドウ内のすべての設定項目を更新

### Phase 3: UI改善

1. 「Presets」セクションを `TerrainGenerationWindow` に追加
2. プリセット一覧、保存ボタン、読み込みボタンを配置
3. フォルダ構造の可視化（オプション）

## 関連タスク

- TASK_010: TerrainGenerationWindow v0 機能改善（完了）
- TASK_011: HeightMapGenerator 改善（完了）

## 備考

- プリセット管理機能は将来的に `TerrainTemplateEditor` と統合する可能性がある
- プリセットの共有機能（エクスポート/インポート）は将来の拡張として検討
