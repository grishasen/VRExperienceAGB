# Rear clearing and default posture revision — October 12, 2026

The rear shrub line is replaced with 24 slate rocks and 100 fern sprouts using the exact `SlateBoulder` and `GroundFern` meshes/materials from the one-tree clearing. They are distributed behind the forest entrance and combined into two meshes without colliders or shadow casting. Side shrubs and distant mountains remain. All generated combined meshes are released when the model garden is replaced.

The current unified interface defaults to seated, the previously user-selected alternative. The legacy fixture keeps its original layout. The seated/standing switch remains available; it moves the presentation, never the tracked head. Tests exercise the initial seated layout, both toggle directions, and both postures' control accessibility.

`LINK` indicated trees containing the predictor selected through search or node-name linking. It was unrelated to score or propensity. The adjacent diamond U+25C6 is absent from the default LiberationSans SDF font and its available fallback lookup, causing a replacement square. The redundant marker is removed; the above-tree text and hover-card line now say `Predictor: ...`.

## Validation

Compilation passed. The full PlayMode run passed 110 tests, with two legacy posture failures and one optional video test skipped. After preserving the legacy fixture default, all 18 legacy tests passed. The strengthened scenery regression passed with nonempty combined meshes and bounded vertex counts. The current interface startup was inspected in Editor: seated=true, 100 trees. Front/side/rear screenshots were captured and the rear view visually inspected. Android Development build `build_41c7c6e24c33` succeeded with zero errors and six warnings. `adb install -r` returned Success and the app was relaunched. APK: `artifacts/builds/VRExperienceAGB-rear-clearing.apk`, 128,925,184 bytes, SHA-256 `268037dcd82d8d16093a1204d62b0fe7d44c00016345705d77483cb277b902b8`. Headset appearance, text clarity and performance remain user/device checks; Editor renders do not establish headset acceptance.

The scene references the original readable rock/fern mesh assets explicitly, because runtime `MeshFilter.sharedMesh` on statically batched scenery can refer to a combined, unreadable mesh. The regression checks positive vertex count and nonzero bounds, not just renderer existence.
