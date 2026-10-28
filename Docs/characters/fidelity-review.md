# Character fidelity review protocol

The owner's target is very high fidelity, aiming for a **subjective review score of at least 95/100** under this rubric. That number is an acceptance target, not an existing result, a percentage of identical pixels, or a percentage of recovered geometry. **No character currently has an assigned score under this protocol.** Successful exports, large triangle counts, passing compilation and gameplay checks do not establish visual fidelity.

## Reference authority and limits

Use the original visible roster identity first, the original gameplay board for scene composition second, the supplied per-character generated sheets third, and conservative inference for unseen details last. Follow [the discrepancy log](reference-authority.md). In particular, Tavi keeps the roster's helmet goggles; Etta has a hairband and no miner helmet; Rumi keeps the visible compact bob; Sola keeps the original wavy bob rather than the auxiliary bun; Beni keeps a delivery jacket and a grounded trolley.

The named original roster PNG is absent from the accessible files. The current source is the preserved **1024 × 682 JPEG copy**, with individual character crops approximately 150–195 pixels wide. It supports silhouette, broad facial arrangement, colour regions, clothing layers and major props. It cannot support exact claims about pore texture, tiny stitching, minute engraved hardware or original UVs. The crop operation preserves its decoded pixels and records hashes in `ArtSource/Characters/References/authority-manifest.json`.

Side, back and hidden surfaces are reconstructed and are excluded from any claim of verified source likeness. They are separately reviewed for consistency, clean construction and deformation. Auxiliary sheets are proposed generated guidance, not exact orthographic drawings or proof of original hidden geometry.

## Scoring categories

Each category is manually rated from 0 to 5 by an identified reviewer after inspecting the evidence. Record a written reason and the remaining differences for every rating. Category contribution = rating ÷ 5 × weight. Sum the contributions to obtain the subjective score out of 100. Do not round 94.6 up to certify the 95 target. Do not copy a score between characters.

| Category | Weight | Required observations |
| --- | ---: | --- |
| Silhouette and proportions | 25 | Head-to-body ratio, helmet-to-head ratio, shoulder width, torso/waist/hip volume, limb lengths, boot/glove mass, individual stance and major outer contour |
| Face and hair | 30 | Eye shape and spacing, eyelid integration, brows, nose, cheeks, mouth/expression, jaw/chin, hairline, visible waves/coils, parting and overall hair mass; facial hair where applicable |
| Clothing construction | 20 | Correct garment type, open/closed layers, apron/bib length and volume, scarf wrapping, sleeve/cuff placement, trouser cut, knee/elbow protection, pocket and seam placement |
| Colour and material appearance | 10 | Broad colour regions, relative lightness/saturation, skin/cloth/leather/rubber/metal/glass distinction and plausible roughness; differences caused by unknown source lighting must be recorded |
| Accessories and pose | 15 | Correct visible tools, belt and pouch placement, cart/tray/pan/box/trolley scale, hand contact, signature pose and safe tool direction |

Shared rating anchors apply to every category:

| Rating | Meaning |
| ---: | --- |
| 0 | Missing or wrong defining design; no credible correspondence |
| 1 | Broad category resemblance only; major identity or construction errors |
| 2 | Several correct features, but large visible deviations at gameplay and review sizes |
| 3 | Recognisable reconstruction with substantial remaining face, form or detail differences |
| 4 | Close at gameplay size; noticeable differences remain in matched close-up review |
| 5 | No material visible discrepancy at the available reference resolution under the recorded matched comparison; inferred surfaces are not thereby verified |

## Critical gates override the total

A numerical total cannot pass a character while any critical gate fails:

