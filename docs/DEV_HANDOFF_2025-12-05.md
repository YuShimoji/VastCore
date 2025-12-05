# 開発申し送りメモ - 2025-12-05

- **対象リポジトリ**: YuShimoji/VastCore
- **ブランチ**: `master`
- **Unity バージョン**: 6000.2.2f1
- **作成者**: Cascade（AIアシスタント）

このメモは、2025-12-05 時点での作業内容・調査結果・未完了タスクをまとめた引き継ぎ用ドキュメントです。

---

## 1. 今回セッションでの主な変更

### 1.1 Structure Generator 周り

#### 1.1.1 RandomControlTab（SG-2 関連）

- 対象ファイル:
  - `Assets/Editor/StructureGenerator/Tabs/Editing/RandomControlTab.cs`
- 変更内容:
  - `ApplyRandomization()` に **Undo 対応** を追加。
    - 変更概要:
      - `Selection.gameObjects` から `null` を除外し、ローカル変数 `selectedObjects` に格納。
      - 選択されたオブジェクトの `Transform` 配列を取得し、`Undo.RecordObjects(transforms, "Randomize Transform")` を呼び出し。
      - その後、既存の `ApplyPositionRandomization` / `ApplyRotationRandomization` / `ApplyScaleRandomization` を適用。
  - 既存のランダム化ロジック（Position/Rotation/Scale の MinMax 制御、Uniform/Individual スケール、プレビューモード等）は変更していません。
- テスト状況（手動・軽め）:
  - Position / Rotation / Scale のランダム化が期待通りに動作することを確認。
  - `Apply to Selected` 実行後に **Undo で元の Transform 状態に戻せる** ことを確認。
  - プレビューモード（スライダー操作→即時反映、復元・適用ボタン）も概ね期待通りに動作。
- 補足:
  - `FUNCTION_TEST_STATUS.md` 上では、Random Control Tab は依然として「🟡 要検証」扱いです。
  - 実務上は「暫定的に OK」と考えて次フェーズに進んでよい状態ですが、**SG1_TEST_VERIFICATION_PLAN に沿った徹底的なテストは未完了**です。

#### 1.1.2 CompositionTab（CT-1 スケルトン実装）

- 新規ファイル:
  - `Assets/Editor/StructureGenerator/Tabs/Editing/CompositionTab.cs`
- 主な内容:
  - `IStructureTab` 実装クラス `CompositionTab` を追加し、Structure Generator の **Editing カテゴリのタブ**として実装。
  - UI 構成:
    - ヘッダー: "Phase 5: Advanced Composition" 表示と説明テキスト。
    - **Source Objects セクション**:
      - 選択中オブジェクト数の表示。
      - 「Add Selected Objects」ボタンで現在選択中のオブジェクトをリストに追加（MeshFilter または MeshRenderer を持つオブジェクトのみ）。
      - 「Clear Source Objects」ボタンでクリア。
      - 追加済みオブジェクトのリスト表示と個別削除ボタン。
    - **CSG Operations セクション**（Foldout）:
      - `CompositionMode` enum（`Union`, `Intersection`, `Difference`）。
      - それぞれのモードの説明文を `HelpBox` で表示。
      - ソースオブジェクトが 2 つ以上ある場合のみ「Execute {Mode}」ボタン有効。
      - 現状の `ExecuteCSGOperation()` は **ログ出力 + `EditorUtility.DisplayDialog` のみ（処理は未実装）**。
    - **Blend Operations セクション**（Foldout）:
      - `BlendMode` enum（`Layered`, `Surface`, `Adaptive`, `Noise`）。
      - `Blend Factor` スライダー。
      - 各モードの説明文を `HelpBox` で表示。
      - 現状の `ExecuteBlendOperation()` は **ログ + ダイアログのみ（未実装）**。
    - **Advanced Operations セクション**（Foldout）:
      - Morph / Volumetric / Distance Field 用のボタンと説明。
      - `Morph Factor`, `Voxel Resolution` スライダー。
      - それぞれ `ExecuteMorph`, `ExecuteVolumetricBlend`, `ExecuteDistanceFieldBlend` は **ダイアログのみのスケルトン実装**。
    - 下部のアクションボタン:
      - 「Preview」「Undo Preview」ボタンはログ出力のみ（実処理未実装）。
