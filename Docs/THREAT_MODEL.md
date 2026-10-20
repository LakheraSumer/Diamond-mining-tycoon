# Threat model

20 October 2026. This release is an offline, children-directed single-player game. Its coins and diamonds have no monetary value, competitive standing or server entitlement. There are no accounts, purchases, production ads or deployed backend. Strong protection for valuable online operations is intentionally separate from this local simulation.

## Boundaries and controls

| Threat | Current control | What it prevents / residual risk |
| --- | --- | --- |
| Edited saves | SHA-256 corruption envelope; bounded strict JSON; finite/range/type validation; cart and buffer capacity checks | Detects accidental changes and rejects malformed states. SHA-256 is unkeyed; an attacker can recompute it or construct an accepted legacy save. Valid-looking local edits remain possible. |
| Corrupt or interrupted writes | Flush temp file, replace with backup; recover backup; archive readable damaged copies; copy-on-success transactions | Protects against covered local write/replacement failures. Both copies share one device and can be lost together. Atomic replace and filesystem behavior still require Android/iOS interruption tests. |
| Modified client / repackaged app | IL2CPP release configuration, stripping, release signing gate, no development reward controls in release | Raises effort and establishes the owner's package signature. Does not stop patched binaries, unofficial distribution or fake on-screen values. |
| Clock manipulation | Eight-hour cap, backward-clock and extreme-forward-jump handling, persisted settlement timestamp | Bounds a normal offline settlement and prevents covered duplicate claims. Repeated clock/save manipulation remains possible in offline play. |
| Forged reward claims / duplicate callbacks | Test provider grants only on completion; durable clone/commit; saved grant flags and token history | Prevents covered duplicate local callbacks and failed-write partial rewards. Not provider-side ad verification. Modified clients can bypass it. |
| Replayed network requests / duplicate transactions | No reward API or purchase API exists | No protected entitlement can currently be minted because none is implemented. These are not passed backend security tests. |
| Compromised devices | No destructive response or root-based ban | A device owner can inspect/change local state and binaries. Ordinary offline play stays available. |
| Stolen sessions | No sessions/accounts | Not applicable to this version; no claim of session theft protection. |
| Exposed credentials | No server secret or private signing key in project; release passwords read from environment and cleared from PlayerSettings | Avoids embedding shared secrets. Host/build infrastructure and owner-held keys still require appropriate access control and backup. |
| Backend abuse / cross-account access | No deployed backend | Not applicable now. Any future implementation must add authentication, authorization, rate limits and transactional storage before exposure. |

No HMAC key embedded in the client is presented as a secret. No destructive anti-tamper action, progress wipe, mandatory integrity gate or promise of an unmodifiable APK is included.

## Optional online design — not implemented

Add a single small service only if a future owner-approved online reward needs server trust. Keep an authoritative protected entitlement ledger separate from all offline coins. Never accept a client balance or reward amount as authority. No cloud synchronization or cross-device merging exists now.

A future transaction should authenticate its account/device session, authorize the resource owner, enforce a small request-size/schema limit, issue a short-lived one-use challenge, bind the provider assertion to the canonical request and account, verify it on the server, and atomically consume the challenge and insert a unique provider event/idempotency key with the ledger update. Apply per-account/device/IP rate limits, server timestamps and a minimal audit record. Concurrent attempts must meet the same database uniqueness/transaction boundary. Store credentials in managed server secrets, not client files; rotate and revoke sessions after compromise. Define retention and child privacy requirements before collecting identifiers.

[Play Integrity](https://developer.android.com/google/play/integrity/overview) and [setup/quota requirements](https://developer.android.com/google/play/integrity/setup) need a linked Cloud/Play configuration; standard request hashes must bind the requested operation. [Apple's App Attest server verification](https://developer.apple.com/documentation/devicecheck/validating-apps-that-connect-to-your-server) requires server-issued challenges and verified assertions, with replay counters handled server-side. A client `true` flag proves nothing. Device integrity is a risk signal, not an automatic ban. These services are not configured or exercised here.

Future rewarded advertising needs the chosen provider's official server callback/signature verification and unique event IDs. Do not assume SDKs have equivalent server verification. No purchases are added to demonstrate receipt validation.

## Failure policy

Offline core production, local base offline earnings and ordinary saves work without any network or identity provider. Current development ad cancel, failure, no-fill and timeout do not remove base earnings. Release builds offer no ads. If a future protected service has an outage, unsupported integrity device, expired session or interrupted transaction, show pending/unavailable and retry the same idempotency key; grant only after verified commit. Preserve pending records through updates and never silently discard an earned base reward.

## Validation

`tests/results/simulation.trx` contains 62 passing tests after these changes. They cover invalid quantities, large buy-max, numeric parsing, save edits, checksum recovery, newer saves, atomic file backup on Windows, offline caps/clocks, interrupted settlement, duplicate local rewards, ad failure outcomes and order replay across saves/prestige. Android filesystem behavior, IL2CPP hardening, signing, provider verification, server replay, authorization, concurrent network transactions, and actual hostile device testing are **not run**. Backend-specific cases are **not applicable to this build**, not simulated passes.
