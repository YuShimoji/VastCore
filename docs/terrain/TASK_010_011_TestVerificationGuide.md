# TASK_010/011 テスト確認手順ガイド

## 概要

このドキュメントは、追加した10個のテストを**実際に確認するための詳細な手順**を説明します。

- **Unity Editor上での確認方法**（GUI操作）
- **コマンドラインでの確認方法**（自動化・CI/CD対応）
- **テスト結果の読み方**

---

## 方法1: Unity Editor上での確認（推奨：初回確認）

### ステップ1: Unity Editorを起動

1. Unity Hubを起動
2. プロジェクト `VastCore` を開く
3. Unity Editorが完全に起動するまで待つ（コンパイル完了を確認）

### ステップ2: Test Runnerウィンドウを開く

1. Unity Editorのメニューバーから `Window > General > Test Runner` を選択
2. Test Runnerウィンドウが開きます

**ウィンドウの構成**:
```
┌─────────────────────────────────────┐
│ Test Runner                         │
├─────────────────────────────────────┤
│ [EditMode] [PlayMode] [Both]       │ ← タブ
├─────────────────────────────────────┤
│ ▼ Vastcore.Tests.EditMode          │ ← テストクラス
│   ▼ HeightMapGeneratorTests        │
│     ☑ GenerateHeights_NoiseMode... │ ← テストメソッド
│     ☑ GenerateHeights_HeightMap... │
│   ▼ TerrainGeneratorIntegration... │
│     ☑ GenerateTerrain_NoiseMode... │
├─────────────────────────────────────┤
│ [Run All] [Run Selected] [Clear]   │ ← 実行ボタン
└─────────────────────────────────────┘
```

### ステップ3: EditModeタブを選択

1. Test Runnerウィンドウの上部で **「EditMode」** タブをクリック
2. 左側のツリービューにテストクラスが表示されます

### ステップ4: テストクラスを展開

以下の2つのテストクラスを展開（▶をクリック）:

1. **`Vastcore.Tests.EditMode.HeightMapGeneratorTests`**
   - テスト1-6が含まれます
   - ユニットテスト（高さマップ生成のロジック検証）

2. **`Vastcore.Tests.EditMode.TerrainGeneratorIntegrationTests`**
   - テスト7-10が含まれます
   - 統合テスト（実際のTerrain生成の検証）

### ステップ5: 個別テストの確認

#### テスト1-6（HeightMapGeneratorTests）の確認

1. `HeightMapGeneratorTests` を展開
2. 以下のテストが表示されることを確認:
   - `GenerateHeights_HeightMapMode_ChannelR_UsesRedChannel` ← テスト1
   - `GenerateHeights_HeightMapMode_ChannelG_UsesGreenChannel` ← テスト2
   - `GenerateHeights_NoiseMode_SameSeed_ProducesSameResult` ← テスト3
   - `GenerateHeights_NoiseMode_DifferentSeed_ProducesDifferentResult` ← テスト4
   - `GenerateHeights_HeightMapMode_UVTiling_AppliesTiling` ← テスト5
   - `GenerateHeights_HeightMapMode_InvertHeight_InvertsHeights` ← テスト6

#### テスト7-10（TerrainGeneratorIntegrationTests）の確認

1. `TerrainGeneratorIntegrationTests` を展開
2. 以下のテストが表示されることを確認:
   - `GenerateTerrain_NoiseMode_WithSeed_ProducesDeterministicResult` ← テスト7
   - `GenerateTerrain_HeightMapMode_WithChannel_AppliesChannelSelection` ← テスト8
   - `GenerateTerrain_HeightMapMode_WithInvertHeight_InvertsTerrain` ← テスト9
   - `GenerateTerrain_CombinedMode_WithNewFeatures_WorksCorrectly` ← テスト10

### ステップ6: テストの実行

#### 個別実行（推奨：初回確認）

1. **テスト1を実行**:
   - `GenerateHeights_HeightMapMode_ChannelR_UsesRedChannel` の左側のチェックボックスをクリック
   - 「Run Selected」ボタンをクリック
   - 結果を確認（緑色のチェックマーク = 成功、赤色のX = 失敗）

2. **テスト3を実行**（Seed決定論の確認）:
   - `GenerateHeights_NoiseMode_SameSeed_ProducesSameResult` を選択
   - 「Run Selected」ボタンをクリック
   - 結果を確認

3. **テスト7を実行**（統合テストの確認）:
   - `GenerateTerrain_NoiseMode_WithSeed_ProducesDeterministicResult` を選択
   - 「Run Selected」ボタンをクリック
   - 結果を確認（Terrain生成のため、少し時間がかかります）

