# Character sheets

Updated 24 October 2026. Main now instantiates twelve imported, skinned character prefabs through `CrewActor`. The old capsule/cube character assembly has been removed from the yard path. Character IDs and unlock conditions are preserved. The SVG files below are historical geometric briefs.

The original illustrated lineup is the identity authority; twelve additional supplied character sheets guide reconstructed surfaces and equipment. The current meshes are reconstructions and **do not achieve the requested 100% match**. Faces, hairstyles, proportions, clothing construction and equipment posing remain visibly different. Blender and FBX import are available and working; the unresolved work is detailed character art. Matching unseen geometry cannot be verified from the front-view roster. See [reference authority](reference-authority.md) and the [fidelity review protocol](fidelity-review.md).

The [25 October intermediate Tavi inspection](tavi-current-model-review.md) is historical and superseded by a later face refinement. Its recorded hashes and observations do not review the latest source. The [latest source-matched face review](tavi-latest-model-review.md) records the eye-ring and smile improvements and remaining likeness differences. Unity evidence requires its own review; no 95/100 score has been awarded.

## Test on this laptop

1. Open `D:\games\DiamondMineTycoon` in Unity 6000.3.26f1 and wait for import/compilation.
2. Stop Play mode if it is active.
3. Choose **Diamond Mine > Characters > Play crew with isolated save**.
4. Use the **Crew preview** window to select any character. All twelve are unlocked only in this separate test save. **Turn toward camera** shows the face.
5. In Game view, use Move or tap the ground, approach bunker 1, interact, Mine, then Send cart to mill.
6. Stop Play to leave the preview. The previous save location is restored.

For normal gameplay, open Main.unity and press Play; normal character unlocks apply. The existing Android APK predates these models and has not been rebuilt for this pass.

## Evidence and source

- [Actual Unity lineup](../../release/screenshots/characters/crew-unity-lineup.png)
- [Reference versus actual Unity models](../../release/screenshots/characters/reference-vs-unity-crew.png)
- [Tavi crop comparison](../../release/screenshots/characters/tavi-reference-vs-unity.png)
- [Actual Main Play-mode front view](../../release/screenshots/characters/play-mode-character-front.png)
- [Mining](../../release/screenshots/characters/play-mode-character-mining.png) and [cart interaction](../../release/screenshots/characters/play-mode-character-cart.png)
- [Detailed checks](../../release/screenshots/characters/checks.txt), [captured errors](../../release/screenshots/characters/review-errors.txt), [review context](../../release/screenshots/characters/review-context.txt)
- [Editable source instructions](../../ArtSource/Characters/README.md)
- [Meshes, textures, materials and controllers](../../Assets/_Project/Art/Crew)
- [Playable prefabs](../../Assets/_Project/Resources/Characters)

Latest individual Unity images, comparisons and517 Main Play checks use the current47-bone/ten-clip models. There are120 sampled pose images with per-state hash records. Actual selection, leg/arm deformation, UV/material binding, reduced motion, mining, cart delivery and UI production/sale/reward/save behaviour passed. Fresh native screenshots are source-hashed. No character received a95/100 acceptance score, and physical phone performance remains unverified. See [current status](../REFERENCE-UPDATE.md).

Each named folder under `ArtSource/Characters` contains its unchanged source crop and supplied auxiliary sheet. Actual model review outputs are numbered `02_Beauty.png` through `10_Turnaround.png`; side/back surfaces are explicitly reconstructed. `model-views.json` records model SHA-256, camera, pose, lighting, render settings and raw/final image hashes. Run `python tools/art/Verify-CharacterViews.py` to check completeness and source freshness after rendering and assembling. It does not assign a fidelity score.

Reproduce using `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Run-CrewReview.ps1`. It uses `.cache/timber-workspace` as an isolated Unity copy and a unique private QA save. `tools/art/Compare-Crew.py` only crops/resizes/layouts screenshots and adds labels; it does not repaint Unity results.

## Historical sheets and unchanged unlocks

| Sheet | Name | Unlock |
| --- | --- | --- |
| [tavi.svg](tavi.svg) | Tavi Flintstep | Free |
| [mira.svg](mira.svg) | Mira Mosswheel | Free |
| [oren.svg](oren.svg) | Oren Pebblewick | Free |
| [sola.svg](sola.svg) | Sola Shelfstone | Free |
| [zela.svg](zela.svg) | Zela Copperfern | Own 6 bunkers |
| [pavi.svg](pavi.svg) | Pavi Dewstone | Wash one batch |
| [nilo.svg](nilo.svg) | Nilo Quartzpatch | Recover 5 diamonds |
| [etta.svg](etta.svg) | Etta Facetbrook | Cut a diamond |
| [rumi.svg](rumi.svg) | Rumi Glowgrain | Polish a diamond |
| [kavi.svg](kavi.svg) | Kavi Loopforge | Craft a ring |
| [beni.svg](beni.svg) | Beni Trailcart | Finish 3 orders |
| [daro.svg](daro.svg) | Daro Rootlamp | Find another region |