- Correct defining headwear and visible hairstyle. Etta, Kavi and Sola must not acquire a miner helmet. A generic ribbed cap cannot substitute for the source's hair silhouette.
- Readable, friendly reference-directed face. Obvious protruding eye discs, a materially wrong jaw, a mismatched expression, or an unrecognisable face fails this gate even if the palette is correct.
- No visible holes, detached shoulders/cuffs, missing neck surfaces, severe intersections, floating clothing, or large depth-sorting defects in the reviewed views and required animations.
- Defining garment is present with the correct scale and construction: Tavi's layered cream scarf, Mira's orange bib overalls, Pavi's long waterproof apron, Kavi's maroon leather apron, Sola's tailored waistcoat, and Beni's blue delivery jacket are examples.
- Signature prop is present, recognisable and supported. Hands must make plausible contact; a cart or trolley rests on its wheels and does not move as a suitcase attached to a wrist. Pavi holds the pan and Nilo holds the tray with both hands.
- Tavi and Mira establish the art standard before the remaining roster is described as completed. Shared topology or rigging does not excuse identical visible faces, silhouettes and poses.
- The imported model is active in actual Main Play mode, without primitive substitution, and movement/mining/cart interaction still work. Required poses deform without visible shoulder, elbow, wrist, hip, knee or ankle failure.

If a gate fails, mark **Not accepted** regardless of the arithmetic score. Fix and recapture it before evaluating acceptance again.

## Required comparison evidence

For each character, retain the exact source crop, actual native model render, native Unity Play-mode capture, labelled side-by-side comparison, gameplay-size preview, and a written remaining-differences list. Keep individual front, left, right, back, face, accessory and material images in addition to the combined turnaround.

Record the source/model/FBX/prefab hashes, Unity and Blender versions, capture dimensions, camera projection and parameters, frame/pose, lighting setup, quality settings and whether the view contains inferred surfaces. Match camera yaw/elevation, body framing and the visible pose before assigning pose/silhouette ratings. If source camera or lighting cannot be recovered, mark those factors as approximate and describe their effect. Do not label an uncalibrated comparison as a measured exact match.

Use the original source crop enlarged only for inspection, clearly labelled as enlarged. Do not stretch it. Do not repaint, smooth or regenerate the source identity reference. A contact sheet may resize images uniformly for layout, but must not be labelled native 4K merely because its composite canvas is large.

Where useful, provide a 50% image overlay after recording the alignment transform. An overlay or image-difference metric is a diagnostic, not a substitute for identity and construction review. Background pixels, lighting and pose can dominate global image metrics; do not use such metrics to hide a wrong face or missing prop.

## Review record template

| Field | Entry |
| --- | --- |
| Character / persistent ID | |
| Reviewer / review date | |
| Exact roster source and crop hash | |
| Actual model source / FBX / prefab hash | |
| Actual Unity capture, native dimensions and method | |
| Camera / pose / lighting alignment and limitations | |
| Five category ratings, written reasons and weighted contributions | Unscored until completed |
| Critical gates | Unreviewed / pass / fail, with evidence for each |
| Inferred surfaces excluded from verified likeness | |
| Remaining differences and next correction | |
| Result | Not reviewed / Not accepted / Accepted under this rubric |

## Current independent look-development observations

The Tavi Blender look-development preview at `release/screenshots/characters/tavi-blender.png` has useful helmet/lamp/goggle construction, seam layering, boot soles/laces and an asymmetrical pouch. It still shows visible differences in eye/lid integration, facial proportions and expression, hair mass, scarf layering, shoulder closure, map grip and trouser/knee silhouette. Its eyes read as separate circular inserts, its mouth is less cheerful than the source, the sleeve joins need deformation review, and the source's fabric folds are simplified. This preview is an intermediate look-development observation. It is not a score, acceptance, or Unity validation of the latest exported source.

The latest exporter audit reports twelve sources with four skinned mesh groups, 47 bones and ten clips each. It reports 60,680–91,434 triangles per character. Those are file-generation observations, not evidence of a target-device performance budget. `Verify-CharacterViews.py` separately checks complete numbered images and source/render hashes; it cannot judge likeness, camera matching, clean silhouette, plausible hand contact or animation quality.

The final model renders must be reviewed afresh after export. An older view or Unity image is not current evidence simply because its filename still exists. A character remains **Not reviewed for this source revision** until the captured `.blend` hash matches the current file and the Main Play-mode evidence identifies the imported FBX/prefab revision. No 95/100 result has been awarded.
