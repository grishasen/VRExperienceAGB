# Project instructions

- Write all documentation, source code, comments, tests, filenames, commit messages, and initial UI copy in English.
- Preserve both experiences: manual branch exploration and deterministic playback of a prepared profile.
- Target standalone Meta Quest 3 with Unity, C#, URP, and OpenXR. Use Meta XR Interaction SDK as the initial interaction layer.
- Read `README.md` and the relevant documents under `docs/` before implementation. Keep their status consistent with what has actually been tested.
- Keep model evaluation independent of Unity scene objects, locomotion, and animation.
- Do not infer a prediction from arbitrary manual choices, root scores, gain, or sample counts. Follow `docs/model-contract.md`.
- Do not silently truncate the scoring ensemble to meet rendering or tour limits.
- Keep real customer profiles and model exports in `data/private/`, which is ignored. Use synthetic data in tracked examples and tests.
- Preserve `.meta` files after Unity creates assets. Commit the Unity package manifest, package lock, and project settings together when they are generated and verified.
- Do not invent successful Editor compilation, simulator sessions, headset results, or compatible package versions.
- Use actual headset measurements for performance acceptance. Keep the user's head pose independent of scripted camera animation.
- Never modify `/Users/gregory/Documents/AGBVisualizer` as a side effect of work in this repository.
- The original slide recommendations are reference material. The agreed Unity direction and these project decisions control implementation.
- Check for a `.codegraph/` directory at the repository root. If it exists, use CodeGraph before searching or reading code for understanding. If absent, do not create an index automatically.
