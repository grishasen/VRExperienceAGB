# Experience scenarios

Status: proposed product specification based on the reviewed prototype and slides 6–7. Updated: 2026-09-27.

## Product intent

Help a person understand how a boosted-tree model reaches an outcome by exploring its structure, choosing branches, and following a prepared profile. The experience should work as an engaging demonstration and as a slower explanation of a specific prediction.

The essential interaction is a choice at a fork. The user explicitly requested both manual decisions and ready-made profiles. Neither mode is a substitute for the other.

Initial audience: a presenter explaining a model to stakeholders and a visitor trying the experience with little VR or machine-learning knowledge. Analytical features are useful extensions, but the first session must be understandable without them.

## One world at two scales

The model is a twilight forest with luminous routes inspired by AGB Grid Drive. The ensemble overview is a walkable garden of miniature pines in low circular planters. Numbered beds preserve boosting order. Pine height encodes maximum decision depth and crown width encodes leaf count, with exact values on nearby plaques. Selection and route progress use separate rings and text. Selecting a pine and choosing Enter opens the decision walkthrough.

Within a tree, the root is the entry node. Branches lead upward and outward through a readable three-dimensional hierarchy. Nodes sit on stable platforms, leaves terminate in contribution stations, and a portal connects one tree to the next. Depth is visible without forcing the user to climb or look vertically for long periods.

The environment includes distant trees, restrained mist, soft moonlight, fireflies, quiet spatial audio, and landmarks for orientation. Decorative scenery stays distinct from model-bearing trees. Text, active branches, and interaction targets remain clear against the background.

The first scene should contain a small polished clearing rather than a large unfinished landscape. A flat console or wrist display carries persistent status; detailed explanations appear beside the current node.

## S01 — Drive the model yourself

**Purpose:** experience how successive branch choices produce leaf contributions.

**Entry:** choose “Explore branches” and a tree or guided route. No complete customer profile is required.

1. A luminous data drop waits at the root. A short instruction demonstrates pointing and selecting.
2. The current split shows a human-readable feature name and condition. Both outgoing branches are labeled with their actual conditions; color is supplemental.
3. The user selects a branch. The drop moves to the next node. The user can remain on a stable observation platform or advance with a deliberate transition.
4. At a leaf, a signed contribution appears. The score display shows the previous total, this contribution, and the new total.
5. “Back” restores the preceding state and removes the committed leaf contribution exactly once. The old route remains faintly visible until the next choice.
6. “Next tree” advances to the next contribution. The final station explains the accumulated score.

**Interaction rules:** controls remain responsive during presentation animations. Repeated input must not commit a branch or leaf twice. “Pause”, “Restart”, “Back”, and “Return to forest” are always reachable. Returning to the forest preserves progress until an explicit restart.

**Consistency:** preserve all manual decisions as a route. Track constraints implied by those decisions. If they become inconsistent across trees, explain the conflicting conditions and offer to revise an earlier choice or continue in free exploration. Free exploration remains available, but an inconsistent route must not be presented as a prediction for a real customer. Unknown consistency is not the same as confirmed consistency.

**Acceptance:** a first-time user can select a branch, reach a leaf, undo it, and explain why the total changed. The path and score remain correct after repeated back/forward operations.

## S02 — Follow a prepared profile

**Purpose:** demonstrate a deterministic prediction with an explanation at every decision.

**Entry:** choose “Follow a profile” and one of the bundled synthetic profiles. Show the profile values before starting.

1. The selected profile appears as a data drop with a stable color and name.
2. At each split, show the condition, the profile's value, and the evaluation result. Example: “Previous responses: 2. Condition: less than 5. Result: true.”
3. The matching branch lights up and the drop advances. The same profile is evaluated independently in every tree.
4. Each reached leaf contributes to the accumulated raw score. The presentation distinguishes the baseline from tree contributions.
5. The final gate applies the model's verified output transformation and displays the probability for the named outcome.

**Playback:** step, play, pause, resume, replay, and return to the previous decision. A short tour animates selected trees in detail and summarizes the rest. It still evaluates the full ensemble and visibly identifies grouped trees. A detailed tour visits every selected decision.

**Take over:** “Try a change” duplicates the original profile. Editing a feature recalculates all affected routes and the complete result. The original profile remains available for comparison. Choosing an arbitrary branch instead enters manual exploration with a clear mode label.

**Acceptance:** the displayed path and final score match a trusted reference for the selected profile. A shortened tour produces the same result as detailed playback.

## S03 — The boosting trail

**Purpose:** understand the ensemble as an ordered collection of contributions.

Trees stand along a trail in boosting order. The user can jump to an iteration, inspect a tree, and return to the same forest position. An iteration control reveals trees progressively and shows the selected profile's intermediate raw score and probability.

Use total gain as a possible size encoding, with a legend and a bounded visual scale. Profile-specific contribution is shown separately by a signed marker or halo. A tree has no universal positive or negative contribution independent of the profile.

The actual values determine the landscape. Later trees need not be smaller than earlier trees. A loss or validation learning curve requires per-iteration training history, which is not part of the scoring-export contract. Without it, call the view “ensemble progression”, not a reconstruction of learning quality.

**Acceptance:** iteration order remains stable, every displayed metric names its meaning, and a user can distinguish model-level gain from a profile's contribution.

