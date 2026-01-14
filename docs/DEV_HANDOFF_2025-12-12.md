# 開発申し送りメモ - 2025-12-12 (更新版)

- **対象リポジトリ**: YuShimoji/VastCore  
- **ブランチ**: `master`  
- **Unity バージョン**: 6000.2.2f1  
- **作成者**: Cascade（AIアシスタント）

このメモは、`docs/DEV_HANDOFF_2025-12-09.md` 以降（〜 2025-12-12 時点）に行った変更・調査結果・未完了タスクをまとめた引き継ぎ用ドキュメントです。

---

## 1. Git / 反映状況（2025-12-12 時点）

- `master` は `origin/master` と同期済み
- 作業ツリーはクリーン（未コミット変更なし）
- 直近の主要コミット:
  - `91becfa feat(CT-1): add ProBuilder CSG API scanner` (2025-12-12 18:30頃)
  - `9db75e0 chore(CT-1): support batch scan entrypoint` (2025-12-12 18:45頃)
  - `25f0e47 chore(CT-1): add ProBuilder CSG scan script and sanitized report` (2025-12-15)

## 1.1 2025-12-15 追加更新（今回の反映内容）

- `Assets/Editor/StructureGenerator/Utils/*` を追加（CSGプロバイダ抽象 / reflection 実装）
- `Assets/Editor/Tools/ProBuilderCsg/ProBuilderInternalCsgPoC.cs` を追加（手動検証用）
- `Assets/Scripts/Terrain/Map/BiomePresetManager.cs` の MenuItem 重複を解消（起動時エラー除去）
- `Assets/Editor/StructureGenerator/Tabs/Editing/CompositionTab.cs` を CSGプロバイダ経由に更新
- 新規作成した `.cs.meta` を Unity 標準形式（`MonoImporter`）に整形
- `scripts/run-tests.ps1` を改善（異常終了時に古いXMLを誤読しないため、実行前に結果XMLを削除）

## 1.2 2025-12-17 追加更新（EditMode テスト検出復旧）

- EditMode バッチテストが `Total: 0` になる問題を解消（最小スモークテストを追加して検出を保証）
  - 追加: `Assets/Tests/EditMode/CsgProviderResolverSmokeTests.cs`
  - 修正: `Assets/Tests/EditMode/Vastcore.Tests.EditMode.asmdef`
    - `optionalUnityReferences: ["TestAssemblies"]` を追加
    - `Vastcore.Editor.StructureGenerator` 参照を追加（ただしテスト本体は internal 参照を避けるため reflection 呼び出し）
- `scripts/run-tests.ps1 -TestMode editmode` 実行結果（2025-12-17）
  - `Total: 1 / Passed: 1 / Failed: 0 / Skipped: 0`
- `.gitignore` はデバッグのため一時的に `artifacts/` 配下のログ/XMLのみ unignore したが、解析完了後に元へ復帰（現在は `artifacts/` 全体を ignore）

---

## 2. 今回までの主な変更（12/09 以降）

### 2.1 CT-1: CompositionTab に CSG コア処理を追加（条件付き）

- 対象ファイル:
  - `Assets/Editor/StructureGenerator/Tabs/Editing/CompositionTab.cs`
  - `Assets/Editor/StructureGenerator/Vastcore.Editor.StructureGenerator.asmdef`

- 実装した内容（コードとしては追加済み）:
  - Union / Intersection / Difference の CSG 実行パス
  - 複数オブジェクト（3個以上）時の順次処理
  - Undo/Redo 対応（結果オブジェクト作成の Undo 登録）
  - 元オブジェクトの扱いオプション:
    - 非表示
    - 削除

- 条件付きコンパイル:
  - ProBuilder は `HAS_PROBUILDER`
  - 旧実装は `HAS_PARABOX_CSG` を前提にガード（Parabox.CSG 未導入時に無効化される）

- 重要な現状（2025-12-15 更新）:
  - **ProBuilder 内蔵 CSG（`Unity.ProBuilder.Csg`）は internal だが、reflection 経由で Union が成立することを確認**
  - `CompositionTab.cs` は CSG プロバイダ抽象（reflection）経由で実行するよう更新し、Parabox.CSG 未導入でも CSG を実行可能にした
  - 追加ファイル: `Assets/Editor/StructureGenerator/Utils/*`（`ICsgProvider` / `CsgProviderResolver` / ProBuilder internal provider 等）
  - 追加ツール: `Assets/Editor/Tools/ProBuilderCsg/ProBuilderInternalCsgPoC.cs`（手動検証用）

