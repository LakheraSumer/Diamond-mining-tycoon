# Reference-directed update — 25 October 2026

The existing game now has revised character sources, six authored scene contexts and a measured eight-screen interface. **The requested 95/100 character target and exact reference appearance are not achieved or certified.** The latest combined revision has now passed the recorded Unity functional checks and produced a signed development Android APK. Physical-device testing and visual acceptance remain pending.

## What changed

- Twelve editable Blender sources and twelve FBX exports, with UVs, materials, four skinned mesh groups, 47 bones including 30 finger bones, and ten distinct clips: Idle, Walk, Run, Mine, Collect, Carry, Wave, Celebrate, Machine and Craft. Tavi and Mira received a second face revision: eyelids join the head mesh, eye protrusion is reduced, and the actual mouth geometry smiles. This improves specific defects; it is not visual acceptance.
- Every named character folder contains all ten requested numbered files, plus an individually labelled reference-versus-model comparison. Full-body renders are native 1200x1600; face renders are native 1200x1200. Other dimensions and composite operations are recorded in `model-views.json`. Composite canvas size does not mean native 4K rendering.
- Explore, Mine, Factory, Workshop, Boutique and RegionMap have editable Blender sources, exported meshes, UVs and persistent Unity materials. Main loads their prefab through `GameplayEnvironmentSet`; the old primitive yard construction path is skipped. The original Works mode remains available and has its own render layer so its scenery and shadows cannot overlap the yard.
- Eight screens use an editable measurement manifest and real uGUI components. Live actions call GameSession; the isolated reference mode has a visible DEMO label and disabled actions. Live rankings show the local record without invented competitors. Native 2160x3840 portrait and 1536x2048 tablet screenshots have been captured and their dimensions verified.
- Eleven dimensional prop icons were rendered from authored 3D geometry at native 512x512 with transparency. Twelve portraits come from the actual model face renders. Nunito and the renamed static bold derivative include OFL licensing and provenance.
- Live navigation now includes More, character selection, inventory, hiring/assignment and refining copper/silver/gold. The live dock scrolls horizontally when needed to retain wider touch targets. These actions preserve the existing rules, prices and saves. This also restores metal refining as a reachable step before jewellery crafting.

## Evidence and its limits

| Evidence | Result | Scope |
| --- | --- | --- |
| Simulation tests | 85 passed, zero failed/skipped | Existing authoritative rules; source/config hashes recorded in `tests/results/verification-summary.json` |
| Installed Unity API compiler | Six configurations passed on latest source | uGUI, simulation, Android, development Android, iOS and editor compilation; not a player build |
| Numbered character views | 12 characters x 10 requested files passed completeness and current-source hash checks | `ArtSource/Characters/view-validation.json`; not a likeness score |
| Latest Main Play review | 517 checks passed, zero captured errors | Twelve characters, animation curves/states, real movement/mining/cart delivery, five-stage UI processing, refining, crafting, selling, task reward replay protection and actual save reload |
| Latest UI capture | Eight source-aspect screens; 221 recorded element rectangles passed the 0.5% implementation target | Includes latest icons, fonts, feedback and scene-layer fixes; source measurement uncertainty remains separate |
| Animation/high-resolution evidence | 120 pose samples plus native 2160x3840 and 1536x2048 screenshots | Actual Unity camera/Game-view captures; sampled poses do not certify every continuous deformation frame |

UI bounds are checked against manually measured decoded JPEG crops. The named original PNG boards were unavailable. Recorded source-edge uncertainty is 1.5 native pixels and can exceed 0.5% of a panel dimension. Bounds agreement does not certify typography, icons, camera composition, lighting or total visual identity.

The fresh test APK is `Builds/Android/DiamondMineTycoon-0.1.1-dev.apk`, 94,246,063 bytes, package `com.diamondmine.tycoon.qa`, version `0.1.1-qa-dev`. It supports API25+ (Android7.1+) on ARM64 and X86_64. Its SHA-256 is `440f4d0be1d84fcf4b29d857fabe26237a1f3d773db430d3ee4713e9bbbd05a6`. The build succeeded with zero errors and14 nonfatal warnings (SDK metadata, deprecated X86_64 target and compressed application-icon quality). Signing, ZIP alignment and native ELF16KB header checks passed. Current Assets/Packages/ProjectSettings hashes match the recorded build inputs. See `release/reference-build-results.json` and `Builds/inspection/DiamondMineTycoon-0.1.1-dev/summary.json`.

This is a development APK signed with the local Android debug certificate, for direct testing rather than store publication. Development builds request INTERNET for development tooling; no production ads, purchase or analytics SDK was added. Static packaging checks do not establish phone installation, rendering speed, touch usability, traffic behaviour or thermal performance. No physical Android device test, latest iOS export or signed store release is claimed.

## Review execution and scripts

The earlier automatic-approval usage limit no longer blocked this run. Final import, Main Play checks, eight-screen capture, comparisons, rules/API verification, development APK build and strict update-package validation completed successfully. The screenshot test now separates capture from screen changes and waits for real cart simulation progress rather than a single wall-clock sample.

Double-click `RUN_FULL_GAME_TEST.cmd` for the complete local review, or `BUILD_ANDROID_TEST.cmd` to review and rebuild the APK. The underlying command is `tools/Resume-ReferenceValidation.ps1 -AndroidApk`. All gameplay fixtures use private QA saves; normal saves are preserved. `-BuildPlayers` also requests an iOS export in a separate build workspace, which was not run here.

The verified source/evidence archive is `release/artwork/DiamondMineTycoon-Reference-Update.zip`. The earlier `Source-Draft.zip` is historical and superseded. Package verification confirms exact evidence/input hashes and GUID closure; it does not accept the unfinished visual likeness.

## Inspect and play manually

Open `VIEW_ALL_IMAGES.html` for source references, current model views and explicitly labelled historical Unity captures. Open `ArtSource/Characters/Tavi/10_Turnaround.png` and `Mira/10_Turnaround.png` first. Sources and game assets remain in the existing project.

For laptop testing, double-click `PLAY_GAME.cmd`, wait for Unity import, open `Assets/_Project/Scenes/Main.unity`, then choose **Diamond Mine > Characters > Play crew with isolated save**. The Crew preview window unlocks all twelve only in that separate test save. Move with the joystick or a ground click, approach a bunker, Interact, Mine, Send cart to mill, and use Process. Select a processing stage to Load/Collect. Use Process > Refinery for metal bars, then Craft and Sell. More exposes inventory, character changes and worker controls. Stop Play to leave the isolated test save. See `Docs/TESTING.md` for full steps.

## Remaining visual work

Tavi/Mira still need closer face proportions, cheeks/jaw, hair volume, scarf folds, shoulder/elbow construction and convincing hand-to-prop contact. The other ten share further face, garment, hair and pose differences. Side/back geometry is reconstructed. Environments still differ in terrain density, foliage, machinery detail, room framing and lighting. No character has an accepted subjective fidelity score. Mobile LODs, material atlases, physical touch usability, FPS, memory and thermal checks remain unverified.

The available modelling tools and import pipeline work. The remaining art work is detailed reconstruction and review, not a missing generic asset pack. Original 3D source geometry was not supplied, so hidden geometry and exact identity cannot be verified from these images alone.
