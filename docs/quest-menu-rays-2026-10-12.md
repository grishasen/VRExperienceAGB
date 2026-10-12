# Hidden scene surfaces intercept menu footer — October 12, 2026

The latest Quest screenshot (`forest-20261012-080933-032-1db06e.png`, 10:09 local time) shows the Dictionary page. The user reports that the lower buttons cannot be selected.

`MenuReadingSpace` disabled scene renderers, canvases and colliders while the unified menu was open, but did not disable the independent Meta `RayInteractable` components. Invisible scene canvases could still win ray selection in front of the anchored menu. A controlled Editor reproduction placed the dictionary behind the learning console: rays toward Main menu, Position scene and Tools reached the hidden console at approximately 3.10/3.06/3.06 m, before the menu at 3.87/3.81/3.81 m. This is a separate lifecycle path from the previously fixed node-details popup.

The modal menu now suppresses non-menu Meta ray interactables and uGUI graphic raycasters as well as scene graphics. Closing the menu restores each component's captured enabled state, including components that were already disabled. Menu controls remain active. The existing scene-rebuild restoration prevents cloned surfaces inheriting the temporary disabled state during imports.

A regression test checks all three footer targets from both controller origins with the hidden console between the viewer and menu, verifies the uGUI hit, dispatches selection through it and checks the destination page. It also verifies input restoration after each menu close and preservation of a previously disabled surface.

Compilation passed. Full PlayMode regression: **110 passed, zero failed, one opt-in video test skipped**, 84.64 seconds. Android Development build `build_34240d75287d` succeeded with zero errors and five warnings. `adb install -r` returned Success; the application was explicitly restarted. APK: `artifacts/builds/VRExperienceAGB-menu-rays.apk`, 128916534 bytes, SHA-256 `3e141c079123272d41ae226ed7ec2faab9f1bd3dc95182e17db5f7df672bc67d`. Evidence: `artifacts/quest-menu-rays-2026-10-12/`. Physical controller acceptance must be checked on Quest: Dictionary → each footer action, repeated menu open/close, followed by manual branch selection and forest tree selection.
