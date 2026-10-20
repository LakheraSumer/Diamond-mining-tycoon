# Maintenance and recovery

Keep `Assets` (including every `.meta`), `Packages`, `ProjectSettings`, tests, tools and documentation together. This workspace is downloaded source, not a Git checkout. Initialize source control deliberately when choosing the remote; do not commit `.cache`, `Library`, logs, builds, reference APKs, signing keys or credentials. Test `.csproj` files are explicitly retained by `.gitignore`.

Pin Unity to `ProjectSettings/ProjectVersion.txt` and dependencies to `Packages/manifest.json`. The bundled uGUI 2.0.0 was used for API checks. The successful desktop Unity import generated `Packages/packages-lock.json`; it records the actual resolved built-in packages and should be preserved with the project.

Run `tools/Run-SimulationTests.ps1` after rule, economy or persistence changes; retain TRX evidence. Run `tools/Compile-UnitySource.ps1` for an additional C# 9 / .NET Standard 2.1 API check against the installed editor. It does not substitute for Unity import, IL2CPP compilation, running gameplay or platform tests. `tools/Ensure-Metadata.py` adds only missing deterministic metadata and preserves existing GUIDs.

Saves live under Unity's platform `Application.persistentDataPath`, named `diamond-mine-save.json`, with `.bak` and transient `.tmp` files. They are not cloud saves. Before manual repair, copy the whole save directory to a safe location. A valid primary is preferred; a malformed primary falls back to backup. Readable damaged files are archived as `.corrupt-<id>` before a replacement. A newer primary or backup blocks writes. Unreadable evidence also blocks writes. Do not delete a newer save just to make an older build start.

Legacy unwrapped v0 money and v1 saves remain readable; the next successful save wraps the payload in checksum-container format 1. The new delivery-claim list defaults empty on old saves. The checksum is for corruption detection, not proof of a legitimate local balance. Keep migrations backward compatible or increment the payload version and add real migration fixtures. Test interrupted writes, interrupted rewards and old/new versions before an update.

Reset has an explicit second confirmation and moves primary/backup files aside. For an accidental reset, stop the game first and restore a retained `.retired-*` file as primary after keeping another backup. No recovery can restore data after device storage is cleared or the app is uninstalled without an external backup. Do not promise recovery from local copies on a destroyed device.

For builds, activate the owner's appropriate Unity license. `tools/Prepare-AndroidTools.py` prepares checksummed, project-local Android prerequisites from retained official manifests. `tools/Build-Player.ps1` reads those paths and scopes their selection to a build. Preserve the owner's upload keystore and password recovery separately; never store either in source or chat logs. A lost upload key requires the applicable Play recovery process. iOS archive/signing remains a Mac/Xcode task.

Before any new SDK or network capability, update the threat model, child audience review, privacy disclosures, licenses and costs. Re-run real device network/permission checks. No external publication, paid deployment or message to store reviewers has been authorized.