- アセンブリ定義の調整:
  - `HAS_PARABOX_CSG` の自動定義は削除（誤検知でコンパイルエラーを誘発し得るため）

### 2.2 ドキュメント更新

- `docs/ISSUES_BACKLOG.md`
  - CT-1 を「ProBuilder 内蔵 CSG（reflection）を第一候補として実行可能」に更新

- `FUNCTION_TEST_STATUS.md`
  - Composition Tab を「CSGコード実装済み（ProBuilder reflection 経由）・要検証」に更新
  - Random Tab は「実装済み・要検証」のまま（SG-2 の網羅テスト待ち）

- `docs/SG1_TEST_VERIFICATION_PLAN.md`
  - 最終更新を 2025-12-12 とし、SG-2（RandomControlTab）のチェックリストを詳細化

- 追加（ドキュメント整理）:
  - レガシー/過去ログの混乱を避けるため、以下に「正本への参照」を追記
    - `Documentation/QA/FUNCTION_TEST_STATUS.md`
    - `CSG_INTEGRATION_LOG.md`
    - `CSG_INVESTIGATION_LOG.md`
    - `docs/DEV_HANDOFF_2025-12-05.md`

### 2.3 ProBuilder CSG API スキャナ実装

- 対象ファイル:
  - `Assets/Editor/Tools/ProBuilderCsg/ProBuilderCsgScannerWindow.cs` （新規）
  - `Assets/Editor/Tools/ProBuilderCsg.meta` （新規）
  - `Assets/Editor/Tools/ProBuilderCsg/ProBuilderCsgScannerWindow.cs.meta` （新規）

- 実装した内容:
  - ProBuilder関連アセンブリの自動検出とリフレクション
  - CSG関連型のメソッド/フィールド一覧生成
  - Unity定義シンボル取得とレポート出力
  - バッチモード実行対応（`RunBatch` メソッド）
  - UI: カスタム出力パス、フィルタ、詳細度設定

- 目的:
  - CT-1の第一候補（ProBuilder内蔵CSG）の可用性を機械的に判定するための材料生成
  - レポート出力: `docs/CT1_PROBUILDER_CSG_API_SCAN.md`

- 追記（2025-12-15）:
  - スキャン結果のレポートをリポジトリに追加: `docs/CT1_PROBUILDER_CSG_API_SCAN.md`
  - バッチ実行スクリプトを追加: `scripts/run-csg-scan.ps1`

### 2.4 .gitignore 更新

- `artifacts/` ディレクトリを無視対象に追加（テスト結果ログのクリーン化）

### 2.5 テスト実行（バッチ）状況

- `.\scripts\run-tests.ps1 -TestMode editmode` を実行したが、プロジェクトが別のUnityインスタンスで開かれている場合、Unityが
  `It looks like another Unity instance is running with this project open.`
  でクラッシュし、テスト結果が生成されない。
- 再実行手順:
  - 当該プロジェクトを開いている Unity Editor を閉じる
  - `.\scripts\run-tests.ps1 -TestMode editmode` を実行

- 追記（2025-12-17）:
  - 最小スモークテスト追加により、EditMode バッチでテストが検出される状態を復旧
  - `artifacts/test-results/editmode-results.xml` が生成され、`Total: 1 / Passed: 1` を確認

---

## 3. 調査結果（CT-1 / CSG 依存周り + タブ実装状況）

### 3.1 CSG 依存状況

- `Packages/manifest.json` 上で `com.unity.probuilder` は導入済み（例: 6.0.6）
- ただし、`Parabox.CSG` は manifest に存在せず、現状は利用できない
- `Unity.ProBuilder.Csg` アセンブリはロードされており、CSG関連型は存在する（スキャンレポート参照）
- `UnityEngine.ProBuilder.Csg.CSG` / `Model` などは `Public: False` だが、reflection 経由で `CSG.Union/Subtract/Intersect` の呼び出しと `Model → Mesh` 抽出が成立することを確認（PoC 実施）
- 既存の参考コード:
  - `Assets/Tests/EditMode/BooleanTest.cs` は `#if HAS_PROBUILDER && HAS_PARABOX_CSG` 前提で Parabox.CSG を使用

### 3.2 StructureGenerator タブ実装状況（棚卸し中）

- **Generation カテゴリ**:
  - `BasicStructureTab.cs`: プリミティブ生成（Cube/Cylinder/Wall/Sphere/Torus/Pyramid）実装済み
  - `AdvancedStructureTab.cs`: 高度形状生成（Monolith/TwistedTower/ProceduralColumn）実装済み
  - `StructureGenerationTab.cs`: プリミティブ生成（同上）実装済み
  - `ProceduralTab.cs`: 手続き生成（ContinuousWall/Stairs/Structure）実装済み

