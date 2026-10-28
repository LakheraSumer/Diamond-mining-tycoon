# Crew model source

Status, 24 October 2026: twelve 3D reconstructions using the owner's original illustrated lineup as the identity authority, with twelve supplied character sheets as secondary modelling guidance. These replace the old capsule puppets in the yard. They are **not a 100% visual match** to the illustration. Facial likeness, hair, clothing silhouettes, prop posing and surface detail still need art refinement. Unseen surfaces are reconstructed; the auxiliary side/back drawings do not verify original geometry. See [the authority and discrepancy record](../../Docs/characters/reference-authority.md).

Open any of the twelve `.blend` files in Blender 4.5 LTS. The latest exporter audit reports editable meshes, UVs, materials, embedded texture images, a 47-bone deformation rig and ten action clips. Thirty of those bones form individual finger chains. The source is saved before preview studio lights and cameras are added. Shared external PNG files are also delivered in `Assets/_Project/Art/Crew/Textures`. Export reports are separate from actual Unity import, deformation and visual acceptance checks.

| Source | Visible design |
| --- | --- |
| tavi.blend | Teal explorer jacket, scarf, map, goggles and satchel |
| mira.blend | Green shirt and helmet, orange overalls, knee pads and a grounded ore cart |
| oren.blend | Brown skin, beard, plum outer shirt, cream inner shirt, backpack and geological sample |
| zela.blend | Red hair, blue workwear, orange protection, backpack and drill |
| pavi.blend | Round glasses, teal apron, cream sleeves, rubber boots and ore pan |
| nilo.blend | Dark hair, mint helmet, navy workwear and sorting tray |
| etta.blend | Brown skin, curly hair, purple frames, orange jacket and hand tool |
| rumi.blend | Dark bob, pale helmet, violet workwear, cloth and gem |
| kavi.blend | Dark hair and beard, cream shirt, maroon leather apron and inspected ring |
| sola.blend | Blonde hair, coral sleeves, green waistcoat, flats and jewel box |
| beni.blend | Broad torso, yellow helmet, blue delivery jacket and grounded trolley with stacked cases |
| daro.blend | Glasses, yellow helmet, olive overalls, plan and clipboard |

`tools/art/character-designs.json` holds the art colours and role equipment. `tools/art/Create-Crew.py` builds the meshes, UVs, textures, rig, action clips and FBX files. It uses portable Blender at `.cache/modelling/blender-4.5.9-windows-x64/blender.exe` on this PC. Set `DMT_CHARACTER_IDS=tavi` for a single look-development export; omit it for all twelve. Set `DMT_CHARACTER_RENDER=0` to skip optional Blender studio renders. Use `--python-exit-code 1` so export failures fail the command.

The FBX files have four equipment groups: `Body`, `RoleProp`, `MiningTool`, `CarryLoad`. The runtime changes visibility and animator states; it does not rebuild anatomy from primitives. `Idle`, `Walk`, `Run`, `Mine`, `Collect`, `Carry`, `Wave`, `Celebrate`, `Machine`, `Craft` provide ten distinct clips. Collect has a separate reach/lift sequence. Anatomical hands have normalized finger weights and role/tool grip keys. There is no facial-expression rig, lip sync or independent facial animation.

Import in Unity with **Diamond Mine > Characters > Import modelled crew**. It creates persistent materials, twelve animation controllers and the twelve Resources/Characters prefabs. PNG maps include shared fabric/leather normals, role charts and one face map per character. Transparent eyewear uses `CrewLens.shader`. Keep the Animator on the imported hierarchy root so FBX curve paths bind correctly. The built-in Animation package is enabled in `Packages/manifest.json`.

For testing, see [the crew review](../../Docs/characters/README.md). These are development assets: the latest exporter audit reports 60,680–91,434 triangles per character, multiple material slots, no mobile LOD or material atlas yet. Physical-device performance is unmeasured.

Each capitalized character folder contains `01_SourceCrop.png`, the supplied `AuxiliarySheet.png` and a `reference.json` authority record. Run Blender with `tools/art/Render-CharacterViews.py`, then `python tools/art/Assemble-CharacterViews.py`, to create the separate numbered Beauty, Front, Left, Right, Back, Face, Accessories, Materials and Turnaround images. Native full-body dimensions default to 1200 × 1600; composites are labelled layouts, not native 4K renders. The renderer resumes completed characters only when exact source/settings/raw-image hashes match. It never saves changes to the `.blend` sources. The assembler refuses stale source revisions and labels from unmodified raw renders. Run `python tools/art/Verify-CharacterViews.py` to check all ten numbered files and provenance. No tool assigns a likeness score; use [the manual fidelity protocol](../../Docs/characters/fidelity-review.md).

The anatomical head and hand topology and morph target data originate from MakeHuman's **CC0 asset data**. The data used is preserved under `third_party/makehuman`, with exact SHA-256 hashes in `third_party/provenance.json`. No MakeHuman/MPFB addon code is embedded in the generator. The asset licence is included; `LICENSE.md` describes the upstream project's separate code licence. Clothing, hair, equipment, custom rig and clips are authored by this project's generator.
