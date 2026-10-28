# Test Diamond Mine Tycoon on this laptop

Updated 25 October 2026. You can test the current project in Unity without a mobile phone. An APK requires an Android runtime/emulator; Windows does not run an APK directly. The Unity project is the quickest route to the current source.

## Open the game

1. Double-click `PLAY_GAME.cmd` in `D:\games\DiamondMineTycoon`.
2. Wait for Unity 6000.3.26f1 to finish importing and compiling. Open `Assets/_Project/Scenes/Main.unity`.
3. Stop Play if it is already running. For a separate test save, choose **Diamond Mine > Characters > Play crew with isolated save**. For normal saved progression, press the ordinary Play triangle instead.
4. Set Game view to a portrait resolution such as 720x1280. Click inside Game view so it receives input.
5. In the Crew preview window, choose Tavi or Mira. All twelve are available only in that isolated preview. Use **Turn toward camera** to inspect a face.
6. Drag the lower-left joystick or click the ground to move. Walk near a bunker, press Interact, then Mine. Use **Send cart to mill** and wait for delivery.
7. Open **Process**, select Crush, then Load batch. When finished, select the stage again and Collect if required. Continue Wash, Sort, Cut and Polish using the real inventory.
8. Mine metal ore and send it to the mill. Use **Process > Refinery** or **More > Refinery** to refine copper, silver or gold. The first ring uses the actual recipe shown in Craft; the live recipe can differ from the reference demonstration artwork.
9. Use **Craft**, choose a recipe and Craft. When it completes, use **Sell**. Progress has real task rewards and production upgrades. Expand has real plot prices/ownership. Rankings show an honest local record; the network service is not connected.
10. Swipe the bottom navigation horizontally if some tabs are off-screen. **More** opens character selection, inventory, worker hiring/assignment, settings and the older Works mode. Stop Play when finished. The isolated preview restores the prior save location when it ends.

If a model/prefab is missing after import, stop Play and choose **Diamond Mine > Art > Import all authored assets**, then repeat the preview. Do not interpret an old screenshot or APK as proof of the latest source revision.

## Inspect images and models

Open `VIEW_ALL_IMAGES.html`. The gallery separates reference artwork, current source-matched Blender views and historical or unverified Unity captures. Click an image to enlarge it. Start with the Tavi and Mira turnaround sheets.

The twelve numbered view folders are under `ArtSource/Characters`. Editable `.blend` files are beside them. Six environment sources are under `ArtSource/GameplayEnvironments`; exported models/materials are under `Assets/_Project/Art`. Current limitations and the execution blocker are in [REFERENCE-UPDATE.md](REFERENCE-UPDATE.md).

## Automated checks

Double-click `RUN_FULL_GAME_TEST.cmd` for Unity gameplay and screenshots, or `BUILD_ANDROID_TEST.cmd` to also rebuild the Android APK. The shorter rules/compiler checks remain available through `TEST_GAME.cmd`, or run from the project folder:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Project.ps1
```

This runs the simulation tests and installed-Unity API compiler checks. It does not enter Play mode or certify device graphics. The retained result is 85 tests and six compiler configurations passing.

With the Unity Editor closed, the prepared full review command is:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Resume-ReferenceValidation.ps1
```

It imports, captures real Main Play mode using private saves, checks character states and UI bounds, compares images and creates the verified update ZIP only if the evidence checks pass. The latest full run completed:517 Main Play checks,221 measured UI rectangles,120 pose samples,85 simulation tests and six compiler configurations passed. These are desktop/automated checks, not physical-phone acceptance.

To additionally request supported development Android/iOS export in a separate build workspace:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Resume-ReferenceValidation.ps1 -BuildPlayers
```

An iOS export is an Xcode project, not a signed iPhone app. Mac/Xcode signing and physical device testing remain separate work. A release AAB requires the owner's external upload key and is not produced by this review command. The latest Android test build is Builds/Android/DiamondMineTycoon-0.1.1-dev.apk (94.2MB), API25+, ARM64/X86_64. Signature/alignment/static native checks pass; phone testing remains pending. Use BUILD_ANDROID_TEST.cmd or add -AndroidApk to the review command to regenerate it.
