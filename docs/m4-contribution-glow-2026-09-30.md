# Forest contribution lighting — September 30, 2026

Planter rings show the contribution of a reached leaf: turquoise for positive, orange for negative, and neutral for zero or unreached contributions. Planter names include a plus, minus or zero marker for reached leaves, so color is not the only signal. Hover details show the signed route contribution in manual mode or profile contribution in prepared-profile mode. These values are raw score contributions, not probabilities, gain or global importance.

Ring vertical thickness increases with absolute contribution, from the original thickness to five times that thickness. The denominator is the largest absolute weighted leaf score across the complete loaded model. The scale stays fixed as trees are explored. Hover does not change contribution thickness or color. Undoing a leaf returns its ring to neutral. Grouped prepared-profile contributions use the existing ensemble ledger.

Neutral rings no longer inherit the cyan material's emission. Two shared emissive materials provide signed colors without new lights or animation. Model evaluation, profiles, traversal and locomotion are unchanged.

## Verification

Evidence is stored in ignored `artifacts/m4-contribution-glow-2026-09-30/`. Headset appearance acceptance remains pending.

32 PlayMode tests pass, including both contribution signs from real demo leaves, unreached neutral emission, bounded thickness, hover stability and undo reset. The 151 EditMode passes from the preceding forest update remain the unchanged model-layer baseline. An Editor capture of two positive and one negative reached-tree rings was visually inspected. Preview routes and camera changes were not saved.

Android build succeeded with zero errors and five warnings. APK: `artifacts/builds/VRExperienceAGB-m4-contribution-glow.apk`, 124,817,059 bytes; SHA-256 `69095820f8256c070c1384d085929baa5fb7c068e8a1b9a0a0cb1cae24febab2`. Installation on Quest 3 succeeded and the app cold-launched. The process remained running. The captured startup log contained no fatal exceptions, null-reference exceptions or Unity errors. Device appearance acceptance remains pending.
