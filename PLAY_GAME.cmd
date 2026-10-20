@echo off
setlocal
set "DiamondMineUnityEditor=D:\Unity\6000.3.26f1\Editor\Unity.exe"
if not exist "%DiamondMineUnityEditor%" (
  echo Unity 6000.3.26f1 was not found. See Docs\TESTING.md.
  pause
  exit /b 2
)
echo Opening Diamond Mine Tycoon in Unity.
echo Open Assets/_Project/Scenes/Main.unity and press Play.
echo If Unity asks for a license, sign in and activate your eligible plan in Unity Hub.
start "" "%DiamondMineUnityEditor%" -projectPath "%~dp0."
exit /b 0
