# SimpleDraw consolidated local delivery

Date: 2026-10-06. This is local engineering delivery, not an independent production acceptance signature.

## App and packages

The current preview at `http://127.0.0.1:5243/draw/` serves `artifacts/simpledraw-v1-final/publish/wwwroot`. The original ten-object drawing survives the guarded update. Tools dock left; canvas center; Beep.Skia libraries/properties right. Ordinary scroll pans, Shift scroll pans horizontally and Ctrl/Cmd scroll zooms. All 249 components across sixteen families remain embedded.

Portable static ZIP: `artifacts/simpledraw-v1-final/simpledraw-static.zip`. SHA256: `63244285a7d4183dd13e6197cbd1846e8ac9bc6430266dbc71bbe361baec01b7`. Mount: `/draw/`; build for the intended mount before deploying elsewhere. Offline version: `8d2fdb3d44b2b25b`. All 151 offline assets and 453 ZIP entries verify. Whole-folder Brotli inventory: 8,457,280 bytes, below 25 MiB. This is an inventory, not a fresh network/startup measurement.

Build inputs: 498 pinned Beep.Skia source files; source SHA256 `e9f5faabe9dac9aee4d8fa38def10d5fddd98a74b3b0bf47e2ef2cef78618c9f`; 56 pinned package archives. Build/publish selects native WebAssembly Skia and fresh Web intermediates. The current shared editing changes are included in this web publish.

Native Codex/Claude Code plugins are installed as `simpledraw@beep-tools`. The shareable archive is `artifacts/simpledraw-plugins-v1.zip`; installation and requirements are in [PLUGINS.md](mcp/PLUGINS.md). Public directory submission is deferred. Browser handoff uses native files, not live synchronization.

## Verification

- Fresh consolidated build: 585 C# and 79 JavaScript checks passed; zero build warnings/errors. Logs, TRX, asset report, provenance, notices mapping and archive verification are in `artifacts/simpledraw-v1-final`.
- Browser imported the MCP-generated native file: six objects and two connectors. Text edit/undo restored the original text; reload restored six objects.
- With the preview server stopped, offline reload, a seventh object insertion and component Label editing/undo passed.
- English and Arabic layouts at 320px measured page width/scroll width 320/320. The temporary viewport override was reset.
- Main preview update activated; ten original user objects restored. Captured warning/error logs for both test and adopted previews are empty.
- Evidence: [browser record](evidence/simpledraw-v1-browser.json), [MCP handoff screenshot](evidence/simpledraw-v1-mcp-handoff.png), [adopted preview screenshot](evidence/simpledraw-v1-user-preview.png).

The unchanged Beep.Skia source fingerprint retains the prior 504-test sibling evidence; no fresh sibling run is claimed here. A subsequent fresh final-release mixed-1,000-object check passes at p95 25.6/26.2 ms with 120 measured samples per run, 140 committed edits each, restored geometry and byte-identical native downloads. This is the existing desktop presentation-frame proxy at 1280x720, not a production-host or physical-GPU measurement. See evidence/simpledraw-v1-mixed-performance.json and evidence/simpledraw-v1-mixed-performance-repeat.json. Older timing records remain historical. The browser download event listener timed out, but the actual completed files were subsequently verified in the browser download folder. The final ten-object native file is byte-exact with the pre-update file: 8,182 bytes, SHA256 497282b981275edcdf0854ea31a6fd298706f31b5203911a3167378852803b3e. See evidence/simpledraw-v1-download.json and evidence/simpledraw-v1-user-native.simpledraw.json. The earlier event timeout is a tool-observation limitation, not an incomplete app download.

## Selected acceptance environment and future work

On 2026-10-06 the user selected **“local embedded chromium”** in response to the remaining host/device test-environment question. Implementation delivery is complete for that selected local environment. The loopback preview is the delivery host; current local startup is 2,032.6 ms, and repeated mixed-1,000-object editing p95 is 25.6/26.2 ms against the 50 ms budget. Offline restore, guarded update, completed downloads and the full embedded catalog are verified against the final artifact.

Physical touch/screen readers, other browser engines, high-DPI hardware, public HTTPS hosting, OS-level PWA installation and hosted CI remain **unverified outside the selected acceptance environment**. Independent reviewer signatures remain unsigned; the environment choice does not manufacture formal production approval. These are future validation records, not blockers to the requested local implementation delivery. Optional collaboration and public plugin publication stay deferred.

The main engineering scope is delivered locally. [Feature candidates](../plans/enhancements/NEXT_FEATURES.md) are a separate backlog: recommended first additions are alignment/snapping, clipboard/images and Beep.Skia starter templates. They are not included in this release or commissioned merely by being listed.



## Completed-file/performance/gallery audit follow-through

Completed user native downloads are byte-exact across the release update. The final 1,000-object v7 board is 714,792 bytes (SHA256 f8659bdc2b97d11797e01235830d44a9bb278faf412f4b2237805eac2b6179cd); repeated 140-edit runs and actual stopped-server restore/download all retain exactly those bytes. Raw p95 is 25.6/26.2 ms against 50, at 1280x720 in the in-app Chromium desktop browser. The final offline release also opens all 249 component types, fits/renders the full gallery and downloads native bytes identical to the original current-defaults fixture. Captured warning/error logs are empty. See evidence/simpledraw-v1-follow-through.json, simpledraw-v1-gallery.json and the performance JSON files/screenshots. These close the previously uncertain final native-download and current-performance/gallery checks. COMPLETION_AUDIT.md maps the implemented scope and selected local acceptance environment to evidence; broader production certification remains unverified.
