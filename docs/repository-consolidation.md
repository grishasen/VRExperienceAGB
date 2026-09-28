# Repository consolidation and selected Editor baseline

Decision date: 2026-09-28. The user authorized retaining `BoostingExperience/`, unifying Git ownership under the parent `VRExperienceAGB/` repository, preserving existing history and work, and using installed Unity **6000.6.3f1 Apple Silicon**.

## Working arrangement

- Open `/Users/gregory/Documents/VRExperienceAGB/BoostingExperience` in Unity Hub with 6000.6.3f1. The product name stays `VRExperienceAGB`.
- Run Git from `/Users/gregory/Documents/VRExperienceAGB`. There is no active inner `.git` directory. Assets, their `.meta` files, package manifest/lock, settings, documentation, and scripts share this repository.
- The root repository's existing remotes were not changed. The former inner repository's remote configuration remains in its local recovery metadata; no push or remote deletion was performed.
- Build outputs, local backups, caches, CodeGraph state, private data, signing files, and development connection credentials stay ignored.
- `Assets/Resources/DevAgentSettings.asset` and its `.meta` remain local because the asset contains development connection credentials. Neither file is committed or erased. A fresh developer environment must configure its own optional connection settings.
- Unity attribute rules now follow the parent formatting policy. Git LFS remains a deliberate future asset-management decision.

## Preserved history and files

Original parent commit: `8e7d711`. Original inner commit: `ca8dccfe0a89371a329631574514e665c62ea820`.

The original inner history is available in the parent as branch **`codex/boostingexperience-history`**. This is an archival branch rooted at the old project; do not switch the current working tree to it during active work. Read historical files with `git show codex/boostingexperience-history:<original-path>` or inspect its log.

The private local recovery directory is:

```text
artifacts/repository-migration-2026-09-28/
  BoostingExperience-history.bundle
  inner-git/
  project-working-files.tar.gz
  files-sha256.json
  inner-status-before.txt
  inner-working-changes.patch
  inner-staged-changes.patch
  inner-refs-before.txt
  bundle-verification.txt
  editor-after.json
```

The history bundle was verified. The archive contains 107 existing project files, including changed/untracked assets and local development settings; every archived file was checked by SHA-256. Deleted tracked files remain represented by the original history and recorded patch. The original `.git` directory was moved intact into `inner-git/`, preserving its refs, index, reflogs, and configuration. Generated Library/Temp/build caches remain at their original location and were not duplicated.

Recovery should be performed into a separate directory: clone the local history bundle, then overlay the working-file archive and apply the recorded deletions as needed. Verify recovered hashes against `files-sha256.json`. The archive includes local credentials and must stay private. Restoring nested Git ownership in the active project would be a separate deliberate decision; do not copy `inner-git/` back over an active repository or reset the current work.

## Verification and limits

The canonical project passes `bash scripts/check-mac.sh`. `VRAGB_PROJECT_DIR` supports an explicit alternate location and produces a truthful failure for a missing project. Shell syntax and the existing synthetic fixture checks pass.

After consolidation, live Unity inspection found the same saved smoke scene, zero missing MonoBehaviour scripts, and expected package versions. Asset, metadata, package, and player-settings hashes were unchanged. A clean-machine checkout and fresh package restore have not been tested.

The previously built development APK installed successfully on Quest 3. Launch, tracking, interaction, independent relaunch, and performance remain untested. Retaining 6000.6.3f1 records the user's baseline decision and does not claim full device qualification.