#### 一括実行（全テスト確認）

1. 「Run All」ボタンをクリック
2. すべてのEditModeテストが実行されます
3. 実行時間: 約5-10秒

### ステップ7: 実行結果の確認

#### 成功時の表示

```
✓ GenerateHeights_HeightMapMode_ChannelR_UsesRedChannel (0.123s)
✓ GenerateHeights_NoiseMode_SameSeed_ProducesSameResult (0.234s)
```

- **緑色のチェックマーク**: テスト成功
- **実行時間**: 各テストの実行時間が表示される

#### 失敗時の表示

```
✗ GenerateHeights_HeightMapMode_ChannelR_UsesRedChannel (0.123s)
  AssertionException: Red channel should be used when HeightMapChannel is R
```

- **赤色のXマーク**: テスト失敗
- **エラーメッセージ**: 失敗理由が表示される
- **スタックトレース**: クリックして詳細を確認可能

---

## 方法2: コマンドラインでの確認（推奨：自動化・CI/CD）

### 前提条件

- Unity Editor 6000.2.2f1 がインストールされていること
- PowerShell が利用可能であること
- プロジェクトルートで実行すること

### ステップ1: プロジェクトルートに移動

```powershell
cd "C:\Users\thank\Storage\Game Projects\VastCore_TerrainEngine\VastCore"
```

### ステップ2: テスト実行スクリプトの確認

```powershell
# スクリプトの存在確認
Test-Path "scripts\run-tests.ps1"
# 期待結果: True
```

### ステップ3: EditModeテストの実行

```powershell
# EditModeテストのみ実行
.\scripts\run-tests.ps1 -TestMode editmode
```

**実行内容**:
1. Unity Editorをバッチモードで起動
2. EditModeテストを実行
3. 結果を `artifacts/test-results/editmode-results.xml` に出力
4. ログを `artifacts/logs/editmode.log` に出力

**実行時間**: 約30-60秒（Unity Editorの起動時間を含む）

### ステップ4: テスト結果の確認

#### XML結果ファイルの確認

```powershell
# 結果ファイルの存在確認
Test-Path "artifacts\test-results\editmode-results.xml"
# 期待結果: True

# 結果ファイルの内容確認（最初の50行）
Get-Content "artifacts\test-results\editmode-results.xml" | Select-Object -First 50
```

**XML形式の例**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<test-run>
  <test-suite>
    <test-case name="GenerateHeights_HeightMapMode_ChannelR_UsesRedChannel" result="Passed" duration="0.123"/>
    <test-case name="GenerateHeights_NoiseMode_SameSeed_ProducesSameResult" result="Passed" duration="0.234"/>
    ...
  </test-suite>
</test-run>
```

#### ログファイルの確認

```powershell
# ログファイルの内容確認（最後の100行）
Get-Content "artifacts\logs\editmode.log" | Select-Object -Last 100
```

**ログ形式の例**:
```
Running editmode tests...
Using Unity Editor: C:\Program Files\Unity\Hub\Editor\6000.2.2f1\Editor\Unity.exe
...
✓ editmode tests passed. Results: artifacts/test-results/editmode-results.xml
```

### ステップ5: 特定のテストクラスのみ実行（オプション）

Unity Test Runnerのコマンドラインオプションを使用:

```powershell
# Unity Editorのパスを取得（自動検出）
$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.2.2f1\Editor\Unity.exe"

# プロジェクトパス
$projectPath = "C:\Users\thank\Storage\Game Projects\VastCore_TerrainEngine\VastCore"

# EditModeテストを実行
& $unityPath -batchmode -nographics -quit `
  -projectPath $projectPath `
  -runTests -testPlatform editmode `
  -testResults "artifacts\test-results\editmode-results.xml" `
  -testFilter "FullyQualifiedName~HeightMapGeneratorTests" `
  -logFile "artifacts\logs\editmode.log"
```

**`-testFilter` オプション**:
- `FullyQualifiedName~HeightMapGeneratorTests`: HeightMapGeneratorTestsクラスのみ実行
- `FullyQualifiedName~TerrainGeneratorIntegrationTests`: TerrainGeneratorIntegrationTestsクラスのみ実行

---

## 方法3: テストコードの直接確認

### ステップ1: テストファイルの存在確認

