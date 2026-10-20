# QA

20 October 2026: **62 passed, 0 failed, 0 skipped**, .NET SDK 8.0.426 on this Windows PC. Evidence: tests/results/simulation.trx. The tests compile the actual simulation source, plus the real local test-ad provider. Runtime/editor API compilation against Unity 6000.3.26f1 and uGUI 2.0.0 also passed; evidence: tests/results/unity-api-compile.txt.

Coverage includes resource conservation, cart/production blocking, upgrade costs/previews, exact affordability, negative quantities, large buy-max, unlocks/objectives, prestige, numbers, tutorial, eight-hour offline cap, clock anomalies, save round trip/migration/backup, newer files, corrupt copies, checksum edits, size/depth/duplicate JSON, range/integer validation, write/replacement interruption, offline claim interruption, duplicate ad completion, cancel/failure/no-fill/timeout, failed reward persistence, and order claims across save/relaunch/prestige.

Earlier restricted-environment editor validation exited 198. This is historical: the normal desktop Unity Editor subsequently resolved the existing Personal license, imported the project, opened Main.unity and entered Play mode on 20 October. Mining input and increasing ore were observed. No compiler/runtime exception matches were found in that desktop log. This brief smoke check does not establish complete gameplay or portrait UI correctness. No Android or iOS build or device test has passed.

| Manual check | Android | iOS |
| --- | --- | --- |
| Fresh install and tutorial | Not run | Not run |
| Existing save / update migration | Not run | Not run |
| Airplane mode | Not run | Not run |
| Pause/resume and repeated focus callbacks | Not run | Not run |
| Forced termination during save, upgrade, order, prestige, offline reward | Not run | Not run |
| Audio interruption and volume/reduced-motion/haptics settings | Not run | Not run |
| Development ad interruption and resume | Not run | Not run |
| Release has no ad controls or network SDK traffic | Not run | Not run |
| Permission denial and merged manifest/privacy inspection | Not run | Not run |
| Phone, tablet, cutouts, safe areas and readable text | Not run | Not run |
| Back / explicit close navigation | Not run | Not run |
| Long session memory, CPU/GPU, heat and battery | Not run | Not run |
| 16 KB native-library/device compatibility | Not run | Not applicable |
| Release signature / archive validation | Not run | Not run |
| Supervised ages 5+ usability / family playtest | Not run | Not run |

Android evidence does not establish iOS behavior. Windows File.Replace tests do not establish mobile filesystem atomicity. API compilation does not establish shader, scene, IL2CPP or runtime behavior.

Future protected online test cases (forged/replayed requests, cross-account authorization, attestation expiry, duplicate provider events, concurrent claims and network transaction failures) are not applicable to the current offline build and have not been run against a backend. No reference game service was tested.

Release gates: no crashes or lost progress; actionable next goal; readable text and non-overlapping touch targets; correct resource/reward accounting; optional features never blocking offline play; stable measured sessions on the agreed devices.
