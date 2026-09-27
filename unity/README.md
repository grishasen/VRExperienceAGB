# Unity workspace

Create the Unity Editor project at `unity/VRExperienceAGB` using the Universal 3D template and the verified editor version. Follow [Mac setup](../docs/macos-setup.md).

This directory is currently a placeholder, not an openable Unity project. Unity will generate these files and directories:

```text
VRExperienceAGB/
  Assets/
  Packages/
    manifest.json
    packages-lock.json
  ProjectSettings/
    ProjectVersion.txt
```

Keep the Git repository at the parent repository root. Do not initialize a second repository inside the Unity project.

After the headset smoke test, introduce project-owned content under `Assets/VRExperienceAGB/`. Create or move assets through Unity so their `.meta` files remain paired with them. See [Architecture](../docs/architecture.md) for the planned boundaries.