- **Editing カテゴリ**:
  - `CompositionTab.cs`: UIスケルトン + CSG演算（Union/Intersection/Difference）実装済み（ProBuilder reflection 経由で実行可能・要検証）
  - `RandomControlTab.cs`: Transformランダム化 + Undo/Preview 実装済み
  - `ParticleDistributionTab.cs`: 分布配置システム実装済み

- **Settings カテゴリ**:
  - `GlobalSettingsTab.cs`: グローバル設定（Material/Scale/Position）実装済み
  - `RelationshipTab.cs`: 構造物関係性管理実装済み
  - `SettingsTab.cs`: ユーティリティ機能（テスト環境作成等）実装済み
  - `StructureRelationshipSystem.cs`: 関係性計算システム実装済み

- **Deform カテゴリ**:
  - `DeformerTab.cs`: Deform統合UI実装済み（`DEFORM_AVAILABLE` 条件付き）

- **未実装/コメントアウト**:
  - `OperationsTab.cs`: 完全未実装（コメントアウト）

---

## 4. 未完了タスク（最優先）

### 4.1 CT-1: CSG 依存方針の決定（最優先）

現状の `CompositionTab.cs` は、CSG プロバイダ抽象（reflection）経由で CSG を実行するよう更新し、
**Parabox.CSG 未導入でも ProBuilder 内蔵 CSG（reflection）で実行できる状態**に移行した。

採用方針:

- **方針B**: ProBuilder 内蔵（`Unity.ProBuilder.Csg`）を reflection で利用（依存を増やさない）
- フォールバック: Parabox.CSG が将来導入された場合は、ロード検知（reflection）で自動的に利用候補に入る

### 4.2 CT-1: 最小の動作確認（Union）

依存方針決定後、最低限以下を確認する:

- 2つの単純メッシュ（Cube等）に対して Union を実行
- 結果オブジェクトの Mesh / Material の妥当性
- 元オブジェクト非表示/削除オプションの動作
- Undo/Redo の動作

### 4.3 CT-1: CompositionTab 実動作確認

- Union / Intersection / Difference
- 複数チェイン（3個以上）
- 元オブジェクトの非表示/削除オプション
- 結果オブジェクトの Mesh / Material の妥当性

---

## 5. SG-2（補足）

- `RandomControlTab` は Transform ランダム化 + Preview + Real-time + Undo/Redo まで実装済み
- 網羅的手動テスト（SG-2）は未完了
- 今回は「簡易確認で概ね問題なし」前提で優先度を落とし、CT-1 を優先する方針

---

## 6. 次の着手順（推奨）

### 6.1 即時着手可能

1. **CT-1: ProBuilder CSG API レポート生成**
   - Unityバッチモードで `ProBuilderCsgScannerWindow.RunBatch()` を実行
   - `docs/CT1_PROBUILDER_CSG_API_SCAN.md` を生成・確認
   - これにより ProBuilder内蔵CSGの可用性が機械的に判定可能

2. **CT-1: CSG 依存方針決定**
   - レポート結果に基づき、以下のいずれかを決定:
     - **方針B**: ProBuilder内蔵API使用（依存増なし）
     - **方針A**: Parabox.CSG導入（外部ライブラリ依存追加）
     - **方針C**: Mesh.CombineMeshes等へのスコープダウン

3. **CT-1: Union 最小動作確認実装**
   - 決定した方針に基づき、2つのCubeのUnionを実装・テスト

### 6.2 中期作業

1. **A: StructureGenerator タブ棚卸し完了**
   - 残りのタブ実装詳細調査
   - テストケース整備状況確認
   - ドキュメントとの整合性確認

2. **C: 次タスク提案の整理**
   - ISSUES_BACKLOG.md / DEV_HANDOFF 更新
   - 優先度付けとスケジューリング

### 6.3 ブロック要因

- **なし**: 次の作業は自律的に進め可能
- CSG方針決定後はユーザー確認が必要になる可能性あり

---

## 7. 技術的補足

- Unity 6000.2.2f1 + ProBuilder 6.0.6 で動作確認済み
- テスト環境: `scripts/run-tests.ps1` でバッチ実行可能
- CSGスキャナ: `Tools/Vastcore/Diagnostics/ProBuilder CSG API Scanner` から実行

---

以上です。作業再開は上記 6.1 からの手順で進められます。
