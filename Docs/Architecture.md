# Architecture

Unity 6000.3.26f1. One player project, two assemblies in the game, plus an editor assembly.

## Projects

- `DiamondMine.Simulation` has no Unity reference. Tests compile the same files.
- `DiamondMine.Runtime` is the mine view, interface, audio, clock, and save lifecycle.
- `DiamondMine.Editor` sets player settings and builds. It is not in the player.

`GameSession` is the only object that changes the live `GameState`. Buttons call it. `ProductionSimulator.Advance` is the only thing that moves ore and diamonds during play. `MinePresenter` reads carts, stocks, and rates.

## Time, files, ads, platform

- `IGameClock` / `SystemGameClock` uses UTC unix milliseconds.
- `ISaveIO` / `SystemSaveIO` writes `diamond-mine-save.json` under the app's persistent folder, plus `.tmp` and `.bak`. The whole mine is not stored in PlayerPrefs.
- Rewards go through `AdRewards`. The test dialog is not an SDK. `DIAMOND_MINE_ADMOB` is a reserved flag and is not defined. Defining it does not add a network SDK.
- `PlatformInfo` covers tablet layout, vibration, and the store link. Android back is the Escape key Unity sends. iOS uses the Close buttons. There is no hardware-back dependency on iOS.

## Saves

Significant actions (buy, unlock, prestige, ad reward, offline apply, mine change) clone the state, change the clone, write it, and copy it back only after the write succeeds. A failed write leaves the running mine unspent. Autosave is about every 20 seconds and also on pause, focus loss, and quit. Pause is not the only path.

A newer save version is not overwritten. The player can leave, or start a new mine after a second confirmation while the newer file is moved aside. A broken primary file falls back to `.bak`. If both are unreadable, a new mine starts and the broken files stay on disk.

`LastActiveUnixMs` is stored with the mine. Offline time is the gap since that stamp. The gap is applied and saved before play continues. If that save fails, play does not advance, so the same gap cannot be added twice on top of a new foreground tick.

Local saves do not transfer between Android and iOS. A later cloud save can sit behind `ISaveIO`. It is not in this version.

## Platforms

Conditional compilation covers platform behavior, isolated editor capture saves and release gating of development reward controls. Game rules are not copied per platform.

Identifiers, version, and build number are in `Branding`. Signing passwords are environment variables read only by the release AAB method, then cleared from the player settings. They are not in source.

## Rendering and UI

Built-in pipeline. One directional light. Quality Low and Medium request 30 FPS. High requests 60. Low turns shadows off. Reduced motion stops worker bob, the crusher jaw, and the sale pop. The canvas uses a 720 by 1280 reference, safe-area anchors, and touch targets around 108 to 140 canvas units. A short, wide, or very large screen docks upgrades on the side instead of stretching the phone column. Text uses LegacyRuntime.

## Audio

Tones are generated at startup. Music and effects have separate levels. Repeated hits of the same cue are throttled. `AudioListener.pause` follows app suspension. A device audio change restarts the music if it was supposed to be playing.

Delivery contracts use OrderService and data-driven definitions; the manual bonus counts diamonds already sold and cannot create/resell resources. Claimed IDs persist through save and prestige. IRewardedAdProvider/LocalTestRewardedAds separate the labeled local test lifecycle from the authoritative durable reward transaction. Release offers/grants are disabled.

SaveGameStore writes a SHA-256 corruption envelope around a versioned payload; it accepts legacy saves, protects newer primary/backup files, archives readable damage and limits parsing. This is not cryptographic proof of an honest player. See THREAT_MODEL.md and MAINTENANCE.md.