- StructureGeneratorWindow への登録:
  - `Assets/Editor/StructureGenerator/Core/StructureGeneratorWindow.cs` にて、タブリストに CompositionTab を追加:
    - 変更前: `// _tabs.Add(new CompositionTab(this)); // 一時的にコメントアウト`
    - 変更後: `_tabs.Add(new CompositionTab(this)); // CT-1: スケルトン実装完了`
- 現状の制約:
  - CSG/Blend/Morph/Volumetric/DistanceField など **実際のメッシュブーリアン処理・合成処理は未実装**。
  - `FUNCTION_TEST_STATUS.md` の Composition Tab セクションは、「CompositionTab.cs ファイル不在」前提の記述が残っており、現状と不整合があります（要修正。後述）。

#### 1.1.3 DeformerTab（P3-2 既存作業の確認）

- 新規作業はありませんが、現状のスコープとして以下を整理:
  - 動的パラメータ UI (`DrawDynamicDeformerParameters`) により 8 種類の Deformer 向けパラメータスライダーを提供済み。
  - `DeformIntegrationManager.DeformerSettings` と連携する構造は整備済み。
  - ただし **各 Deformer への実際の適用フロー（ApplyDeformer を通じた運用）やプリセット保存機構は未実装** です（P3-3 にて対応予定）。

---

### 1.2 Terrain 統合（T4 関連）

#### 1.2.1 UnifiedTerrainParams / NoiseSettings

- 新規ファイル:
  - `Assets/Scripts/Terrain/UnifiedTerrainParams.cs`
- 内容概要:
  - `UnifiedTerrainParams` 構造体:
    - `worldSize`, `maxElevation`, `meshResolution`, `noiseSettings`, `outputType` などを統合。
    - `Default()`, `SmallTerrain()`, `LargeTerrain()` といったファクトリメソッドを提供。
  - `NoiseSettings` 構造体:
    - `scale`, `octaves`, `persistence`, `lacunarity`, `offset`, `seed`, `noiseType` 等。
    - MeshGenerator / TerrainGenerator の既存パラメータの中間値をデフォルトに設定。

#### 1.2.2 TerrainParamsConverter

- 対象ファイル:
  - `Assets/Scripts/Terrain/TerrainParamsConverter.cs`
- 概要:
  - 方針A（T3_GAP_ANALYSIS）の「パラメータ統一層」として、
    - `UnifiedTerrainParams` ⇔ `MeshGenerator.TerrainGenerationParams`
    - `UnifiedTerrainParams` ⇔ `PrimitiveTerrainGenerator.PrimitiveGenerationParams`
    の相互変換ユーティリティを実装。
- 主な API:
  - `ToPrimitive(UnifiedTerrainParams unified)`
    - 返り値型: `PrimitiveTerrainGenerator.PrimitiveGenerationParams`
    - `worldSize` / `maxElevation` から scale を構築し、`noiseIntensity` や `subdivisionLevel` を推定変換。
  - `ToMeshGenerator(UnifiedTerrainParams unified)`
    - `MeshGenerator.TerrainGenerationParams.Default()` をベースに、resolution / size / maxHeight / noise 系パラメータを上書き。
  - `FromPrimitive(PrimitiveTerrainGenerator.PrimitiveGenerationParams primitive)`
  - `FromMeshGenerator(MeshGenerator.TerrainGenerationParams meshParams)`
- バグ修正:
  - もともと存在しないトップレベル型 `PrimitiveGenerationParams` を参照していたため `CS0246` が発生していた。
  - 修正内容:
    - メソッドシグネチャと `new` 箇所をすべて `PrimitiveTerrainGenerator.PrimitiveGenerationParams` の **フル修飾名** に変更。

#### 1.2.3 現時点での制約