## S04 — Diorama and immersion

**Purpose:** move between ensemble overview and readable detail.

Startup shows an empty forest clearing with two cases. M3 opens a paginated list of every tree in the selected model, followed by the existing individual-tree walkthrough. M4 opens the complete pine garden. Pointing at a pine reveals structural information; clicking enters that tree directly. Returning restores the garden location and saved route, or the originating M3 list page.

The current default is the user-selected 50-tree nested demonstration export. In M4, explicit standing-point markers along the central and cross-paths provide deliberate teleports to groups and planters. The right thumbstick provides snap turns. A small Menu button and A / desktop Tab recall the two-case menu. The former field-guide dashboard and auxiliary profile, tour and ledger buttons are hidden in this structure-preview flow. Navigation moves an observer origin, never tracked head/controller local transforms. Prepared-profile mechanics and their verified synthetic fixtures remain independent of this default preview. See the [simplified navigation record](m4-simple-navigation-2026-09-30.md).

For dense trees, show a bounded neighborhood around the selected node. Collapsed branches retain their identity and a count of hidden nodes. Selecting a branch can place an enlarged subtree on an inspection table, with a marker connecting it to its original location.

**Acceptance:** the user can find a tree, enter it, inspect a leaf, and return without losing orientation or route state. Large-model rendering limits do not remove trees from evaluation.

## S05 — Predictor fireflies

**Purpose:** locate the use of a feature across the ensemble.

Selecting a feature lights up matching split nodes and adds distant tree markers. “Next occurrence” and “Previous occurrence” navigate the matches. The inspector shows the split, gain, sample count, and tree iteration.

Provide separate scopes: “Entire model” and “Current profile path”. Report occurrence count and gain explicitly. Neither occurrence frequency nor split gain should be labeled as a causal effect or an exact individual feature attribution.

**Acceptance:** highlighted nodes match the parsed feature identifier, including repeated features and supported categorical or missing-value conditions. Switching scopes changes the result count correctly.

## S06 — Audit as environment

**Purpose:** find model structures that deserve a closer look.

An optional audit layer adds local haze, muted light, and signs near flagged structures. Selecting a sign explains the finding, underlying metric, threshold, and suggested investigation. Severity is also written in text.

The existing audit engine is an exploratory reference, not a validated assurance system. Its heuristics must be reviewed before porting. A small sample, a dominant feature, or a low score is not by itself proof of a defective tree. Avoid “dead tree” imagery that presents a suspicion as a conclusion. Missing evidence receives its own state rather than a green rating.

**Acceptance:** every visual warning links to an inspectable finding and evidence. Turning the audit layer off restores the ordinary forest without altering predictions.

## S07 — Guided review

**Purpose:** let a presenter lead a coherent discussion.

A tour consists of saved stops: a forest view, a tree, a node, a profile run, or a comparison. Each stop contains a short English explanation and an optional pointer target. The visitor can pause and inspect, then resume the tour.

Start with a single-user recorded route. Live multi-user review, avatars, shared pointers, and voice are later features requiring explicit session ownership and synchronization.

**Acceptance:** replay preserves stop order, selected profile, and target node IDs. Missing or changed targets produce an explanation rather than a broken camera move.

## S08 — Two profiles, two paths

**Purpose:** explain how a model prediction changes under a hypothetical feature edit.

Duplicate a profile and change one permitted feature. Two colored drops share common route segments and diverge where an evaluated condition differs. Highlight the first divergence in each tree and show the difference between final raw scores and probabilities.

This is a model sensitivity demonstration, not a claim about the causal effect of changing a person's behavior. Do not silently change dependent features; either specify their relationship or keep the comparison explicitly hypothetical.

**Acceptance:** each profile is evaluated independently, the original is unchanged, and every displayed delta reconciles with the two complete predictions.

## Movement and presentation

Default to a stationary observer, controller-directed transitions, and a clear return point. Offer a seated configuration. Continuous riding is an optional later comfort mode, started and stopped by the user. Head orientation always follows tracking; no scripted head bob, forced yaw, camera shake, or surprise acceleration.

Provide a stable score panel as an alternative to a wrist display. Use readable English labels, a consistent legend, and text or shape cues alongside color. Keep decorative motion away from decision text.

## Proposed 90-second demonstration

| Time | Experience | Intended understanding |
| --- | --- | --- |
| 0–15 s | Observe the forest, choose a profile | The model contains many ordered trees |
| 15–30 s | Enter one tree and read a split | Values determine branches |
| 30–45 s | Advance to a leaf | A leaf adds a signed contribution |
| 45–60 s | Watch more trees; summarize the remaining ensemble | Contributions accumulate |
| 60–75 s | View the output gate and explanation | The raw score becomes an output probability |
| 75–90 s | Return to a fork and try a feature change | A changed input can change the route and result |

Timing is an editorial target, not a guarantee. Detailed playback remains available.

## Release boundaries

The first vertical slice includes S01, S02 on one tree, and a small immersive clearing. The demonstration MVP adds the full synthetic ensemble, basic S04, a profile takeover, and a coherent forest environment. S03's richer analytics, S05, S06, S07, S08, hand tracking, and continuous riding are extensions. Real AGB prediction claims require the scoring verification described in [Model contract](model-contract.md).
