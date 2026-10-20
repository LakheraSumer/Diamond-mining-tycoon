# Build and signing

Pinned editor: Unity 6000.3.26f1 (de8167e7440c). It is installed with Android/iOS modules. A valid owner license is required; the latest validation exited 198. No successful Unity import or player build is claimed.

Prepare exact Android dependencies using tools/Prepare-AndroidTools.py, after retaining the official Unity release and Google repository metadata under analysis/evidence. The installer verifies official digests and writes only .cache/android-tools. It does not accept new SDK licenses automatically or alter the existing SDK. JDK17, NDK r27c (27.2.12479018), build tools 36.0.0, API36, command-line tools16 and CMake3.22.1 are the selected minimum toolset. Existing platform tools and previously accepted license files are reused. Installed state is recorded in ENVIRONMENT.md.

The build script selects project-local SDK/NDK/JDK paths through temporary Android tool settings and restores prior settings afterward. Android testing APK: ARM64 and x86_64, development build. Release AAB: ARM64, IL2CPP Release, no development features, no forced Internet permission. Native 16 KB packaging/device compatibility must be verified on the resulting artifact; source settings are not proof.

Use PowerShell process-scoped execution-policy override if the machine blocks local scripts; no global policy change is needed:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Project.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Validate-Unity.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Capture-Review.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Build-Player.ps1 -Target apk
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Build-Player.ps1 -Target ios
```

Verify-Project runs the real core tests and compiler/API checks, then retains TRX, source hashes and pacing results. Validate-Unity performs real editor import/configuration/scene validation. Capture-Review runs actual play with an isolated editor save and scripted tap actions, requests a screen capture and exits. It needs graphics and must not run with -nographics or -quit; actual image size and layout still need inspection. None bypasses licensing.

The AAB command requires KEYSTORE_PATH, KEYSTORE_PASS, KEY_ALIAS and KEY_PASS in the build environment, with a real owner upload key outside source control. Do not paste passwords into scripts, command arguments or reports. The editor clears signing fields after the attempt. Placeholder application IDs, publisher and support/privacy details must be replaced in Branding.cs before a release candidate.

Logs: Logs/editor-validation.log, Logs/review-capture.log and Builds/unity-<target>.log. BuildCommands records target/version/editor/result manifests only after execution reaches the build code. Compute artifact SHA-256 only after a successful file exists. An editor exit 198 produces no APK/AAB/export.

The iOS command exports a Unity Xcode project, not a signed app. A compatible Mac/Xcode environment, current minimum upload SDK, valid bundle/team, capabilities, provisioning, physical device pass and archive validation are still required. No Mac connection or signing credentials were supplied. See Store.md for current official requirements and MAINTENANCE.md for recovery.