- `UnifiedTerrainParams` / `TerrainParamsConverter` は **「基盤レイヤー」までの実装**であり、
  - まだどの Editor ツールや Manager からも直接は呼ばれていません。
- 今後、どこか 1 箇所でも良いので:
  - `UnifiedTerrainParams` をインスペクタ/ウィンドウから編集 → `TerrainParamsConverter.ToMeshGenerator()` 経由で `MeshGenerator` を呼ぶ、
  といった実用パスを作るのが次ステップになります。

---

### 1.3 ドキュメント・バックログ更新

#### 1.3.1 ISSUES_BACKLOG.md

- 対象ファイル:
  - `docs/ISSUES_BACKLOG.md`
- 主な更新:
  - T4: Terrain 統合パラメータ実装を **Completed (2025-12-05)** として整理。
  - 新規バックログ項目の追加:
    - `RC-1: RandomControlTab 高度機能実装`
    - `CT-1: CompositionTab 実装`
    - `P3-3: Deformer プリセットシステム`
  - CT-1 のスケルトン実装完了に合わせてタスクを一部チェック済みに更新:
    - `[x] CompositionTab.cs スケルトン作成`
    - `[x] StructureGeneratorWindowに登録`
  - SG-2 については、依然として **Status: In Progress (ドキュメント準備完了)** のままです。
    - 手動テストを一部実施したものの、計画書に沿った完全な検証は未完了、という状態を反映しています。

#### 1.3.2 FUNCTION_TEST_STATUS.md

- 対象ファイル:
  - `FUNCTION_TEST_STATUS.md`
- 状態整理のみ（ファイル自体はこのセッションでは未編集）:
  - **Composition Tab セクション**:
    - 現在も「CompositionTab.cs ファイル不在」を前提に 0/10 機能と評価しており、
      実際には **CompositionTab.cs が追加され UI スケルトンが存在する**ため、情報が古くなっています。
  - **Random Control Tab セクション**:
    - Position/Rotation/Scale/Preview/Real-time/Constraints については「✅ 実装済み / 🟡 要検証」となっており、
      コード実装と概ね一致しています。
    - 高度機能（Adaptive Random / Preset Management / Mesh Deformation）は `❌ 未実装 / 🔴 未実装` と明示されています。
  - **全体システム状況テーブル**:
    - Composition / Random の行が、現コードベースよりも「完了度が高い」表現になっており、
      将来的に修正すべき不整合として残っています。

#### 1.3.3 WORK_SUMMARY.md

- 対象ファイル:
  - `WORK_SUMMARY.md`
- 内容:
  - 2025-12-05 時点での作業サマリーとして、主に以下を記載:
    - T4: Terrain 統合パラメータ基盤実装
    - P3-2: DeformerTab 動的パラメータ UI 実装
    - T3, P3-1, SG-1, T2 完了状況
  - 今回追加した CT-1 スケルトン実装 / SG-2 手動テスト（暫定）については、
    - **本申し送りメモで詳細を補完している**状況です。
    - 必要であれば、次セッションで WORK_SUMMARY にも追記するとよいです。

---

### 1.4 その他の観測事項

- コンパイル時の警告:
  - `CS0219`: 代入されているが未使用のローカル変数（RuntimeTerrainManagerTest などのテストコード）。
  - `CS0414`: 代入されているが未使用のフィールド（LOD/キャッシュ/パフォーマンス最適化関連マネージャ）。
  - `CS0618`: `Profiler.GetMonoHeapSize()` / `GetMonoUsedSize()` の非推奨 API 使用（MemoryMonitor）。
  - いずれも **ビルドや基本動作を阻害しない軽度の警告** であり、本セッションでは修正していません。
- Unity メニュー重複警告:
  - `Vastcore/Create New Biome Preset` メニュー項目が重複して定義されており、
    Unity のログに以下のような警告が出力されています:
    - `Cannot add menu item 'Vastcore/Create New Biome Preset' ... because a menu item with the same name already exists.`
  - 実際のメニューは片方が有効になっており、致命的な問題ではありませんが、
    将来的にどちらの `MenuItem` を正とするかを決めて片方を削除・統合するのが望ましいです。

