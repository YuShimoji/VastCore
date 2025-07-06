@echo off
echo ========================================
echo Vastcore Git セットアップスクリプト
echo ========================================
echo.

REM Gitがインストールされているかチェック
git --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [エラー] Gitがインストールされていません。
    echo https://git-scm.com/download/win からダウンロードしてください。
    pause
    exit /b 1
)

echo [OK] Gitがインストールされています。
echo.

REM 既にGitリポジトリが初期化されているかチェック
if exist ".git" (
    echo [情報] 既にGitリポジトリが初期化されています。
    echo.
    goto :status
)

echo [実行] Gitリポジトリを初期化しています...
git init
if %errorlevel% neq 0 (
    echo [エラー] Git初期化に失敗しました。
    pause
    exit /b 1
)

echo [OK] Gitリポジトリを初期化しました。
echo.

REM ユーザー設定の確認
echo [確認] Gitユーザー設定をチェックしています...
git config --global user.name >nul 2>&1
if %errorlevel% neq 0 (
    echo [設定] Gitユーザー名を設定してください。
    set /p username="ユーザー名を入力してください: "
    git config --global user.name "%username%"
    
    set /p email="メールアドレスを入力してください: "
    git config --global user.email "%email%"
    
    echo [OK] ユーザー設定を完了しました。
) else (
    echo [OK] ユーザー設定済みです。
)
echo.

REM 初回コミット
echo [実行] 初回コミットを作成しています...
git add .
git commit -m "Initial commit: Vastcore project setup with Structure Generator"
if %errorlevel% neq 0 (
    echo [エラー] 初回コミットに失敗しました。
    pause
    exit /b 1
)

echo [OK] 初回コミットを作成しました。
echo.

:status
echo [状態] 現在のGit状態:
git status --short
echo.

echo [履歴] コミット履歴:
git log --oneline -5
echo.

echo ========================================
echo セットアップ完了！
echo ========================================
echo.
echo 次のステップ:
echo 1. GitHubアカウントを作成（https://github.com/）
echo 2. 新しいリポジトリを作成
echo 3. 以下のコマンドでリモートリポジトリを追加:
echo    git remote add origin https://github.com/yourusername/vastcore.git
echo    git push -u origin main
echo.
echo 詳細は GIT_SETUP_GUIDE.md を参照してください。
echo.
pause 