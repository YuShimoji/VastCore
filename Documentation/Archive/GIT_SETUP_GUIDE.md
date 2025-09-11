# Vastcore Git セットアップガイド

## 🎯 目的
Vastcoreプロジェクトのバージョン管理を効率的に行い、開発の巻き戻しやバックアップを可能にします。

## 📋 前提条件

### 1. Gitのインストール
Gitがインストールされていない場合は、以下からダウンロードしてインストールしてください：
- **Windows**: https://git-scm.com/download/win
- **Mac**: https://git-scm.com/download/mac
- **Linux**: パッケージマネージャーでインストール

### 2. GitHubアカウント（推奨）
無料のGitHubアカウントを作成することをお勧めします：
- https://github.com/

## 🔧 初期セットアップ

### 1. Gitの初期化
```bash
# プロジェクトディレクトリで実行
git init

# ユーザー情報の設定（初回のみ）
git config --global user.name "あなたの名前"
git config --global user.email "your.email@example.com"
```

### 2. 初回コミット
```bash
# 現在の状態をステージング
git add .

# 初回コミット
git commit -m "Initial commit: Vastcore project setup with Structure Generator"
```

## 📊 バージョン管理されるファイル

### ✅ 管理対象（重要ファイル）
- `Assets/` - ゲームアセット
- `ProjectSettings/` - プロジェクト設定
- `Packages/manifest.json` - パッケージ設定
- `*.cs` - C#スクリプト
- `*.md` - ドキュメント
- `*.unity` - シーン
- `*.prefab` - プレハブ
- `*.mat` - マテリアル

### ❌ 除外対象（容量削減）
- `Library/` - Unity生成キャッシュ
- `Temp/` - 一時ファイル
- `Logs/` - ログファイル
- `UserSettings/` - ユーザー設定
- `*.csproj` - 自動生成プロジェクトファイル

## 🚀 基本的な使用方法

### 1. 変更の保存（コミット）
```bash
# 変更されたファイルを確認
git status

# 変更をステージング
git add .

# コミット（変更を記録）
git commit -m "機能追加: Boolean操作の修正完了"
```

### 2. 履歴の確認
```bash
# コミット履歴を表示
git log --oneline

# 特定のファイルの変更履歴
git log --oneline -- Assets/Editor/OperationsTab.cs
```

### 3. 巻き戻し
```bash
# 特定のコミットに戻る（危険：変更が失われる）
git reset --hard <コミットID>

# 安全な巻き戻し（新しいコミットを作成）
git revert <コミットID>

# 作業ディレクトリの変更を破棄
git checkout -- <ファイル名>
```

## 🌐 GitHubとの連携（推奨）

### 1. リモートリポジトリの追加
```bash
# GitHubでリポジトリを作成後
git remote add origin https://github.com/yourusername/vastcore.git

# 初回プッシュ
git push -u origin main
```

### 2. 定期的なバックアップ
```bash
# 変更をGitHubにプッシュ
git push origin main

# GitHubから最新版を取得
git pull origin main
```

## 💡 推奨ワークフロー

### 1. 開発開始時
```bash
# 最新版を取得
git pull origin main

# 新しい機能ブランチを作成
git checkout -b feature/new-function
```

### 2. 開発中
```bash
# 定期的にコミット
git add .
git commit -m "進捗: Phase 5実装中"

# 重要な節目でプッシュ
git push origin feature/new-function
```

### 3. 機能完成時
```bash
# メインブランチに戻る
git checkout main

# 機能ブランチをマージ
git merge feature/new-function

# リモートにプッシュ
git push origin main
```

## 📦 容量管理

### 現在の設定での予想容量
- **通常のコミット**: 1-5MB
- **大きなアセット追加時**: 10-50MB
- **GitHubの無料制限**: 1GBまで

### 容量削減のコツ
1. **大きなファイルの除外**: `.gitignore`で自動除外
2. **定期的なクリーンアップ**: 不要なアセットの削除
3. **Git LFS使用**: 大きなファイルは別管理

## 🔍 トラブルシューティング

### 1. 容量オーバー
```bash
# 大きなファイルを確認
git ls-files | xargs ls -la | sort -k5 -n

# 特定のファイルを履歴から削除
git filter-branch --force --index-filter 'git rm --cached --ignore-unmatch <大きなファイル>' --prune-empty --tag-name-filter cat -- --all
```

### 2. 競合の解決
```bash
# 競合が発生した場合
git status
# 競合ファイルを手動で編集
git add <競合ファイル>
git commit -m "競合解決"
```

## 📈 バージョン管理のベストプラクティス

### 1. コミットメッセージの書き方
```
修正: Boolean操作のNullReferenceException修正
追加: Distribution Tabの実装
更新: DEV_PLAN.mdの進捗反映
削除: 不要なテストファイル削除
```

### 2. 定期的なコミット
- 機能完成時
- 重要な修正時
- 作業終了時

### 3. ブランチ戦略
- `main`: 安定版
- `develop`: 開発版
- `feature/*`: 新機能開発
- `hotfix/*`: 緊急修正

## 🎯 次のステップ

1. **Git初期化**: `git init`
2. **初回コミット**: 現在の状態を保存
3. **GitHubリポジトリ作成**: バックアップ先設定
4. **定期的なコミット**: 開発進捗の記録

このガイドに従って、安全で効率的なバージョン管理を始めましょう！ 