---

## 2. 現在のプロジェクト状態（サマリ）

### 2.1 コンパイル・実行状態

- Unity 6000.2.2f1 上で **コンパイルエラー 0 件** を確認済み。
- 複数の警告（上記 CS0219/CS0414/CS0618 等）は残存していますが、
  現時点では "将来のクリーンアップ候補" として扱っています。

### 2.2 Structure Generator タブ構成

- `StructureGeneratorWindow` 内のタブ一覧（2025-12-05 時点）:
  - GlobalSettings
  - Basic
  - Advanced
  - Relationship
  - Particle Distribution
  - Deformer
  - **Composition**（UI スケルトンのみ、処理は未実装）
  - Random Control

### 2.3 Terrain まわり

- 3 系統の地形システム（T3 時点の整理）:
  - `PrimitiveTerrainGenerator`: ProBuilder ベースのプリミティブ構造物。
  - `MeshGenerator`: ノイズベースのメッシュ地形。
  - `TerrainGenerator (V0)`: Unity Terrain ベースの地形。
- 統合状態:
  - `UnifiedTerrainParams` / `TerrainParamsConverter` により、
    パラメータ統一層の**設計と実装**までは完了。
  - 各システムがまだこの層を通ってはいないため、
    実運用の統一はこれから段階的に進める必要があります。

---

## 3. 未完了タスク・既知のギャップ

### 3.1 高優先度（短期）

1. **CT-1: CompositionTab CSG 基本実装**
   - 現状:
     - UI スケルトンのみ。CSG / Blend / Advanced はすべてダイアログのみの未実装。
   - 具体的タスク案:
     - ProBuilder CSG（または外部ライブラリ）との統合方針を決定。
     - 最小スコープとして **Union / Intersection / Difference** の 3 モードを実装。
       - ソースオブジェクト 2 つ以上を前提とし、結果メッシュを新規 GameObject として生成。
       - 元オブジェクトの扱い（残す / 非表示 / 削除）をオプション化するかは要検討。
     - 実装後、`FUNCTION_TEST_STATUS.md` の Composition セクションと
       全体システム状況テーブルを現実に合わせて更新。

2. **SG-2: RandomControlTab 手動テスト（本格版）とドキュメント更新**
   - 現状:
     - コード実装は完了（Undo 対応も追加済み）。
     - 軽い目視テストで「概ね期待通り」であることを確認。
     - ただし SG1_TEST_VERIFICATION_PLAN に定義された観点を網羅したテストは未完。
   - 具体的タスク案:
     - `docs/SG1_TEST_VERIFICATION_PLAN.md` に沿って全観点テストを実施。
     - 結果を `FUNCTION_TEST_STATUS.md` の Random セクションに反映。
     - 必要に応じて `ISSUES_BACKLOG.md` の SG-2 ステータスを `Completed` に更新。

### 3.2 中期

3. **RC-1: RandomControlTab 高度機能実装**
   - 未実装機能:
     - Adaptive Random（周囲環境・バイオームなどを考慮したランダム化）
     - Preset Management（乱数設定の保存・読み込み）
     - Mesh Deformation（Transform ではなくメッシュ頂点レベルの変形）
   - 推奨アプローチ:
     - すべてを一度に実装するのではなく、
       - まずは **Preset Management** など UI/UX 寄りの機能から着手すると安全。

4. **P3-3: Deformer プリセットシステム & 実適用**
   - 現状:
     - 動的パラメータ UI と `DeformerSettings` 構造体は存在。
     - DeformerTab から実オブジェクトの Deformer コンポーネントへ適用する
       一連のフローやプリセット保存機構は未実装。
   - 具体的タスク案:
     - ScriptableObject ベースのプリセット定義を設計。
     - DeformerTab からプリセットの保存/読み込み UI を提供。
     - `DeformIntegrationManager.ApplyDeformer` と連動させ、
       タブで選択・編集した設定をオブジェクトに適用できるようにする。