```powershell
# テストファイルの存在確認
Test-Path "Assets\Tests\EditMode\HeightMapGeneratorTests.cs"
Test-Path "Assets\Tests\EditMode\TerrainGeneratorIntegrationTests.cs"
# 期待結果: 両方とも True
```

### ステップ2: テストメソッドの確認

```powershell
# テストメソッドの一覧を取得
Select-String -Path "Assets\Tests\EditMode\HeightMapGeneratorTests.cs" -Pattern "\[Test\]" | Measure-Object
# 期待結果: Count = 14（既存8 + 新規6）

Select-String -Path "Assets\Tests\EditMode\TerrainGeneratorIntegrationTests.cs" -Pattern "\[Test\]" | Measure-Object
# 期待結果: Count = 7（既存3 + 新規4）

# 実際の確認結果（2026-01-04）:
# - HeightMapGeneratorTests: 14テスト ✓
# - TerrainGeneratorIntegrationTests: 7テスト ✓
```

### ステップ3: 特定のテストメソッドの確認

```powershell
# テスト1の確認
Select-String -Path "Assets\Tests\EditMode\HeightMapGeneratorTests.cs" -Pattern "GenerateHeights_HeightMapMode_ChannelR_UsesRedChannel" -Context 0,5

# テスト3の確認
Select-String -Path "Assets\Tests\EditMode\HeightMapGeneratorTests.cs" -Pattern "GenerateHeights_NoiseMode_SameSeed_ProducesSameResult" -Context 0,5
```

---

## トラブルシューティング

### 問題1: Test Runnerウィンドウにテストが表示されない

**原因**: コンパイルエラーがある可能性

**対処**:
1. Unity EditorのConsoleウィンドウを確認（`Window > General > Console`）
2. エラーを修正
3. Test Runnerウィンドウを再読み込み（「Clear」ボタンをクリック後、「Run All」をクリック）

### 問題2: コマンドライン実行でUnity Editorが見つからない

**原因**: Unity Editorのパスが正しく設定されていない

**対処**:
```powershell
# Unity Editorのパスを手動指定
.\scripts\run-tests.ps1 -TestMode editmode -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.2.2f1\Editor\Unity.exe"
```

### 問題3: テストが失敗する

**原因**: 実装に問題がある可能性

**対処**:
1. エラーメッセージを確認
2. `Assets/MapGenerator/Scripts/HeightMapGenerator.cs` を確認
3. `Assets/MapGenerator/Scripts/TerrainGenerator.cs` を確認
4. 必要に応じて実装を修正

### 問題4: テスト実行に時間がかかりすぎる

**原因**: Terrain生成の統合テストが重い

**対処**:
- 統合テスト（テスト7-10）は実際にTerrainを生成するため、時間がかかります
- これは正常な動作です
- 個別実行で特定のテストのみ実行することで、時間を短縮できます

---

## 確認チェックリスト

### Unity Editor上での確認

- [ ] Test Runnerウィンドウが開ける
- [ ] EditModeタブが選択できる
- [ ] `HeightMapGeneratorTests` クラスが表示される
- [ ] `TerrainGeneratorIntegrationTests` クラスが表示される
- [ ] テスト1-10がすべて表示される
- [ ] テスト1が成功する（緑色のチェックマーク）
- [ ] テスト3が成功する（Seed決定論）
- [ ] テスト7が成功する（統合テスト）
- [ ] 「Run All」で全テストが成功する

### コマンドラインでの確認

- [ ] `scripts\run-tests.ps1` が存在する
- [ ] `.\scripts\run-tests.ps1 -TestMode editmode` が実行できる
- [ ] `artifacts\test-results\editmode-results.xml` が生成される
- [ ] XMLファイルにテスト結果が含まれている
- [ ] すべてのテストが `result="Passed"` である

### テストコードの確認

- [ ] `Assets\Tests\EditMode\HeightMapGeneratorTests.cs` が存在する
- [ ] `Assets\Tests\EditMode\TerrainGeneratorIntegrationTests.cs` が存在する
- [ ] テスト1-10のメソッドが存在する
- [ ] コンパイルエラーがない

---

## 関連ドキュメント

- **テスト解説**: `docs/terrain/TASK_010_011_TestDocumentation.md`
- **テスト計画**: `docs/terrain/V01_TestPlan.md`
- **TASK_010**: `docs/tasks/TASK_010_TerrainGenerationWindow_v0_FeatureParity.md`
- **TASK_011**: `docs/tasks/TASK_011_HeightMapGenerator_Determinism_Channel_UV.md`

---

**最終更新**: 2026-01-04  
**作成者**: Orchestrator (Cursor)
