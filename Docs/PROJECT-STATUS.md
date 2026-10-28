# Current project status

25 October 2026: see [the reference update](REFERENCE-UPDATE.md) for current implementation, validation scope, current test APK and remaining fidelity work. The earlier records below are historical and do not certify the latest combined revision. Existing APKs are present; their source/build/inspection limits are described in that update.

# Diamond Mine Tycoon project status

Yard update, 24 October 2026. The 20 October notes below still describe the works mine. They are not a claim that mobile release work finished.

## Yard update

Implemented in the shared simulation and compiled against the Unity 6000.3.26f1 API for Android, iOS, and editor:

- Save version 2. Version 0 and 1 loads keep coins and works upgrades, grant the five starter bunkers once, and keep an already unlocked Deep Core as plot 5 without charging. Rules are in Docs/Save-Migration.md.
- 100 regions by 1,024 plots. Addresses are generated on demand. A full 102,400-plot description pass is inside the automated suite.
- Character choice, twelve cosmetic looks, yard movement rules, five-stage diamond batches, metal refining, ten jewellery recipes, shop sales, scripted orders, tasks, one yard crew, and prestige rules that keep bunker ownership.
- The first scripted journey mines diamond rock, sorts a diamond, makes one ring, sells it, and buys plot 5. Reloading does not create a second ring or a second recovered diamond.
- Online ranking code rejects a raw client total and a replayed event. `RankRules.ProductionEnabled` is false. The yard says the device total is practice only.

Automated result after this change: 84 passed, 0 failed, 0 skipped. Unity API compilation passed for simulation, Android, Android development, iOS, and editor. The runtime compile reported no errors. One inherited-name warning was removed after the run that first showed it.

The later timber-bunker pass below supersedes the Play-mode and screenshot limitation for that specific area. No new Android APK/AAB or iOS export was built for the 24 October yard/art changes. Device QA, FPS, thermal and supervised family play remain unverified. No store submission.

The working title collides with an older Android idle game that store listings also call Diamond Mine Tycoon, package `com.theholycowstudio.diamondmerchant`, and with a Roblox experience named Diamond Mining Tycoon. Full names such as Tavi Flintstep were not found as existing characters in this search. A web search is not a trademark clearance.

## Timber bunker art pass

Authored one detailed timber entrance, recessed tunnel, rocky terrain patch, vegetation, shaped curved rails and metal ore cart in portable Blender 4.5.9 LTS. Exported four FBX models with applied bevels, UVs and vertex contact occlusion, eight texture maps and 24 persistent Unity materials. Main instantiates the two Resources/Yard prefabs; the old primitive bunker/cart generator is skipped for plot 0.

Actual Main Play-mode captures, a close-up Unity render and the supplied-reference comparison are in `release/screenshots/timber-bunker`. All 20 rendered integration checks passed with zero captured errors, covering imported mesh provenance, UVs, wheel pivots, reflection lighting, walking, bunker interaction, mining and exact cart delivery using an isolated save. The simulation suite now passes 85/85, zero failed/skipped, and all installed-Unity API compiler checks passed. Details, source files and manual steps: [Timber bunker review](Timber-Bunker.md).

This is one finished modelled area, not a claim that the full world, character lineup or eight gameplay screens match the concept board. The earlier APK does not include the new art; it must be rebuilt before testing this pass as an Android app.

## Crew reconstruction pass

Twelve Blender 4.5.9 LTS sources and twelve rigged FBX exports now replace the yard's old capsule figures, including the two NPCs. Each has UVs, fabric/leather and face maps, separate equipment meshes, a 17-bone rig, and nine clips serving ten game poses. The Unity assets include 286 persistent materials, eighteen PNG textures, twelve controllers and twelve Resources/Characters prefabs. Runtime character generation no longer substitutes primitives. A stale movement hint that kept the walk animation playing after arrival was fixed.

The final actual Main Play review passed 242 checks with zero captured errors. It exercised every character through the existing selection rule, checked real leg/arm deformation, reduced motion and prop visibility, and confirmed movement, mining and exact cart delivery. The simulation suite passed 85/85; all six installed-Unity API compiler configurations passed. Screenshots and labelled comparisons are under `release/screenshots/characters`; editable sources under `ArtSource/Characters`. [Laptop preview steps and detailed limits](characters/README.md).

The requested 100% visual match is **not achieved**. Faces, hair, proportions, garment detail and prop poses remain different from the single supplied image. Unseen sides are reconstructed. No facial or individual-finger animation, mobile LOD, material atlas or physical-device performance result is provided. No new APK/AAB/iOS build includes this pass yet. Blender, export and Unity import work; detailed art refinement is the outstanding dependency for the requested appearance.

## Earlier status

Evidence reviewed 20 October 2026. Version 0.1.0, build 1.

## Current outcome

The project contains an implemented original Unity idle-mining game. The normal desktop Unity Editor imported the project, opened Main.unity and entered Play mode. Mining input and increasing ore were observed. The retained automated result is 62 passed, zero failed/skipped, and all 25 recorded production C# files still match that verification record.

