# Reference findings

Verified 20 October 2026. The supplied filename does not identify its contents.

## Identity and preservation

**Confirmed by static evidence:** `D:\games\uptodown-com.lumber.inc.apk` is the **Uptodown App Store**, package `com.uptodown`, version `7.41`, version code `741`. Its launcher is `com.uptodown.activities.MainActivity`. It is not a verified copy of Lumber Inc. Evidence: `evidence/badging.txt`, `evidence/manifest-tree.txt`.

Original size: 15,560,578 bytes. SHA-256: `dc885921fc2d141a53906675bd555f6f5d3705119f37f892d3edf3e61b342550`. The source was left unchanged and rehashed. Inspection used `evidence/reference-working-copy.apk`, which has the same hash. Reproduce using `python tools/Inspect-Reference.py` from the project root.

**Confirmed by static evidence:** all 2,169 ZIP entries passed CRC checks; no duplicate entry names; total uncompressed bytes 32,522,515. Four DEX files, four ABIs (ARM64, ARMv7, x86, x86_64), and Uptodown/AndroidX native libraries are present. No embedded APK/OBB or Unity `libunity`, IL2CPP binary, or `global-metadata.dat` was found in this container. This is an inventory result, not proof that external downloads never occur. Evidence: `evidence/apk-summary.json`, `evidence/zip-inventory.json`, REA's `evidence/rea-artifact.json`.

**Confirmed by static evidence:** Android minimum API 23, target API 36, compile API 37. Manifest permissions include networking, account access, package installation, storage, boot and wake-lock capabilities. These are declarations of the supplied app-store package, not permissions selected for our game. Full declarations remain in `evidence/manifest-tree.txt`.

**Confirmed by static evidence:** Android `apksigner` verifies v1, v2 and v3 signatures with one signer. Certificate subject identifies Uptodown Technologies SL; certificate SHA-256 is `822b9ca12b534ebcf426632221d951bfc60eb08f9f0cf2839c321b0685c2e8a4`. SourceStamp was not verified. Retained warnings concern META-INF entries. The certificate was not compared against an independently trusted publisher fingerprint. A valid signature is not proof of publisher authenticity or safety. Evidence: `evidence/signing.txt`.

## Design questions and limits

| Question | Classification | Finding / next step |
| --- | --- | --- |
| Lumber Inc production stages, workers, queues and storage | Unknown | The supplied package is an app store, so no Lumber Inc gameplay conclusions can be drawn. |
| Upgrade, automation and expansion rules | Unknown | No recovered game formulas or unlocks are claimed. |
| Orders, objectives and offline rewards | Unknown | No matching game data has been established. |
| Lumber Inc UI and feedback | Unknown | No game runtime screenshots or visual observations were made. |
| Lumber Inc server dependencies and ads | Unknown | Uptodown manifest components do not establish the game's integrations. |
| Supplied container's likely purpose | Inferred | Package, label and launcher consistently identify an Android app-store client. |
| Our mining chain and economy | Proposed original design | Implemented independently; see `design/FEATURE_DECISIONS.md` and `Assets/_Project/Resources/economy.json`. |

The investigation was time-boxed to identity, packaging, engine markers and signature verification. Decompiling an unrelated app-store client would not answer the mining design questions. No JADX engine, Ghidra, Hopper, paid decompiler or reference asset extractor was added. No target code was run, installed, modified or contacted over the network. ADB reported no attached devices and the emulator reported no AVDs. An isolated reference gameplay run is therefore blocked both by the package mismatch and by lack of a configured isolated device.

REA 6.7.0 was prepared from the supplied source checkout using the pinned Node/npm toolchain and lockfile; it successfully produced the static artifact graph. Missing optional providers are not a reason to install every reverse-engineering engine. REA is available as a local CLI; agent registrations were left untouched.

No code, art, audio, text or layout from the reference container was copied into `Assets`. The reference and its extracted observations remain under `analysis` only. Supplying a gameplay video is not required for original implementation to continue.

**Confirmed by static evidence:** the supplied app-store manifest names Firebase Messaging, Analytics connector and Crashlytics components (manifest-tree.txt around lines 881-918). These declarations do not prove runtime activation or identify any SDK used by Lumber Inc. No split or external-asset completeness claim is made for the unrelated reference game.
