@echo off
echo ===== Vastcore コンパイルテスト =====
echo.

echo Unity テスト（EditMode/PlayMode）を実行中...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\run-tests.ps1" -TestMode all

echo.
if %ERRORLEVEL%==0 (
    echo ✓ テスト成功（EditMode/PlayMode）
) else (
    echo ❌ テスト失敗（詳細は artifacts/logs/ を確認）
    exit /b 1
)

echo.
echo テスト完了
pause
