# Unity workspace

The canonical Unity Editor project is [`../BoostingExperience/`](../BoostingExperience/), selected by the user on 2026-09-28. Open that directory in **Unity 6000.6.3f1 Apple Silicon**. This `unity/` directory is an instructions placeholder, not an Editor project; do not create a second project here.

Manage Git from the parent `VRExperienceAGB/` repository. The earlier nested Git history and original working files are preserved as described in [Repository consolidation](../docs/repository-consolidation.md).

```text
VRExperienceAGB/
  .git/
  docs/
  BoostingExperience/
    Assets/
    Packages/
      manifest.json
      packages-lock.json
    ProjectSettings/
      ProjectVersion.txt
```

Follow [Mac setup](../docs/macos-setup.md). Create and move project-owned Unity content under `Assets/VRExperienceAGB/` through the Editor, preserving `.meta` identities. See [Architecture](../docs/architecture.md) for module boundaries.
