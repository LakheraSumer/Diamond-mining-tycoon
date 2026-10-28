# Reference UI implementation and measurement limits

The editable source is `Docs/UI-Reference/layout-manifest.json`. Its source image is the preserved `Docs/design-boards/gameplay-board.jpg`, 1024 x 682 pixels, SHA-256 `c585dc10bb0249f25f6a0c80a1c1791d9bea36b62e427955147621e017686492`. The requested original PNG was not found. Crops preserve decoded JPEG pixels; original-PNG pixel identity is unverified.

## Panel extraction

Each screen has its own measured content rectangle. Board title, outer gutters and the whole presentation-ribbon horizontal strip are excluded. This conservative strip exclusion removes some background behind the ribbon as well; it does not reconstruct hidden pixels.

| Internal screen | Display name | Content size | Review viewport |
| --- | --- | --- | --- |
| explore | Explore | 247 x 284 | 988 x 1136 |
| mine | Mine | 247 x 284 | 988 x 1136 |
| factory | Process | 270 x 284 | 1080 x 1136 |
| craft | Craft | 225 x 284 | 900 x 1136 |
| shop | Sell | 247 x 295 | 988 x 1180 |
| map | Expand | 247 x 295 | 988 x 1180 |
| tasks | Progress | 246 x 295 | 984 x 1180 |
| rank | Rankings | 243 x 295 | 972 x 1180 |

`Source.png` in each screen folder is the exact content crop. `Measured-Bounds.png` annotates the manually inspected element rectangles. The manifest records source and crop pixel rectangles, normalized rectangles, anchors/pivot, z-order, padding, radius, font relative size, appearance tokens, interaction and source-edge uncertainty. Bounds use a top-left origin. Runtime converts y to Unity bottom-left anchors as `anchorMin.y = 1 - v - h`, `anchorMax.y = 1 - v`.

## What is implemented

`YardReferenceUi.cs` builds actual editable uGUI components over the existing 3D game. It uses the measured arrays in `Resources/Ui/reference-layout-runtime.json`, generated with `python tools/art/Sync-ReferenceUiLayout.py`. No source screenshot is a game background.

- Explore: compact top currency, joystick, Home and circular Interact; no central card.
- Mine: separate upper-right inventory/carry cards, circular Mine, real current plot reserves. Live play also provides Send cart to mill.
- Process: five independent stage labels/badges, arrows, lower-left input summary and selected-machine Upgrade. Selecting a stage opens real Load/Collect details.
- Craft: Ring/Necklace/Earrings initial carousel, selectable recipes, queue slots/progress and Craft. Next cycles the existing recipe catalogue. Live ingredient counts use the actual recipe definitions.
- Sell: request bubble, product, authoritative catalogue price and Sell; live delivery action uses the current real customer order.
- Expand: generated plot selection, region/page navigation, remaining reserves, authoritative price and purchase. Plot IDs and the 102,400-plot simulation stay unchanged.
- Progress: three real task/reward rows and three real machine upgrades. Live Tasks, Upgrades and Milestones tabs operate on existing rules. The simulation has no pickaxe/cart upgrade rule, so live cards accurately show production machines. Reference-only sample labels match the board without adding economy rules.
- Rankings: reference-only labelled demo rows. Live play shows only the local player's stored record and explicitly says no online/Friends service is connected. The offline footer acknowledges coins already settled on launch; it never grants a second reward.

The old fabricated zero-inventory substitutions, pretend ring stock, fixed live task progress/prices and fake live rankings were removed. Live numbers update in place, preserving held pointers and focus. Actions still call `GameSession` and its transactional save paths.

## Review and live modes

`BeginReferenceReview(screen)` fixes the presentation to that panel's original aspect and uses UI-only deterministic sample values. Buttons are disabled and a visible DEMO / REFERENCE REVIEW watermark identifies the demonstration. Values are never copied into `GameState`. The automated review additionally uses `DIAMOND_REVIEW_SAVE_ROOT` beneath a separate QA directory.

