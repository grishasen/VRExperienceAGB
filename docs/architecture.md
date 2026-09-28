# Architecture

Status: Domain and normalized synthetic Import implemented for M2-01–M2-05; 84 EditMode tests pass. Application/session, presentation, XR integration, and environment modules below remain planned. See [implementation record](m2-foundation-2026-09-28.md).

## Boundaries

| Module | Responsibility | Must not own |
| --- | --- | --- |
| Domain | Model, profiles, validation, evaluation, route constraints | Scene objects or headset APIs |
| Import | Normalized synthetic JSON v1 implemented; AGB parsing/normalization planned | Guessing unknown operators or scoring semantics |
| Application | Session state, commands, undo, playback progression | Rendering or input-device details |
| Presentation | Layout, nodes, paths, drop animation, score view | Prediction truth |
| XR | Controller actions, tracked rig, comfort transitions | Model evaluation |
| Environment | Art, lighting, audio, landmarks | Model-health verdicts |
| Analytics (later) | Predictor queries, reviewed audit findings, comparisons | Unqualified causal claims |

Use the same domain and session state for desktop inspection and the XR scene. Input devices issue commands such as `ChooseBranch`, `StepProfile`, `UndoDecision`, `EditProfile`, and `ReturnToDiorama`. Visuals subscribe to the resulting state rather than modifying scores directly.

## Planned Unity organization

Domain, Import and EditMode tests now exist and were imported by Unity. Add the remaining folders when their implementation needs them:

```text
Assets/VRExperienceAGB/
  Runtime/
    Domain/
    Import/
    Application/
    Presentation/
    XR/
    Environment/
  Editor/
  Tests/
    EditMode/
    PlayMode/
  Scenes/
  Prefabs/
  Materials/
  Audio/
  Data/
```

Use assembly definitions to keep domain tests independent of XR packages. The exact assembly split is an implementation decision; do not create empty assemblies that add no useful boundary.

## Session ownership

One session owns the selected model, interaction mode, original profile, editable profile copy, tree/node position, decision history, committed leaf contributions, score, and playback state. Diorama and immersive views share this session.

Proposed progression: forest overview, tree entry, split decision, branch transit, leaf review, inter-tree transit, result review. Pause is an explicit state that stops presentation advancement. Headset tracking continues normally.

Undo operates on domain/session events. It does not infer history from the drop's position. A leaf contribution has a stable event identity and is committed once. Returning across that event removes it once. Restart rebuilds the initial state including the baseline.

## Evaluation and visibility

Evaluate the full model independently of how many trees or labels are visible. A render budget may hide geometry, collapse branches, or group tour steps. It must not truncate the ensemble's prediction.

Use stable IDs for trees and nodes across layout rebuilds. Cache model-derived geometry and profile-derived routes separately. Animate only the active neighborhood; distant trees can use simpler representations. Avoid an independent per-frame behavior on every node.

## Interaction layer

Start with Meta XR Core/Interaction SDK over Unity OpenXR. Use one rig and one interaction stack. Introduce Unity XR Interaction Toolkit only through an explicit design decision if its capabilities are needed; do not place competing rigs or event systems in the same scene.

For the initial scene, use the controller ray and a large selection target. A node's readable label need not be its entire collider. Confirm selection visually and with a brief haptic response where supported.

## Rendering and environment

Use URP and an Android-appropriate material set. Design illumination and silhouettes before adding post-processing. Tree layout must preserve visible parent-child direction and offer a focus mode when depth causes occlusion.

Keep a common world-space legend. Use separate encodings for split truth, signed leaf contribution, gain, profile identity, and audit status. Avoid overloading red/green with all five meanings.

## Data ownership

Tracked demo data is synthetic. Real exports and profiles live in ignored `data/private/` and are admitted to builds deliberately. No network service is required by the MVP. Package installation may need network access, but the built experience should run offline.

The initial repository's Python verifier is a small executable specification for synthetic fixtures. Production evaluation belongs in C# and requires independent reference checks for real AGB imports.