5. **T4 続き: UnifiedTerrainParams の実用ルート作成**
   - どこか 1 つのワークフローでよいので、
     - `UnifiedTerrainParams` を UI から編集 → `TerrainParamsConverter` 経由で
       `MeshGenerator` あるいは `PrimitiveTerrainGenerator` を呼ぶ統合パスを構築する。

6. **T5: 自動テスト・可観測性の強化**
   - 方向性:
     - Structure Generator / Terrain 周りの EditMode テストを少しずつ追加。
     - テストランを CI に統合する仕組みづくり（GitHub Actions 等）。

### 3.3 長期

7. **T6: Unity MCP 導入 PoC**
8. **Terrain ストリーミングシステム**（大規模地形の動的ロード/アンロード）
9. **uGUI → UITK 移行**（既存 UI が安定してからの大きめタスク）

---

## 4. 次セッションへのおすすめ着手順

1. **CT-1: CompositionTab の CSG 基本実装**
   - Union / Intersection / Difference の 3 モードに絞った最小実装から着手。
   - ProBuilder CSG もしくは外部ライブラリを利用する場合は、
     依存関係とライセンスを確認した上で採用を検討。

2. **FUNCTION_TEST_STATUS.md / ISSUES_BACKLOG.md の整合性修正**
   - Composition Tab:
     - 「CompositionTab.cs 不在」前提の記述を更新し、
       現状（UI スケルトン + 未実装処理）に合わせた評価へ変更。
   - Random Control Tab:
     - 今回のテスト結果（軽い目視確認）と、今後の追加検証計画を反映。

3. **SG-2 本格テスト or RC-1/P3-3 のいずれかに着手**
   - 品質重視の場合:
     - SG-2 を計画どおりフルテストし、テスト結果をドキュメントへ反映。
   - 新機能優先の場合:
     - RC-1（高度なランダム化）または P3-3（Deformer プリセット）から、
       どちらか一方を選んで掘り下げる。

---

## 5. 参考ファイル一覧

- **Structure Generator 関連**
  - `Assets/Editor/StructureGenerator/Core/StructureGeneratorWindow.cs`
  - `Assets/Editor/StructureGenerator/Tabs/Editing/RandomControlTab.cs`
  - `Assets/Editor/StructureGenerator/Tabs/Editing/CompositionTab.cs`
  - `Assets/Editor/StructureGenerator/Tabs/Deform/DeformerTab.cs`

- **Terrain 統合関連**
  - `Assets/Scripts/Terrain/UnifiedTerrainParams.cs`
  - `Assets/Scripts/Terrain/TerrainParamsConverter.cs`
  - `Assets/Scripts/Terrain/Map/PrimitiveTerrainGenerator.cs`
  - `Assets/Scripts/Terrain/Map/MeshGenerator.cs`
  - `docs/T3_TERRAIN_GAP_ANALYSIS.md`

- **テスト・ドキュメント関連**
  - `FUNCTION_TEST_STATUS.md`
  - `docs/ISSUES_BACKLOG.md`
  - `WORK_SUMMARY.md`
  - `docs/SG1_TEST_VERIFICATION_PLAN.md`

---

## 6. 引き継ぎメモ（実務的な注意点）

- Unity 起動時:
  - コンパイル警告は出ますが、**エラー 0 件** の状態です。
  - Biome Preset 関連のメニュー重複警告が Console に出ますが、
    実行に重大な支障はありません。

- RandomControlTab 利用時:
  - `Apply to Selected` 前後で Undo/Redo が動作することを前提に設計されています。
  - 大量オブジェクト選択時は Undo スタックの負荷に注意してください。

- CompositionTab 利用時:
  - 現時点では **すべての実行ボタンが「ダイアログのみ」のダミー実装**です。
  - 実際の CSG/Blend 処理はまだ入っていないため、「UI プレビュー専用」とみなしてください。

- 次に着手する際は、本メモの「3. 未完了タスク」「4. 次セッションへのおすすめ着手順」を起点にしていただくと、
  スムーズに作業を再開できます。
