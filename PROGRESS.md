# Progress

Updated 24 October 2026. The yard loop is in the simulation and the Unity API compile. This is not a tested mobile release candidate.

Open the project, press Play, and use Yard. Choose a free miner, walk to a bunker, mine, then use the mill and the shop. Works still opens the older mine screen. Automated checks: `TEST_GAME.cmd`. Latest local result is 84 passed. See Docs/PROJECT-STATUS.md.

## Earlier progress

Updated 20 October 2026. The Unity source is implemented, but this is not a tested mobile release candidate.

## Current evidence

- Preserved the existing Unity project and extended it; no reference assets or code were copied into Assets.
- Supplied APK verified as Uptodown App Store (com.uptodown 7.41), not Lumber Inc. Hash, ZIP/manifest/signature evidence and limitations are in analysis/REFERENCE_FINDINGS.md.
- Prepared REA 6.7.0 from the supplied source with Node 24.18.0 / npm 11.16.0, locked dependencies and documented matching Windows native files. Local CLI artifact inspection succeeded. Agent registrations were not changed; unrelated analysis providers were not installed.
- 62 automated tests passed, zero failed/skipped, .NET SDK 8.0.426. Evidence: tests/results/simulation.trx.
- C# 9 / .NET Standard 2.1 API compilation against Unity 6000.3.26f1 and bundled uGUI 2.0.0 passed for simulation, Android release/development, iOS and editor code. This is a compiler check, not Unity import or IL2CPP/device verification.
- Added six delivery orders with saved one-time bonuses, duplicate protection and prestige preservation.
- Hardened JSON numeric/size/depth handling, cart/buffer validation, newer-save protection, corruption checksums and archival recovery. Tightened purchase quantities and exact affordability; fixed buy-max overflow and save timestamp mutation on a failed checkpoint.
- Added a real local test-ad provider interface; disabled all offers/grants in release builds. Completion/failure/retry/duplicate cases are tested. No production ad, analytics, purchase or backend integration exists.
- Fixed missing uGUI assembly reference, invalid credits constant, and missing audio listener. Added faceted gem meshes and stable missing Unity metadata.
- Enlarged portrait UI and controls for the chosen child audience; privacy/credits scroll. Actual visual and touch QA is not run.
- Added original icon/promotional drafts under release/artwork. They are not gameplay screenshots.

## Owner decision

Ages 5+ with broad family appeal, primarily children. Version-one release remains ad-free. No account, tracking, chat, purchases or network reward service is added. Production advertising would require a separate child-audience SDK/privacy/store review. See Docs/Privacy.md.

## Build state

Unity 6000.3.26f1 is installed at D:/Unity/6000.3.26f1 with Android and iOS modules. Normal desktop launch on 20 October successfully imported the project and entered Play mode in Main.unity. Mining input and increasing ore were observed. Hub 3.22.3 is at D:/games/installed software/Unity Hub and shows Personal activated 15 October. Earlier sandbox exit198 did not reflect the desktop license. Stopped our conflicting sandbox editor/licensing processes and used the desktop Retry action to reconnect. Evidence: Logs/interactive-play.log. Mobile builds and full portrait/device QA are still pending.

Exact Android prerequisites are prepared under .cache/android-tools from the official Unity release and Google repository manifests, with checksum verification. See Docs/ENVIRONMENT.md for final installation evidence. Java 21 is already available in Android Studio for APK inspection; Unity Android needs the separately pinned Java 17.

No APK, AAB or Xcode export has been produced. No signed iOS app/archive, TestFlight upload or device profile exists. No Mac connection, signing credentials or store account access was supplied.

## Resume

1. Use the normal desktop Unity session; the project is open and running for the owner. Set a portrait Game view and complete gameplay/UI review. Run batch validation outside the restricted environment with the editor closed when ready.
2. Run tools/Capture-Review.ps1 (graphics enabled) and inspect the actual image; it uses an isolated review save. This has not yet succeeded.
3. Build and play the APK with tools/Build-Player.ps1 -Target apk. Test Android phone/tablet and 16 KB devices; collect logs and real screenshots.
4. Export iOS with -Target ios; archive/sign on a compatible Mac with Xcode and the required SDK, then test iPhone/iPad.
5. Complete Docs/QA.md, performance and child usability reviews. Replace placeholder IDs/contacts; review artwork and name availability.
6. Build the release AAB only with the owner's external keystore. Review final permissions, privacy report and store disclosures. Prepare submission for owner review; do not publish without explicit authorization.

Detailed threat model, recovery, feature decisions, defects, costs and honest release status are in Docs and design.

## Quick validation follow-up, 20 October

Re-ran tests and Unity validation at the owner's request. 62 tests passed after adding backup-preservation regressions; Unity API compilation passed. Fixed panel close/rebuild cache invalidation, disabled stale panel objects before deferred destruction, and blocked gameplay panels when no state is loaded. Fixed deletion of the only backup when saving a missing primary. The restricted validation at that point exited198; the later desktop import/Play success above supersedes it for desktop execution. Added PLAY_GAME.cmd, TEST_GAME.cmd and Docs/TESTING.md for direct owner testing. Full rendered panel QA remains pending.
