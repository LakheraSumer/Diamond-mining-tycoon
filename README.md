# Diamond Mine Tycoon

Current reference-directed update, 25 October 2026: [implementation, evidence and remaining work](Docs/REFERENCE-UPDATE.md). [Open the visual gallery](VIEW_ALL_IMAGES.html). Latest Unity checks and the Android development build pass; the reference match and physical-device acceptance remain unfinished.

Working title. Check that the final name is free before publication. Branding and store identifiers live in `Assets/_Project/Scripts/Runtime/Branding.cs`. The values there are development placeholders.

Single-player, offline-first idle mine for Android phones and tablets and for iPhone and iPad. Portrait only. One shared Unity C# project. Diamonds and coins are fictional. There is no account, no multiplayer, and no real-money shop.

## Open and play

1. Install Unity `6000.3.26f1` (changeset `de8167e7440c`) with the Android and iOS modules, OpenJDK 17, and Android NDK r27c (`27.2.12479018`).
2. Open `D:\games\DiamondMineTycoon`.
3. Open `Assets/_Project/Scenes/Main.unity` and press Play.

The scene contains one bootstrap object. It creates the game session, interface, audio and world presentation. Bunker 1 loads the authored Blender models and material prefabs from `Resources/Yard`; it is not rebuilt from runtime primitives. The editor script `DiamondMine.Editor.BuildCommands` applies portrait, IL2CPP, package ids, and SDK levels the first time the editor loads the project.

To inspect the timber bunker without changing your normal save, stop Play mode and choose **Diamond Mine > Art > Play bunker 1 with isolated save**. See [the art source instructions](ArtSource/TimberBunker/README.md) and [the visual review](Docs/Timber-Bunker.md). This pass covers one entrance, its terrain, cart and track; the other authored contexts now have a separate implementation and review status in Docs/REFERENCE-UPDATE.md.

To try all twelve imported, animated crew models on this laptop, choose **Diamond Mine > Characters > Play crew with isolated save**. The Crew preview window enables all twelve only in that test save. [Character review and comparisons](Docs/characters/README.md). These models replace the yard's capsule figures but still differ from the illustrated reference; visual matching is unfinished.

## Test the rules without Unity

The production rules are plain C# in `Assets/_Project/Scripts/Simulation`. From this machine:

```powershell
powershell -File D:\games\DiamondMineTycoon\tools\Run-SimulationTests.ps1
```

That uses the .NET SDK at `D:\dotnet`, the existing NuGet cache, and project-local temporary files. The latest result on 24 October 2026 is 85 tests passed, zero failed/skipped.

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

Updated 24 October 2026: 85 simulation tests passed; all six Unity API compiler configurations passed. The crew review passed 242 checks in actual Main Play mode using an isolated save, including all twelve character selections, bone animation, reduced motion, movement, mining and cart delivery. The earlier timber review passed 20 checks. Physical-device QA and new mobile builds remain pending. See `Docs/PROJECT-STATUS.md`.

The owner targets ages 5+, mainly children. Release builds have no production ads, accounts or purchases. Six gem-box orders are under Goals. The supplied APK is Uptodown App Store, not Lumber Inc; see `analysis/REFERENCE_FINDINGS.md`. The game remains an original implementation.

For the quickest PC test, double-click PLAY_GAME.cmd, open Main.unity and press Play in Unity. Use a portrait Game view. Double-click TEST_GAME.cmd for automated checks. Step-by-step instructions: Docs/TESTING.md.