This is a development project with an initial desktop smoke check. It is not yet a tested Android/iOS release candidate. No APK, AAB, Xcode export, signed iOS archive or store submission exists. The last desktop preview was ultrawide; portrait presentation and complete gameplay need systematic review.

## Original request and reference investigation

The requested product is an English, portrait, low-poly, single-player offline mining tycoon for Android phones/tablets and iPhone/iPad. Existing source was preserved and extended in D:/games/DiamondMineTycoon.

The supplied D:/games/uptodown-com.lumber.inc.apk was preserved and hashed. Static package inspection identified Uptodown App Store, com.uptodown version 7.41, rather than a verified Lumber Inc game. Its gameplay cannot support claims about Lumber Inc. No reference code, models, UI, graphics, audio or credentials were copied into the game. It was not installed or executed. See analysis/REFERENCE_FINDINGS.md.

REA 6.7.0 was prepared from the supplied checkout using pinned Node 24.18.0/npm 11.16.0, locked dependencies and documented Windows native recovery. Its local static artifact inspection worked. No unrelated reverse-engineering engines or agent registrations were added.

## Implemented game systems

| Area | Implemented behavior | Verification limit |
| --- | --- | --- |
| Production | Rock/ore, cart transport, processing/recovery, finishing/sorting, vault, automatic sales and reinvestment | Rules tested; complete visual loop not manually signed off |
| One authoritative simulation | GameSession changes live state; simulation moves resources; presentation reads state | Architecture and production-source tests |
| Three mines | Warmstone Drift, Crystal Cavern and Deep Core; unlocked mines simulate together | Unlock/progression tests; full manual playthrough pending |
| Upgrades | Buy one, ten or maximum; exact affordability; before/after effects; data-driven prices and limits | Automated arithmetic/regression tests |
| Automation | Hire and train miners using fictional coins; no ad prerequisite | Automated progression tests |
| Guidance | Resumable tutorial, one-time objectives and text bottleneck explanations | Core tests; child comprehension/manual navigation pending |
| Orders | Six finite gem-box bonuses under Goals; based on diamonds already sold; no deadline or second sale of stock | Claim/save/prestige duplicate tests |
| Prestige | Mastery preview and second confirmation; resets run resources/upgrades/expansion while retaining specified permanent progress | Core tests; rendered confirmation flow pending |
| Offline | Eight-hour cap; actual equipment/buffer constraints; new ore requires automation; persisted settlement and duplicate prevention | Core tests; device lifecycle tests pending |
| Interface | Mine view, upgrade panel, Goals/Orders, Mines, Prestige, Settings, offline report, credits/privacy and confirmation panels | Initial desktop view only; portrait/touch/tablet review pending |
| Settings | Separate music/effect levels, haptics, reduced motion and quality options | Implemented; device audio/haptics and readability pending |
| Presentation | Original procedural meshes, workers/machinery, carts, faceted gems, palettes, animations and synthesized audio | Development presentation requiring polish/review |

The balance lives in Assets/_Project/Resources/economy.json. Coins are the wallet; diamonds are production goods; mastery is the prestige reward. They have no cash-out or real-money trading function.

Latest retained guided-bot timings without ads or manually collected order bonuses: first sale 8.5 seconds, first upgrade 43.3 seconds, automation 282.3 seconds, Crystal Cavern 836.3 seconds, prestige 2578 seconds, Deep Core 5213 seconds. These are simulated results, not human usability results. See Docs/Economy-Results.md.

## Saves and reliability

Versioned JSON saves, legacy migration, temporary writes, backup replacement, recovery/archival of readable damage and protection of newer-format primary/backup files are implemented. Important purchases/rewards change a cloned state and commit it only after persistence succeeds. Autosave is approximately every 20 seconds, with significant-action and lifecycle save paths.

Parsing bounds size/depth, duplicate fields, invalid numbers, integer/range violations and excessive cart/resource quantities. A SHA-256 envelope detects corruption; it does not prove an honest local balance. Progress stays on the device. Cloud save, account synchronization and guaranteed recovery after uninstall/device loss do not exist.

## Defects already fixed

- Missing uGUI assembly reference and an invalid credits constant expression.
- Missing camera AudioListener.
- Negative purchase counts being interpreted as buy-max, buy-max midpoint overflow and inconsistent affordability checks.
- Invalid save buffers/cart loads/numeric fields, protection of newer backups and preservation of damaged evidence.
- Failed checkpoints advancing the live offline timestamp and failed save transactions partially changing progress.
- Deleting the only recovery backup when the primary was absent and installing its replacement failed; both success/failure regressions were added.
- Modal Close sharing a render-cache sentinel with invalidated panels; old UI objects now deactivate before deferred destruction.
- Gameplay panel entry before state readiness.
- Development reward offers/grants being available in release compilation; release policy now disables both.

Core and API regression checks passed. Full rendered navigation, sound playback and phone interruption behavior still need manual verification.

## Audience, advertising and security

The owner selected ages 5+ with broad family appeal, primarily children. This is a product decision, not an assigned store age rating. The current v1 release implementation is ad-free, with no account, purchases, analytics, chat or deployed backend. Production rewarded ads from the original brief are deferred and are not implemented.

