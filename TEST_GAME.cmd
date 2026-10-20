@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Verify-Project.ps1"
set "DiamondMineTestResult=%errorlevel%"
if "%DiamondMineTestResult%"=="0" (
  echo Game rules and Unity API compiler checks passed.
) else (
  echo Checks failed. Read the output above.
)
echo Reports are under tests\results. These checks do not run the game.
pause
exit /b %DiamondMineTestResult%
