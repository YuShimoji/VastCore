# ドキュメント体系化ガイドライン

## 必要なドキュメント（新規プロジェクト用）

### 1. 仕様書（Specifications/）
```
├── FUNCTIONAL_REQUIREMENTS.md    # 機能要件
├── NON_FUNCTIONAL_REQUIREMENTS.md # 非機能要件
├── USER_STORIES.md               # ユーザーストーリー
└── ACCEPTANCE_CRITERIA.md        # 受入基準
```

### 2. 設計書（Architecture/）
```
├── SYSTEM_ARCHITECTURE.md        # システム設計
├── DATABASE_SCHEMA.md            # データ構造
├── API_DESIGN.md                 # API設計
└── CLASS_DIAGRAMS.md             # クラス図
```

### 3. 開発ガイド（Development/）
```
├── CODING_STANDARDS.md           # コーディング規約
├── GIT_WORKFLOW.md              # Git運用ルール
├── BUILD_GUIDE.md               # ビルド手順
└── TESTING_GUIDE.md             # テスト指針
```

### 4. 運用ドキュメント（Operations/）
```
├── DEPLOYMENT_GUIDE.md          # デプロイ手順
├── MONITORING_GUIDE.md          # 監視設定
├── TROUBLESHOOTING.md          # トラブルシューティング
└── MAINTENANCE_LOG.md           # 保守記録
```

## 現在のドキュメント分類

### 削除対象（体系化されていない作業記録）
- DEV_LOG.md
- FUNCTION_TEST_STATUS.md
- COMPILATION_FIX_REPORT.md
- COMPILATION_STATUS_REPORT.md
- CSG_INTEGRATION_LOG.md
- CSG_INVESTIGATION_LOG.md
- PHASE3_DEFORM_TECHNICAL_INVESTIGATION.md
- REFACTORING_HANDOVER_DOCUMENT.md
- TASK_PRIORITIZATION.md
- その他の作業ログ

### 移設対象（有用な情報を含む）
- README.md → 要書き直し
- WORKFLOW_OPTIMIZATION.md → Development/WORKFLOW.md
- CORE_SYSTEM_DEFINITION.md → Architecture/CORE_SYSTEMS.md

### 新規作成が必要
- 明確な機能仕様書
- クリーンアーキテクチャ設計書
- Unity Version Control運用ガイド
- パフォーマンス基準書

## ドキュメント作成・更新ルール

### 1. 命名規則
- 大文字スネークケース（DOCUMENT_NAME.md）
- 日付は含めない（バージョン管理で追跡）
- 具体的で説明的な名前

### 2. 構造規則
```markdown
# ドキュメントタイトル

## 概要
簡潔な説明

## 目的
このドキュメントの目的

## 詳細
### サブセクション1
内容

### サブセクション2
内容

## 更新履歴
| 日付 | バージョン | 変更内容 | 作成者 |
```

### 3. 更新タイミング
- **仕様変更時**: 即座に更新
- **実装完了時**: API仕様を更新
- **バグ修正時**: トラブルシューティング追記
- **リリース時**: 全体レビューと更新

### 4. レビュープロセス
1. 変更前にIssue作成
2. ドキュメント更新
3. プルリクエスト
4. レビューと承認
5. マージ

## アクション項目
- [ ] 不要なドキュメントをArchiveへ移動
- [ ] 必要なドキュメントの骨子を作成
- [ ] 移設用ドキュメントテンプレート準備
- [ ] 新規プロジェクトのREADME作成
