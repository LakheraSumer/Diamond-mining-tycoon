# Release readiness

24 October 2026: **not ready for public release**. The yard loop is implemented and covered by automated rules. No mobile player build or gameplay screenshot is claimed.

| Yard deliverable | State | Evidence |
| --- | --- | --- |
| Save migration and five starter bunkers | Tested | AdventureJourneyTests, Docs/Save-Migration.md |
| 102,400 plot addresses | Tested as data, not as drawn objects | Unique ids and a full description pass in the same suite |
| Mine, factory, ring, sale, sixth bunker | Tested | Journey test, reload does not duplicate the ring |
| Twelve cosmetic characters | Implemented as a shared procedural puppet | Docs/characters, YardWorld.cs. No external model |
| Verified online ranking | Blocked | Rules exist and reject forged totals. Production flag is false. No server |
| Android APK / iOS export | Not run | Still the gap from 20 October |

| Deliverable | State | Evidence / remaining work |
| --- | --- | --- |
| Environment and REA setup | Prepared | Docs/ENVIRONMENT.md and analysis/evidence |
| APK inspection | Tested statically | Package is Uptodown, not Lumber Inc; analysis/REFERENCE_FINDINGS.md |
| Original game design | Implemented in source | design/FEATURE_DECISIONS.md; Docs/Design.md |
| Shared Unity project | Imported and running in desktop Editor | Main.unity opened in Unity 6000.3.26f1; mining input and increasing ore observed; Logs/interactive-play.log |
| Three mines, economy, automation, tutorial, objectives, orders, prestige | Implemented; core tests passed | 62-test TRX and economy pacing output |
| Saves and offline progress | Implemented; Windows/core tests passed | Checksum, backup/archival recovery, migrations, transaction tests; device termination tests not run |
| Art, audio and UI | Original source implemented; presentation review not run | Procedural scene/audio, faceted gems, larger child-audience UI; store drafts visually inspected |
| Client security | Proportionate controls implemented | Docs/THREAT_MODEL.md; release IL2CPP/signing not built |
| Production ads / online protected entitlements | Deferred / absent | Ad-free child-directed release; no fake server-verification claim |
| Backend | Excluded from current release | Optional future trust design only; no hosted endpoint or payment system |
| Android APK / AAB | Not built | Retry outside restricted environment; upload AAB also requires owner's keystore |
| iOS Xcode export | Not built | Retry outside restricted environment; installed iOS module alone is not an export |
| iOS archive / signing / TestFlight | Blocked | No supplied Mac/Xcode connection, Apple team or signing configuration |
| Android / iOS manual QA | Not run on either platform | Docs/QA.md |
| Actual device performance | Not run | Docs/Performance.md |
| Store copy, privacy, disclosures, costs and rollout drafts | Ready for owner review as drafts | Docs/Store.md, Privacy.md, Costs.md, release/artwork |
| Store submission / publication | Not performed or authorized | Owner must review completed candidate and explicitly authorize publication |

Latest local automated result: 62 passed, 0 failed, 0 skipped. The Unity API compiler check passed all listed code configurations without warnings. Normal desktop Unity import and initial Play mode succeeded on 20 October. Hub 3.22.3 shows an active Personal license; the earlier restricted-environment exit198 is superseded for desktop play. The brief ultrawide Editor check does not establish portrait UI, audio, native stripping, signing or device behavior.

The owner selected ages 5+, mainly children. Final store rating/category declarations and privacy-policy/support details still require owner review; the broad family audience does not remove child-directed requirements. The current release has no production ad SDK or outward store-link button.

Open release gates: full portrait gameplay and UI review; platform builds; real phone/tablet and iPhone/iPad QA; measured performance/thermal/memory; supervised family usability; final art/name and bundle identifiers; merged permission/SDK/privacy inspection; signing and account-specific store requirements. Do not recommend release until these gates have evidence.
