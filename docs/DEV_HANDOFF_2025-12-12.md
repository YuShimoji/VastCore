# 開発申し送りメモ - 2025-12-12

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
  - `07c1412 feat(CT-1): CSG コア実装を CompositionTab に追加`

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
  - CSG 実装は `HAS_PARABOX_CSG` を前提にガード

- 重要な現状:
  - **Parabox.CSG パッケージがプロジェクトに導入されていないため、現状は CSG が実行できない**
  - `#if HAS_PARABOX_CSG` のブロック外ではフォールバックダイアログを出すのみ

- アセンブリ定義の調整:
  - `HAS_PARABOX_CSG` の自動定義は削除（誤検知でコンパイルエラーを誘発し得るため）

### 2.2 ドキュメント更新

- `docs/ISSUES_BACKLOG.md`
  - CT-1 を「CSGコード実装済み（Parabox.CSG待ち）」として反映

- `FUNCTION_TEST_STATUS.md`
  - Composition Tab を「CSGコード実装済み（Parabox.CSG待ち）」として反映
  - Random Tab は「実装済み・要検証」のまま（SG-2 の網羅テスト待ち）

- `docs/SG1_TEST_VERIFICATION_PLAN.md`
  - 最終更新を 2025-12-12 とし、SG-2（RandomControlTab）のチェックリストを詳細化

- 追加（ドキュメント整理）:
  - レガシー/過去ログの混乱を避けるため、以下に「正本への参照」を追記
    - `Documentation/QA/FUNCTION_TEST_STATUS.md`
    - `CSG_INTEGRATION_LOG.md`
    - `CSG_INVESTIGATION_LOG.md`
    - `docs/DEV_HANDOFF_2025-12-05.md`
    - `docs/DEV_HANDOFF_2025-12-09.md`

---

## 3. 調査結果（CT-1 / CSG 依存周り）

- `Packages/manifest.json` 上で `com.unity.probuilder` は導入済み（例: 6.0.6）
- ただし、`Parabox.CSG` は manifest に存在せず、現状は利用できない
- 既存の参考コード:
  - `Assets/Tests/EditMode/BooleanTest.cs` は `#if HAS_PROBUILDER && HAS_PARABOX_CSG` 前提で Parabox.CSG を使用

---

## 4. 未完了タスク（最優先）

### 4.1 CT-1: CSG 依存方針の決定（最優先）

現状の `CompositionTab.cs` は Parabox.CSG 前提で CSG 実行パスが実装されているが、
**プロジェクトに Parabox.CSG が導入されていないため動作しない**。

次のいずれかを決める必要がある:

- **方針A**: Parabox.CSG を明示導入して現行実装を有効化する
- **方針B**: ProBuilder 内蔵（`Unity.ProBuilder.Csg`）の API へ寄せて実装を切り替える（依存を増やさない）
- **方針C**: 暫定として `Mesh.CombineMeshes` 等の簡易結合にスコープダウン（Union 相当のみ）

推奨（暫定案）:

- **方針B を第一候補**（ProBuilder だけで完結させる）
- うまくいかない場合のみ **方針A** を再検討

### 4.2 CT-1: 最小の動作確認（Union）

依存方針決定後、最低限以下を確認する:

- 2つの単純メッシュ（Cube等）に対して Union を実行
- 結果オブジェクトの Mesh / Material の妥当性
- 元オブジェクト非表示/削除オプションの動作
- Undo/Redo の動作

---

## 5. SG-2（補足）

- `RandomControlTab` は Transform ランダム化 + Preview + Real-time + Undo/Redo まで実装済み
- 網羅的手動テスト（SG-2）は未完了
- 今回は「簡易確認で概ね問題なし」前提で優先度を落とし、CT-1 を優先する方針

---

## 6. 次の着手順（推奨）

1. **CT-1: CSG 依存方針決定（A/B/C）**
2. **CT-1: Union の最小動作確認まで到達**
3.（任意）SG-2 のテスト結果反映（簡易版でも可）

---

以上です。
