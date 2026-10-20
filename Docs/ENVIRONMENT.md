# Environment and setup report

Verified 20 October 2026 on Windows NT 10.0.26300, x64. This is a downloaded Unity/source workspace, not a Git checkout. Historical missing-editor and nearly-full-disk reports from 15 October are superseded.

## Paths and instructions

- Project: D:/games/DiamondMineTycoon (existing source preserved and extended).
- REA source: D:/games/rea-main/rea-main. Read AGENTS.md, README, package.json, package-lock.json, .nvmrc, .npmrc, installation and Android guides, build/prepare scripts, and Windows native recovery documentation.
- Supplied reference: D:/games/uptodown-com.lumber.inc.apk. Original unchanged; working copy and observations remain under analysis/evidence.

## Available versions

| Tool | Actual observation |
| --- | --- |
| Node.js | 24.18.0, matches REA .nvmrc |
| npm | 11.16.0, matches packageManager; use npm.cmd because npm.ps1 is blocked by the host script policy |
| REA | 6.7.0 source build succeeded; local CLI help, doctor and inspect-artifact executed |
| Existing full JDK | Android Studio JBR 21.0.10, javac 21.0.10; used for apksigner |
| Project JDK | Temurin/OpenJDK 17.0.18+8, javac 17.0.18; actual executables ran |
| NDK | r27c, 27.2.12479018; clang 18.0.3 build12470979 |
| Android build tools | Project-local 36.0.0; pre-existing inspection tools 36.1.0 and 37.0.0 |
| Android platform | Project-local android-36, platform16/revision2/base SDK; existing SDK had android-36.1 |
| Android command-line tools | Project-local 16.0 |
| CMake | 3.22.1-g37088a8-dirty from the official pinned SDK archive |
| ADB | 1.0.41 / 37.0.0-14910828, pre-existing Android SDK; no attached devices |
| Emulator | Executable exists; -list-avds returned no configured AVDs |
| Unity editor | 6000.3.26f1, changeset de8167e7440c, installed D:/Unity/6000.3.26f1 |
| Unity platform modules | AndroidPlayer, iOSSupport and Windows standalone support directories present |
| Unity Hub | 3.22.3 at D:/games/installed software/Unity Hub/Unity Hub.exe, discovered from the running desktop app. Already signed in; no authentication automated. |
| Unity license | Hub shows Personal activated 15 October 2026. Normal desktop editor resolved entitlements and successfully entered Play mode on 20 October. Earlier restricted-environment exit198 does not describe the desktop license. |
| C#/.NET | SDK 8.0.426 at D:/dotnet; C#9 / .NET Standard2.1 reference/API compiler checks against installed Unity passed |
| uGUI | Bundled 2.0.0, exact manifest request; actual bundled source compiled for API checks |
| Git | 2.56.0.windows.2 |
| Python / Pillow | 3.14.4 / 12.2.0; original art generation completed |
| macOS / Xcode | No Mac connection, Xcode environment or Apple team credentials supplied; no remote host probed or purchased |

## Exact REA preparation

The checkout documents npm ci then npm run build:cached. tools/Prepare-Rea.ps1 used the matching Node/npm, locked install with dependency lifecycle scripts suppressed (--ignore-scripts), HUSKY=0, a project-local npm/temp cache, and the explicit reviewed build script. 540 packages installed; no global npm update or agent registration was performed. Frida's install hook was not run.

The first build correctly refused the source checkout's missing Windows native controls. Following native/windows/README.md, the exact rea-agents@6.7.0 registry tarball was fetched and verified against its published SHA-512, then only manifest.json and rea-windows-x64.node were restored. The matching loader validated the native artifact. The subsequent cached build completed three tasks successfully (18.622 seconds reported by Turbo). tools/Restore-ReaNative.ps1 and .py preserve this procedure.

Current source lock SHA-256: b0d2cf0da353f85a24e7de55521864fb1eae986d5469ef64ee932878dc5b823e.

No rea setup registration was applied; CLI access suffices. The audit-wide doctor initially reported a sandbox temp-path admission failure. A scoped readiness run with project-local TEMP and the existing Java21 resolved that boundary and correctly reported the optional JADX0.7.1 JAR absent. See rea-jadx-readiness.json. Ghidra/Hopper/IDA/JADX engines were not installed because the supplied APK is an unrelated app store. Safe REA artifact inspection succeeded and retained artifact-bound evidence in rea-artifact.json.

