# P4 usability audit — 2026-10-05
Scope: current published P3 app, actual in-app browser at 1280×720 and 320×720. This focused audit is not WCAG certification or an axe/screen-reader run.

| Finding | Evidence | Planned correction |
|---|---|---|
| Text dialog permits focus to leave | Shift+Tab from textarea focused Drawing canvas; history/zoom/export remain in AX tree | One modal focus boundary, inert background, Tab wrap and Escape/return focus |
| No keyboard-only create gesture | Canvas handles arrows only for moving selected objects; Enter unhandled | Enter starts/finishes current tool; arrows move preview cursor; same pointer commit/history |
| Narrow board is crowded | Actual 320px screenshot: style always expanded, tools horizontally clipped, status/export/history overlap | Collapsed mobile inspector, wrapping tools, distinct status/dock rows, fit-to-drawing |
| Selection announcements incomplete | AX exposes pressed object buttons but selection changes have no live summary | Named object list and selection/object count announcement |
| Touch pinch absent | Existing bridge rejects every secondary pointer | Opt-in two-touch viewport promotion cancelling the active drawing first; pen rejects palm touch |
| Contrast and small targets | CSS contains #92939d/#93959e labels and 20px swatches at mobile | Verified contrast tokens, 44px mobile targets and explicit focus/forced-colors support |
| Drag-only resize | Existing handles require pointer drag | Numeric selection sizing through the same resize state/action |
| RTL/resources absent | Hardcoded English/physical left-right layout | Reviewed bilingual shell/control resources and logical CSS; canvas world coordinates unchanged |

Positive baseline: semantic HTML controls, color names, real pressed states, a named canvas region, live save/error status, ordered pointer capture/cancel, visible focus rules, reduced-motion rule and recoverable edits.

## Rendered contrast follow-up

The actual current P6 runtime on origin 5231 exposed remaining low-contrast width selection, numeric input borders, canvas focus and small object-count text. [Rendered control colors](evidence/p4-rendered-contrast-before.json) and [count text](evidence/p4-rendered-count-contrast-before.json) preserve the browser-computed colors; these findings concern active controls, not disabled buttons. Beep.Skia already supplies a -4 px inward canvas outline offset; the canvas correction concerns color and thickness.

The existing CSS rules now use the same purple focus/selection color `#6d28d9`, input border `#64748b`, and count text `#475569`. Width selection has a 2 px border; canvas keyboard focus has a 3 px outline using the host's existing inward offset. Existing numeric/select borders change in place. No document coordinates, scene/history, swatch palette or keyboard contracts change. The scope follows [W3C SC 1.4.11](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html) for meaningful state/focus/boundary contrast; normal count text requires 4.5:1. This is focused contrast work, not full WCAG certification or screen-reader acceptance.

| Rendered visual cue | Before ratio | After ratio | Required minimum |
|---|---:|---:|---:|
| Width/height input boundary on white | 2.56 | 4.76 | 3 |
| Selected stroke-width border against lavender / white panel | 1.19 / 1.39 | 6.10 / 7.10 | 3 |
| Keyboard canvas focus against paper | 2.65 | 6.91 | 3 |
| 12 px object-count text on white | 3.48 | 7.58 | 4.5 |
| Text-size select boundary on white | 1.25 | 4.76 | 3 |

Ratios come from actual computed opaque browser colors with the WCAG sRGB relative-luminance formula; thresholds are checked without rounding. [Raw values and summary](evidence/p4-contrast-summary.json) and [focused canvas screenshot](evidence/p4-contrast-after.png) retain the result. `node tests/verify-browser-contrast.mjs` rechecks these actual captures, exact native download equality, width-4 edit/undo UI states, and the [320px result](evidence/p4-contrast-narrow.json): no horizontal overflow and all eleven tools 44×44 CSS px. The actual reopened board remained 40×20, and the final downloaded native file is byte-identical to its baseline. App warning/error logs were empty.

Final candidate `artifacts/p4-contrast-final`, base `/draw/`, offline `b8c46ebd9ba44fe5`, passes **188 C# + 70 JS**, zero-warning native build/publish, pinned source/packages/toolchain, all 134 offline assets and all 402 ZIP entries. [Asset report](evidence/p4-contrast-asset-report.json), [provenance](evidence/p4-contrast-provenance.json), [TRX](evidence/p4-contrast-tests.trx), [JS log](evidence/p4-contrast-javascript-tests.log), and [archive hash](evidence/p4-contrast-archive.sha256) retain the full gate. [Runtime delta](evidence/p4-contrast-runtime-delta.json) compares every physical file to the network-profile candidate: only CSS and generated worker metadata plus their encodings differ; all native/managed/other runtime bytes are identical. Identity/Brotli/gzip inventory is 29,913,096 / 8,144,713 / 10,761,219 bytes. Pinned Beep.Skia source remains `b80560e6fcddb0ba973287babfe178312819b5e19de0e94b421a4c5356f46568`.

Physical devices, real screen readers, actual browser zoom/high DPI, forced-color settings, other engines and independent acceptance remain open. The source's forced-color and reduced-motion rules are unchanged; this increment does not claim those settings were exercised.

After the seven-tool preview saved, its server process was explicitly stopped and reported terminal exit 1 from Ctrl+C. The cached final candidate reloaded and restored all seven objects, with the corrected purple width boundary and darker object count still computed from offline CSS; warning/error logs were empty. [Offline AX](evidence/p4-contrast-offline-reload.txt), [colors](evidence/p4-contrast-offline-colors.json), and [screenshot](evidence/p4-contrast-offline-reload.png) retain the proof. The server was restarted afterward to keep the current preview available; this reused origin is not a cold-start measurement.

Implementation remains authorized by the active user goal. Follow the P4 SDD and preserve the existing scene/history. Relevant references: [WAI modal dialog behavior](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/), [MDN two-pointer pinch](https://developer.mozilla.org/en-US/docs/Web/API/Pointer_events/Pinch_zoom_gestures). Real NVDA/VoiceOver, Android and iOS Safari checks must remain explicitly unverified until performed.
