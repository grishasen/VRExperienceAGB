# Near scenery artwork

Generated with OpenAI ImageGen on October 17, 2026 for this project. Exact prompts and source image references are in `generation-prompts.json`.

- `ForestFloor.png`: generated edit of the project's `Art/Carezza/AlpineGround.png`; soil, needles, gravel and restrained moss.
- `LayeredSlate.png`: generated edit of the project's `Art/Moonlit/SlateAlbedo.png`; mineral strata, crevices and restrained lichen.
- `SpruceBranch.png`: newly generated transparent branch spray, used on individual bent branch strips rather than whole-tree cards.

Sources are 1254 × 1254 PNGs. Unity imports all three at 1024 × 1024 with mipmaps, trilinear filtering and Android ASTC 6×6. Ground uses anisotropy 4; rock and foliage use 2. Foliage preserves alpha coverage across mip levels. No normal maps, runtime texture generation, additional lights or real-time shadows are introduced.

`NearSceneryInstaller.Apply()` recreates shared materials and geometry while retaining existing asset GUIDs, assigns the scene references, and saves through Unity. Run it only outside Play mode. The installer does not rerun the broader environment authoring tools.

The flat ground shader uses one world-XZ sample. Rocks retain three-axis projection. Branches use one texture sample, alpha clipping, depth writing and vertex tint; no transparent blending. Generated fir crowns contain 484 / 144 / 72 triangles. Existing trunks add 1296 triangles in the first two LODs. Three seeded rock shapes contain 60 triangles each.

The first three eligible interior decorative firs are selected relative to the forest entrance. Small models with fewer than two planter rows have no interior firs to replace. Model-bearing trees and distant perimeter cards are unchanged. Rock artwork also appears in the tree walkthrough; twelve nearby authored rocks receive the new silhouettes. Forest perimeter rocks remain one combined mesh.
