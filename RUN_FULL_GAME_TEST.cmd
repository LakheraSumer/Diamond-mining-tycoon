@echo off
setlocal
cd /d "%~dp0"
echo Diamond Mine Tycoon - full local test and screenshot review
echo Close the Unity Editor before running this test.
echo Tests use isolated saves. Your normal game save is not used.
echo This can take several minutes. Logs are written under Logs.
echo.
if /i "%~1"=="apk" (
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Resume-ReferenceValidation.ps1" -AndroidApk
) else (
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Resume-ReferenceValidation.ps1"
)
set "DiamondMineTestExit=%ERRORLEVEL%"
echo.
if "%DiamondMineTestExit%"=="0" (
  echo Automated validation completed. Visual fidelity and device approval remain separate.
) else (
  echo Test stopped with exit code %DiamondMineTestExit%. Inspect Logs and release\screenshots error files.
)
pause
exit /b %DiamondMineTestExit%
