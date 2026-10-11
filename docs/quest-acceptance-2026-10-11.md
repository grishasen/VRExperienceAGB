# Quest 3 acceptance checklist — review follow-up

For ordinary AGB exports and CDH profiles, also complete the [import, A/B and persistence checks](agb-cdh-import-2026-10-11.md#quest-checks). CDH propensity reconciliation is explicitly deferred.

Status: prepared for the revised build. No headset acceptance or current device performance is claimed. Editor evidence and implementation status are recorded in `review-follow-up-2026-10-11.md`.

## Candidate to install manually

`artifacts/builds/VRExperienceAGB-review-follow-up.apk` — Android IL2CPP Development, Unity 6000.6.3f1. Build GUID `63e396f1d7a7484892c1440b7320dde2`; SHA-256 `14d4248f1753640acfed69174a58015d2894b19942ab53f87fe2622abd64f0bd`. Built successfully with zero errors and six documented warnings; not installed or launched by this follow-up. Use it for functional acceptance and diagnosis. Repeat final sustained performance acceptance on a release build without script debugging.

## Record the test configuration

- [ ] Record commit/worktree revision, APK build GUID, app version, Unity version, headset OS, controller firmware, display refresh rate, battery, thermal state and available storage.
- [ ] Record model identity, file fingerprint, tree/node count and profile provenance. Use an approved synthetic fixture first.
- [ ] Keep the existing sample unchanged. It is not an approved description of the target production model; successful playback does not validate its business semantics.
- [ ] Run seated and standing, with the headset fitted normally and both controllers tracked. Record tester feedback separately from measured performance.

## 1. Startup, orientation and learning

- [ ] The first menu is readable without leaning or rotating the head excessively. All buttons fit comfortably in the viewing area.
- [ ] Learn → manual question → reached leaf → prepared profile → complete model → A/B comparison can be completed without assistance.
- [ ] The visitor can explain that a leaf contributes a raw value and the complete result includes every tree.
- [ ] Resume preserves the route. Restart tree and Restart all trees have distinct, predictable effects.
- [ ] Go to tree accepts the first and last tree; zero, out-of-range and empty input produce a useful response.

## 2. Controller interaction and readable details

- [ ] Point and click with each controller in turn. One trigger press causes one action; moving to another page invalidates stale presses.
- [ ] Hover hints do not cover targets or intercept clicks.
- [ ] The current question and both branch meanings can be read binocularly at normal posture. Future-node labels do not overlap.
- [ ] Full identifier, original condition, units, source address and evidence are available in details. Long identifiers and category sets remain inspectable.
- [ ] Click a label to pin its details; move the pointer away, scroll, then unpin. Opening a menu or changing the accepted decision dismisses obsolete details.
- [ ] Menu open from forest, tree, table, result and playback: no model geometry, duplicate text or hidden target captures input. Close restores the same scene state.

## 3. Comfort and tracking

- [ ] Teleport-only blocks stick walking while path targets remain usable.
- [ ] Optional walking and speed changes work only in the appropriate forest view. Snap turns behave consistently.
- [ ] Menus, pinned reading, model information, comparison and extension tools block unintended locomotion and snap turning.
- [ ] Seated/standing layout, menu height and deliberate recenter do not modify tracked local head/controller poses.
- [ ] After placement, turn and move the head: menus, explanation boards and forest result remain anchored. Recenter deliberately repositions them.
- [ ] Test transition fade on/off and record comfort. Stop a session if the tester feels unwell; do not use the test to establish a universal comfort claim.
- [ ] Ambience, selection sound and haptics can be controlled independently. Essential information remains available with sound/vibration off and without relying on color.

## 4. Prepared playback and results

- [ ] Run A and A/B in Learn, Present, Fast and Step. Step does not advance by itself; pause remains stable while opening and closing menus.
- [ ] A sphere and B cube/letter markings remain identifiable when routes overlap or diverge. A shorter path waits at its leaf.
- [ ] Read the current observed value, branch outcome, reached contribution and subtotal before advancing in Learn mode.
- [ ] Calculate whole model produces the same complete numerical result as detailed playback. Record all-tree counts and reference values from the synthetic fixture.
- [ ] Forest and table results show outcome, profile identities and source-verification status. Intermediate prefixes and manual routes are not labelled as full profile probabilities.
- [ ] Application focus loss, headset removal, pause/resume and return from tools do not skip decisions or double-count contributions.

## 5. A/B input review and what-if

- [ ] A and B names/values correspond to the selected profiles. Differences-only and search retain the selected pair and show accurate page counts.
- [ ] Select an input row to open its complete identifier and exact A/B values. Long values paginate without overlapping navigation.
- [ ] Explicit Missing and Not supplied are distinguishable. Empty results and zero-profile models do not crash.
- [ ] Copy A to B, change one permitted input, apply, then inspect changed inputs, the first divergent split and largest tree deltas.
- [ ] Cancel leaves accepted results intact. Applying recomputes the full ensemble and preserves A.
- [ ] Model sensitivity is not presented as causal uplift. Observed sample categories are not presented as a production dictionary.

## 6. Forest, table and model evidence

- [ ] Test small, 100-tree and a larger agreed synthetic fixture. Define the supported device limit from measurements, not from configured defaults.
- [ ] Switch All trees / explicit groups; zoom, rotate, hover and inspect selected detail. Every scoring tree remains included; group state is visible.
- [ ] Sparse labels remain discoverable; All labels is available. Upper rows stay within comfortable table height.
- [ ] Inspect A/B signed values in table details. Inner/outer rings and profile identity are understandable without color alone.
- [ ] Verify dynamic namespace groups with synthetic `Party.*`, `Customer.*`, another namespace and unqualified IDs. `Customer.Scores.*` belongs to Customer and has no dedicated category.
- [ ] Namespace names are not asserted to be business roles. Model passport, gain overview, dictionary and verification pages remain readable at their longest tested content.
- [ ] Bound metadata matches the source fingerprint and complete tree/node mapping. Unrelated evidence remains unavailable.

## 7. Import and recovery

- [ ] Import a valid normalized model, then separate compatible profiles; reopen both from local storage.
- [ ] Import a nested structural export: manual inspection works and unverified profile scoring stays disabled.
- [ ] While a route or result exists, try malformed JSON, wrong modelId, unsupported condition and finite numeric fields that overflow during evaluation. Previous model/session/result stay usable.
- [ ] An invalid incoming file is not added to the accepted library. Error text names the failure and a recovery action.
- [ ] Test system picker where available, direct Wi-Fi upload from another device, expiration, cancellation, connection loss and insufficient storage. Prior USB-forwarded HTTP tests do not establish direct Wi-Fi acceptance.

## 8. Sustained performance and build acceptance

- [ ] Use Tools → Performance capture. Record forest, tree, A/B playback, table, long reading panels and model transitions separately, then run a mixed session.
- [ ] Each capture includes three seconds of warm-up and up to 60 seconds of measured frames. Repeat after at least ten minutes of normal use to assess sustained behavior and thermal effects.
- [ ] Save JSON from the app's persistent `Measurements` folder. Record median/p95/p99/max frame time, average FPS, memory, refresh rate, availability flags and view transitions.
- [ ] Collect CPU/GPU and compositor dropped-frame counters with the device profiling tools. The application's frames-over-budget count is an estimate, not a compositor counter; unavailable timing fields are not zero cost.
- [ ] Compare measurements with the selected refresh target and agreed budgets. Record loading stalls separately from steady-state frame pacing.
- [ ] Check memory across repeated import/menu/table/playback cycles. Investigate persistent growth, unexpected pauses and rendering artifacts before acceptance.
- [ ] Mark each item Pass / Fail / Not tested with evidence and reproduction steps. Do not convert an Editor pass into a headset pass.

## External model-verification gate

Production scorer agreement requires approved exports, feature preparation/missing/unseen-category rules, reference profiles/predictions and preferably visited leaves. Record the platform version, actual configuration and tolerance. This gate is separate from device acceptance and remains open until those inputs are supplied.
