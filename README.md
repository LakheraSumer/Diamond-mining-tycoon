# Diamond Mine Tycoon

Working title. Check that the final name is free before publication. Branding and store identifiers live in `Assets/_Project/Scripts/Runtime/Branding.cs`. The values there are development placeholders.

Single-player, offline-first idle mine for Android phones and tablets and for iPhone and iPad. Portrait only. One shared Unity C# project. Diamonds and coins are fictional. There is no account, no multiplayer, and no real-money shop.

## Open and play

1. Install Unity `6000.3.26f1` (changeset `de8167e7440c`) with the Android and iOS modules, OpenJDK 17, and Android NDK r27c (`27.2.12479018`).
2. Open `D:\games\DiamondMineTycoon`.
3. Open `Assets/_Project/Scenes/Main.unity` and press Play.

The scene contains one bootstrap object. It builds the mine, the interface, and the audio at runtime. The editor script `DiamondMine.Editor.BuildCommands` applies portrait, IL2CPP, package ids, and SDK levels the first time the editor loads the project.

## Test the rules without Unity

The production rules are plain C# in `Assets/_Project/Scripts/Simulation`. From this machine:

```powershell
powershell -File D:\games\DiamondMineTycoon\tools\Run-SimulationTests.ps1
```

That uses the .NET SDK at `D:\dotnet`, the existing NuGet cache, and project-local temporary files. The latest result is 62 tests passed.

## Build

```powershell
powershell -File D:\games\DiamondMineTycoon\tools\Build-Player.ps1 -Target apk
powershell -File D:\games\DiamondMineTycoon\tools\Build-Player.ps1 -Target aab
powershell -File D:\games\DiamondMineTycoon\tools\Build-Player.ps1 -Target ios
```

The script stops if `D:\Unity\6000.3.26f1\Editor\Unity.exe` is missing. A release App Bundle also needs `KEYSTORE_PATH`, `KEYSTORE_PASS`, `KEY_ALIAS`, and `KEY_PASS` outside the repo. An iOS export is an Xcode project, not a signed app.

## Read next

`PROGRESS.md` is the resume point. Design, architecture, economy, QA, costs, privacy, and store drafts are in `Docs/`.

## Current verification

Updated 20 October 2026: 62 automated tests and the installed Unity API compiler checks pass. The normal desktop Unity Editor successfully imported the project and entered Play mode; mining input and increasing ore were observed. The earlier restricted-environment license error is superseded for desktop play. Mobile builds, portrait layout QA and device testing remain pending. See `Docs/Release-Readiness.md` and `Docs/ENVIRONMENT.md`.

The owner targets ages 5+, mainly children. Release builds have no production ads, accounts or purchases. Six gem-box orders are under Goals. The supplied APK is Uptodown App Store, not Lumber Inc; see `analysis/REFERENCE_FINDINGS.md`. The game remains an original implementation.

For the quickest PC test, double-click PLAY_GAME.cmd, open Main.unity and press Play in Unity. Use a portrait Game view. Double-click TEST_GAME.cmd for automated checks. Step-by-step instructions: Docs/TESTING.md.
