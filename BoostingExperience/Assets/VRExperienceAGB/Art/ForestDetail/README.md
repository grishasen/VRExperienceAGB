# Forest detail assets

Generated with the built-in ImageGen tool on October 17, 2026. Exact prompts and source references are in `generation-prompts.json`.

- `LatemarDetail.png`: refinement of the existing Carezza panorama, preserving its composition and transparent sky. Source size is 2172 × 724; this is an artwork refinement, not a higher-resolution source. The same artwork supplies the mountain strip and lake reflection.
- `SpruceBark.png`: neutral spruce bark albedo, imported at 1024 pixels with repeat wrapping, mipmaps, anisotropy 4 and Android ASTC 6×6. The opaque shader uses one texture sample and simple vertex lighting.
- `AlpineGrass.png`: transparent grass tuft, imported at 512 pixels with coverage-preserving mipmaps, anisotropy 2 and Android ASTC 6×6. Three fixed intersecting planes make each six-triangle tuft; no per-frame billboarding or wind.

`ForestDetailInstaller.Apply()` builds the shared meshes and materials and saves scene assignments through Unity. Existing GUIDs are retained on reapplication. It depends on the first-pass branch artwork under `Art/NearScenery/` and distant spruce artwork under `Art/Carezza/`.

Decorative firs use three shared crown/trunk pairs: 604, 168 and 168 triangles. This applies to six interior trees in the 100-tree garden and 42 trees in the walkthrough clearing. Model crowns use 360 triangles nearby and 144 at distance, with equal bounds and no distance culling; their depth/leaf-count size mapping and pointer targets are preserved. The 72-branch far crown is shared with decorative firs and the miniature table. Following Quest feedback, complete model crowns remain visible about 4.17 times farther away, and the first two decorative detail levels transition about four times farther away, without a global LOD-bias change. Forty-two distant backdrop trees use six/four-triangle levels; the 72 additional shore trees use four spatial batches for frustum culling. Garden grass combines scattered tufts and small clusters around stones, capped at 624 tufts in four spatial batches. Up to 86 small slate rocks share four more spatial batches. Both avoid walking/standing areas and planter labels, reuse the clearing materials and have no colliders.

The panorama and its water reflection share `Art/Carezza/CarezzaPanorama.hlsl`. Side ridges use mirrored full-width artwork and matched horizontal/vertical sampling density, avoiding the former stretched narrow edge crops while retaining lower side ridges and haze.

These geometry counts are asset budgets, not measured draw calls or device performance. Test/build/device evidence and acceptance limitations are recorded in `docs/design/near-scenery-quality-plan-2026-10-17.md` at the repository root.
