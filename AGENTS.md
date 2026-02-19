# VastCore AI Agent Protocol

本ドキュメントは VastCore プロジェクトにおける AI エージェント (Orchestrator / Worker / Claude Code 等) の行動規約を定める。

最終更新: 2026-02-18

---

## 1. 必読ドキュメント (実装前に必ず確認)

| 優先度 | ドキュメント | 内容 |
|--------|------------|------|
| **必須** | `docs/02_design/ASSEMBLY_ARCHITECTURE.md` | asmdef 依存グラフ、名前空間規約、絶対ルール |
| **必須** | `docs/03_guides/UNITY_CODE_STANDARDS.md` | コーディング規約、禁止パターン |
| **必須** | `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md` | コンパイルエラーの予防・診断手順 |
| 推奨 | `docs/SSOT_WORLD.md` | プロジェクト全体の SSOT 階層 |
| 推奨 | `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md` | 開発ロードマップ |

---

## 2. OpenSpec ワークフロー

### 2.1 変更提案
機能実装・バグ修正の際は OpenSpec プロセスに従う:

1. `openspec/changes/{change-name}/proposal.md` に提案を作成
2. `specs/` にアセンブリ影響範囲を含む仕様を記述
3. `tasks.md` に具体的なタスクを分解
4. 実装 → 検証 → アーカイブ

### 2.2 仕様記述の必須項目
- **影響アセンブリ**: 変更が影響する asmdef の一覧
- **新規 using / 参照**: 追加する名前空間インポートと、その asmdef 参照
- **型の追加/移動**: 新規型の完全修飾名と配置先アセンブリ

---

## 3. 実装規約

### 3.1 コード変更の鉄則

**変更前:**
1. 変更対象ファイルのアセンブリを特定する
2. `docs/02_design/ASSEMBLY_ARCHITECTURE.md` で依存関係を確認する
3. 追加する型/using の参照先が利用可能か確認する

**変更中:**
4. 1 アセンブリの scope 内で完結させる（跨ぐ場合は明示する）
5. 同名型の重複を作らない
6. C# 9.0 の制約を守る
7. 条件コンパイルシンボルは完全に `#if` で隔離する

**変更後:**
8. 追加した using ごとに asmdef 参照を確認する
9. Unity Editor コンパイル成功を確認する
10. asmdef を変更した場合は ASSEMBLY_ARCHITECTURE.md を更新する

### 3.2 禁止事項

| 禁止 | 理由 | 代替 |
|------|------|------|
| `Debug.Log` | ログ統一 | `VastcoreLogger.Instance.LogInfo(...)` |
| `FindFirstObjectByType<IInterface>()` | CS0311 | `FindGameObjectWithTag` + `GetComponent` |
| 引数なし struct コンストラクタ | C# 9 非対応 | オブジェクト初期化子 / static factory |
| 同名型の複数アセンブリ定義 | CS0029 | 1 型 = 1 アセンブリ |
| `#if` 外のオプショナルシンボル参照 | CS0103 | `#if` で完全隔離 |
| 下位→上位の asmdef 参照追加 | 循環参照 | アーキテクチャを見直す |
| `autoReferenced` の無断変更 | Assembly-CSharp 破壊 | ASSEMBLY_ARCHITECTURE.md と同時更新 |

### 3.3 エラー修正の原則
1. **場当たり修正を禁止する。** `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md` の診断フローに従う。
2. **原因を特定してから修正する。** 「とりあえず動くようにする」は許可しない。
3. **修正後は全エラー消失を確認してから次の作業に移る。**
4. **asmdef 変更は ASSEMBLY_ARCHITECTURE.md と同時更新する。**

---

## 4. ドキュメント規約

### 4.1 ドキュメントの配置

| 種別 | 配置先 | 例 |
|------|--------|-----|
| 設計・アーキテクチャ | `docs/02_design/` | ASSEMBLY_ARCHITECTURE.md |
| ガイド・手順 | `docs/03_guides/` | COMPILATION_GUARD_PROTOCOL.md |
| 計画・ロードマップ | `docs/01_planning/` | DEVELOPMENT_ROADMAP_2026.md |
| 作業レポート | `docs/04_reports/` | REPORT_TASK_XXX.md |
| タスクチケット | `docs/tasks/` | TASK_XXX.md |
| 変更提案 | `openspec/changes/{name}/` | proposal.md, specs/, tasks.md |

### 4.2 ドキュメント作成の義務

以下の場合、ドキュメント更新が**必須**:
- asmdef の追加・変更 → ASSEMBLY_ARCHITECTURE.md
- 新規アセンブリの追加 → ASSEMBLY_ARCHITECTURE.md + 依存グラフ更新
- 既存の設計パターンの変更 → 該当する設計ドキュメント
- タスク完了 → レポート (docs/inbox/ または docs/04_reports/)

### 4.3 散逸防止
- 同じ情報を複数箇所に書かない。SSOT を 1 つ決め、他からはリンクする。
- 新しいドキュメントを作る前に、既存ドキュメントで対応できないか確認する。
- ルートディレクトリにドキュメントファイルを増やさない。`docs/` 配下に配置する。

---

## 5. Orchestrator 固有の責務

- タスクチケットに **変更対象アセンブリ名** と **ASSEMBLY_ARCHITECTURE.md 参照** を含める
- Worker 完了レポートに「コンパイル確認済み」がない場合、DONE にしない
- 構造的整合性 (型の重複、循環参照) の確認を Worker に明示的に依頼する
- 機能実装だけでなく、コード品質・アーキテクチャ整合の DoD を設定する

---

## 6. Worker 固有の責務

- 実装前に ASSEMBLY_ARCHITECTURE.md を読み、影響アセンブリを特定する
- 実装後に COMPILATION_GUARD_PROTOCOL.md の「最小検証」を実施する
- エラー発生時は診断フローに従い、レポートに原因・対応を記録する
- レポートに以下を必ず含める:
  - 変更したファイルとそのアセンブリ名
  - 追加した using / asmdef 参照の一覧
  - コンパイル確認結果
  - (該当する場合) ASSEMBLY_ARCHITECTURE.md への更新内容

---

## 7. 品質ゲート

変更をアーカイブ (完了) とする前に、以下すべてを満たすこと:

- [ ] Unity Editor でコンパイルエラーなし
- [ ] 既存機能への breaking change なし
- [ ] ASSEMBLY_ARCHITECTURE.md が最新状態
- [ ] 同名型の重複なし
- [ ] 循環参照なし
- [ ] テスト通過 (該当する場合)
- [ ] 作業レポート作成済み

---

## 8. 緊急時手順

コンパイルが壊れた場合:
1. **即座にコード変更を止める。**
2. `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md` の診断フローに従う。
3. 各エラーの**原因を特定**してから修正する（場当たり修正禁止）。
4. 全エラーが消えたことを確認してから次の作業に進む。
5. 原因がアーキテクチャの問題であれば、ASSEMBLY_ARCHITECTURE.md を更新する。
