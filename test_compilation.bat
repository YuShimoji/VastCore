@echo off
echo ===== Vastcore コンパイルテスト =====
echo.

echo Unity エディタでコンパイルテスト実行中...
"C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe" -batchmode -quit -projectPath "%~dp0" -logFile "%~dp0compile_test.log"

echo.
echo コンパイル結果確認中...
if exist "%~dp0compile_test.log" (
    echo ログファイルが生成されました
    findstr /i "error" "%~dp0compile_test.log" > nul
    if errorlevel 1 (
        echo ✓ コンパイルエラーなし
    ) else (
        echo ❌ コンパイルエラーが検出されました
        echo エラー詳細:
        findstr /i "error" "%~dp0compile_test.log"
    )
) else (
    echo ⚠️ ログファイルが見つかりません
)

echo.
echo テスト完了
pause
