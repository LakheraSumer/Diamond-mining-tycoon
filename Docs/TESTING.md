# Quick testing

## Play on this PC

1. Double-click `PLAY_GAME.cmd` in the project folder.
2. Unity Hub 3.22.3 is installed at `D:\games\installed software\Unity Hub`. Its Personal license is active. The earlier exit **198** occurred in the restricted agent environment; the normal desktop editor successfully imported this project on 20 October. Run Unity from your desktop session. [Official license help](https://docs.unity.com/en-us/hub/manage-license).
3. Open `Assets/_Project/Scenes/Main.unity`, then press the triangular **Play** button in Unity. The game was launched successfully on 20 October. Use a portrait Game view (for example, a custom 720 x 1280 resolution) for mobile layout testing; Free Aspect on an ultrawide monitor is not the target layout.
4. Tap the rock, watch a cart deliver ore and coins arrive, then buy an upgrade. Open and close Goals, Settings and Mines. Orders are inside Goals. Hire a miner once affordable.
5. Stop Play and start again to check saved progress. Leave for two minutes and reopen to check offline progress; new ore requires a hired miner. Try sound levels and reduced motion.

## Automated checks

Double-click `TEST_GAME.cmd`. It runs the same production-rule tests and compiler/API checks used here and leaves results visible. Or run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Project.ps1
```

Results: `tests/results/simulation.trx`, `unity-api-compile.txt`, and `verification-summary.json`. Automated rules/API checks do not run Unity gameplay or validate phone graphics.

## Android

After editor activation and a successful PC play-test:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Build-Player.ps1 -Target apk
```

Expected successful output: `Builds/Android/DiamondMineTycoon-0.1.0-dev.apk`. It does not exist yet. Test on a spare Android device or isolated emulator after the build succeeds. No device install is automated. iPhone/iPad testing additionally needs a Mac/Xcode build and signing.
