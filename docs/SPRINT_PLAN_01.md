# Sprint 01 Plan (Vastcore)

- バージョン: 1.0
- 期間（目安）: 2025-09-28 〜 2025-10-05（1週間スプリント）
- スプリント目標: Deform 統合を安定化し、シーン/Prefab の Missing Script をゼロにすることで、以降の機能開発とパフォーマンステストの基盤を固める。

---

## バックログの優先度付け（docs/ISSUES_BACKLOG.md より）

- 高
  - 1. Deform パッケージ統合の最終整備（asmdef/defineConstraints/参照確認）
  - 2. シーン/Prefab の Missing Script 修復（UI 名前空間統一の反映）
- 中
  - 3. RuntimeGenerationManager/PrimitiveTerrainManager の本実装強化（時間予算、フレーム分散、統計）
  - 4. Test Runner/CI 整備（PlayMode テストの標準化と CI 自動実行）
  - 5. Obsolete API/未使用フィールドの整理（CS0618/CS0414 の削減）
- 低（または完了）
  - 6. プロジェクトルートの不要ログの削除（DEBUGLOG_*）…現状 Done（.gitignore 追加・不要バックアップ削除済）

---

## 今スプリント（Sprint 01）の対象（優先度「高」）

### Issue: Deform パッケージ統合の最終整備（asmdef/defineConstraints/参照確認）

- Title
  - Deform パッケージ統合の最終整備（asmdef/DEFORM_AVAILABLE/参照の完全整合）

- Background & Goal
  - Deform は外部パッケージ（`com.beans.deform`）であり、アセンブリ定義の `references` と `versionDefines` の整合、および `DEFORM_AVAILABLE` の条件付きコンパイルが安定して機能することが必須。過去に依存設定不備により、`CS1503/CS1061` 等のエラーや条件分岐の無効化が発生した。目的は、導入・未導入いずれの環境でもコンパイル/実行が安定し、テストが通る状態にすること。

- Proposed Implementation
  - asmdef 再点検:
    - `Assets/Scripts/Core/Vastcore.Core.asmdef` と `Assets/Tests/Runtime/Vastcore.Testing.asmdef` にて、`Deform`（`com.beans.deform`）への `references` と `versionDefines: DEFORM_AVAILABLE` が正しく設定されているか再確認。
    - バリアント環境（Deform 導入・未導入）での定義有無を検証（Unity 再起動/再スキャンを含む）。
  - コード側の安定化:
    - `VastcoreDeformManager.cs` は `#if DEFORM_AVAILABLE` 時に `Deformable` 強型を使用し、未導入時は `object` 経由でダミー処理を維持。
    - `DeformPresetLibrary.cs` は反射ヘルパー（`TrySetProperty`/`SetAny`）で Deform の API 差異（`Factor`/`Strength`/`Scale` 等）を吸収。
  - テスト連携:
    - `Assets/Tests/Runtime/DeformIntegrationTest.cs` の PlayMode 実行により、標準プリミティブ生成と Deform 適用が例外なく動作することを確認。
  - シーン健全性検査（クロス連携）:
    - シーン単位の Warning を確認（Missing Script は別 Issue でゼロ化）。

- Acceptance Criteria
  - `com.beans.deform` 導入環境:
    - `DEFORM_AVAILABLE` が有効化され、`Deformable` 型を使用する経路でコンパイルエラーが 0。
    - `DeformIntegrationTest`（PlayMode）がパスする。
  - 未導入環境:
    - `DEFORM_AVAILABLE` が無効化され、ダミー経路でコンパイルエラーが 0。
  - 共通:
    - `Assets/Scripts/Core/VastcoreDeformManager.cs` と `Assets/Scripts/Core/DeformPresetLibrary.cs` のビルドが安定。
    - `CS1061/CS1503/CS0136` 等の再発がないこと。

---

### Issue: シーン/Prefab の Missing Script 修復（UI 名前空間統一の反映）

- Title
  - シーン/Prefab の Missing Script を完全修復（`Vastcore.UI` 名称統一を反映）

- Background & Goal
  - 過去の UI 名前空間/構造の変更により、シーン/Prefab に Missing Script が残存。Missing Script は実行時不具合や警告の温床となるため、早期にゼロ化する。副目標として、意図しないパッケージ配下改変（例: ProBuilder）をリセットし、パッケージ整合性を回復する。

- Proposed Implementation
  - Missing Script 検出:
    - エディタユーティリティ（Editor 拡張）で `Assets/` 配下のシーン/Prefab を走査し、Missing Script の有無と件数を一覧化。
    - 対象ディレクトリ: `Assets/Scenes/`, `Assets/Prefabs/`, `Assets/UI/` 等。
  - 置換方針:
    - 既存の UI コンポーネントを `Vastcore.UI` 配下の正規コンポーネント（例: `ModernUIManager`/`SliderBasedUISystem` など）に差し替え。
    - 参照切れのスクリプトは、名称・機能に基づき 1:1 置換または除去。判断に迷う場合は TODO コメント化 + ログ出力で追跡。
  - ProBuilder のリセット:
    - パッケージ配下に差分がある場合は再インポート（リセット）で復元。

- Acceptance Criteria
  - `Assets/` 配下の全シーン/Prefab に Missing Script が 0。
  - シーンを開いた際に Missing Script/参照切れ警告が表示されない。
  - 既知の UI 操作（HUD/スライダー/メニュー）でエラーが発生しない。
  - ProBuilder パッケージ配下の差分がなく、パッケージとして整合。

---

## スプリント内マイルストーン

- Day 1–2: asmdef/define 再点検、Deform 統合テスト安定化（PlayMode）
- Day 3–5: Missing Script 全走査と置換作業、UI 動作確認
- Day 6–7: 回帰テスト、ドキュメント更新、次スプリント計画のドラフト

## 成果物/更新対象

- コード: `Assets/Scripts/Core/` 配下（必要に応じて）、Editor ユーティリティ（検出用）
- ドキュメント: `Documentation/Logs/DEV_LOG.md`, `Documentation/QA/FUNCTION_TEST_STATUS.md`, `docs/ISSUES_BACKLOG.md`
- プロジェクト整備: パッケージ再インポート（ProBuilder）

## リスクと対策

- Deform API の差異再発
  - 反射ヘルパーのプロパティ候補を適宜拡充し、リグレッションを抑制。
- シーン/Prefab の人手差し替えコスト
  - Editor ツールで検出→一括置換の補助を検討（次スプリント範囲）。
- CI 未整備による検知遅延
  - 本スプリントでは手動テストの手順を明記し、次スプリントで CI 導入を上げる。

## 参照

- `docs/ISSUES_BACKLOG.md`
- `docs/TEST_RUNNER_SETUP.md`
- `Documentation/Logs/DEV_LOG.md`
- `Documentation/QA/FUNCTION_TEST_STATUS.md`