An ad interface and clearly labeled local test provider exist for editor/development builds. Tests cover completion, duplicate completion, cancellation, failure, no-fill, timeout and persistence failure. It does not contact an ad network or provide server verification.

Local validation and reward consistency controls are implemented. IL2CPP, stripping and minimal-permission release settings are configured in source, but binary/signing/manifest verification is pending. A modified offline client can change its local state. Play Integrity, App Attest, provider-side reward verification, protected online balances, authentication and backend deployment are not implemented. Their optional future trust design and test requirements are documented in Docs/THREAT_MODEL.md.

## Environment and execution

Unity 6000.3.26f1 is at D:/Unity/6000.3.26f1 with Android, iOS and Windows modules. Hub 3.22.3 is at D:/games/installed software/Unity Hub and showed an existing Personal activation dated 15 October. Earlier exit198 results came from the restricted launch environment. Removing our conflicting sandbox client and retrying the normal desktop editor resolved licensing for desktop Play. Batch/player builds still need a successful run outside that restriction.

Project-local Android prerequisites are prepared and checksummed: OpenJDK 17.0.18+8, NDK r27c/27.2.12479018, SDK platform/build-tools 36, command-line tools 16.0 and CMake 3.22.1. The existing ADB/emulator tools are available; the last checked state had no attached device or configured emulator. No Mac/Xcode connection, Apple signing identity or store account access was supplied.

## Verification evidence

| Check | Result |
| --- | --- |
| Production-source rules/persistence/local-provider suite | 62 passed, 0 failed, 0 skipped; tests/results/simulation.trx |
| C#9/.NET Standard2.1 Unity API checks | Simulation, Android release/development runtime, iOS runtime and editor passed; tests/results/unity-api-compile.txt |
| Current production C# versus retained verification hashes | 25 files checked, zero differences |
| Real Unity import and Main scene | Passed in normal desktop Editor; Logs/interactive-play.log |
| Initial desktop Play/input | Mining input and increasing ore observed; no compiler/runtime exception matches in the reviewed desktop log |
| Full portrait playthrough and presentation | Pending |
| Android/iOS player builds and device QA | Not run successfully |
| FPS/memory/loading/thermal/battery measurements | Not run |
| Saved store gameplay captures | Not produced; desktop UI was inspected on screen |

No tests were rerun for this status-document update; the retained test evidence was inspected and production C# hashes were checked. New source changes require appropriate revalidation.

## Build and release preparation

PLAY_GAME.cmd opens the project; TEST_GAME.cmd runs automated verification. Tools include simulation tests, Unity API compilation, real Unity validation, isolated-save review capture and APK/AAB/iOS export commands. These scripts exist; their existence is not a successful player build.

Original icon drafts (512/1024), a gem SVG and promotional illustration exist under release/artwork. Store descriptions/release notes/reviewer notes, privacy/disclosure worksheets, QA matrix, performance plan, defect log, asset provenance, costs and recovery/maintenance guidance are drafted. Artwork is not a gameplay screenshot.

Branding remains development data: publisher name, support@example.invalid, example.invalid privacy URL and com.diamondmine.tycoon.dev identifiers. The public name has not been checked for availability. No upload keystore, signed AAB, Apple archive, TestFlight upload or store publication exists. No paid asset, service, enrollment or deployment was purchased. Source-control/remote backup setup is still pending; this folder is not a Git checkout.

## Remaining work in order

1. Set portrait Game view and complete the actual loop, tutorial, all panels, upgrades, orders, save/relaunch, offline report and prestige manually. Fix layout/clipping/camera/audio issues found. The initial ultrawide preview does not establish phone quality.
2. Produce and inspect saved actual gameplay captures using the isolated review save. Review original art/audio and polish the development presentation.
3. Build a development Android APK outside the restricted environment. Resolve any real IL2CPP/Gradle/SDK/packaging failure.
4. Test Android phone/tablet fresh install, airplane mode, background/resume, forced termination during writes/rewards, updates preserving saves, safe areas, audio and long sessions. Verify native/16 KB compatibility.
5. Profile exact device/build CPU/GPU, FPS, startup, GC, memory, heat and battery; optimize measured failures. Current 30/60 FPS settings are targets, not results.
6. Obtain Mac/Xcode and owner signing access; create the iOS export, device build and archive, then test iPhone/iPad and prepare TestFlight.
7. Run supervised family usability tests, particularly icon comprehension, reading load, touch accuracy, pacing and accidental reset/prestige prevention.
8. Finalize name, publisher, bundle IDs, support contact, hosted privacy policy and truthful audience/content/privacy declarations. Inspect actual packaged SDKs, permissions and network behavior.
9. Configure owner-held signing keys/accounts outside source; produce release AAB/iOS archive and fulfill the actual accounts' testing/review requirements.
10. Prepare final listings and approved gameplay screenshots for owner review. Public submission/publication requires explicit owner authorization.

Production ads, purchases, multiplayer, social features, cloud saves and a backend are separate future scope decisions rather than prerequisites for the current ad-free offline design.