Codex Process Jobs was attempted but returned Unsupported platform: win32. Finite setup/download operations therefore used normal tracked commands on this host; no successful durable CPJ job or completion notification is claimed. A stalled Python HTTP transfer was stopped only by identifying our own project process; the Android downloader now uses bounded transfers and four ranges for large archives, followed by full-archive digest verification.

## Android project-local prerequisites

Official release metadata is retained in analysis/evidence/unity-release.json; Google's checksums are in android-repository.xml. Installation evidence with source URLs, official checksums and archive SHA-256 is in android-tools-installed.json. JDK uses the Unity-published SHA-256 in its filename; Google repository packages use Google's published SHA-1 plus TLS delivery and retained SHA-256. Those checks establish consistency with the published archives, not an unlimited supply-chain guarantee.

| Component | Installed path | Check |
| --- | --- | --- |
| android-open-jdk-17.0.18+8 | `D:\games\DiamondMineTycoon\.cache\android-tools\OpenJDK` | Verified official archive checksum |
| android-ndk-r27c | `D:\games\DiamondMineTycoon\.cache\android-tools\NDK` | Verified official archive checksum |
| android-sdk-build-tools-36.0.0 | `D:\games\DiamondMineTycoon\.cache\android-tools\SDK\build-tools\36.0.0` | Verified official archive checksum |
| android-sdk-platforms-36 | `D:\games\DiamondMineTycoon\.cache\android-tools\SDK\platforms\android-36` | Verified official archive checksum |
| android-sdk-command-line-tools-16.0 | `D:\games\DiamondMineTycoon\.cache\android-tools\SDK\cmdline-tools\16.0` | Verified official archive checksum |
| cmake-3.22.1 | `D:\games\DiamondMineTycoon\.cache\android-tools\SDK\cmake\3.22.1` | Verified official archive checksum |

Only API36/build dependencies were selected; unneeded SDK platforms34/35/37 and reverse-engineering engines were not installed. Existing platform-tools and accepted android-sdk-license files were copied into the private SDK. No new license acceptance was automated. Original SDK directories/global environment were not modified. Build-Player.ps1 discovers these paths; build-time selections restore prior Unity Android tool settings afterward. Runtime and required license acceptance remain subject to a real Unity build once activated.

Supported versions were verified using [Unity's 6.3 dependency table](https://docs.unity3d.com/6000.3/Documentation/Manual/android-supported-dependency-versions.html). The installed editor and modules are not proof of a usable license or a tested player.

## Results and precise blockers

- 62 core tests passed, zero failures/skips; real production-rule source is compiled into the tests. TRX and pacing evidence are under tests/results and Docs/Economy-Results.md.
- Simulation/runtime Android release/runtime Android development/runtime iOS/editor API compilation passed against the installed Unity APIs. This bypasses no license: it is ordinary C# compilation and does not execute Unity or produce a player.
- Earlier sandbox validation and APK attempts exited198; their logs remain historical evidence. A normal desktop launch on 20 October imported the project without C# compiler errors, opened Main.unity and entered Play mode. Mining input and ore accumulation were observed using Computer Use. Evidence: Logs/interactive-play.log. A conflicting sandbox licensing client was stopped and the desktop editor's Retry action reconnected to the existing licensed Hub client. No APK, AAB, Xcode export or signed iOS build was produced; mobile layout and device QA remain pending.
- No physical Android/iOS device or isolated AVD is configured. No device performance/QA result exists.
- Release AAB additionally needs the owner's external upload key. iOS signing/archive additionally needs a compatible Mac/Xcode and Apple team. Account-specific store checks remain unknown.
- Owner selected mainly children ages5+; release ads/analytics/accounts remain absent. Public contacts, privacy policy hosting, bundle IDs, final art and truthful store declarations require owner review.

Commands and recovery steps are in Build.md and MAINTENANCE.md. Paid deployment and public publication were not performed.

License recovery: for an eligible Unity Personal plan, sign in to Unity Hub as described in [Unity license management](https://docs.unity.com/en-us/hub/manage-license). Activation/account interaction remains an owner step; no license files were fabricated or bypassed.

The first run under a new project-local .NET CLI home printed the SDK first-time development-certificate notice. No trust command or HTTPS service was run; subsequent test commands explicitly suppress first-time certificate generation. Existing certificates were not removed or changed deliberately.
