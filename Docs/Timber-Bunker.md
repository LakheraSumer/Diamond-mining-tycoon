# Timber bunker visual slice — 24 October 2026

The existing gameplay is retained. This pass authors bunker 1 and its immediate
terrain, rail and cart. Explore, mining, processing, crafting, selling, expansion,
progress and practice rankings keep their existing rules and interface. The
other bunkers, production stations, characters and eight-screen visual design
have not received this level of art. The supplied board is a direction for the
single-area composition, not a claim of pixel-identical reproduction.

## Assets and integration

Blender was not installed in the checked application locations or PATH. Official
portable Blender 4.5.9 LTS was downloaded to `.cache/modelling`, checked against
Blender's published SHA256, and used to author and export the meshes. Unity does
not require Blender to load the exported assets.

- `ArtSource/TimberBunker/TimberBunker.blend`: editable source, four collections,
  look-development lights and camera.
- `Assets/_Project/Art/TimberBunker/Models`: four FBX files for the timber portal,
  terrain patch, shaped track and ore cart. Bevels are applied to geometry.
- `Assets/_Project/Art/TimberBunker/Textures`: eight original PNG maps: albedo and
  normal maps for oak, rock, packed earth and forged metal, plus a fixed soft-sky
  reflection cubemap authored in Unity for the metal surfaces.
- `Assets/_Project/Art/TimberBunker/Materials`: 24 persistent Unity materials.
- `Assets/_Project/Resources/Yard`: the bunker and cart prefabs used by Main.
- `Assets/_Project/Art/Shaders/TimberSurface.shader`: textured Standard lighting
  with exported vertex contact occlusion and an emissive lantern.
- `tools/art/Create-TimberBunker.py`: reproducible offline mesh/UV/material
  creation and FBX export. No reference pixels are used as game materials.

The portal has bevelled oak beams, knee braces, iron brackets, bolts, an embossed
number board, a cage lantern and a two-metre recessed tunnel. The cliff uses
irregular rock hulls with applied bevels. The trail has a layered rock edge,
embedded stones, moss, grass blades, fern leaves and flowers. The track has a
continuous head/web/foot profile, curved rails, sleepers, chairs and spikes.
The cart has a thick open tapered metal hopper, rim, straps, rivets, chassis,
axles, flanged wheels, bearings, couplers and a loose ore load. Its four wheel
meshes retain axle-centred pivots.

`YardArt.BuildSite` skips the old bunker/cart generator for plot 0 and instantiates
`TimberBunkerArea`. Missing authored prefabs report their exact Resources path;
there is no primitive fallback for that area. The older yard ground is depressed
under the new trail. `YardWorld` uses the imported cart for transport, follows the
curved local rail into the yard main line, and hides the parked cart during a
trip. Simulation quantities, trip duration and grants remain unchanged. The mine
light is disabled while its camera is hidden, avoiding a second directional
light over the yard.

The existing walking actor had scaled mesh objects used as parent joints, which
shrunk its descendants. Those joints now have unit scale and separate mesh
children. This fixes visibility and arm animation; it does not replace the
character blockout with the illustrated character lineup.

## Evidence

The review runs the actual Main scene in Unity 6000.3.26f1 Play mode in
`.cache/timber-workspace`, with a newly created save under `.cache/qa-saves`.
It uses the game's existing tap-to-move controller and synthetic Unity UI pointer
events. It does not use the owner's normal save or reset normal progress.

- `release/screenshots/timber-bunker/play-mode-main.png`: actual 720×1280
  Game-view ScreenCapture, including the existing UI and surrounding blockout.
- `play-mode-mine-interaction.png`: the real mining panel.
- `play-mode-cart-moving.png`: the imported cart during a real haul transaction.
- `unity-closeup.png`: a Unity camera render made while Main is playing, using
  the same instantiated assets and lights. The camera excludes UI and the
  unfinished surrounding plots for this close-up.
- `reference-vs-unity.png`: supplied reference crop on the left; Unity render
  on the right. `comparison.txt` records the source crop exactly. The source
  crop is only 120×133 pixels and is enlarged for inspection.
- `checks.txt`, `interaction-results.txt`, `review-errors.txt`: the final
  integration results. Adjacent screenshot text records capture dimensions and
  provenance. `ArtSource/TimberBunker/mesh-audit.json` records mesh/UV counts.

The first import exposed a reversed FBX depth axis; it was corrected in export
and a portal-facing regression check was added. Cart wheels were combined into
four meshes with proper axle pivots. Visual iterations are retained in the
project cache; the release screenshot folder holds the latest capture.

The final rendered review passed 20/20 integration checks with zero captured
errors. The normal C# simulation suite passed 85/85 tests with zero failures/skips.
The installed-Unity API checks passed for simulation, Android, Android
development, iOS and editor. Rendered integration covers asset provenance, UV0
and vertex AO, wheel pivots, movement, bunker interaction, mining, cart motion,
parked-cart visibility and exact warehouse delivery.

## Manual review

1. Open the project in Unity 6000.3.26f1 and stop Play mode.
2. Choose **Diamond Mine > Art > Play bunker 1 with isolated save**.
3. Walk beside bunker 1, press **Bunker 1**, scroll down and press **Mine**.
4. Press **Send cart to mill**, return to the world and watch the cart move.
5. Verify the carried amount appears in warehouse inventory after delivery.

For reproducible capture, run
`powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Run-TimberReview.ps1`.
The manual preview restores the previous editor save-path environment setting
when Play mode ends.

This art pass does not rebuild the earlier APK. Physical phone/tablet performance,
touch ergonomics and battery/thermal behaviour are not established by these
editor results. Full eight-screen visual parity with the gameplay board remains
pending; online rankings are still disabled as documented by the existing game.