`EndReferenceReview()` returns to live data. Phone/tablet mode uses the device safe area and a live-only navigation band below the measured composition. It adapts normalized anchors rather than claiming identical pixels on a different aspect ratio. Touch targets expand transparently to at least 44 density-adjusted pixels where possible, while preserving visual rectangles. Dense neighbours can constrain touch-target separation. Only Explore accepts joystick/tap movement; opening another context clears the target.

The eight source aspect ratios differ and are not 9:16. Review screenshots use the exact per-panel aspect at 4x decoded native crop size. Native source detail is not increased by enlargement.

## Typography and assets

Nunito is bundled unmodified from the official Google Fonts repository under SIL Open Font License 1.1, with the full copyright/license in `Resources/Fonts/Nunito-OFL.txt`. Source URLs, retrieval time and byte hashes are in `ArtSource/Fonts/source-manifest.json`. Live labels remain text. `UiFactory.Font` loads the included font and falls back to Unity LegacyRuntime only if unavailable.

Bold labels use a static weight-800 derivative named DiamondMine Rounded, with the upstream copyright/license retained. `tools/art/Bake-NunitoBold.py` produces it; `ArtSource/Fonts/bold-derivative.json` records the modifications and hash. This avoids the light default face observed in Unity's variable-font style mapping. Regular body text still uses Nunito.

The exact illustrated reference face and its weight are unidentified. Line breaks, illustrations, card radii/shadows and button highlights require visual review; these have not been certified identical. Home, lock, pin and directional symbols use code-native mesh graphics to avoid missing font glyphs. Progress tracks/fills use a white scalable capsule so colour is not multiplied into a dark precoloured sprite.

`tools/art/Render-UiIcons.py` authors and renders eleven prop icons at a native 512 x 512 pixels with transparent backgrounds. It preserves editable `.blend` files and per-image hashes under `ArtSource/UI/Icons`. The cart icon uses the actual detailed `OreCart` collection from the existing timber model; the remaining helper geometry is authored in that script. These are reconstructed 3D prop renders, not recovered original icon assets. Check `render-manifest.json` for which outputs were actually rendered.

`tools/art/Build-CrewPortraits.py` creates twelve 256px runtime portraits and 128px previews from the actual `07_Face.png` model renders. The original renders are untouched; only the UI derivative is resized, clipped to a circle and given a narrow ivory rim. `ArtSource/UI/crew-portrait-manifest.json` records source/output hashes. Review aliases use a fixed crew mapping, and live rankings use the selected character's portrait. They replace the smiley placeholders without pretending that the offline record is an online ranking.

## Evidence and acceptance interpretation

Run `DiamondMine.Editor.ReferenceUiReview.Run` from an isolated editor. It captures actual Main Game-view screenshots under `release/screenshots/ui-reference/reference` and `live`, plus per-element `*-bounds.json`, checks/errors, and a SHA-256 capture/input manifest. The reference report measures every actual RectTransform with `GetWorldCorners` after layout and compares position/size per axis against the recorded source rectangle, with a normalized 0.005 target. Missing elements fail the implementation check.

Run `python tools/art/Compare-ReferenceUi.py` afterward. It produces labelled side-by-side comparisons, 50% overlays, absolute RGB differences and UI rectangle overlays. It also reports source measurement uncertainty separately. These diagnostic composites are not raw Unity screenshots.

**A rectangle check pass only means runtime rectangles implement the recorded measurements.** Source edges were manually inspected from a small JPEG with approximately 1.5 native pixels uncertainty. That is about 0.61-0.67% of the narrow panel widths and can exceed the requested 0.5% original-source target. Therefore even a numerical runtime pass cannot certify original-PNG 0.5% accuracy. Nor does it certify icons, typography, silhouettes, materials, lighting, camera framing or overall screen identity. No total visual match percentage has been assigned.

Pending until the actual capture/review run: confirm every screen is visually readable, test safe-area adaptations and pointer interactions, inspect font import/rendering, and record concrete remaining image differences. Consult capture results rather than treating this implementation note as passed validation.
