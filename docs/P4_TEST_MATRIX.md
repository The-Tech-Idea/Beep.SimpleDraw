# P4 usability verification matrix
Date: 2026-10-05. Final artifact: `artifacts/p4-complete/wwwroot`; loopback preview on port 5200.
Engineering implementation is verified within the scope below. Device/accessibility acceptance is open.

## Automated evidence
Release C# suite: **67 passed**. JavaScript event/storage/worker/keyboard harnesses: **24 passed**. Retained Beep.Skia Blazor host net8 build: zero warnings/errors. Release WASM publish passed.

`UsabilityTests.cs` covers all six geometry/path tools through keyboard gestures with one undo, viewport resize cancellation, preserved text editor state, anchored pinch promotion/rollback without document changes, numeric multiselection resize/undo/identity, current CSS viewport fit and the actual keyboard-created browser download. Existing 56 drawing/document tests continue to pass.

The bridge harness covers two-touch promotion, ordered cancel/start/update/end delivery, centroid/scale, third-pointer refusal, no accidental remaining-finger stroke, pen metadata/palm refusal, resize/blur cancellation and observer cleanup. These are event-policy tests, not hardware pen/touch evidence. The keyboard harness covers modal Tab wrap, focus containment/return, removed invoker fallback, composition safety, command suppression and native-control keyboard behavior.

## Actual browser evidence
Platform: Windows, Codex in-app browser (Chromium; exact embedded engine version not exposed by the browser tool). No other browser engine is inferred.

| Scenario | Result | Scope/evidence |
|---|---|---|
| Keyboard-only add | Passed | Rectangle, line and pen via tool shortcut/Enter/arrows/Enter; text via T/Enter and Ctrl+Enter |
| Cancel text | Passed | Escape left four committed objects; cancelled text absent from downloaded file |
| Select/move/delete/save | Passed | Ctrl+A and object-list Tab/Enter selection, V/arrows, Delete/undo and Ctrl+S download |
| Native roundtrip | Passed | Actual four-object `p4-keyboard-roundtrip.simpledraw.json` reopened in another origin; native suite checks committed content and movement |
| Text/help modal focus | Passed | Text initial focus; Shift+Tab/Tab wraps; Escape restores canvas; help launched from button returns to that button |
| Replacement modal | Passed | Initial Keep drawing focus, reverse/forward wrap, Escape keeps drawing and returns to New; background controls absent from AX tree |
| Numeric resize and undo | Passed on final port 5200 | 60×40 → 120×80; one undo restores the object and both inputs to 60×40 |
| Draft across viewport resize | Passed | Desktop text draft remained in focused editor after changing viewport to 320×720; Escape kept existing object |
| 320×720 layout | Passed within inspected workflows | All 11 tool controls 44×44; no document horizontal overflow; collapsed inspector, scrollable style content, objects, history/viewport/export/save accessible |
| Arabic/RTL shell | Passed for inspected controls | Logical chrome mirrored; canvas geometry unchanged; names/help/common status translated; final toggle updates Arabic selection summary |
| 640×360 reflow | Passed | Short-height help scrolls and Escape returns focus; this is viewport reflow evidence, not a real 200% browser zoom test |
| Final runtime logs | Passed | No warning/error logs in final port 5200 smoke |

Evidence: [desktop](evidence/p4-final-desktop.png), [320px](evidence/p4-final-320.png), [RTL](evidence/p4-320-rtl.png), [short-height modal](evidence/p4-640-dialog.png). Earlier baseline is [p4-baseline-320.png](evidence/p4-baseline-320.png).

## Remaining external/device gates
The later [rendered contrast audit](P4_ACCESSIBILITY_AUDIT.md) verifies the exact current `artifacts/p4-contrast-final` on port 5233. Five existing CSS rules improve input/select borders, selected stroke width, keyboard canvas focus and small object-count text. Actual computed colors meet the targeted 3:1 / 4.5:1 thresholds; native reopen/style/undo/download preserves exact drawing bytes; 320px has no overflow and all eleven tools remain 44×44. Full native gate passes 188 C# + 70 JS. This closes those focused contrast defects without closing the device/screen-reader/zoom matrix below.

| Gate | Status | Required evidence |
|---|---|---|
| Android Chrome real touch/pen | Not run | Finger drawing, pinch promotion, lift/restart, palm behavior and interrupted/orientation gesture |
| iOS Safari/VoiceOver | Not run | Same touch policy, focus/modal behavior and local document workflows on actual device |
| Windows NVDA and macOS VoiceOver | Not run | Object names, pressed selection, live announcements, modal boundaries and complete keyboard workflow |
| Actual 200% zoom and high-DPI | Not run | Browser zoom settings/device scale, readable controls, pointer alignment, modal scroll and saved geometry |
| Firefox/Edge/Safari desktop | Not run | Engine versions, the named keyboard/modal/storage/export cases |
| Forced colors/reduced motion | CSS implemented, device setting not exercised | Actual OS/browser setting evidence |
| Arabic editorial review | Open | Review labels/help/plurals and remaining English diagnostics; UI translation does not extend the bundled drawing font's glyph support |
| PO/SK/QA acceptance | Open | Independent sign-off; no sign-off implied by agent engineering evidence |

Formal WCAG certification, axe results, installed-PWA support and physical device behavior are not claimed. These gates remain visible while P5/P6 implementation proceeds.

## Production session overlay follow-up — 2026-10-06

The [session-overlay release audit](P6_SESSION_OVERLAYS.md) repeats actual 320px eleven-control 44×44/no-overflow, numeric resize/undo, fitted pointer mapping with otherwise exact native fields, 640×360 scrolling help/focus return and 640×720 draft cancellation. Native release passes 289 C# + 72 JS, including 48 production-overlay cases / 528 byte-exact frames at incoming device transforms 1.25/2. These native transforms do not certify actual browser zoom/high-DPI or physical touch/screen-reader behavior; the external matrix above remains open.
