# VastCore Terrain Generation - Handover Note (2025-11-25)

## 現在の進捗状況

### 完了したタスク

- **仕様整理とドキュメント化完了**
  - `docs/terrain/TerrainGenerationV0_Spec.md` を新規作成
  - レガシー仕様（Primitive/LOD/メモリ管理系）を段階的廃棄対象として分類
  - 棚上げ仕様（高度TerrainEngine/Deform連携）を将来フェーズに延期
  - 現行採用仕様（単一タイルHeightMap/Noise生成）をTerrainGenerationV0として定義

- **Editor UI設計完了**
  - TerrainGenerationWindow(v0)のセクション構成と各項目仕様を詳細定義
  - 将来フェーズのTemplate Browser/Editorの骨組みをShelvedとして記載
  - TODOリストの関連タスクをすべて完了状態に更新

- **プロジェクト総点検とリファクタリング**
  - 空ファイル（0バイト）28件を削除
  - 空フォルダ7件を削除
  - `TerrainGenerationConstants.cs` を新規作成し、ハードコーディングを定数化
  - `TerrainGenerator.cs`, `PrimitiveTerrainGenerator.cs` を定数参照に更新
  - 総点検レポート: `docs/progress/ProjectAudit_Nov25.md`

- **Phase 0.5: TerrainGenerationProfile**
  - `TerrainGenerationProfile.cs` ScriptableObject を新規作成
  - 生成モード、サイズ、HeightMap、Noise 設定を含む
  - `TerrainGenerator` との連携メソッド（LoadFromProfile/SaveToProfile）を追加

- **Phase 1: TerrainGenerationWindow(v0)**
  - `TerrainGenerationWindow.cs` Editor UI を新規作成
  - メニュー: `Tools/Vastcore/Terrain/Terrain Generation (v0)`
  - セクション: Context, Generation Mode, Size/Resolution, HeightMap, Noise, Profile, Actions
  - Profile との完全な連携（読み込み/保存/新規作成）

- **CS0101 エラー修正（最新）**
  - `TerrainGenerationMode` 重複定義を解消: V0 用を独立ファイルに、TerrainEngine 用を `TerrainEngineMode` にリネーム
  - レガシー `TerrainGeneratorStub.cs` を削除し、本物の `TerrainGenerator` に一本化
  - アセンブリ定義追加: `Vastcore.MapGenerator.asmdef`, `Vastcore.Editor.Root.asmdef`
  - アセンブリ参照更新: Editor/Testing から MapGenerator を参照可能に
  - 結果: namespace 内での enum 衝突を完全に解消、コンパイルエラーゼロ

### プロジェクト状態

- **コンパイルエラー**: ゼロ（CS0101 修正で解消）
- **警告**: エディタパッケージ関連のインフォメーションのみ、動作に影響なし
- **ドキュメント**: TerrainEngine_OpenSpec_v1.0等既存ドキュメントを参照しつつ、v0仕様に整理
- **コード変更**: TerrainGenerationMode 分離、TerrainEngine 修正、スタブ削除、アセンブリ整理
- **アセンブリ構造**:
  - `Vastcore.Generation`: V0 コア（Profile, Mode, Constants）
  - `Vastcore.MapGenerator`: Runtime 生成（TerrainGenerator, HeightMapGenerator）
  - `Vastcore.Editor.Root`: Editor 拡張（TerrainGenerationWindow）

## 今後の計画（V01 以降の全体ロードマップ）

### Phase V0.1: V01 基盤の安定化（直近）

- **目的**: V0 の Editor / Runtime を「安心して触れる状態」にする
- **タスク**:
  - `TerrainGenerationWindow` / `TerrainGenerationProfile` の動作確認
  - 手動テスト観点・シナリオの Markdown 整理
  - 実プレイテストで見つかったバグ修正（UI 文言・バリデーション強化）

### Phase 1.5: Runtime 責務整理（設計 → 実装）

- **目的**: V0 のランタイムクラスを次フェーズ以降でも拡張しやすい形に
- **タスク**:
  - `TerrainGenerator`, `HeightMapGenerator`, `TerrainGenerationProfile` の責務分離設計
  - 将来 `TerrainEngine` / Template 系との連携境界設計
  - デザインドキュメント作成後、小さい PR 単位で実リファクタ実施

### Phase 2: DesignerTerrainTemplate ライト版 & UI

- **目的**: 「テンプレートを選んで生成する」最低限の体験提供
- **タスク**:
  - `DesignerTerrainTemplate` ScriptableObject ライト版仕様整理
  - Template Browser（一覧＋選択）Editor ウィンドウ
  - Template Editor（最低限パラメータ編集）
  - V0 Generator を内部で呼ぶ構成に限定（Biome/Streaming 除外）

### Phase 2.5: TerrainEngine Lite 統合

- **目的**: `TerrainEngine` を簡易タイル管理レイヤーとして V0 と接続
- **タスク**:
  - `TerrainEngineMode`（TemplateOnly / ProceduralOnly / Hybrid）を V0 + Template にマッピング
  - 小規模タイル生成管理に限定（本格ストリーミングは後続）

### Phase 3 以降: 高度機能群

- Biome / Climate / Streaming の本格実装
- Deform / StructureGenerator / RandomControl との統合
- 既存設計ドキュメント（OpenSpec, Deform Integration, StructureGenerator 等）を統合

### 横断フェーズ: レガシー整理 & QA

- **タスク**:
  - `TerrainEngine` / Streaming / Biome 系のコードを Active/Shelved/Legacy に分類
  - 段階的削除／隔離ロードマップ作成
  - 自動テスト・プロファイリング・ドキュメント更新の並行実施

## 注意点とリスク

- **レガシーコード**: Primitive/LOD/メモリ管理系はビルドに影響ない範囲で段階削除
- **依存関係**: Deform/高度構造物連携はPhase 3以降、v0では一切触れない
- **テスト**: 新UI実装後は単体タイル生成の動作確認を優先
- **アセンブリ**: 新規コード追加時は適切な asmdef 配下に配置（衝突防止）

## 連絡先

- AIアシスタント: Cascade
- 最終更新: 2025-11-26 13:30 UTC+09:00

---

## 2025-11-26 更新

### 完了した作業

#### プロジェクト総点検（継続）

- HeightMapGenerator のマジックナンバーを `TerrainGenerationConstants` に抽出
- コード品質検証: NotImplementedException 0件、仮実装コメント 0件、命名規則違反なし
- `TerrainGeneratorStub.cs` を削除（空ファイル、レガシー）

#### V01 テスト計画作成

- `docs/terrain/V01_TestPlan.md` を新規作成
- 手動テストシナリオ 26 項目を定義
- テスト実行チェックリストを含む

#### Phase 1.5 設計ドキュメント作成

- `docs/design/Phase15_RuntimeRefactor_Design.md` を新規作成
- V01 コンポーネントの責務分析を実施
- 将来の拡張ポイント（Phase 2, 2.5）を設計
- 結論: V01 コンポーネントは品質良好、リファクタリング不要

### 次のステップ

- **V01 動作確認**: テスト計画に基づく手動テスト実行
- **Phase 2 準備**: DesignerTerrainTemplate ライト版の仕様整理
- **テスト追加**: HeightMapGenerator / TerrainGenerator のユニットテスト
