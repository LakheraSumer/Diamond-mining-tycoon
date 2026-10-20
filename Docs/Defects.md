# Defects

Updated 20 October 2026. Confirmed source defects and environment blockers are separate from tests not run.

| ID | Severity | State | Evidence / resolution |
| --- | --- | --- | --- |
| BUILD-1 | Environment | Resolved for desktop Editor | Existing Hub Personal license is active. Normal desktop import/Play succeeded after removing our conflicting sandbox licensing client and retrying. Earlier exit198 is historical; batch/player builds outside the restriction still need verification. Logs/interactive-play.log. |
| BUILD-2 | iOS archive blocker | Open | No Mac/Xcode build connection, Apple team or signing information supplied. |
| BUILD-3 | Android prerequisite | Prepared | Exact JDK17, NDK r27c and SDK tools are installed only into the project cache; final status in ENVIRONMENT.md. |
| SOURCE-1 | Compile blocker | Fixed, API compiler passed | InfoCopy credits concatenated an integer into an invalid const expression. Changed to static readonly. |
| SOURCE-2 | Compile blocker | Fixed; real import passed | Runtime asmdef lacked UnityEngine.UI reference. Added explicit reference; API check and subsequent desktop Unity import passed. |
| AUDIO-1 | Major | Fixed in source, runtime not run | Camera had no AudioListener. Added a single listener. |
| SAVE-1 | Major | Fixed, core tests passed | Newer backup with missing/broken primary could be treated as fresh progress. Store now protects it from writes. |
| SAVE-2 | Major | Fixed, core tests passed | Some oversized buffers, injected cart loads, overflow/fractional levels and timestamps were accepted. Added complete local shape/range checks. |
| SAVE-3 | Major | Fixed, core tests passed | Damaged primary/backup evidence could be overwritten on the next save. Readable damage is archived first; unreadable evidence blocks writes. |
| SAVE-4 | Major | Fixed in source | Failed autosave changed the live offline timestamp. Timestamp now updates only after successful save. Transaction failure behavior is covered by core tests. |
| ECON-1 | Major | Fixed, tests passed | Negative quantities became buy-max; buy-max integer search could overflow; affordability tolerances disagreed. Reject negatives and use consistent exact comparisons and safe midpoint. |
| RELEASE-1 | Major | Fixed in source and provider tests | Development reward controls were offered in release. Compile-policy guard disables release offers/grants; test-provider completion/failure/duplicate cases passed. |
| UI-1 | Major presentation risk | Source improved; visual QA open | Increased reference scale and touch layout, added scrolling privacy/credits. Check clipping and real touch targets on every platform. |
| QA-1 | Release blocker | Open: mobile/full QA | Initial desktop Play and mining input succeeded. No mobile build, saved gameplay capture, device interruption test or device performance profile exists. Full portrait/UI and progression playthrough remain pending. |
| REF-1 | Reference blocker only | Open | Supplied APK is Uptodown, not Lumber Inc. Original implementation continues independently. |

No untested runtime fix is represented as a device pass. Temporary procedural machinery/worker geometry and synthesized sound need a presentation review before calling them final shipping assets.

## Follow-up validation

- UI-2: fixed in source/API-compiled. Panel cache used None for both an invalidated render and a closed screen, so Close could return early and leave a panel visible. It now uses an explicit NotRendered state; old child objects deactivate immediately. Actual rendered behavior is not run.
- SAVE-5: fixed and regression-tested for both successful and failed moves. A missing primary no longer causes deletion of the existing recovery backup before installing the new save.
- UI-3: gameplay panel entry is blocked until game state is ready, avoiding null-state access after startup/configuration failure. API compilation and initial desktop startup passed; the explicit failure-state UI case is not manually verified.
