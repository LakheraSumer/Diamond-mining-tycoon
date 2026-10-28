# Timber bunker source

This folder contains the editable Blender 4.5 LTS source for one mine entrance,
its immediate terrain, track and ore cart. Geometry and texture maps were
created for this project. The supplied world board is a visual reference; its
pixels are not used as game materials.

`TimberBunker.blend` contains four named collections: `TimberBunker`,
`TerrainPatch`, `MineTrack`, and `OreCart`. Bevels are applied in the exported
geometry. Every mesh has UV0 and baked vertex contact occlusion. The source
includes the camera and look-development lighting. `mesh-audit.json` records
the exported mesh counts.

Recreate the source, texture maps, and four FBX exports from the project root:

```powershell
$env:DIAMOND_BLENDER_RENDER = '1'
& '.cache\modelling\blender-4.5.9-windows-x64\blender.exe' -b -t 4 -P 'tools\art\Create-TimberBunker.py'
```

Blender is portable and project-local. No global Blender installation is
required to open or run the Unity project; Unity consumes the exported FBX
files, PNG texture maps, material assets and prefabs.

In Unity, choose **Diamond Mine > Art > Import timber bunker** after a new
export. This builds the material assets and the two prefabs under
`Assets/_Project/Resources/Yard`. `TimberBunkerArea` loads those prefabs in Main.
Missing assets produce an explicit error rather than a primitive substitute.

For a manual check, stop Play mode and choose **Diamond Mine > Art > Play
bunker 1 with isolated save**. It opens Main beside the finished entrance with
Tavi selected. Walk, press **Bunker 1**, scroll down, press **Mine**, then
**Send cart to mill**. The preview restores the prior save-path setting when
Play mode ends.

Run the isolated Main/Play review with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\Run-TimberReview.ps1
```

The review uses `.cache/timber-workspace` and a new save under that workspace's
`.cache/qa-saves`. It does not reset the owner's normal save. Captures and the
interaction report are written under `release/screenshots/timber-bunker`.
