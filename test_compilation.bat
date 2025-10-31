@echo off
setlocal EnableDelayedExpansion

rem UTF-8コードページに切り替えて文字化けを防止
chcp 65001 > nul

echo ===== Vastcore Compilation Test =====
echo.

rem 絶対パスを明示的に設定
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe"
set "PROJECT_PATH=%~dp0"
set "LOG_FILE=%~dp0compile_test.log"

rem Unity実行ファイルの存在確認
if not exist "%UNITY_EXE%" (
    echo [ERROR] Unity Editor not found at: %UNITY_EXE%
    echo Please check Unity installation path.
    pause
    exit /b 1
)

rem 古いログファイルを削除
if exist "%LOG_FILE%" del "%LOG_FILE%"

echo Running Unity in batch mode...
echo Project: %PROJECT_PATH%
echo Log: %LOG_FILE%
echo.

"%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "%LOG_FILE%"
set "UNITY_EXIT=%ERRORLEVEL%"

echo.
echo Unity process completed. Exit code: !UNITY_EXIT!
echo.

rem ログファイル存在確認
if not exist "%LOG_FILE%" (
    echo [WARN] Log file was not generated.
    echo Unity may not have run correctly.
    pause
    exit /b 1
)

echo Analyzing compilation log...
findstr /i /c:"error" "%LOG_FILE%" > nul
if !ERRORLEVEL! equ 0 (
    echo [FAIL] Compilation errors detected!
    echo --------------------------------------
    findstr /i /c:"error" "%LOG_FILE%"
    echo --------------------------------------
    pause
    exit /b 1
) else (
    echo [PASS] No compilation errors found.
    echo Compilation test successful!
)

echo.
echo Test completed successfully.
pause
endlocal
exit /b 0
