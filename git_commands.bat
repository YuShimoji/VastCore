@echo off
echo ========================================
echo Vastcore Git コマンド集
echo ========================================
echo.

:menu
echo 1. 現在の状態を確認
echo 2. 変更を保存（コミット）
echo 3. 履歴を表示
echo 4. 巻き戻し
echo 5. GitHubにプッシュ
echo 6. GitHubから取得
echo 7. 終了
echo.
set /p choice="選択してください (1-7): "

if "%choice%"=="1" goto :status
if "%choice%"=="2" goto :commit
if "%choice%"=="3" goto :history
if "%choice%"=="4" goto :rollback
if "%choice%"=="5" goto :push
if "%choice%"=="6" goto :pull
if "%choice%"=="7" goto :exit

echo 無効な選択です。
goto :menu

:status
echo.
echo [状態] 現在のGit状態:
git status
echo.
echo [変更されたファイル]:
git diff --name-only
echo.
pause
goto :menu

:commit
echo.
echo [変更されたファイル]:
git status --short
echo.
set /p message="コミットメッセージを入力してください: "
if "%message%"=="" (
    echo メッセージが空です。
    goto :menu
)

git add .
git commit -m "%message%"
if %errorlevel% neq 0 (
    echo [エラー] コミットに失敗しました。
) else (
    echo [OK] コミットを作成しました。
)
echo.
pause
goto :menu

:history
echo.
echo [履歴] 最新10件のコミット:
git log --oneline -10
echo.
pause
goto :menu

:rollback
echo.
echo [警告] 巻き戻しは慎重に行ってください。
echo.
git log --oneline -10
echo.
set /p commit_id="巻き戻し先のコミットID（最初の7文字）を入力してください: "
if "%commit_id%"=="" (
    echo コミットIDが空です。
    goto :menu
)

echo [確認] %commit_id% に巻き戻しますか？ (y/n)
set /p confirm=""
if not "%confirm%"=="y" goto :menu

git reset --hard %commit_id%
if %errorlevel% neq 0 (
    echo [エラー] 巻き戻しに失敗しました。
) else (
    echo [OK] 巻き戻しを実行しました。
)
echo.
pause
goto :menu

:push
echo.
echo [実行] GitHubにプッシュしています...
git push origin main
if %errorlevel% neq 0 (
    echo [エラー] プッシュに失敗しました。
    echo リモートリポジトリが設定されていない可能性があります。
) else (
    echo [OK] プッシュを完了しました。
)
echo.
pause
goto :menu

:pull
echo.
echo [実行] GitHubから最新版を取得しています...
git pull origin main
if %errorlevel% neq 0 (
    echo [エラー] プルに失敗しました。
) else (
    echo [OK] 最新版を取得しました。
)
echo.
pause
goto :menu

:exit
echo.
echo Git管理を終了します。
echo.
pause
exit /b 0 