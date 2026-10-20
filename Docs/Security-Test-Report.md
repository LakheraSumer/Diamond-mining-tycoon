# Security validation report

20 October 2026. Final shared-rule suite: **62 passed, 0 failed, 0 skipped** on Windows/.NET 8.0.426. Evidence and source hashes: `tests/results/simulation.trx` and `verification-summary.json`. Unity API compiler checks and initial desktop Editor Play passed. No standalone Android/iOS player has been built or tested.

| Controlled case in our project | Result | Practical limit |
| --- | --- | --- |
| Negative quantities, short balances, huge buy-max and prestige counter limit | Passed | Consistency of local rules; a modified binary can replace them |
| Edited checksum, invalid numbers, fractional/overflow levels, injected cart stocks and oversized buffers | Passed | Well-formed plausible edits and recomputed/legacy checksums remain possible |
| Duplicate JSON fields, excessive depth/size and invalid numeric syntax | Passed | Parser input bounds, not a complete hostile-device security boundary |
| Corrupt primary/backup, newer backup, interrupted write/replace and failed purchase persistence | Passed | Windows/core tests; mobile filesystem interruption not run |
| Offline cap, backward/extreme clock, interrupted settlement and repeated saved bonus | Passed | Local clocks and snapshots can still be manipulated |
| Ad completion duplicated or interrupted; cancel/fail/no-fill/timeout | Passed | The real local test provider only; no ad-network server verification exists |
| Delivery claim after save/relaunch/prestige | Passed | One-time local accounting, not a protected online entitlement |
| Android/iOS release stripping, permissions, signature and runtime tamper behavior | Not run | No player builds exist; the earlier sandbox license issue was resolved for desktop Editor Play |
| Forged/replayed server requests, expired attestations, cross-account access, concurrent network claims and transaction outage | Not applicable / not run | No backend, account, production ad reward or purchased entitlement is implemented |

No reference service was contacted or attacked. No exploit test was run against the supplied APK. No local checksum or IL2CPP setting is presented as proof of an unmodifiable or uncheatable client. Threats, future server verification requirements and failure handling are in `THREAT_MODEL.md`.
